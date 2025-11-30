using System;

namespace Blazor.WhyDidYouRender.Records.StateTracking;

/// <summary>
/// Represents detailed information about the current state of the state tracking cache.
/// This record provides immutable snapshot data about cache performance and resource usage.
/// </summary>
/// <remarks>
/// CacheInfo provides comprehensive insights into cache behavior including entry counts,
/// memory usage estimates, access patterns, and performance statistics. This information
/// is essential for monitoring cache effectiveness and identifying optimization opportunities.
/// </remarks>
public record CacheInfo
{
	/// <summary>
	/// Gets the total number of entries currently stored in the cache.
	/// </summary>
	public int TotalEntries { get; init; }

	/// <summary>
	/// Gets the number of background loading tasks currently in progress.
	/// </summary>
	public int LoadingTasks { get; init; }

	/// <summary>
	/// Gets the estimated total memory usage of all cache entries in bytes.
	/// </summary>
	/// <remarks>
	/// This is an approximation based on object sizes and may not reflect actual memory usage.
	/// Use for relative comparisons and trend analysis rather than absolute memory tracking.
	/// </remarks>
	public long TotalMemoryEstimate { get; init; }

	/// <summary>
	/// Gets the timestamp of the oldest entry in the cache.
	/// </summary>
	public DateTime? OldestEntry { get; init; }

	/// <summary>
	/// Gets the timestamp of the newest entry in the cache.
	/// </summary>
	public DateTime? NewestEntry { get; init; }

	/// <summary>
	/// Gets the timestamp of the most recently accessed entry.
	/// </summary>
	public DateTime? MostRecentlyAccessed { get; init; }

	/// <summary>
	/// Gets the timestamp of the least recently accessed entry.
	/// </summary>
	public DateTime? LeastRecentlyAccessed { get; init; }

	/// <summary>
	/// Gets comprehensive cache performance statistics.
	/// </summary>
	public CacheStatistics Statistics { get; init; } = CacheStatistics.Empty();

	/// <summary>
	/// Gets the estimated memory usage in megabytes.
	/// </summary>
	public double MemoryUsageMB => TotalMemoryEstimate / (1024.0 * 1024.0);

	/// <summary>
	/// Gets the age of the cache (time since oldest entry).
	/// </summary>
	public TimeSpan CacheAge => OldestEntry.HasValue ? DateTime.UtcNow - OldestEntry.Value : TimeSpan.Zero;

	/// <summary>
	/// Gets the time since the last cache access.
	/// </summary>
	public TimeSpan TimeSinceLastAccess => MostRecentlyAccessed.HasValue ? DateTime.UtcNow - MostRecentlyAccessed.Value : TimeSpan.MaxValue;

	/// <summary>
	/// Gets whether the cache appears to be actively used.
	/// </summary>
	public bool IsActivelyUsed => TimeSinceLastAccess < TimeSpan.FromMinutes(5);

	/// <summary>
	/// Gets whether the cache has high memory usage.
	/// </summary>
	public bool HasHighMemoryUsage => MemoryUsageMB > 50; // More than 50MB

	/// <summary>
	/// Gets whether the cache has many stale entries.
	/// </summary>
	public bool HasStaleEntries => LeastRecentlyAccessed.HasValue && DateTime.UtcNow - LeastRecentlyAccessed.Value > TimeSpan.FromHours(1);

	/// <summary>
	/// Gets the cache efficiency rating based on hit rate and memory usage.
	/// </summary>
	public string EfficiencyRating =>
		Statistics.HitRate switch
		{
			> 0.9 when !HasHighMemoryUsage => "Excellent",
			> 0.8 when !HasHighMemoryUsage => "Good",
			> 0.6 => "Fair",
			> 0.3 => "Poor",
			_ => "Very Poor",
		};

	/// <summary>
	/// Creates empty cache information.
	/// </summary>
	/// <returns>Empty CacheInfo instance.</returns>
	public static CacheInfo Empty() => new();

	/// <summary>
	/// Creates cache information with basic metrics.
	/// </summary>
	/// <param name="totalEntries">Total number of cache entries.</param>
	/// <param name="loadingTasks">Number of loading tasks in progress.</param>
	/// <param name="memoryEstimate">Estimated memory usage in bytes.</param>
	/// <param name="statistics">Cache performance statistics.</param>
	/// <returns>A new CacheInfo instance.</returns>
	public static CacheInfo Create(int totalEntries, int loadingTasks = 0, long memoryEstimate = 0, CacheStatistics? statistics = null) =>
		new()
		{
			TotalEntries = totalEntries,
			LoadingTasks = loadingTasks,
			TotalMemoryEstimate = memoryEstimate,
			Statistics = statistics ?? CacheStatistics.Empty(),
		};

	/// <summary>
	/// Gets potential issues with the current cache state.
	/// </summary>
	/// <returns>A list of identified cache issues.</returns>
	public IReadOnlyList<string> GetPotentialIssues()
	{
		var issues = new List<string>();

		if (HasHighMemoryUsage)
			issues.Add($"High memory usage: {MemoryUsageMB:F1}MB");

		if (Statistics.HitRate < 0.5)
			issues.Add($"Low hit rate: {Statistics.HitRate:P1}");

		if (HasStaleEntries)
			issues.Add("Cache contains stale entries that haven't been accessed recently");

		if (LoadingTasks > TotalEntries / 2)
			issues.Add($"High number of loading tasks: {LoadingTasks} (entries: {TotalEntries})");

		if (!IsActivelyUsed && TotalEntries > 0)
			issues.Add($"Cache not actively used (last access: {TimeSinceLastAccess})");

		return issues;
	}

	/// <summary>
	/// Gets a formatted summary of the cache information.
	/// </summary>
	/// <returns>A formatted string with comprehensive cache information.</returns>
	public string GetFormattedSummary()
	{
		var summary =
			$"Cache Information (Efficiency: {EfficiencyRating}):\n"
			+ $"  Total Entries: {TotalEntries:N0}\n"
			+ $"  Loading Tasks: {LoadingTasks:N0}\n"
			+ $"  Memory Usage: {MemoryUsageMB:F2}MB\n"
			+ $"  Cache Age: {CacheAge}\n"
			+ $"  Last Access: {TimeSinceLastAccess}\n"
			+ $"  Hit Rate: {Statistics.HitRate:P2}\n"
			+ $"  Actively Used: {IsActivelyUsed}";

		if (OldestEntry.HasValue)
			summary += $"\n  Oldest Entry: {OldestEntry.Value:yyyy-MM-dd HH:mm:ss}";

		if (NewestEntry.HasValue)
			summary += $"\n  Newest Entry: {NewestEntry.Value:yyyy-MM-dd HH:mm:ss}";

		var issues = GetPotentialIssues();
		if (issues.Count > 0)
		{
			summary += $"\n  Issues ({issues.Count}):\n";
			foreach (var issue in issues)
			{
				summary += $"    • {issue}\n";
			}
		}

		return summary;
	}
}
