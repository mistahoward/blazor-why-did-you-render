namespace Blazor.WhyDidYouRender.Records;

/// <summary>
/// Represents a change in state between two snapshots.
/// This record provides immutable information about field changes during state tracking.
/// </summary>
public record StateChange
{
    /// <summary>
    /// Gets the name of the field that changed.
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
    /// Gets the type of change that occurred.
    /// </summary>
    public required StateChangeType ChangeType { get; init; }

    /// <summary>
    /// Gets a formatted description of this state change.
    /// </summary>
    /// <returns>A formatted string describing the change.</returns>
    public string GetFormattedDescription()
    {
        return ChangeType switch
        {
            StateChangeType.Added => $"Field '{FieldName}' added with value: {FormatValue(CurrentValue)}",
            StateChangeType.Modified => $"Field '{FieldName}' changed from {FormatValue(PreviousValue)} to {FormatValue(CurrentValue)}",
            StateChangeType.Removed => $"Field '{FieldName}' removed (was: {FormatValue(PreviousValue)})",
            _ => $"Field '{FieldName}' changed (unknown change type)"
        };
    }

    /// <summary>
    /// Formats a value for display in change descriptions.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <returns>A formatted string representation of the value.</returns>
    private static string FormatValue(object? value)
    {
        if (value == null)
            return "null";

        if (value is string str)
            return $"\"{str}\"";

        return value.ToString() ?? "null";
    }
}
