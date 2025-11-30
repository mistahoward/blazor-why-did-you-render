using System;

namespace Blazor.WhyDidYouRender.Core.StateTracking;

/// <summary>
/// Implements a comparison strategy optimized for simple value types and strings.
/// This strategy uses the Equals method to perform value-based comparison.
/// </summary>
/// <remarks>
/// SimpleValueComparisonStrategy is designed for primitive types, strings, and other
/// simple value types that implement meaningful Equals() methods. It provides accurate
/// change detection for these types while maintaining good performance.
///
/// This strategy handles:
/// - All primitive types (int, bool, double, etc.)
/// - String comparisons
/// - DateTime, TimeSpan, Guid
/// - Nullable versions of value types
/// - Enums
///
/// The strategy automatically handles null values correctly and uses the type's
/// built-in Equals implementation for accurate value comparison.
/// </remarks>
public class SimpleValueComparisonStrategy : ComparisonStrategy
{
	/// <summary>
	/// Compares two simple values using their Equals implementation.
	/// </summary>
	/// <param name="previous">The previous value.</param>
	/// <param name="current">The current value.</param>
	/// <param name="comparer">The state comparer instance (not used in this strategy).</param>
	/// <returns>True if the values are equal according to their Equals method; otherwise, false.</returns>
	/// <remarks>
	/// This method safely handles null values and uses the object's Equals method for comparison.
	/// For value types, this provides accurate value-based comparison. For strings, this
	/// provides ordinal string comparison which is appropriate for state tracking.
	/// </remarks>
	public override bool Compare(object previous, object current, StateComparer comparer)
	{
		// Handle null cases
		if (previous == null && current == null)
			return true;
		if (previous == null || current == null)
			return false;

		// Use the object's Equals method for value comparison
		return previous.Equals(current);
	}

	/// <summary>
	/// Gets a description of this comparison strategy.
	/// </summary>
	/// <returns>A string describing the strategy behavior.</returns>
	public override string GetDescription() => "Value-based comparison using Equals() method for simple types";

	/// <summary>
	/// Gets whether this strategy is suitable for the given type.
	/// </summary>
	/// <param name="type">The type to check.</param>
	/// <returns>True if the type is a simple value type; otherwise, false.</returns>
	public static bool IsSuitableFor(Type type)
	{
		var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

		return underlyingType == typeof(string)
			|| underlyingType == typeof(bool)
			|| underlyingType == typeof(char)
			|| underlyingType == typeof(int)
			|| underlyingType == typeof(long)
			|| underlyingType == typeof(short)
			|| underlyingType == typeof(byte)
			|| underlyingType == typeof(uint)
			|| underlyingType == typeof(ulong)
			|| underlyingType == typeof(ushort)
			|| underlyingType == typeof(sbyte)
			|| underlyingType == typeof(float)
			|| underlyingType == typeof(double)
			|| underlyingType == typeof(decimal)
			|| underlyingType == typeof(DateTime)
			|| underlyingType == typeof(DateTimeOffset)
			|| underlyingType == typeof(TimeSpan)
			|| underlyingType == typeof(Guid)
			|| underlyingType.IsEnum;
	}

	/// <summary>
	/// Gets performance characteristics of this comparison strategy.
	/// </summary>
	/// <returns>A string describing performance characteristics.</returns>
	public override string GetPerformanceCharacteristics() => "O(1) - Fast value comparison using built-in Equals implementation";

	/// <summary>
	/// Gets the types that this strategy is optimized for.
	/// </summary>
	/// <returns>A string listing supported types.</returns>
	public string GetSupportedTypes() =>
		"Primitives (int, bool, double, etc.), string, DateTime, TimeSpan, Guid, enums, and their nullable versions";

	/// <summary>
	/// Gets recommendations for when to use this strategy.
	/// </summary>
	/// <returns>A string with usage recommendations.</returns>
	public override string GetUsageRecommendations() =>
		"Automatically used for simple value types. Provides accurate change detection "
		+ "with minimal performance overhead. Ideal for component state fields that store "
		+ "primitive values, strings, or simple value types.";
}
