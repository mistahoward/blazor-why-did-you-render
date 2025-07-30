using System;
using System.Collections.Generic;

namespace Blazor.WhyDidYouRender.Attributes;

/// <summary>
/// Provides fine-grained control over state tracking behavior for a specific component.
/// This attribute allows customizing state tracking settings at the component level,
/// overriding global configuration for the decorated component.
/// </summary>
/// <remarks>
/// Use this attribute when you need different state tracking behavior for specific components
/// compared to the global configuration. This is useful for performance-critical components
/// or components with unique state tracking requirements.
/// 
/// Example usage:
/// <code>
/// [StateTrackingOptions(MaxFields = 20, AutoTrackSimpleTypes = false)]
/// public class CustomComponent : ComponentBase
/// {
///     // Only explicitly marked fields will be tracked, max 20 fields
/// }
/// 
/// [StateTrackingOptions(EnableStateTracking = false)]
/// public class NoStateTrackingComponent : ComponentBase
/// {
///     // State tracking completely disabled for this component
/// }
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class StateTrackingOptionsAttribute : Attribute {
	/// <summary>
	/// Gets or sets whether state tracking is enabled for this component.
	/// When not set, uses global configuration. When false, no state tracking will occur regardless of other settings.
	/// Default is true.
	/// </summary>
	public bool EnableStateTracking { get; set; } = true;

	/// <summary>
	/// Gets or sets whether simple value types should be automatically tracked.
	/// When false, only fields with [TrackState] attribute will be tracked.
	/// Default is true.
	/// </summary>
	public bool AutoTrackSimpleTypes { get; set; } = true;

	/// <summary>
	/// Gets or sets the maximum number of fields to track for this component.
	/// Helps prevent performance issues with components that have many fields.
	/// Default is -1 (use global configuration).
	/// </summary>
	public int MaxFields { get; set; } = -1;

	/// <summary>
	/// Gets or sets whether state changes should be logged for this component.
	/// When false, state changes won't appear in diagnostic output.
	/// Default is true.
	/// </summary>
	public bool LogStateChanges { get; set; } = true;

	/// <summary>
	/// Gets or sets the maximum depth for complex object comparison.
	/// Only applies to fields with [TrackState(UseCustomComparer = true)].
	/// Default is -1 (use global configuration).
	/// </summary>
	public int MaxComparisonDepth { get; set; } = -1;

	/// <summary>
	/// Gets or sets whether to track inherited fields from base classes.
	/// When false, only fields declared directly in this component are tracked.
	/// Default is true.
	/// </summary>
	public bool TrackInheritedFields { get; set; } = true;

	/// <summary>
	/// Gets or sets a custom description for this component's state tracking configuration.
	/// This description will be included in diagnostic output.
	/// </summary>
	public string? Description { get; set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="StateTrackingOptionsAttribute"/> class.
	/// </summary>
	public StateTrackingOptionsAttribute() {
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="StateTrackingOptionsAttribute"/> class with a description.
	/// </summary>
	/// <param name="description">A description of the state tracking configuration for this component.</param>
	public StateTrackingOptionsAttribute(string description) {
		Description = description;
	}

	/// <summary>
	/// Validates the attribute configuration and returns any validation errors.
	/// </summary>
	/// <returns>An array of validation error messages, empty if configuration is valid.</returns>
	internal string[] Validate() {
		List<string> errors = [];

		if (MaxFields >= 0 && MaxFields > 1000)
			errors.Add("MaxFields should not exceed 1000 to avoid performance issues");

		if (MaxComparisonDepth >= 0 && MaxComparisonDepth > 10)
			errors.Add("MaxComparisonDepth should not exceed 10 to avoid performance issues");

		return [.. errors];
	}

	/// <summary>
	/// Determines if state tracking should be enabled based on this attribute's configuration.
	/// </summary>
	/// <param name="globalDefault">The global default setting.</param>
	/// <returns>True if state tracking should be enabled.</returns>
	internal bool ShouldEnableStateTracking(bool globalDefault) =>
		EnableStateTracking;

	/// <summary>
	/// Gets the effective maximum number of fields to track.
	/// </summary>
	/// <param name="globalDefault">The global default setting.</param>
	/// <returns>The maximum number of fields to track.</returns>
	internal int GetEffectiveMaxFields(int globalDefault) =>
		MaxFields == -1 ? globalDefault : MaxFields;

	/// <summary>
	/// Gets the effective auto-track simple types setting.
	/// </summary>
	/// <param name="globalDefault">The global default setting.</param>
	/// <returns>True if simple types should be auto-tracked.</returns>
	internal bool GetEffectiveAutoTrackSimpleTypes(bool globalDefault) =>
		AutoTrackSimpleTypes;

	/// <summary>
	/// Gets the effective log state changes setting.
	/// </summary>
	/// <param name="globalDefault">The global default setting.</param>
	/// <returns>True if state changes should be logged.</returns>
	internal bool GetEffectiveLogStateChanges(bool globalDefault) =>
		LogStateChanges;

	/// <summary>
	/// Gets the effective maximum comparison depth setting.
	/// </summary>
	/// <param name="globalDefault">The global default setting.</param>
	/// <returns>The maximum comparison depth.</returns>
	internal int GetEffectiveMaxComparisonDepth(int globalDefault) =>
		MaxComparisonDepth == -1 ? globalDefault : MaxComparisonDepth;

	/// <summary>
	/// Gets the effective track inherited fields setting.
	/// </summary>
	/// <param name="globalDefault">The global default setting.</param>
	/// <returns>True if inherited fields should be tracked.</returns>
	internal bool GetEffectiveTrackInheritedFields(bool globalDefault) =>
		TrackInheritedFields;
}
