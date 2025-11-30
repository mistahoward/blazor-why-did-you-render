using System;

namespace Blazor.WhyDidYouRender.Helpers;

/// <summary>
/// Utility class for common type checking operations used throughout the WhyDidYouRender system.
/// Provides centralized logic for determining type characteristics to avoid code duplication.
/// </summary>
public static class TypeHelper
{
	/// <summary>
	/// Determines if the specified type is a simple value type that should be auto-tracked.
	/// Simple value types include primitives, strings, and common .NET value types.
	/// </summary>
	/// <param name="type">The type to check.</param>
	/// <returns>True if the type is a simple value type; otherwise, false.</returns>
	/// <remarks>
	/// This method handles nullable types by checking the underlying type.
	/// The following types are considered simple value types:
	/// - string
	/// - bool, char
	/// - All integer types (int, long, short, byte, uint, ulong, ushort, sbyte)
	/// - All floating-point types (float, double, decimal)
	/// - Date/time types (DateTime, DateTimeOffset, TimeSpan)
	/// - Guid
	/// </remarks>
	public static bool IsSimpleValueType(Type type)
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
			|| underlyingType == typeof(Guid);
	}

	/// <summary>
	/// Determines if the specified type is a collection type.
	/// </summary>
	/// <param name="type">The type to check.</param>
	/// <returns>True if the type implements IEnumerable but is not a string; otherwise, false.</returns>
	/// <remarks>
	/// String is excluded because while it implements IEnumerable&lt;char&gt;,
	/// it's treated as a simple value type for state tracking purposes.
	/// </remarks>
	public static bool IsCollectionType(Type type) =>
		typeof(System.Collections.IEnumerable).IsAssignableFrom(type) && type != typeof(string);

	/// <summary>
	/// Determines if a value is a complex object worth detailed inspection.
	/// This is useful for logging and debugging scenarios.
	/// </summary>
	/// <param name="value">The value to check.</param>
	/// <returns>True if the value is a complex object; otherwise, false.</returns>
	public static bool IsComplexObject(object? value)
	{
		if (value == null)
			return false;

		var type = value.GetType();

		return !type.IsPrimitive
			&& type != typeof(string)
			&& type != typeof(DateTime)
			&& type != typeof(DateTimeOffset)
			&& type != typeof(TimeSpan)
			&& type != typeof(Guid)
			&& !type.IsEnum;
	}
}
