using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Blazor.WhyDidYouRender.Attributes;
using Blazor.WhyDidYouRender.Core.StateTracking;
using Microsoft.AspNetCore.Components;

namespace Blazor.WhyDidYouRender.Core;

/// <summary>
/// Service responsible for detecting and analyzing parameter changes in Blazor components.
/// </summary>
public class ParameterChangeDetector
{
	/// <summary>
	/// Cache to store previous parameter values for comparison.
	/// </summary>
	private readonly ConcurrentDictionary<ComponentBase, Dictionary<string, object?>> _previousParameters = new();

	/// <summary>
	/// State comparer used for opt-in deep parameter comparison.
	/// </summary>
	private readonly StateComparer _stateComparer = new();

	/// <summary>
	/// Cache of parameter metadata (including tracking attributes) per component type.
	/// </summary>
	private static readonly ConcurrentDictionary<Type, List<ParameterMetadata>> _parameterMetadataCache = new();

	/// <summary>
	/// Metadata describing how a particular component parameter should be compared.
	/// </summary>
	private sealed class ParameterMetadata
	{
		public string Name { get; init; } = string.Empty;
		public PropertyInfo PropertyInfo { get; init; } = default!;
		public TrackStateAttribute? TrackStateAttribute { get; init; }
		public bool EnableDeepComparison => TrackStateAttribute != null;
	}

	/// <summary>
	/// Gets cached parameter metadata for a component type.
	/// </summary>
	private static List<ParameterMetadata> GetParameterMetadata(Type componentType)
	{
		return _parameterMetadataCache.GetOrAdd(
			componentType,
			type =>
			{
				var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
				var list = new List<ParameterMetadata>();

				foreach (var prop in properties)
				{
					if (prop.GetCustomAttribute<ParameterAttribute>() == null)
						continue;

					var trackState = prop.GetCustomAttribute<TrackStateAttribute>();
					list.Add(
						new ParameterMetadata
						{
							Name = prop.Name,
							PropertyInfo = prop,
							TrackStateAttribute = trackState,
						}
					);
				}

				return list;
			}
		);
	}

	/// <summary>
	/// Detects parameter changes for a component.
	/// </summary>
	/// <param name="component">The component to check for parameter changes.</param>
	/// <param name="method">The lifecycle method being called.</param>
	/// <returns>A dictionary of parameter changes, or null if no changes detected.</returns>
	public Dictionary<string, object?>? DetectParameterChanges(ComponentBase component, string method)
	{
		// Only track parameter changes for OnParametersSet and OnParametersSetAsync
		if (method != "OnParametersSet" && method != "OnParametersSetAsync")
		{
			return null;
		}

		try
		{
			var componentType = component.GetType();
			var parameterMetadata = GetParameterMetadata(componentType);

			if (parameterMetadata.Count == 0)
				return null;

			var previousParameters = _previousParameters.GetOrAdd(component, _ => new Dictionary<string, object?>());
			var changes = new Dictionary<string, object?>();
			foreach (var metadata in parameterMetadata)
			{
				object? currentValue;
				try
				{
					currentValue = metadata.PropertyInfo.GetValue(component);
				}
				catch
				{
					// If we can't get the value, record the failure but do not attempt
					// deep comparison.
					currentValue = "[Unable to read]";
				}
				var paramName = metadata.Name;
				var paramType = metadata.PropertyInfo.PropertyType;

				if (previousParameters.TryGetValue(paramName, out var previousValue))
				{
					if (HasParameterValueChanged(previousValue, currentValue, paramType))
					{
						// If this parameter has opted in via [TrackState], we use deep
						// comparison to determine whether the change is actually
						// meaningful. Otherwise, every detected change is treated as
						// meaningful.
						var isMeaningful =
							!metadata.EnableDeepComparison || !AreParameterValuesDeepEqual(previousValue, currentValue, metadata);

						changes[paramName] = new
						{
							Previous = previousValue,
							Current = currentValue,
							Changed = true,
							IsMeaningful = isMeaningful,
						};
					}
				}
				else
				{
					// First time we've seen this parameter for this component
					// instance. Treat non-null values as meaningful.
					if (currentValue != null)
					{
						changes[paramName] = new
						{
							Previous = (object?)null,
							Current = currentValue,
							Changed = true,
							IsMeaningful = true,
						};
					}
				}

				// Update snapshot for the next comparison
				previousParameters[paramName] = currentValue;
			}

			return changes.Count > 0 ? changes : null;
		}
		catch
		{
			// If parameter detection fails, return null
			return null;
		}
	}

	/// <summary>
	/// Determines if a parameter change is meaningful (not just reference equality).
	/// </summary>
	/// <param name="change">The parameter change data.</param>
	/// <returns>True if the change is meaningful; otherwise, false.</returns>
	public static bool HasMeaningfulParameterChange(object? change)
	{
		if (change == null)
			return false;

		try
		{
			// Prefer the explicit IsMeaningful flag when DetectParameterChanges has
			// computed it (for opt-in deep comparison scenarios).
			var changeType = change.GetType();
			var isMeaningfulProp = changeType.GetProperty("IsMeaningful");
			if (isMeaningfulProp != null && isMeaningfulProp.PropertyType == typeof(bool))
			{
				var value = (bool?)isMeaningfulProp.GetValue(change);
				return value ?? true;
			}

			// Legacy fallback: infer meaning from Previous / Current values only.
			var previousProp = changeType.GetProperty("Previous");
			var currentProp = changeType.GetProperty("Current");

			if (previousProp == null || currentProp == null)
				return true;

			var previous = previousProp.GetValue(change);
			var current = currentProp.GetValue(change);

			// If both are null, not meaningful
			if (previous == null && current == null)
				return false;

			// If one is null, it's meaningful
			if (previous == null || current == null)
				return true;

			// If they're the same reference, not meaningful
			if (ReferenceEquals(previous, current))
				return false;

			// If they're equal by value, not meaningful
			if (previous.Equals(current))
				return false;

			// Otherwise, assume it's meaningful
			return true;
		}
		catch
		{
			return true; // If we can't determine, assume it's meaningful
		}
	}

	/// <summary>
	/// Determines whether a parameter value has changed between renders, using
	/// reference-based semantics for complex types and value semantics for
	/// simple types.
	/// </summary>
	private static bool HasParameterValueChanged(object? previous, object? current, Type parameterType)
	{
		if (previous is null && current is null)
			return false;
		if (previous is null || current is null)
			return true;

		var underlyingType = Nullable.GetUnderlyingType(parameterType) ?? parameterType;
		if (underlyingType.IsValueType || underlyingType == typeof(string))
			return !previous.Equals(current);

		// For reference types (including collections), treat a new reference as a
		// parameter change regardless of value equality. Deep comparison is
		// handled separately when a parameter has opted into it.
		return !ReferenceEquals(previous, current);
	}

	/// <summary>
	/// Performs opt-in deep comparison for parameters that have been annotated
	/// with [TrackState].
	/// </summary>
	private bool AreParameterValuesDeepEqual(object? previous, object? current, ParameterMetadata metadata)
	{
		var parameterType = metadata.PropertyInfo.PropertyType;
		return _stateComparer.AreParameterValuesEqual(previous, current, parameterType, metadata.TrackStateAttribute);
	}

	/// <summary>
	/// Cleans up parameter history for components that are no longer active.
	/// </summary>
	/// <param name="activeComponents">Set of currently active components.</param>
	public void CleanupInactiveComponents(IEnumerable<ComponentBase> activeComponents)
	{
		var activeSet = activeComponents.ToHashSet();
		var keysToRemove = _previousParameters.Keys.Where(key => !activeSet.Contains(key)).ToList();

		foreach (var key in keysToRemove)
		{
			_previousParameters.TryRemove(key, out _);
		}
	}

	/// <summary>
	/// Gets the current parameter count for diagnostics.
	/// </summary>
	/// <returns>The number of components being tracked for parameter changes.</returns>
	public int GetTrackedComponentCount() => _previousParameters.Count;

	/// <summary>
	/// Clears all parameter history.
	/// </summary>
	public void ClearAll() => _previousParameters.Clear();
}
