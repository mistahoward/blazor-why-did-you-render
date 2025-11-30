namespace Blazor.WhyDidYouRender.Core.StateTracking;

/// <summary>
/// Base class for comparison strategies.
/// </summary>
public abstract class ComparisonStrategy
{
	/// <summary>
	/// Compares two values using this strategy.
	/// </summary>
	/// <param name="previous">The previous value.</param>
	/// <param name="current">The current value.</param>
	/// <param name="comparer">The state comparer instance.</param>
	/// <returns>True if the values are equal.</returns>
	public abstract bool Compare(object previous, object current, StateComparer comparer);

	/// <summary>
	/// Gets a description of this comparison strategy.
	/// </summary>
	/// <returns>A string describing the strategy behavior.</returns>
	public abstract string GetDescription();

	/// <summary>
	/// Gets performance characteristics of this comparison strategy.
	/// </summary>
	/// <returns>A string describing performance characteristics.</returns>
	public virtual string GetPerformanceCharacteristics() => "Performance characteristics not specified";

	/// <summary>
	/// Gets usage recommendations for this comparison strategy.
	/// </summary>
	/// <returns>A string with usage recommendations.</returns>
	public virtual string GetUsageRecommendations() => "No specific usage recommendations";
}
