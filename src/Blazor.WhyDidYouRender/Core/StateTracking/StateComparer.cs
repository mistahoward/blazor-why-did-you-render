using System.Collections;
using Blazor.WhyDidYouRender.Attributes;
using Blazor.WhyDidYouRender.Records;
using Blazor.WhyDidYouRender.Records.StateTracking;

namespace Blazor.WhyDidYouRender.Core.StateTracking;

/// <summary>
/// Provides efficient comparison logic for different types of field values.
/// This class implements optimized comparison strategies for various data types
/// to accurately detect state changes while maintaining performance.
/// </summary>
public class StateComparer
{
	/// <summary>
	/// Cache of type-specific comparison strategies for performance.
	/// </summary>
	private readonly Dictionary<Type, ComparisonStrategy> _comparisonStrategies = [];

	/// <summary>
	/// Initializes a new instance of the <see cref="StateComparer"/> class.
	/// </summary>
	public StateComparer()
	{
		InitializeComparisonStrategies();
	}

	/// <summary>
	/// Compares two field values for equality using the appropriate comparison strategy.
	/// </summary>
	/// <param name="previous">The previous field value.</param>
	/// <param name="current">The current field value.</param>
	/// <param name="fieldType">The type of the field being compared.</param>
	/// <param name="trackingInfo">Optional tracking information for custom comparison settings.</param>
	/// <returns>True if the values are equal.</returns>
	public bool AreEqual(object? previous, object? current, Type fieldType, FieldTrackingInfo? trackingInfo = null)
	{
		// Fast paths for reference and null equality
		if (ReferenceEquals(previous, current))
			return true;

		if (previous == null || current == null)
			return false;

		var underlyingType = Nullable.GetUnderlyingType(fieldType) ?? fieldType;

		// When TrackCollectionContents is enabled for a field, always prefer content
		// comparison for collections, regardless of whether a custom comparer was
		// requested. This ensures that state tracking for collection fields only
		// reports changes when the actual contents differ rather than on every
		// render due to snapshot cloning.
		if (trackingInfo is { TrackCollectionContents: true })
		{
			var isCollection = typeof(IEnumerable).IsAssignableFrom(underlyingType) && underlyingType != typeof(string);
			if (isCollection)
			{
				var depth = trackingInfo.MaxComparisonDepth <= 0 ? 1 : trackingInfo.MaxComparisonDepth;
				return CompareCollectionContents(previous as IEnumerable, current as IEnumerable, depth);
			}
		}

		// For non-collection fields, allow opt-in custom comparison via
		// IEquatable<T> when requested.
		if (trackingInfo?.UsesCustomComparison == true)
		{
			return CompareWithCustomLogic(previous, current, underlyingType, trackingInfo);
		}

		var strategy = GetComparisonStrategy(underlyingType);
		return strategy.Compare(previous, current, this);
	}

	/// <summary>
	/// Compares two <em>parameter</em> values for equality, honoring opt-in deep comparison
	/// semantics configured via <see cref="TrackStateAttribute"/>.
	/// </summary>
	/// <param name="previous">The previous parameter value.</param>
	/// <param name="current">The current parameter value.</param>
	/// <param name="parameterType">The declared parameter type.</param>
	/// <param name="trackStateAttribute">Optional tracking attribute applied to the parameter.</param>
	/// <returns>
	/// True if the values are considered equal under the configured comparison rules; otherwise false.
	/// </returns>
	public bool AreParameterValuesEqual(object? previous, object? current, Type parameterType, TrackStateAttribute? trackStateAttribute)
	{
		// Fast paths first: null/reference equality
		if (ReferenceEquals(previous, current))
			return true;

		if (previous is null || current is null)
			return false;

		// When no attribute is present, fall back to simple value semantics. This
		// preserves the default, WDYR-style behavior for unannotated parameters.
		if (trackStateAttribute is null)
			return previous.Equals(current);

		var underlyingType = Nullable.GetUnderlyingType(parameterType) ?? parameterType;
		var isCollection = typeof(IEnumerable).IsAssignableFrom(underlyingType) && underlyingType != typeof(string);

		// For collections, opting in via [TrackState] means we default to comparing
		// collection contents rather than just the collection reference.
		if (isCollection)
		{
			var depth = trackStateAttribute.MaxComparisonDepth <= 0 ? 1 : trackStateAttribute.MaxComparisonDepth;
			return CompareCollectionContents(previous as IEnumerable, current as IEnumerable, depth);
		}

		// For non-collection complex types, allow the consumer to opt into custom
		// comparison via IEquatable<T>. If that is not available we fall back to
		// the type's Equals implementation.
		if (trackStateAttribute.UseCustomComparer)
		{
			if (TryUseIEquatable(previous, current, underlyingType, out var equatableResult))
				return equatableResult;
		}

		// Final fallback – rely on the type's own equality semantics.
		return previous.Equals(current);
	}

	/// <summary>
	/// Gets detailed comparison information between two state snapshots.
	/// </summary>
	/// <param name="previous">The previous state snapshot.</param>
	/// <param name="current">The current state snapshot.</param>
	/// <param name="metadata">The field metadata for the component.</param>
	/// <returns>Detailed comparison results.</returns>
	public StateComparisonResult GetDetailedComparison(StateSnapshot? previous, StateSnapshot current, StateFieldMetadata metadata)
	{
		if (previous == null)
		{
			var initialComparisons = current.FieldValues.ToDictionary(
				kvp => kvp.Key,
				kvp => FieldComparisonResult.Changed(kvp.Key, null, kvp.Value, "Field added")
			);
			return StateComparisonResult.WithChanges(initialComparisons);
		}

		var hasChanges = false;
		var changedFields = new List<string>();
		var fieldComparisons = new Dictionary<string, FieldComparisonResult>();

		foreach (var fieldInfo in metadata.AllTrackedFields)
		{
			var fieldName = fieldInfo.FieldInfo.Name;
			var previousValue = previous.GetFieldValue(fieldName);
			var currentValue = current.GetFieldValue(fieldName);

			var areEqual = AreEqual(previousValue, currentValue, fieldInfo.FieldInfo.FieldType, fieldInfo);

			if (!areEqual)
			{
				hasChanges = true;
				changedFields.Add(fieldName);
			}

			var reason = areEqual ? "No change" : GetChangeReason(previousValue, currentValue, fieldInfo);
			fieldComparisons[fieldName] = areEqual
				? FieldComparisonResult.NoChange(fieldName, currentValue)
				: FieldComparisonResult.Changed(fieldName, previousValue, currentValue, reason);
		}

		return hasChanges ? StateComparisonResult.WithChanges(fieldComparisons) : StateComparisonResult.NoChanges(fieldComparisons);
	}

	/// <summary>
	/// Initializes the comparison strategies for different types.
	/// </summary>
	private void InitializeComparisonStrategies()
	{
		var simpleValueStrategy = new SimpleValueComparisonStrategy();
		_comparisonStrategies[typeof(string)] = simpleValueStrategy;
		_comparisonStrategies[typeof(bool)] = simpleValueStrategy;
		_comparisonStrategies[typeof(char)] = simpleValueStrategy;
		_comparisonStrategies[typeof(int)] = simpleValueStrategy;
		_comparisonStrategies[typeof(long)] = simpleValueStrategy;
		_comparisonStrategies[typeof(short)] = simpleValueStrategy;
		_comparisonStrategies[typeof(byte)] = simpleValueStrategy;
		_comparisonStrategies[typeof(uint)] = simpleValueStrategy;
		_comparisonStrategies[typeof(ulong)] = simpleValueStrategy;
		_comparisonStrategies[typeof(ushort)] = simpleValueStrategy;
		_comparisonStrategies[typeof(sbyte)] = simpleValueStrategy;
		_comparisonStrategies[typeof(float)] = simpleValueStrategy;
		_comparisonStrategies[typeof(double)] = simpleValueStrategy;
		_comparisonStrategies[typeof(decimal)] = simpleValueStrategy;
		_comparisonStrategies[typeof(DateTime)] = simpleValueStrategy;
		_comparisonStrategies[typeof(DateTimeOffset)] = simpleValueStrategy;
		_comparisonStrategies[typeof(TimeSpan)] = simpleValueStrategy;
		_comparisonStrategies[typeof(Guid)] = simpleValueStrategy;

		_comparisonStrategies[typeof(IEnumerable)] = new CollectionComparisonStrategy();
		_comparisonStrategies[typeof(object)] = new ReferenceComparisonStrategy();
	}

	/// <summary>
	/// Gets the appropriate comparison strategy for a given type.
	/// </summary>
	/// <param name="type">The type to get a strategy for.</param>
	/// <returns>The comparison strategy.</returns>
	private ComparisonStrategy GetComparisonStrategy(Type type)
	{
		var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

		if (_comparisonStrategies.TryGetValue(underlyingType, out var strategy))
			return strategy;

		if (typeof(IEnumerable).IsAssignableFrom(underlyingType) && underlyingType != typeof(string))
			return _comparisonStrategies[typeof(IEnumerable)];

		return _comparisonStrategies[typeof(object)];
	}

	/// <summary>
	/// Performs comparison using custom logic based on tracking information.
	/// </summary>
	/// <param name="previous">The previous value.</param>
	/// <param name="current">The current value.</param>
	/// <param name="fieldType">The field type.</param>
	/// <param name="trackingInfo">The tracking information.</param>
	/// <returns>True if the values are equal.</returns>
	private bool CompareWithCustomLogic(object previous, object current, Type fieldType, FieldTrackingInfo trackingInfo)
	{
		try
		{
			// try to use IEquatable<T> if available
			if (TryUseIEquatable(previous, current, fieldType, out var result))
				return result;

			// for collections, use collection-specific comparison if requested
			if (trackingInfo.TrackCollectionContents && typeof(IEnumerable).IsAssignableFrom(fieldType))
				return CompareCollectionContents(previous as IEnumerable, current as IEnumerable, trackingInfo.MaxComparisonDepth);

			// fall back to Equals method
			return previous.Equals(current);
		}
		catch (Exception)
		{
			// if custom comparison fails, fall back to reference equality
			return ReferenceEquals(previous, current);
		}
	}

	/// <summary>
	/// Attempts to use IEquatable&lt;T&gt; for comparison.
	/// </summary>
	/// <param name="previous">The previous value.</param>
	/// <param name="current">The current value.</param>
	/// <param name="fieldType">The field type.</param>
	/// <param name="result">The comparison result.</param>
	/// <returns>True if IEquatable was used successfully.</returns>
	private static bool TryUseIEquatable(object previous, object current, Type fieldType, out bool result)
	{
		result = false;

		var equatableInterface = typeof(IEquatable<>).MakeGenericType(fieldType);
		if (equatableInterface.IsAssignableFrom(fieldType))
		{
			var equalsMethod = equatableInterface.GetMethod("Equals", [fieldType]);
			if (equalsMethod != null)
			{
				result = (bool)equalsMethod.Invoke(previous, [current])!;
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Compares collection contents up to a specified depth.
	/// </summary>
	/// <param name="previous">The previous collection.</param>
	/// <param name="current">The current collection.</param>
	/// <param name="maxDepth">The maximum comparison depth.</param>
	/// <returns>True if the collections have the same contents.</returns>
	private bool CompareCollectionContents(IEnumerable? previous, IEnumerable? current, int maxDepth)
	{
		if (previous == null && current == null)
			return true;

		if (previous == null || current == null)
			return false;

		if (maxDepth <= 0)
			return ReferenceEquals(previous, current);

		try
		{
			var previousList = previous.Cast<object?>().ToList();
			var currentList = current.Cast<object?>().ToList();

			if (previousList.Count != currentList.Count)
				return false;

			for (int i = 0; i < previousList.Count; i++)
			{
				var prevItem = previousList[i];
				var currItem = currentList[i];

				// Use the most specific runtime type we can for element comparison so
				// that value types and simple types get proper value semantics.
				var itemType = prevItem?.GetType() ?? currItem?.GetType() ?? typeof(object);
				if (!AreEqual(prevItem, currItem, itemType))
					return false;
			}

			return true;
		}
		catch (Exception)
		{
			// if collection comparison fails, fall back to reference equality
			return ReferenceEquals(previous, current);
		}
	}

	/// <summary>
	/// Gets a human-readable reason for why two values are different.
	/// </summary>
	/// <param name="previous">The previous value.</param>
	/// <param name="current">The current value.</param>
	/// <param name="fieldInfo">The field tracking information.</param>
	/// <returns>A description of the change.</returns>
	private static string GetChangeReason(object? previous, object? current, FieldTrackingInfo fieldInfo)
	{
		if (previous == null && current != null)
			return "Value changed from null to non-null";

		if (previous != null && current == null)
			return "Value changed from non-null to null";

		if (previous?.GetType() != current?.GetType())
			return "Value type changed";

		if (fieldInfo.UsesCustomComparison)
			return "Custom comparison detected change";

		return "Value changed";
	}
}
