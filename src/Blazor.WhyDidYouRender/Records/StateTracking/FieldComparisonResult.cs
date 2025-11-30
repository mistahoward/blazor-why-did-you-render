using System;

namespace Blazor.WhyDidYouRender.Records.StateTracking;

/// <summary>
/// Represents the result of comparing a specific field between two state snapshots.
/// This record provides immutable information about field-level changes detected during state comparison.
/// </summary>
/// <remarks>
/// FieldComparisonResult contains detailed information about individual field changes including
/// the field name, previous and current values, change status, and the reason for the change.
/// This granular information is essential for debugging state changes and understanding
/// component re-render triggers.
/// </remarks>
public record FieldComparisonResult
{
	/// <summary>
	/// Gets the name of the field that was compared.
	/// </summary>
	public required string FieldName { get; init; }

	/// <summary>
	/// Gets the previous value of the field.
	/// </summary>
	public object? PreviousValue { get; init; }

	/// <summary>
	/// Gets the current value of the field.
	/// </summary>
	public object? CurrentValue { get; init; }

	/// <summary>
	/// Gets whether the field value has changed.
	/// </summary>
	public bool HasChanged { get; init; }

	/// <summary>
	/// Gets a detailed explanation of why the change was detected or not detected.
	/// </summary>
	public string ChangeReason { get; init; } = string.Empty;

	/// <summary>
	/// Gets the type of change that occurred.
	/// </summary>
	public FieldChangeType ChangeType { get; init; }

	/// <summary>
	/// Gets the comparison strategy that was used for this field.
	/// </summary>
	public string? ComparisonStrategy { get; init; }

	/// <summary>
	/// Gets additional metadata about the comparison operation.
	/// </summary>
	public string? Metadata { get; init; }

	/// <summary>
	/// Gets whether this represents a meaningful change (not just a reference change).
	/// </summary>
	public bool IsMeaningfulChange => HasChanged && ChangeType != FieldChangeType.ReferenceOnly;

	/// <summary>
	/// Gets whether this change involved null values.
	/// </summary>
	public bool InvolvedNullValues => PreviousValue == null || CurrentValue == null;

	/// <summary>
	/// Gets whether this change was from null to non-null.
	/// </summary>
	public bool IsNullToValue => PreviousValue == null && CurrentValue != null;

	/// <summary>
	/// Gets whether this change was from non-null to null.
	/// </summary>
	public bool IsValueToNull => PreviousValue != null && CurrentValue == null;

	/// <summary>
	/// Gets the field type if available.
	/// </summary>
	public Type? FieldType { get; init; }

	/// <summary>
	/// Gets a brief summary of the change.
	/// </summary>
	public string ChangeSummary =>
		HasChanged ? $"{FieldName}: {GetValueDescription(PreviousValue)} → {GetValueDescription(CurrentValue)}" : $"{FieldName}: No change";

	/// <summary>
	/// Creates a field comparison result indicating no change.
	/// </summary>
	/// <param name="fieldName">The name of the field.</param>
	/// <param name="value">The unchanged value.</param>
	/// <param name="comparisonStrategy">The strategy used for comparison.</param>
	/// <returns>A FieldComparisonResult indicating no change.</returns>
	public static FieldComparisonResult NoChange(string fieldName, object? value, string? comparisonStrategy = null) =>
		new()
		{
			FieldName = fieldName,
			PreviousValue = value,
			CurrentValue = value,
			HasChanged = false,
			ChangeReason = "Values are equal",
			ChangeType = FieldChangeType.None,
			ComparisonStrategy = comparisonStrategy,
		};

	/// <summary>
	/// Creates a field comparison result indicating a value change.
	/// </summary>
	/// <param name="fieldName">The name of the field.</param>
	/// <param name="previousValue">The previous value.</param>
	/// <param name="currentValue">The current value.</param>
	/// <param name="changeReason">The reason for the change.</param>
	/// <param name="changeType">The type of change.</param>
	/// <param name="comparisonStrategy">The strategy used for comparison.</param>
	/// <returns>A FieldComparisonResult indicating a change.</returns>
	public static FieldComparisonResult Changed(
		string fieldName,
		object? previousValue,
		object? currentValue,
		string changeReason = "Value changed",
		FieldChangeType changeType = FieldChangeType.ValueChanged,
		string? comparisonStrategy = null
	) =>
		new()
		{
			FieldName = fieldName,
			PreviousValue = previousValue,
			CurrentValue = currentValue,
			HasChanged = true,
			ChangeReason = changeReason,
			ChangeType = changeType,
			ComparisonStrategy = comparisonStrategy,
		};

	/// <summary>
	/// Creates a field comparison result for a reference-only change.
	/// </summary>
	/// <param name="fieldName">The name of the field.</param>
	/// <param name="previousValue">The previous value.</param>
	/// <param name="currentValue">The current value.</param>
	/// <param name="comparisonStrategy">The strategy used for comparison.</param>
	/// <returns>A FieldComparisonResult indicating a reference change.</returns>
	public static FieldComparisonResult ReferenceChanged(
		string fieldName,
		object? previousValue,
		object? currentValue,
		string? comparisonStrategy = null
	) =>
		new()
		{
			FieldName = fieldName,
			PreviousValue = previousValue,
			CurrentValue = currentValue,
			HasChanged = true,
			ChangeReason = "Object reference changed",
			ChangeType = FieldChangeType.ReferenceOnly,
			ComparisonStrategy = comparisonStrategy,
		};

	/// <summary>
	/// Gets a formatted description of the comparison result.
	/// </summary>
	/// <returns>A formatted string with detailed comparison information.</returns>
	public string GetFormattedDescription()
	{
		var description =
			$"Field: {FieldName}\n"
			+ $"  Changed: {HasChanged}\n"
			+ $"  Change Type: {ChangeType}\n"
			+ $"  Previous: {GetValueDescription(PreviousValue)}\n"
			+ $"  Current: {GetValueDescription(CurrentValue)}\n"
			+ $"  Reason: {ChangeReason}";

		if (!string.IsNullOrEmpty(ComparisonStrategy))
			description += $"\n  Strategy: {ComparisonStrategy}";

		if (!string.IsNullOrEmpty(Metadata))
			description += $"\n  Metadata: {Metadata}";

		return description;
	}

	/// <summary>
	/// Gets a safe string description of a value for display purposes.
	/// </summary>
	/// <param name="value">The value to describe.</param>
	/// <returns>A string description of the value.</returns>
	private static string GetValueDescription(object? value) =>
		value switch
		{
			null => "null",
			string s => $"\"{s}\"",
			_ => value.ToString() ?? "null",
		};
}

/// <summary>
/// Defines the types of field changes that can be detected.
/// </summary>
public enum FieldChangeType
{
	/// <summary>
	/// No change was detected.
	/// </summary>
	None,

	/// <summary>
	/// The field value changed meaningfully.
	/// </summary>
	ValueChanged,

	/// <summary>
	/// Only the object reference changed (content may be the same).
	/// </summary>
	ReferenceOnly,

	/// <summary>
	/// The field was added (not present in previous snapshot).
	/// </summary>
	Added,

	/// <summary>
	/// The field was removed (not present in current snapshot).
	/// </summary>
	Removed,

	/// <summary>
	/// The field type changed.
	/// </summary>
	TypeChanged,
}
