using System;

namespace Blazor.WhyDidYouRender.Attributes;

/// <summary>
/// Marks a field or property to be explicitly excluded from state tracking during render analysis.
/// This attribute can be applied to any field or property to opt it out of state change detection,
/// even if it would normally be auto-tracked (like simple value types) or explicitly tracked.
/// </summary>
/// <remarks>
/// Use this attribute when you have fields that change frequently but don't affect rendering,
/// or when you want to exclude performance-sensitive fields from tracking.
///
/// Example usage:
/// <code>
/// [IgnoreState]
/// private string _internalDebugId; // Excluded even though string is normally auto-tracked
///
/// [IgnoreState]
/// private DateTime _lastAccessTime; // Performance-sensitive field
///
/// [IgnoreState("This field is for internal caching only")]
/// private Dictionary&lt;string, object&gt; _cache = new();
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class IgnoreStateAttribute : Attribute
{
	/// <summary>
	/// Gets the reason why this field is excluded from state tracking.
	/// This is used for documentation and diagnostic purposes.
	/// </summary>
	public string? Reason { get; }

	/// <summary>
	/// Gets or sets whether this exclusion should be logged for diagnostic purposes.
	/// When true, the state tracking system will log that this field was explicitly ignored.
	/// Default is false to avoid log noise.
	/// </summary>
	public bool LogExclusion { get; set; } = false;

	/// <summary>
	/// Gets or sets whether this exclusion should apply to inherited classes.
	/// When true (default), derived classes will also ignore this field.
	/// When false, derived classes can override this exclusion.
	/// </summary>
	public bool ApplyToInheritedClasses { get; set; } = true;

	/// <summary>
	/// Initializes a new instance of the <see cref="IgnoreStateAttribute"/> class.
	/// </summary>
	public IgnoreStateAttribute() { }

	/// <summary>
	/// Initializes a new instance of the <see cref="IgnoreStateAttribute"/> class with a reason.
	/// </summary>
	/// <param name="reason">The reason why this field should be excluded from state tracking.</param>
	public IgnoreStateAttribute(string reason)
	{
		Reason = reason;
	}

	/// <summary>
	/// Gets a formatted description of why this field is ignored, suitable for logging.
	/// </summary>
	/// <returns>A formatted string describing the exclusion reason.</returns>
	internal string GetFormattedReason()
	{
		if (string.IsNullOrWhiteSpace(Reason))
			return "Field explicitly excluded from state tracking";

		return $"Field excluded from state tracking: {Reason}";
	}

	/// <summary>
	/// Determines if this exclusion should be applied in the given context.
	/// </summary>
	/// <param name="isInheritedField">True if the field is inherited from a base class.</param>
	/// <returns>True if the field should be excluded from tracking.</returns>
	internal bool ShouldApplyExclusion(bool isInheritedField)
	{
		if (!isInheritedField)
			return true;

		return ApplyToInheritedClasses;
	}
}
