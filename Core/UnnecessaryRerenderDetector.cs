using System.Collections.Concurrent;
using System.Reflection;

using Microsoft.AspNetCore.Components;

using Blazor.WhyDidYouRender.Configuration;
using Blazor.WhyDidYouRender.Records;
using Blazor.WhyDidYouRender.Core.StateTracking;

namespace Blazor.WhyDidYouRender.Core;

/// <summary>
/// Service responsible for detecting unnecessary re-renders in Blazor components.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="UnnecessaryRerenderDetector"/> class.
/// </remarks>
public class UnnecessaryRerenderDetector {
	/// <summary>
	/// Cache to store component state snapshots for comparison.
	/// </summary>
	private readonly ConcurrentDictionary<ComponentBase, object?> _componentStates = new();

	/// <summary>
	/// Configuration for unnecessary re-render detection.
	/// </summary>
	private readonly WhyDidYouRenderConfig _config;

	/// <summary>
	/// Lazy state tracking provider for advanced state tracking with deferred initialization.
	/// </summary>
	private readonly LazyStateTrackingProvider? _lazyStateTrackingProvider;

	/// <summary>
	/// Initializes a new instance of the <see cref="UnnecessaryRerenderDetector"/> class.
	/// </summary>
	/// <param name="config">The configuration for detection.</param>
	public UnnecessaryRerenderDetector(WhyDidYouRenderConfig config) {
		_config = config ?? throw new ArgumentNullException(nameof(config));

		if (_config.EnableStateTracking)
			_lazyStateTrackingProvider = new LazyStateTrackingProvider(_config);
	}

	/// <summary>
	/// Detects if a render is unnecessary based on component state and parameter changes.
	/// </summary>
	/// <param name="component">The component being rendered.</param>
	/// <param name="method">The lifecycle method being called.</param>
	/// <param name="parameterChanges">Any parameter changes detected.</param>
	/// <param name="firstRender">Whether this is the first render.</param>
	/// <returns>A tuple indicating if the render is unnecessary and the reason.</returns>
	public (bool IsUnnecessary, string? Reason) DetectUnnecessaryRerender(
		ComponentBase component,
		string method,
		Dictionary<string, object?>? parameterChanges,
		bool? firstRender) {

		if (firstRender == true)
			return (false, null);

		if (_config.EnableStateTracking && _lazyStateTrackingProvider != null)
			return DetectUnnecessaryRerenderWithStateTracking(component, method, parameterChanges);

		return DetectUnnecessaryRerenderLegacy(component, method, parameterChanges);
	}

	/// <summary>
	/// Detects unnecessary re-renders using the advanced state tracking system.
	/// </summary>
	/// <param name="component">The component being rendered.</param>
	/// <param name="method">The lifecycle method being called.</param>
	/// <param name="parameterChanges">Any parameter changes detected.</param>
	/// <returns>A tuple indicating if the render is unnecessary and the reason.</returns>
	private (bool IsUnnecessary, string? Reason) DetectUnnecessaryRerenderWithStateTracking(
		ComponentBase component,
		string method,
		Dictionary<string, object?>? parameterChanges) {

		if (method == "OnParametersSet") {
			if (parameterChanges == null || parameterChanges.Count == 0)
				return (true, "OnParametersSet called but no parameter changes detected");

			var hasMeaningfulParameterChanges = parameterChanges.Values
				.Any(ParameterChangeDetector.HasMeaningfulParameterChange);

			if (!hasMeaningfulParameterChanges)
				return (true, "OnParametersSet called but parameter changes are not meaningful");

			return (false, null);
		}

		if (method == "StateHasChanged") {
			var (hasStateChanges, stateChanges) = _lazyStateTrackingProvider!.SnapshotManager.DetectStateChanges(component);

			if (!hasStateChanges)
				return (true, "StateHasChanged called but no state changes detected");

			if (_config.LogDetailedStateChanges && stateChanges.Any()) {
				var changeDescriptions = stateChanges.Select(c => c.GetFormattedDescription());
				var reason = $"State changes detected: {string.Join(", ", changeDescriptions)}";
				return (false, reason);
			}

			return (false, null);
		}

		return (false, null);
	}

	/// <summary>
	/// Legacy detection method for backward compatibility.
	/// </summary>
	/// <param name="component">The component being rendered.</param>
	/// <param name="method">The lifecycle method being called.</param>
	/// <param name="parameterChanges">Any parameter changes detected.</param>
	/// <returns>A tuple indicating if the render is unnecessary and the reason.</returns>
	private (bool IsUnnecessary, string? Reason) DetectUnnecessaryRerenderLegacy(
		ComponentBase component,
		string method,
		Dictionary<string, object?>? parameterChanges) {

		if (method == "OnParametersSet") {
			if (parameterChanges == null || parameterChanges.Count == 0)
				return (true, "OnParametersSet called but no parameter changes detected");

			var hasMeaningfulChanges = parameterChanges.Values
				.Any(ParameterChangeDetector.HasMeaningfulParameterChange);

			if (!hasMeaningfulChanges)
				return (true, "OnParametersSet called but parameter changes are not meaningful");
		}

		if (method == "StateHasChanged") {
			var currentState = CreateComponentStateSnapshot(component);
			var previousState = _componentStates.GetOrAdd(component, currentState);

			if (AreStatesEquivalent(previousState, currentState))
				return (true, "StateHasChanged called but component state hasn't changed");

			_componentStates[component] = currentState;
		}

		return (false, null);
	}

	/// <summary>
	/// Creates a snapshot of the component's current state for comparison.
	/// </summary>
	/// <param name="component">The component to snapshot.</param>
	/// <returns>A snapshot object representing the component's current state.</returns>
	private static Dictionary<string, object?>? CreateComponentStateSnapshot(ComponentBase component) {
		try {
			var componentType = component.GetType();
			var fields = componentType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
				.Where(f => !f.Name.StartsWith('<') && // skip compiler-generated fields
						   !f.Name.Contains("k__BackingField") && // skip auto-property backing fields
						   f.FieldType != typeof(RenderTrackerService)) // skip our tracker
				.ToList();

			if (fields.Count == 0) return null;

			var snapshot = new Dictionary<string, object?>();
			foreach (var field in fields) {
				try {
					snapshot[field.Name] = field.GetValue(component);
				}
				catch {
					snapshot[field.Name] = "[Unable to read]";
				}
			}

			return snapshot;
		}
		catch {
			return null;
		}
	}

	/// <summary>
	/// Compares two state snapshots to determine if they are equivalent.
	/// </summary>
	/// <param name="previous">Previous state snapshot.</param>
	/// <param name="current">Current state snapshot.</param>
	/// <returns>True if states are equivalent; otherwise, false.</returns>
	private static bool AreStatesEquivalent(object? previous, object? current) {
		if (previous == null && current == null) return true;
		if (previous == null || current == null) return false;
		if (ReferenceEquals(previous, current)) return true;

		if (previous is Dictionary<string, object?> prevDict && current is Dictionary<string, object?> currDict) {
			if (prevDict.Count != currDict.Count) return false;

			foreach (var kvp in prevDict) {
				if (!currDict.TryGetValue(kvp.Key, out var currValue)) return false;
				if (!Equals(kvp.Value, currValue)) return false;
			}

			return true;
		}

		return previous.Equals(current);
	}

	/// <summary>
	/// Gets detailed state tracking information for a component.
	/// </summary>
	/// <param name="component">The component to get information for.</param>
	/// <returns>State tracking information, or null if state tracking is disabled.</returns>
	public StateTrackingInfo? GetStateTrackingInfo(ComponentBase component) {
		if (!_config.EnableStateTracking || _lazyStateTrackingProvider == null)
			return null;

		try {
			var metadata = _lazyStateTrackingProvider.StateFieldAnalyzer.AnalyzeComponentType(component.GetType());
			var currentSnapshot = _lazyStateTrackingProvider.SnapshotManager.GetCurrentSnapshot(component);

			return new StateTrackingInfo {
				ComponentType = component.GetType(),
				IsStateTrackingEnabled = !metadata.IsStateTrackingDisabled,
				TrackedFieldCount = metadata.TrackedFieldCount,
				AutoTrackedFields = metadata.AutoTrackedFields.Select(f => f.FieldInfo.Name).ToList(),
				ExplicitlyTrackedFields = metadata.ExplicitlyTrackedFields.Select(f => f.FieldInfo.Name).ToList(),
				IgnoredFields = metadata.IgnoredFields.Select(f => f.FieldInfo.Name).ToList(),
				HasCurrentSnapshot = currentSnapshot != null,
				LastSnapshotTime = currentSnapshot?.CapturedAt
			};
		}
		catch (Exception) {
			return null;
		}
	}

	/// <summary>
	/// Gets comprehensive statistics about state tracking and render detection.
	/// </summary>
	/// <returns>Statistics about the detection process.</returns>
	public EnhancedUnnecessaryRerenderStatistics GetEnhancedStatistics() {
		var legacyStats = GetStatistics();

		var enhancedStats = new EnhancedUnnecessaryRerenderStatistics {
			TrackedComponents = legacyStats.TrackedComponents,
			IsEnabled = legacyStats.IsEnabled,
			IsStateTrackingEnabled = _config.EnableStateTracking,
			StateTrackingStatistics = _lazyStateTrackingProvider?.SnapshotManager.GetStatistics()
		};

		if (_lazyStateTrackingProvider != null && _lazyStateTrackingProvider.IsInitialized)
			enhancedStats.FieldAnalyzerCacheSize = _lazyStateTrackingProvider.StateFieldAnalyzer.GetCacheSize();

		return enhancedStats;
	}

	/// <summary>
	/// Gets the state snapshot manager instance for direct access to state change detection.
	/// </summary>
	/// <returns>The state snapshot manager instance, or null if not available.</returns>
	public StateSnapshotManager? GetStateSnapshotManager() =>
		_lazyStateTrackingProvider?.SnapshotManager;

	/// <summary>
	/// Cleans up state snapshots for components that are no longer active.
	/// </summary>
	/// <param name="activeComponents">Set of currently active components.</param>
	public void CleanupInactiveComponents(IEnumerable<ComponentBase> activeComponents) {
		var activeSet = activeComponents.ToHashSet();
		var keysToRemove = _componentStates.Keys.Where(key => !activeSet.Contains(key)).ToList();

		foreach (var key in keysToRemove)
			_componentStates.TryRemove(key, out _);
	}

	/// <summary>
	/// Gets the current state snapshot count for diagnostics.
	/// </summary>
	/// <returns>The number of components being tracked for state changes.</returns>
	public int GetTrackedComponentCount() => _componentStates.Count;

	/// <summary>
	/// Clears all state snapshots.
	/// </summary>
	public void ClearAll() => _componentStates.Clear();

	/// <summary>
	/// Gets statistics about unnecessary re-render detection.
	/// </summary>
	/// <returns>Statistics about the detection process.</returns>
	public UnnecessaryRerenderStatistics GetStatistics() =>
		new() {
			TrackedComponents = _componentStates.Count,
			IsEnabled = _config.DetectUnnecessaryRerenders
		};

	/// <summary>
	/// Initializes state tracking components asynchronously for improved startup performance.
	/// </summary>
	/// <returns>A task representing the initialization operation.</returns>
	public async Task InitializeStateTrackingAsync() {
		if (_lazyStateTrackingProvider != null)
			await _lazyStateTrackingProvider.InitializeAsync();
	}

	/// <summary>
	/// Pre-warms the state tracking cache with common component types.
	/// </summary>
	/// <param name="componentTypes">Component types to pre-analyze.</param>
	/// <returns>A task representing the pre-warming operation.</returns>
	public async Task PreWarmStateTrackingCacheAsync(IEnumerable<Type> componentTypes) {
		if (_lazyStateTrackingProvider != null)
			await _lazyStateTrackingProvider.PreWarmCacheAsync(componentTypes);
	}

	/// <summary>
	/// Gets comprehensive diagnostics about the state tracking system.
	/// </summary>
	/// <returns>Diagnostic information about state tracking.</returns>
	public StateTrackingDiagnostics? GetStateTrackingDiagnostics() =>
		_lazyStateTrackingProvider?.GetDiagnostics();

	/// <summary>
	/// Performs maintenance on state tracking components.
	/// </summary>
	public void PerformStateTrackingMaintenance() =>
		_lazyStateTrackingProvider?.PerformMaintenance();

	/// <summary>
	/// Resets all state tracking components to their initial state.
	/// </summary>
	public void ResetStateTracking() =>
		_lazyStateTrackingProvider?.Reset();
}
