using System;

namespace Blazor.WhyDidYouRender.Attributes;

/// <summary>
/// Marks a component class to be completely excluded from state tracking during render analysis.
/// When applied to a component, no fields or properties will be tracked for state changes,
/// regardless of their types or other attributes.
/// </summary>
/// <remarks>
/// Use this attribute on components where state tracking is not needed or would cause
/// performance issues. Parameter tracking and other WhyDidYouRender features will still work.
/// 
/// Example usage:
/// <code>
/// [IgnoreStateTracking]
/// public class PerformanceCriticalComponent : ComponentBase
/// {
///     // No state tracking will occur for any fields in this component
/// }
/// 
/// [IgnoreStateTracking("This component has complex state that doesn't affect rendering")]
/// public class DataProcessingComponent : ComponentBase
/// {
///     // State changes won't be tracked
/// }
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class IgnoreStateTrackingAttribute : Attribute {
	/// <summary>
	/// Gets the reason why state tracking is disabled for this component.
	/// This is used for documentation and diagnostic purposes.
	/// </summary>
	public string? Reason { get; }

	/// <summary>
	/// Gets or sets whether this exclusion should be logged for diagnostic purposes.
	/// When true, the state tracking system will log that this component was explicitly excluded.
	/// Default is false to avoid log noise.
	/// </summary>
	public bool LogExclusion { get; set; } = false;

	/// <summary>
	/// Gets or sets whether this exclusion should apply to derived classes.
	/// When true (default), classes that inherit from this component will also have state tracking disabled.
	/// When false, derived classes can re-enable state tracking.
	/// </summary>
	public bool ApplyToInheritedClasses { get; set; } = true;

	/// <summary>
	/// Initializes a new instance of the <see cref="IgnoreStateTrackingAttribute"/> class.
	/// </summary>
	public IgnoreStateTrackingAttribute() {
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="IgnoreStateTrackingAttribute"/> class with a reason.
	/// </summary>
	/// <param name="reason">The reason why state tracking should be disabled for this component.</param>
	public IgnoreStateTrackingAttribute(string reason) {
		Reason = reason;
	}

	/// <summary>
	/// Gets a formatted description of why state tracking is disabled, suitable for logging.
	/// </summary>
	/// <returns>A formatted string describing the exclusion reason.</returns>
	internal string GetFormattedReason() {
		if (string.IsNullOrWhiteSpace(Reason))
			return "Component explicitly excluded from state tracking";

		return $"Component excluded from state tracking: {Reason}";
	}

	/// <summary>
	/// Determines if state tracking should be disabled for the given component type.
	/// </summary>
	/// <param name="componentType">The component type to check.</param>
	/// <param name="isInheritedAttribute">True if this attribute is inherited from a base class.</param>
	/// <returns>True if state tracking should be disabled.</returns>
	internal bool ShouldDisableStateTracking(Type componentType, bool isInheritedAttribute) {
		if (!isInheritedAttribute)
			return true;

		return ApplyToInheritedClasses;
	}
}
