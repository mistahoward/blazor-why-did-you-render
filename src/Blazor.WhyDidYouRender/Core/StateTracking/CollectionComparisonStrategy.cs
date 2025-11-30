namespace Blazor.WhyDidYouRender.Core.StateTracking;

/// <summary>
/// Implements a comparison strategy specifically designed for collection types.
/// This strategy uses reference equality to determine if collections have changed.
/// </summary>
/// <remarks>
/// CollectionComparisonStrategy is optimized for performance when dealing with collections
/// like List&lt;T&gt;, Dictionary&lt;K,V&gt;, and other IEnumerable implementations. It uses
/// reference equality rather than deep comparison to avoid expensive enumeration operations.
///
/// This approach means that:
/// - Replacing a collection instance will be detected as a change
/// - Modifying collection contents without replacing the instance may not be detected
/// - Performance is optimal for large collections
///
/// For content-aware collection tracking, use the TrackCollectionContents attribute option.
/// </remarks>
public class CollectionComparisonStrategy : ComparisonStrategy
{
	/// <summary>
	/// Compares two collection values using reference equality.
	/// </summary>
	/// <param name="previous">The previous collection value.</param>
	/// <param name="current">The current collection value.</param>
	/// <param name="comparer">The state comparer instance (not used in this strategy).</param>
	/// <returns>True if the collections are the same reference; otherwise, false.</returns>
	/// <remarks>
	/// This method intentionally uses reference equality rather than deep comparison
	/// for performance reasons. Collections are considered equal only if they are
	/// the exact same object instance.
	/// </remarks>
	public override bool Compare(object previous, object current, StateComparer comparer) => ReferenceEquals(previous, current);

	/// <summary>
	/// Gets a description of this comparison strategy.
	/// </summary>
	/// <returns>A string describing the strategy behavior.</returns>
	public override string GetDescription() => "Collection comparison using reference equality for optimal performance";

	/// <summary>
	/// Gets whether this strategy is suitable for the given type.
	/// </summary>
	/// <param name="type">The type to check.</param>
	/// <returns>True if the type is a collection type; otherwise, false.</returns>
	public static bool IsSuitableFor(Type type) => typeof(System.Collections.IEnumerable).IsAssignableFrom(type) && type != typeof(string);

	/// <summary>
	/// Gets performance characteristics of this comparison strategy.
	/// </summary>
	/// <returns>A string describing performance characteristics.</returns>
	public override string GetPerformanceCharacteristics() => "O(1) - Constant time reference comparison, optimal for large collections";
}
