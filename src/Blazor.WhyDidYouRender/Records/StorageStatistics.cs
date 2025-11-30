namespace Blazor.WhyDidYouRender.Records;

/// <summary>
/// Statistics for storage operations providing insights into storage performance and usage patterns.
/// </summary>
public record StorageStatistics
{
	/// <summary>
	/// Gets the total number of store operations performed.
	/// </summary>
	public long Stores { get; init; }

	/// <summary>
	/// Gets the total number of cache hits.
	/// </summary>
	public long Hits { get; init; }

	/// <summary>
	/// Gets the total number of cache misses.
	/// </summary>
	public long Misses { get; init; }

	/// <summary>
	/// Gets the total number of removal operations.
	/// </summary>
	public long Removals { get; init; }

	/// <summary>
	/// Gets the total number of cleanup operations performed.
	/// </summary>
	public long Cleanups { get; init; }

	/// <summary>
	/// Gets the total number of clear operations.
	/// </summary>
	public long Clears { get; init; }

	/// <summary>
	/// Gets the cache hit ratio as a percentage (0.0 to 1.0).
	/// </summary>
	public double HitRatio => Hits + Misses > 0 ? (double)Hits / (Hits + Misses) : 0.0;

	/// <summary>
	/// Gets the total number of access operations (hits + misses).
	/// </summary>
	public long TotalAccesses => Hits + Misses;

	/// <summary>
	/// Gets whether the storage has been actively used.
	/// </summary>
	public bool HasActivity => Stores > 0 || TotalAccesses > 0;
}
