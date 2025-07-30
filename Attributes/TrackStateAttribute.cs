using System;
using System.Collections;

using Blazor.WhyDidYouRender.Helpers;

namespace Blazor.WhyDidYouRender.Attributes;

/// <summary>
/// Marks a field or property to be explicitly tracked for state changes during render analysis.
/// This attribute is required for complex types (classes, custom structs, collections) to opt them into state tracking.
/// Simple value types (string, int, bool, DateTime, etc.) are tracked automatically without this attribute.
/// </summary>
/// <remarks>
/// Use this attribute when you want to track changes to complex objects that affect component rendering.
/// The state tracking system will monitor changes to the decorated field/property and include them
/// in unnecessary render detection analysis.
/// 
/// Example usage:
/// <code>
/// [TrackState]
/// private MyCustomStateObject _complexState = new();
/// 
/// [TrackState]
/// private List&lt;string&gt; _items = new();
/// 
/// [TrackState(UseCustomComparer = true)]
/// private Dictionary&lt;string, object&gt; _data = new();
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class TrackStateAttribute : Attribute {
	/// <summary>
	/// Gets or sets whether to use a custom equality comparer for this field.
	/// When true, the system will attempt to use IEquatable&lt;T&gt; or override Equals() method.
	/// When false (default), reference equality is used for complex types.
	/// </summary>
	/// <remarks>
	/// Setting this to true can improve accuracy of change detection but may impact performance
	/// for complex objects. Use with caution for large collections or deeply nested objects.
	/// </remarks>
	public bool UseCustomComparer { get; set; } = false;

	/// <summary>
	/// Gets or sets the maximum depth to traverse when comparing complex objects.
	/// Only applies when UseCustomComparer is true. Default is 1 (shallow comparison).
	/// </summary>
	/// <remarks>
	/// Higher values provide more accurate change detection but significantly impact performance.
	/// Use the minimum depth necessary for your use case.
	/// </remarks>
	public int MaxComparisonDepth { get; set; } = 1;

	/// <summary>
	/// Gets or sets whether to track changes to collection contents (for IEnumerable types).
	/// When true, changes to collection items will trigger state change detection.
	/// When false (default), only collection reference changes are detected.
	/// </summary>
	/// <remarks>
	/// Enabling this for large collections can significantly impact performance.
	/// Consider using this only for small collections or when collection content changes
	/// are critical for render analysis.
	/// </remarks>
	public bool TrackCollectionContents { get; set; } = false;

	/// <summary>
	/// Gets or sets a custom description for this tracked state field.
	/// This description will be included in diagnostic output to help identify
	/// which state changes triggered renders.
	/// </summary>
	public string? Description { get; set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="TrackStateAttribute"/> class.
	/// </summary>
	public TrackStateAttribute() {
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TrackStateAttribute"/> class with a description.
	/// </summary>
	/// <param name="description">A description of what this state field represents.</param>
	public TrackStateAttribute(string description) {
		Description = description;
	}

	/// <summary>
	/// Validates the attribute configuration and returns any validation errors.
	/// </summary>
	/// <returns>An array of validation error messages, empty if configuration is valid.</returns>
	internal string[] Validate() {
		List<string> errors = [];

		if (MaxComparisonDepth < 0)
			errors.Add("MaxComparisonDepth cannot be negative");

		if (MaxComparisonDepth > 10)
			errors.Add("MaxComparisonDepth should not exceed 10 to avoid performance issues");

		if (UseCustomComparer && MaxComparisonDepth == 0)
			errors.Add("MaxComparisonDepth must be greater than 0 when UseCustomComparer is true");

		return [.. errors];
	}

	/// <summary>
	/// Determines if this attribute configuration is suitable for the given field type.
	/// </summary>
	/// <param name="fieldType">The type of the field this attribute is applied to.</param>
	/// <returns>True if the configuration is appropriate for the field type.</returns>
	internal bool IsValidForType(Type fieldType) {
		if (TypeHelper.IsSimpleValueType(fieldType))
			return false;

		if (TrackCollectionContents && !typeof(IEnumerable).IsAssignableFrom(fieldType))
			return false;

		return true;
	}


}
