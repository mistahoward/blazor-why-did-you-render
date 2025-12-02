using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Blazor.WhyDidYouRender.Attributes;
using Blazor.WhyDidYouRender.Configuration;
using Blazor.WhyDidYouRender.Helpers;
using Blazor.WhyDidYouRender.Records;
using Blazor.WhyDidYouRender.Records.StateTracking;
using Microsoft.AspNetCore.Components;

namespace Blazor.WhyDidYouRender.Core.StateTracking;

/// <summary>
/// Analyzes component types to discover and categorize trackable fields.
/// This class caches analysis results to avoid repeated reflection operations.
/// </summary>
public class StateFieldAnalyzer
{
	/// <summary>
	/// Enhanced cache for component type metadata with performance optimizations.
	/// </summary>
	private readonly StateFieldCache _metadataCache;

	/// <summary>
	/// Configuration for state tracking behavior.
	/// </summary>
	private readonly WhyDidYouRenderConfig _config;

	/// <summary>
	/// Initializes a new instance of the <see cref="StateFieldAnalyzer"/> class.
	/// </summary>
	/// <param name="config">The configuration for state tracking.</param>
	public StateFieldAnalyzer(WhyDidYouRenderConfig config)
	{
		_config = config ?? throw new ArgumentNullException(nameof(config));

		var cacheConfig = new CacheConfiguration
		{
			MaxCacheSize = Math.Max(100, _config.MaxTrackedComponents / 2),
			MaxEntryAgeMinutes = _config.MaxStateSnapshotAgeMinutes,
			MaintenanceIntervalMinutes = _config.StateSnapshotCleanupIntervalMinutes,
		};

		_metadataCache = new StateFieldCache(cacheConfig);
	}

	/// <summary>
	/// Analyzes a component type and returns metadata about its trackable fields.
	/// Results are cached for performance.
	/// </summary>
	/// <param name="componentType">The component type to analyze.</param>
	/// <returns>Metadata about the component's trackable fields.</returns>
	public StateFieldMetadata AnalyzeComponentType(Type componentType)
	{
		ArgumentNullException.ThrowIfNull(componentType);

		if (!typeof(ComponentBase).IsAssignableFrom(componentType))
			throw new ArgumentException($"Type {componentType.Name} is not a Blazor component", nameof(componentType));

		var cachedMetadata = _metadataCache.GetIfCached(componentType);
		if (cachedMetadata != null)
			return cachedMetadata;

		return AnalyzeComponentTypeInternal(componentType);
	}

	/// <summary>
	/// Analyzes a component type asynchronously with enhanced caching.
	/// This method provides better performance for concurrent scenarios.
	/// </summary>
	/// <param name="componentType">The component type to analyze.</param>
	/// <returns>A task that represents the analysis operation.</returns>
	public async Task<StateFieldMetadata> AnalyzeComponentTypeAsync(Type componentType)
	{
		ArgumentNullException.ThrowIfNull(componentType);

		if (!typeof(ComponentBase).IsAssignableFrom(componentType))
			throw new ArgumentException($"Type {componentType.Name} is not a Blazor component", nameof(componentType));

		return await _metadataCache.GetOrCreateAsync(componentType, AnalyzeComponentTypeInternal);
	}

	/// <summary>
	/// Determines the tracking strategy for a specific field.
	/// </summary>
	/// <param name="field">The field to analyze.</param>
	/// <param name="componentOptions">Component-level tracking options.</param>
	/// <returns>The appropriate tracking strategy.</returns>
	public TrackingStrategy DetermineTrackingStrategy(FieldInfo field, StateTrackingOptionsAttribute? componentOptions = null)
	{
		ArgumentNullException.ThrowIfNull(field);

		// member-level attributes always take precedence over heuristic skipping rules.
		// this ensures that developers can explicitly opt in or out of tracking even for
		// fields that would normally be skipped (e.g., readonly backing fields).
		var ignoreAttribute = field.GetCustomAttribute<IgnoreStateAttribute>();
		var trackAttribute = field.GetCustomAttribute<TrackStateAttribute>();

		// IgnoreState always wins when present. !!
		if (ignoreAttribute != null)
			return TrackingStrategy.Ignore;

		// explicit TrackState should be honored even for fields that would normally be skipped
		// by ShouldSkipField (such as readonly fields). This allows scenarios like explicitly
		// tracking collection fields whose contents change over time.
		if (trackAttribute != null)
			return TrackingStrategy.ExplicitTrack;

		// for fields without explicit attributes, fall back to skip heuristics
		if (ShouldSkipField(field))
			return TrackingStrategy.Skip;

		var autoTrackSimpleTypes =
			componentOptions?.GetEffectiveAutoTrackSimpleTypes(_config.AutoTrackSimpleTypes) ?? _config.AutoTrackSimpleTypes;

		if (autoTrackSimpleTypes && TypeHelper.IsSimpleValueType(field.FieldType))
			return TrackingStrategy.AutoTrack;

		// default to skip for complex types without explicit tracking
		return TrackingStrategy.Skip;
	}

	/// <summary>
	/// Clears the metadata cache. Useful for testing or when configuration changes.
	/// </summary>
	public void ClearCache() => _metadataCache.Invalidate();

	/// <summary>
	/// Gets the current cache size for diagnostic purposes.
	/// </summary>
	/// <returns>The number of cached component types.</returns>
	public int GetCacheSize() => _metadataCache.GetCacheInfo().TotalEntries;

	/// <summary>
	/// Gets detailed cache performance information.
	/// </summary>
	/// <returns>Cache performance statistics and information.</returns>
	public CacheInfo GetCacheInfo() => _metadataCache.GetCacheInfo();

	/// <summary>
	/// Gets cache performance statistics.
	/// </summary>
	/// <returns>Cache performance statistics.</returns>
	public CacheStatistics GetCacheStatistics() => _metadataCache.GetStatistics();

	/// <summary>
	/// Performs cache maintenance to optimize memory usage.
	/// </summary>
	public void PerformCacheMaintenance() => _metadataCache.PerformMaintenance();

	/// <summary>
	/// Performs the actual analysis of a component type.
	/// </summary>
	/// <param name="componentType">The component type to analyze.</param>
	/// <returns>Metadata about the component's trackable fields.</returns>
	private StateFieldMetadata AnalyzeComponentTypeInternal(Type componentType)
	{
		var ignoreStateTracking = componentType.GetCustomAttribute<IgnoreStateTrackingAttribute>();
		var componentOptions = componentType.GetCustomAttribute<StateTrackingOptionsAttribute>();

		// If state tracking is disabled at component level, return empty metadata
		var isStateTrackingDisabled =
			ignoreStateTracking != null || (componentOptions?.EnableStateTracking == false) || !_config.EnableStateTracking;

		if (isStateTrackingDisabled)
		{
			return new StateFieldMetadata(
				componentType,
				Array.Empty<FieldTrackingInfo>(),
				Array.Empty<FieldTrackingInfo>(),
				Array.Empty<FieldTrackingInfo>(),
				componentOptions,
				true
			);
		}

		// Get all instance fields
		var fields = GetAnalyzableFields(componentType, componentOptions);

		// Categorize fields by tracking strategy
		var autoTrackedFields = new List<FieldTrackingInfo>();
		var explicitlyTrackedFields = new List<FieldTrackingInfo>();
		var ignoredFields = new List<FieldTrackingInfo>();

		foreach (var field in fields)
		{
			var strategy = DetermineTrackingStrategy(field, componentOptions);
			var trackingInfo = CreateFieldTrackingInfo(field, strategy);

			switch (strategy)
			{
				case TrackingStrategy.AutoTrack:
					autoTrackedFields.Add(trackingInfo);
					break;
				case TrackingStrategy.ExplicitTrack:
					explicitlyTrackedFields.Add(trackingInfo);
					break;
				case TrackingStrategy.Ignore:
					ignoredFields.Add(trackingInfo);
					break;
				case TrackingStrategy.Skip:
					break;
			}
		}

		var maxFields =
			componentOptions?.GetEffectiveMaxFields(_config.MaxTrackedFieldsPerComponent) ?? _config.MaxTrackedFieldsPerComponent;

		if (autoTrackedFields.Count + explicitlyTrackedFields.Count > maxFields)
		{
			var totalExplicit = explicitlyTrackedFields.Count;
			var remainingSlots = Math.Max(0, maxFields - totalExplicit);

			if (remainingSlots < autoTrackedFields.Count)
			{
				autoTrackedFields = autoTrackedFields.Take(remainingSlots).ToList();
			}
		}

		return new StateFieldMetadata(
			componentType,
			autoTrackedFields.AsReadOnly(),
			explicitlyTrackedFields.AsReadOnly(),
			ignoredFields.AsReadOnly(),
			componentOptions,
			false
		);
	}

	/// <summary>
	/// Gets all fields that can potentially be analyzed for tracking.
	/// </summary>
	/// <param name="componentType">The component type.</param>
	/// <param name="componentOptions">Component-level options.</param>
	/// <returns>A collection of analyzable fields.</returns>
	private FieldInfo[] GetAnalyzableFields(Type componentType, StateTrackingOptionsAttribute? componentOptions)
	{
		var bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

		var trackInheritedFields =
			componentOptions?.GetEffectiveTrackInheritedFields(_config.TrackInheritedFields) ?? _config.TrackInheritedFields;

		if (trackInheritedFields)
			return componentType.GetFields(bindingFlags);
		else
			return componentType.GetFields(bindingFlags | BindingFlags.DeclaredOnly);
	}

	/// <summary>
	/// Creates tracking information for a specific field.
	/// </summary>
	/// <param name="field">The field to create tracking info for.</param>
	/// <param name="strategy">The tracking strategy.</param>
	/// <returns>Field tracking information.</returns>
	private static FieldTrackingInfo CreateFieldTrackingInfo(FieldInfo field, TrackingStrategy strategy)
	{
		var trackAttribute = field.GetCustomAttribute<TrackStateAttribute>();
		var ignoreAttribute = field.GetCustomAttribute<IgnoreStateAttribute>();

		return new FieldTrackingInfo(field, strategy, trackAttribute, ignoreAttribute);
	}

	/// <summary>
	/// Determines if a field should be skipped from tracking consideration.
	/// </summary>
	/// <param name="field">The field to check.</param>
	/// <returns>True if the field should be skipped.</returns>
	private static bool ShouldSkipField(FieldInfo field)
	{
		if (field.IsStatic)
			return true;

		if (field.IsInitOnly)
			return true;

		if (IsCompilerGeneratedField(field))
			return true;

		if (IsInfrastructureField(field))
			return true;

		return false;
	}

	/// <summary>
	/// Determines if a field is compiler-generated.
	/// </summary>
	/// <param name="field">The field to check.</param>
	/// <returns>True if the field is compiler-generated.</returns>
	private static bool IsCompilerGeneratedField(FieldInfo field)
	{
		var name = field.Name;

		// auto-property backing fields
		if (name.Contains("k__BackingField"))
			return true;

		// compiler-generated fields typically start with '<'
		if (name.StartsWith('<'))
			return true;

		// anonymous type fields
		if (name.Contains("__"))
			return true;

		return false;
	}

	/// <summary>
	/// Determines if a field is part of the WhyDidYouRender infrastructure.
	/// </summary>
	/// <param name="field">The field to check.</param>
	/// <returns>True if the field is infrastructure-related.</returns>
	private static bool IsInfrastructureField(FieldInfo field)
	{
		var fieldType = field.FieldType;

		// skip our own tracker service
		if (fieldType == typeof(RenderTrackerService))
			return true;

		// skip other WhyDidYouRender types
		if (fieldType.Namespace?.StartsWith("Blazor.WhyDidYouRender") == true)
			return true;

		return false;
	}
}
