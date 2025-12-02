using System;
using System.Collections.Generic;
using System.Linq;
using Blazor.WhyDidYouRender.Records;
using Microsoft.AspNetCore.Components;

namespace Blazor.WhyDidYouRender.Core.StateTracking;

/// <summary>
/// Represents a snapshot of a component's state at a specific point in time.
/// This class captures field values for comparison to detect state changes.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="StateSnapshot"/> class.
/// </remarks>
/// <param name="componentType">The component type.</param>
/// <param name="fieldValues">The captured field values.</param>
public class StateSnapshot(Type componentType, IReadOnlyDictionary<string, object?> fieldValues)
{
	/// <summary>
	/// Gets the component type this snapshot was taken from.
	/// </summary>
	public Type ComponentType { get; } = componentType ?? throw new ArgumentNullException(nameof(componentType));

	/// <summary>
	/// Gets the field values captured in this snapshot.
	/// Key is the field name, value is the field value at the time of capture.
	/// </summary>
	public IReadOnlyDictionary<string, object?> FieldValues { get; } = fieldValues ?? throw new ArgumentNullException(nameof(fieldValues));

	/// <summary>
	/// Gets the component name for display purposes.
	/// </summary>
	public string ComponentName => ComponentType.Name;

	/// <summary>
	/// Gets the field snapshots as a dictionary (alias for FieldValues for compatibility).
	/// </summary>
	public IReadOnlyDictionary<string, object?> FieldSnapshots => FieldValues;

	/// <summary>
	/// Gets the timestamp when this snapshot was created.
	/// </summary>
	public DateTime CapturedAt { get; } = DateTime.UtcNow;

	/// <summary>
	/// Gets the number of fields captured in this snapshot.
	/// </summary>
	public int FieldCount => FieldValues.Count;

	/// <summary>
	/// Gets whether this snapshot contains any field values.
	/// </summary>
	public bool HasValues => FieldCount > 0;

	/// <summary>
	/// Creates a state snapshot from a component instance using the provided metadata.
	/// </summary>
	/// <param name="component">The component to capture state from.</param>
	/// <param name="metadata">The metadata describing which fields to capture.</param>
	/// <returns>A new state snapshot.</returns>
	public static StateSnapshot Create(ComponentBase component, StateFieldMetadata metadata)
	{
		ArgumentNullException.ThrowIfNull(component);
		ArgumentNullException.ThrowIfNull(metadata);

		if (metadata.IsStateTrackingDisabled || !metadata.HasTrackableFields)
			return new StateSnapshot(component.GetType(), new Dictionary<string, object?>());

		var fieldValues = new Dictionary<string, object?>();

		foreach (var fieldInfo in metadata.AllTrackedFields)
		{
			try
			{
				var value = fieldInfo.FieldInfo.GetValue(component);

				if (fieldInfo.TrackCollectionContents && value is System.Collections.IEnumerable enumerable && value is not string)
					value = CreateCollectionCopy(enumerable);

				fieldValues[fieldInfo.FieldInfo.Name] = value;
			}
			catch (Exception)
			{
				fieldValues[fieldInfo.FieldInfo.Name] = "[Unable to read]";
			}
		}

		return new StateSnapshot(component.GetType(), fieldValues);
	}

	/// <summary>
	/// Gets the changes between this snapshot and another snapshot.
	/// </summary>
	/// <param name="previous">The previous snapshot to compare with.</param>
	/// <returns>A collection of state changes.</returns>
	public IEnumerable<StateChange> GetChangesFrom(StateSnapshot? previous)
	{
		if (previous == null)
			return FieldValues.Select(kvp => new StateChange
			{
				FieldName = kvp.Key,
				PreviousValue = null,
				CurrentValue = kvp.Value,
				ChangeType = StateChangeType.Added,
			});

		if (ComponentType != previous.ComponentType)
			throw new ArgumentException("Cannot compare snapshots from different component types", nameof(previous));

		List<StateChange> changes = [];

		foreach (var kvp in FieldValues)
		{
			if (previous.FieldValues.TryGetValue(kvp.Key, out var previousValue))
			{
				if (!AreValuesEqual(kvp.Value, previousValue))
				{
					changes.Add(
						new StateChange
						{
							FieldName = kvp.Key,
							PreviousValue = previousValue,
							CurrentValue = kvp.Value,
							ChangeType = StateChangeType.Modified,
						}
					);
				}
			}
			else
			{
				// New field added in current snapshot
				changes.Add(
					new StateChange
					{
						FieldName = kvp.Key,
						PreviousValue = null,
						CurrentValue = kvp.Value,
						ChangeType = StateChangeType.Added,
					}
				);
			}
		}

		foreach (var kvp in previous.FieldValues)
			if (!FieldValues.ContainsKey(kvp.Key))
				changes.Add(
					new StateChange
					{
						FieldName = kvp.Key,
						PreviousValue = kvp.Value,
						CurrentValue = null,
						ChangeType = StateChangeType.Removed,
					}
				);

		return changes;
	}

	/// <summary>
	/// Gets the changes between this snapshot and a previous snapshot using
	/// field metadata and the <see cref="StateComparer"/> to ensure
	/// comparison semantics (including collection content tracking) are
	/// consistent with the overall state tracking system.
	/// </summary>
	/// <param name="previous">The previous snapshot to compare against.</param>
	/// <param name="metadata">The field metadata used for tracking.</param>
	/// <param name="comparer">The state comparer to use for equality checks.</param>
	/// <returns>An enumerable of state changes.</returns>
	public IEnumerable<StateChange> GetChangesFrom(StateSnapshot? previous, StateFieldMetadata metadata, StateComparer comparer)
	{
		if (metadata == null)
			throw new ArgumentNullException(nameof(metadata));
		if (comparer == null)
			throw new ArgumentNullException(nameof(comparer));

		// When there is no previous snapshot, treat all tracked fields as added.
		if (previous == null)
		{
			var addedChanges = new List<StateChange>();
			foreach (var fieldInfo in metadata.AllTrackedFields)
			{
				if (!FieldValues.TryGetValue(fieldInfo.FieldInfo.Name, out var currentValue))
					continue;

				addedChanges.Add(
					new StateChange
					{
						FieldName = fieldInfo.FieldInfo.Name,
						PreviousValue = null,
						CurrentValue = currentValue,
						ChangeType = StateChangeType.Added,
					}
				);
			}

			return addedChanges;
		}

		// Component type mismatch indicates an invalid comparison scenario.
		if (previous.ComponentType != ComponentType)
			throw new ArgumentException("Cannot compare snapshots from different component types.", nameof(previous));

		var changes = new List<StateChange>();

		foreach (var fieldInfo in metadata.AllTrackedFields)
		{
			var fieldName = fieldInfo.FieldInfo.Name;
			var hasCurrent = FieldValues.TryGetValue(fieldName, out var currentValue);
			var hasPrevious = previous.FieldValues.TryGetValue(fieldName, out var previousValue);

			if (hasCurrent && hasPrevious)
			{
				var areEqual = comparer.AreEqual(previousValue, currentValue, fieldInfo.FieldInfo.FieldType, fieldInfo);
				if (!areEqual)
				{
					changes.Add(
						new StateChange
						{
							FieldName = fieldName,
							PreviousValue = previousValue,
							CurrentValue = currentValue,
							ChangeType = StateChangeType.Modified,
						}
					);
				}
			}
			else if (hasCurrent)
			{
				// Newly tracked field.
				changes.Add(
					new StateChange
					{
						FieldName = fieldName,
						PreviousValue = null,
						CurrentValue = currentValue,
						ChangeType = StateChangeType.Added,
					}
				);
			}
			else if (hasPrevious)
			{
				// Field was present but is no longer tracked or available.
				changes.Add(
					new StateChange
					{
						FieldName = fieldName,
						PreviousValue = previousValue,
						CurrentValue = null,
						ChangeType = StateChangeType.Removed,
					}
				);
			}
		}

		return changes;
	}

	/// <summary>
	/// Gets the value of a specific field from this snapshot.
	/// </summary>
	/// <param name="fieldName">The name of the field.</param>
	/// <returns>The field value, or null if the field is not in this snapshot.</returns>
	public object? GetFieldValue(string fieldName) => FieldValues.TryGetValue(fieldName, out var value) ? value : null;

	/// <summary>
	/// Creates a deep copy of a collection to prevent reference sharing between snapshots.
	/// </summary>
	/// <param name="enumerable">The collection to copy.</param>
	/// <returns>A new collection containing copies of the original items.</returns>
	private static object CreateCollectionCopy(System.Collections.IEnumerable enumerable)
	{
		try
		{
			var items = enumerable.Cast<object?>().ToList();
			return new List<object?>(items);
		}
		catch (Exception)
		{
			// if copying fails, fallback to reference
			return enumerable;
		}
	}

	/// <summary>
	/// Compares two values for equality, handling null values appropriately.
	/// </summary>
	/// <param name="value1">The first value.</param>
	/// <param name="value2">The second value.</param>
	/// <returns>True if the values are equal.</returns>
	private static bool AreValuesEqual(object? value1, object? value2)
	{
		if (ReferenceEquals(value1, value2))
			return true;

		if (value1 == null || value2 == null)
			return false;

		return value1.Equals(value2);
	}
}
