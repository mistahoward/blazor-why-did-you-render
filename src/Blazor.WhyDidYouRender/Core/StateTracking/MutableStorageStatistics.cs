using System.Threading;
using Blazor.WhyDidYouRender.Records;

namespace Blazor.WhyDidYouRender.Core.StateTracking;

/// <summary>
/// Mutable storage statistics for internal use by OptimizedSnapshotStorage.
/// Provides thread-safe operations for tracking storage performance metrics.
/// </summary>
internal class MutableStorageStatistics
{
	private long _stores = 0;
	private long _hits = 0;
	private long _misses = 0;
	private long _removals = 0;
	private long _cleanups = 0;
	private long _clears = 0;

	/// <summary>
	/// Gets the current number of store operations.
	/// </summary>
	public long Stores => _stores;

	/// <summary>
	/// Gets the current number of cache hits.
	/// </summary>
	public long Hits => _hits;

	/// <summary>
	/// Gets the current number of cache misses.
	/// </summary>
	public long Misses => _misses;

	/// <summary>
	/// Gets the current number of removal operations.
	/// </summary>
	public long Removals => _removals;

	/// <summary>
	/// Gets the current number of cleanup operations.
	/// </summary>
	public long Cleanups => _cleanups;

	/// <summary>
	/// Gets the current number of clear operations.
	/// </summary>
	public long Clears => _clears;

	/// <summary>
	/// Gets the current cache hit ratio.
	/// </summary>
	public double HitRatio => _hits + _misses > 0 ? (double)_hits / (_hits + _misses) : 0.0;

	/// <summary>
	/// Records a store operation.
	/// </summary>
	public void RecordStore() => Interlocked.Increment(ref _stores);

	/// <summary>
	/// Records a cache hit.
	/// </summary>
	public void RecordHit() => Interlocked.Increment(ref _hits);

	/// <summary>
	/// Records a cache miss.
	/// </summary>
	public void RecordMiss() => Interlocked.Increment(ref _misses);

	/// <summary>
	/// Records a removal operation.
	/// </summary>
	public void RecordRemoval() => Interlocked.Increment(ref _removals);

	/// <summary>
	/// Records cleanup operations.
	/// </summary>
	/// <param name="count">The number of items cleaned up.</param>
	public void RecordCleanup(int count) => Interlocked.Add(ref _cleanups, count);

	/// <summary>
	/// Records a clear operation.
	/// </summary>
	public void RecordClear() => Interlocked.Increment(ref _clears);

	/// <summary>
	/// Creates an immutable snapshot of the current statistics.
	/// </summary>
	/// <returns>An immutable StorageStatistics record.</returns>
	public StorageStatistics CreateSnapshot()
	{
		return new StorageStatistics
		{
			Stores = _stores,
			Hits = _hits,
			Misses = _misses,
			Removals = _removals,
			Cleanups = _cleanups,
			Clears = _clears,
		};
	}
}
