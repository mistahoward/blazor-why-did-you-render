using System;

namespace Blazor.WhyDidYouRender.Core.StateTracking;

/// <summary>
/// Implements a comparison strategy that uses reference equality for object comparison.
/// This strategy is the fastest comparison method but only detects when object references change.
/// </summary>
/// <remarks>
/// ReferenceComparisonStrategy is the default strategy for complex objects and provides
/// optimal performance by comparing only object references rather than object contents.
/// 
/// This approach means that:
/// - Assigning a new object instance will be detected as a change
/// - Modifying properties of an existing object will NOT be detected as a change
/// - Performance is optimal (O(1)) regardless of object complexity
/// 
/// Use this strategy when:
/// - Performance is critical
/// - Objects are typically replaced rather than modified
/// - You follow immutable object patterns
/// - Deep comparison would be too expensive
/// </remarks>
public class ReferenceComparisonStrategy : ComparisonStrategy {
    /// <summary>
    /// Compares two object values using reference equality.
    /// </summary>
    /// <param name="previous">The previous object value.</param>
    /// <param name="current">The current object value.</param>
    /// <param name="comparer">The state comparer instance (not used in this strategy).</param>
    /// <returns>True if the objects are the same reference; otherwise, false.</returns>
    /// <remarks>
    /// This method uses ReferenceEquals which is the fastest possible comparison.
    /// It will return true only if both parameters refer to the exact same object instance.
    /// Null values are handled correctly - two null references are considered equal.
    /// </remarks>
    public override bool Compare(object previous, object current, StateComparer comparer) =>
        ReferenceEquals(previous, current);

    /// <summary>
    /// Gets a description of this comparison strategy.
    /// </summary>
    /// <returns>A string describing the strategy behavior.</returns>
    public override string GetDescription() =>
        "Reference equality comparison for optimal performance with complex objects";

    /// <summary>
    /// Gets whether this strategy is suitable for the given type.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is a reference type; otherwise, false.</returns>
    public static bool IsSuitableFor(Type type) =>
        !type.IsValueType || (Nullable.GetUnderlyingType(type) != null);

    /// <summary>
    /// Gets performance characteristics of this comparison strategy.
    /// </summary>
    /// <returns>A string describing performance characteristics.</returns>
    public override string GetPerformanceCharacteristics() =>
        "O(1) - Constant time reference comparison, fastest possible strategy";

    /// <summary>
    /// Gets recommendations for when to use this strategy.
    /// </summary>
    /// <returns>A string with usage recommendations.</returns>
    public override string GetUsageRecommendations() =>
        "Best for: Immutable objects, performance-critical scenarios, large complex objects. " +
        "Avoid for: Objects that are modified in-place, when deep equality is required.";
}
