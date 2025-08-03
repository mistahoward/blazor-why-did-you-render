using System;
using System.Collections.Generic;
using System.Linq;

namespace Blazor.WhyDidYouRender.Records.StateTracking;

/// <summary>
/// Represents the comprehensive result of comparing two component state snapshots.
/// This record provides immutable information about all detected changes between states.
/// </summary>
/// <remarks>
/// StateComparisonResult aggregates field-level comparison results to provide a complete
/// picture of state changes. It includes summary information about whether changes occurred,
/// which fields changed, and detailed comparison results for each field.
/// 
/// This information is essential for:
/// - Determining if a component re-render is necessary
/// - Debugging unnecessary re-renders
/// - Understanding state change patterns
/// - Performance analysis and optimization
/// </remarks>
public record StateComparisonResult
{
    /// <summary>
    /// Gets whether any changes were detected between the state snapshots.
    /// </summary>
    public bool HasChanges { get; init; }

    /// <summary>
    /// Gets the list of field names that have changed.
    /// </summary>
    public IReadOnlyList<string> ChangedFields { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets detailed comparison results for each field that was compared.
    /// </summary>
    public IReadOnlyDictionary<string, FieldComparisonResult> FieldComparisons { get; init; } = 
        new Dictionary<string, FieldComparisonResult>();

    /// <summary>
    /// Gets the timestamp when this comparison was performed.
    /// </summary>
    public DateTime ComparisonTime { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the total number of fields that were compared.
    /// </summary>
    public int TotalFieldsCompared => FieldComparisons.Count;

    /// <summary>
    /// Gets the number of fields that changed.
    /// </summary>
    public int ChangedFieldCount => ChangedFields.Count;

    /// <summary>
    /// Gets the number of fields that remained unchanged.
    /// </summary>
    public int UnchangedFieldCount => TotalFieldsCompared - ChangedFieldCount;

    /// <summary>
    /// Gets the percentage of fields that changed.
    /// </summary>
    public double ChangePercentage => TotalFieldsCompared > 0 
        ? (double)ChangedFieldCount / TotalFieldsCompared * 100 
        : 0.0;

    /// <summary>
    /// Gets field comparisons that resulted in changes.
    /// </summary>
    public IEnumerable<FieldComparisonResult> ChangedFieldComparisons => 
        FieldComparisons.Values.Where(fc => fc.HasChanged);

    /// <summary>
    /// Gets field comparisons that resulted in no changes.
    /// </summary>
    public IEnumerable<FieldComparisonResult> UnchangedFieldComparisons => 
        FieldComparisons.Values.Where(fc => !fc.HasChanged);

    /// <summary>
    /// Gets field comparisons that represent meaningful changes (not just reference changes).
    /// </summary>
    public IEnumerable<FieldComparisonResult> MeaningfulChanges => 
        FieldComparisons.Values.Where(fc => fc.IsMeaningfulChange);

    /// <summary>
    /// Gets field comparisons that represent only reference changes.
    /// </summary>
    public IEnumerable<FieldComparisonResult> ReferenceOnlyChanges => 
        FieldComparisons.Values.Where(fc => fc.HasChanged && fc.ChangeType == FieldChangeType.ReferenceOnly);

    /// <summary>
    /// Gets whether this comparison detected only reference changes (no meaningful value changes).
    /// </summary>
    public bool HasOnlyReferenceChanges => HasChanges && !MeaningfulChanges.Any();

    /// <summary>
    /// Gets whether this comparison detected meaningful value changes.
    /// </summary>
    public bool HasMeaningfulChanges => MeaningfulChanges.Any();

    /// <summary>
    /// Gets a summary description of the comparison result.
    /// </summary>
    public string Summary => HasChanges 
        ? $"{ChangedFieldCount} of {TotalFieldsCompared} fields changed ({ChangePercentage:F1}%)"
        : $"No changes detected in {TotalFieldsCompared} fields";

    /// <summary>
    /// Creates a state comparison result indicating no changes.
    /// </summary>
    /// <param name="fieldComparisons">The field comparison results.</param>
    /// <returns>A StateComparisonResult indicating no changes.</returns>
    public static StateComparisonResult NoChanges(IReadOnlyDictionary<string, FieldComparisonResult> fieldComparisons) => new()
    {
        HasChanges = false,
        ChangedFields = Array.Empty<string>(),
        FieldComparisons = fieldComparisons
    };

    /// <summary>
    /// Creates a state comparison result with detected changes.
    /// </summary>
    /// <param name="fieldComparisons">The field comparison results.</param>
    /// <returns>A StateComparisonResult with detected changes.</returns>
    public static StateComparisonResult WithChanges(IReadOnlyDictionary<string, FieldComparisonResult> fieldComparisons)
    {
        var changedFields = fieldComparisons.Values
            .Where(fc => fc.HasChanged)
            .Select(fc => fc.FieldName)
            .ToList();

        return new StateComparisonResult
        {
            HasChanges = changedFields.Count > 0,
            ChangedFields = changedFields,
            FieldComparisons = fieldComparisons
        };
    }

    /// <summary>
    /// Creates an empty state comparison result.
    /// </summary>
    /// <returns>An empty StateComparisonResult.</returns>
    public static StateComparisonResult Empty() => new()
    {
        HasChanges = false,
        ChangedFields = Array.Empty<string>(),
        FieldComparisons = new Dictionary<string, FieldComparisonResult>()
    };

    /// <summary>
    /// Gets a specific field comparison result by field name.
    /// </summary>
    /// <param name="fieldName">The name of the field.</param>
    /// <returns>The field comparison result, or null if not found.</returns>
    public FieldComparisonResult? GetFieldComparison(string fieldName) =>
        FieldComparisons.TryGetValue(fieldName, out var result) ? result : null;

    /// <summary>
    /// Gets field comparisons of a specific change type.
    /// </summary>
    /// <param name="changeType">The type of change to filter by.</param>
    /// <returns>Field comparisons matching the specified change type.</returns>
    public IEnumerable<FieldComparisonResult> GetFieldsByChangeType(FieldChangeType changeType) =>
        FieldComparisons.Values.Where(fc => fc.ChangeType == changeType);

    /// <summary>
    /// Gets a formatted summary of the state comparison result.
    /// </summary>
    /// <returns>A formatted string with comprehensive comparison information.</returns>
    public string GetFormattedSummary()
    {
        var summary = $"State Comparison Result:\n" +
                     $"  Comparison Time: {ComparisonTime:yyyy-MM-dd HH:mm:ss.fff}\n" +
                     $"  Total Fields: {TotalFieldsCompared}\n" +
                     $"  Changed Fields: {ChangedFieldCount}\n" +
                     $"  Unchanged Fields: {UnchangedFieldCount}\n" +
                     $"  Change Percentage: {ChangePercentage:F1}%\n" +
                     $"  Has Changes: {HasChanges}\n" +
                     $"  Meaningful Changes: {MeaningfulChanges.Count()}\n" +
                     $"  Reference-Only Changes: {ReferenceOnlyChanges.Count()}";

        if (HasChanges)
        {
            summary += $"\n  Changed Fields:\n";
            foreach (var field in ChangedFields.Take(10)) // Show first 10
            {
                var comparison = FieldComparisons[field];
                summary += $"    • {comparison.ChangeSummary}\n";
            }

            if (ChangedFields.Count > 10)
            {
                summary += $"    ... and {ChangedFields.Count - 10} more fields\n";
            }
        }

        return summary;
    }

    /// <summary>
    /// Gets performance metrics about the comparison operation.
    /// </summary>
    /// <returns>A dictionary of performance metrics.</returns>
    public Dictionary<string, object> GetPerformanceMetrics() => new()
    {
        ["TotalFieldsCompared"] = TotalFieldsCompared,
        ["ChangedFieldCount"] = ChangedFieldCount,
        ["ChangePercentage"] = ChangePercentage,
        ["HasMeaningfulChanges"] = HasMeaningfulChanges,
        ["HasOnlyReferenceChanges"] = HasOnlyReferenceChanges,
        ["ComparisonTime"] = ComparisonTime
    };
}
