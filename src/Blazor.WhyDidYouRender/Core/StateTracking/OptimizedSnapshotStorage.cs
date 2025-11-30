using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Blazor.WhyDidYouRender.Records;
using Microsoft.AspNetCore.Components;

namespace Blazor.WhyDidYouRender.Core.StateTracking;

/// <summary>
/// Optimized storage system for state snapshots that minimizes memory usage
/// and provides efficient access patterns for state tracking operations.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="OptimizedSnapshotStorage"/> class.
/// </remarks>
/// <param name="config">Storage configuration.</param>
public class OptimizedSnapshotStorage(StorageConfiguration? config = null)
{
	/// <summary>
	/// Storage for component snapshots using weak references to prevent memory leaks.
	/// </summary>
	private readonly ConcurrentDictionary<int, SnapshotEntry> _snapshots = new();

	/// <summary>
	/// Object pool for reusing snapshot objects.
	/// </summary>
	private readonly ObjectPool<Dictionary<string, object?>> _dictionaryPool = new();

	/// <summary>
	/// Configuration for storage optimization.
	/// </summary>
	private readonly StorageConfiguration _config = config ?? new StorageConfiguration();

	/// <summary>
	/// Statistics for storage operations.
	/// </summary>
	private readonly MutableStorageStatistics _statistics = new();

	/// <summary>
	/// Stores a snapshot for a component.
	/// </summary>
	/// <param name="component">The component.</param>
	/// <param name="snapshot">The snapshot to store.</param>
	public void StoreSnapshot(ComponentBase component, StateSnapshot snapshot)
	{
		if (component == null || snapshot == null)
			return;

		var componentHash = GetComponentHash(component);
		var optimizedSnapshot = OptimizeSnapshot(snapshot);

		var entry = new SnapshotEntry
		{
			ComponentReference = new WeakReference<ComponentBase>(component),
			Snapshot = optimizedSnapshot,
			StoredAt = DateTime.UtcNow,
			AccessCount = 0,
		};

		_snapshots.AddOrUpdate(
			componentHash,
			entry,
			(_, existing) =>
			{
				// Return the old snapshot to the pool
				ReturnSnapshotToPool(existing.Snapshot);
				return entry;
			}
		);

		_statistics.RecordStore();
	}

	/// <summary>
	/// Retrieves a snapshot for a component.
	/// </summary>
	/// <param name="component">The component.</param>
	/// <returns>The stored snapshot, or null if not found.</returns>
	public StateSnapshot? GetSnapshot(ComponentBase component)
	{
		if (component == null)
			return null;

		var componentHash = GetComponentHash(component);

		if (_snapshots.TryGetValue(componentHash, out var entry))
		{
			if (entry.ComponentReference.TryGetTarget(out var storedComponent) && ReferenceEquals(storedComponent, component))
			{
				entry.AccessCount++;
				entry.LastAccessTime = DateTime.UtcNow;
				_statistics.RecordHit();

				return RestoreSnapshot(entry.Snapshot, component.GetType());
			}
			else
			{
				_snapshots.TryRemove(componentHash, out _);
				ReturnSnapshotToPool(entry.Snapshot);
				_statistics.RecordMiss();
			}
		}
		else
			_statistics.RecordMiss();

		return null;
	}

	/// <summary>
	/// Removes a snapshot for a component.
	/// </summary>
	/// <param name="component">The component.</param>
	/// <returns>True if a snapshot was removed.</returns>
	public bool RemoveSnapshot(ComponentBase component)
	{
		if (component == null)
			return false;

		var componentHash = GetComponentHash(component);

		if (_snapshots.TryRemove(componentHash, out var entry))
		{
			ReturnSnapshotToPool(entry.Snapshot);
			_statistics.RecordRemoval();
			return true;
		}

		return false;
	}

	/// <summary>
	/// Performs cleanup of old and invalid snapshots.
	/// </summary>
	/// <returns>The number of snapshots cleaned up.</returns>
	public int PerformCleanup()
	{
		var cleanedUp = 0;
		var cutoffTime = DateTime.UtcNow.AddMinutes(-_config.MaxSnapshotAgeMinutes);
		var keysToRemove = new List<int>();

		foreach (var kvp in _snapshots)
		{
			var entry = kvp.Value;

			// remove if component has been garbage collected
			if (!entry.ComponentReference.TryGetTarget(out _))
			{
				keysToRemove.Add(kvp.Key);
				continue;
			}

			// remove if snapshot is too old
			if (entry.StoredAt < cutoffTime)
			{
				keysToRemove.Add(kvp.Key);
				continue;
			}

			// remove if not accessed recently and we're over capacity
			if (_snapshots.Count > _config.MaxSnapshots && entry.LastAccessTime < cutoffTime)
			{
				keysToRemove.Add(kvp.Key);
			}
		}

		// remove the identified entries
		foreach (var key in keysToRemove)
		{
			if (_snapshots.TryRemove(key, out var entry))
			{
				ReturnSnapshotToPool(entry.Snapshot);
				cleanedUp++;
			}
		}

		_statistics.RecordCleanup(cleanedUp);
		return cleanedUp;
	}

	/// <summary>
	/// Gets storage statistics.
	/// </summary>
	/// <returns>Storage performance statistics.</returns>
	public StorageStatistics GetStatistics() => new();

	/// <summary>
	/// Gets detailed storage information.
	/// </summary>
	/// <returns>Detailed storage information.</returns>
	public StorageInfo GetStorageInfo()
	{
		var activeSnapshots = 0;
		var totalMemoryEstimate = 0L;
		var componentTypes = new Dictionary<Type, int>();

		foreach (var entry in _snapshots.Values)
		{
			if (entry.ComponentReference.TryGetTarget(out var component))
			{
				activeSnapshots++;
				totalMemoryEstimate += EstimateSnapshotMemoryUsage(entry.Snapshot);

				var componentType = component.GetType();
				componentTypes[componentType] = componentTypes.GetValueOrDefault(componentType) + 1;
			}
		}

		return new StorageInfo
		{
			TotalSnapshots = _snapshots.Count,
			ActiveSnapshots = activeSnapshots,
			EstimatedMemoryUsage = totalMemoryEstimate,
			ComponentTypes = componentTypes,
			PooledDictionaries = _dictionaryPool.Count,
			Statistics = GetStatistics(),
		};
	}

	/// <summary>
	/// Clears all stored snapshots.
	/// </summary>
	public void Clear()
	{
		foreach (var entry in _snapshots.Values)
			ReturnSnapshotToPool(entry.Snapshot);

		_snapshots.Clear();
		_dictionaryPool.Clear();
		_statistics.RecordClear();
	}

	/// <summary>
	/// Gets a hash code for a component that's stable across renders.
	/// </summary>
	/// <param name="component">The component.</param>
	/// <returns>A hash code for the component.</returns>
	private static int GetComponentHash(ComponentBase component) => RuntimeHelpers.GetHashCode(component);

	/// <summary>
	/// Optimizes a snapshot for storage by using object pooling and compression.
	/// </summary>
	/// <param name="snapshot">The snapshot to optimize.</param>
	/// <returns>An optimized snapshot representation.</returns>
	private OptimizedSnapshot OptimizeSnapshot(StateSnapshot snapshot)
	{
		var dictionary = _dictionaryPool.Get();
		dictionary.Clear();

		// Copy field values, applying optimizations
		foreach (var kvp in snapshot.FieldValues)
		{
			var optimizedValue = OptimizeValue(kvp.Value);
			dictionary[kvp.Key] = optimizedValue;
		}

		return new OptimizedSnapshot
		{
			ComponentType = snapshot.ComponentType,
			FieldValues = dictionary,
			CapturedAt = snapshot.CapturedAt,
		};
	}

	/// <summary>
	/// Restores a snapshot from its optimized representation.
	/// </summary>
	/// <param name="optimizedSnapshot">The optimized snapshot.</param>
	/// <param name="componentType">The component type.</param>
	/// <returns>A restored state snapshot.</returns>
	private static StateSnapshot RestoreSnapshot(OptimizedSnapshot optimizedSnapshot, Type componentType)
	{
		var fieldValues = new Dictionary<string, object?>();

		if (optimizedSnapshot.FieldValues != null)
		{
			foreach (var kvp in optimizedSnapshot.FieldValues)
			{
				var restoredValue = RestoreValue(kvp.Value);
				fieldValues[kvp.Key] = restoredValue;
			}
		}

		return new StateSnapshot(componentType, fieldValues);
	}

	/// <summary>
	/// Optimizes a field value for storage by applying memory-efficient transformations.
	/// </summary>
	/// <param name="value">The value to optimize.</param>
	/// <returns>An optimized representation of the value.</returns>
	private static object? OptimizeValue(object? value)
	{
		if (value == null)
			return null;

		// for strings, intern common values to reduce memory usage
		if (value is string str)
		{
			// intern small strings that are likely to be repeated
			if (str.Length <= 50 && (str.All(char.IsLetterOrDigit) || str.All(char.IsWhiteSpace)))
				return string.Intern(str);
		}

		// for small collections, consider if they're worth optimizing
		if (value is System.Collections.ICollection collection && collection.Count == 0)
		{
			// return a shared empty collection instance for the type
			var collectionType = value.GetType();
			if (collectionType.IsGenericType)
			{
				var genericDef = collectionType.GetGenericTypeDefinition();
				if (genericDef == typeof(List<>))
				{
					// use Array.Empty<T> for empty lists
					var elementType = collectionType.GetGenericArguments()[0];
					var emptyArray = Array.CreateInstance(elementType, 0);
					return emptyArray;
				}
			}
		}

		// For now, return most values as-is
		// TODO: Consider additional optimizations like:
		// * Compress large strings
		// * Use flyweight pattern for common values
		// * Serialize complex objects to byte arrays
		return value;
	}

	/// <summary>
	/// Restores a value from its optimized representation.
	/// </summary>
	/// <param name="optimizedValue">The optimized value.</param>
	/// <returns>The restored value.</returns>
	private static object? RestoreValue(object? optimizedValue)
	{
		if (optimizedValue == null)
			return null;

		if (optimizedValue is Array array && array.Length == 0)
		{
			var elementType = array.GetType().GetElementType();
			if (elementType != null)
			{
				var listType = typeof(List<>).MakeGenericType(elementType);
				return Activator.CreateInstance(listType);
			}
		}

		return optimizedValue;
	}

	/// <summary>
	/// Returns an optimized snapshot to the object pool.
	/// </summary>
	/// <param name="optimizedSnapshot">The snapshot to return to the pool.</param>
	private void ReturnSnapshotToPool(OptimizedSnapshot optimizedSnapshot)
	{
		if (optimizedSnapshot.FieldValues != null)
			_dictionaryPool.Return(optimizedSnapshot.FieldValues);
	}

	/// <summary>
	/// Estimates the memory usage of an optimized snapshot.
	/// </summary>
	/// <param name="optimizedSnapshot">The snapshot to estimate.</param>
	/// <returns>Estimated memory usage in bytes.</returns>
	private static long EstimateSnapshotMemoryUsage(OptimizedSnapshot optimizedSnapshot)
	{
		const long baseSize = 128; // base object overhead
		const long fieldSize = 64; // estimated size per field

		return baseSize + (optimizedSnapshot.FieldValues?.Count ?? 0) * fieldSize;
	}
}
