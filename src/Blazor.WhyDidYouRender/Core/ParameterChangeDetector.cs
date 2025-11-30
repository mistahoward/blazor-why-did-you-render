using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
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
	/// Cached JSON serializer options for performance.
	/// </summary>
	private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = false };

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
			var parameterProperties = componentType.GetProperties().Where(p => p.GetCustomAttribute<ParameterAttribute>() != null).ToList();

			if (parameterProperties.Count == 0)
				return null;

			var currentParameters = new Dictionary<string, object?>();
			foreach (var prop in parameterProperties)
			{
				try
				{
					currentParameters[prop.Name] = prop.GetValue(component);
				}
				catch
				{
					// If we can't get the value, skip this parameter
					currentParameters[prop.Name] = "[Unable to read]";
				}
			}

			var previousParameters = _previousParameters.GetOrAdd(component, _ => new Dictionary<string, object?>());

			var changes = new Dictionary<string, object?>();
			foreach (var kvp in currentParameters)
			{
				var paramName = kvp.Key;
				var currentValue = kvp.Value;

				if (previousParameters.TryGetValue(paramName, out var previousValue))
				{
					if (!AreParameterValuesEqual(previousValue, currentValue))
					{
						changes[paramName] = new
						{
							Previous = previousValue,
							Current = currentValue,
							Changed = true,
						};
					}
				}
				else
				{
					changes[paramName] = new
					{
						Previous = (object?)null,
						Current = currentValue,
						Changed = true,
					};
				}
			}

			foreach (var kvp in currentParameters)
			{
				previousParameters[kvp.Key] = kvp.Value;
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
	/// Determines if two parameter values are equal.
	/// </summary>
	/// <param name="previous">The previous parameter value.</param>
	/// <param name="current">The current parameter value.</param>
	/// <returns>True if the values are equal; otherwise, false.</returns>
	private static bool AreParameterValuesEqual(object? previous, object? current)
	{
		// Handle null cases
		if (previous == null && current == null)
			return true;
		if (previous == null || current == null)
			return false;

		// Handle reference equality
		if (ReferenceEquals(previous, current))
			return true;

		// Handle value types and strings
		if (previous.Equals(current))
			return true;

		// For complex objects, try JSON comparison as a fallback
		try
		{
			var previousJson = JsonSerializer.Serialize(previous, _jsonOptions);
			var currentJson = JsonSerializer.Serialize(current, _jsonOptions);
			return previousJson == currentJson;
		}
		catch
		{
			// If serialization fails, assume they're different
			return false;
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
			// Try to extract Previous and Current values from the change object
			var changeType = change.GetType();
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
