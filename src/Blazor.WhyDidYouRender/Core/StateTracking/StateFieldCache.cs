using System.Collections.Concurrent;

using Blazor.WhyDidYouRender.Records;
using Blazor.WhyDidYouRender.Records.StateTracking;

namespace Blazor.WhyDidYouRender.Core.StateTracking;

/// <summary>
/// High-performance cache for state field metadata with advanced features like
/// lazy loading, cache invalidation, and memory management.
/// </summary>
public class StateFieldCache {
	/// <summary>
	/// Cache entries with metadata and access tracking.
	/// </summary>
	private readonly ConcurrentDictionary<Type, CacheEntry> _cache = new();

	/// <summary>
	/// Lazy loading tasks for components currently being analyzed.
	/// </summary>
	private readonly ConcurrentDictionary<Type, Lazy<Task<StateFieldMetadata>>> _loadingTasks = new();

	/// <summary>
	/// Statistics for cache performance monitoring.
	/// </summary>
	private readonly CacheStatistics _statistics = new();

	/// <summary>
	/// Configuration for cache behavior.
	/// </summary>
	private readonly CacheConfiguration _config;

	/// <summary>
	/// Timer for periodic cache maintenance.
	/// </summary>
	private readonly Timer _maintenanceTimer;

	/// <summary>
	/// Initializes a new instance of the <see cref="StateFieldCache"/> class.
	/// </summary>
	/// <param name="config">Cache configuration options.</param>
	public StateFieldCache(CacheConfiguration? config = null) {
		_config = config ?? new CacheConfiguration();

		// Start maintenance timer
		_maintenanceTimer = new Timer(
			PerformMaintenance,
			null,
			TimeSpan.FromMinutes(_config.MaintenanceIntervalMinutes),
			TimeSpan.FromMinutes(_config.MaintenanceIntervalMinutes));
	}

	/// <summary>
	/// Gets metadata for a component type, using cache or loading if necessary.
	/// </summary>
	/// <param name="componentType">The component type.</param>
	/// <param name="metadataFactory">Factory function to create metadata if not cached.</param>
	/// <returns>The state field metadata.</returns>
	public async Task<StateFieldMetadata> GetOrCreateAsync(
		Type componentType,
		Func<Type, StateFieldMetadata> metadataFactory) {
		ArgumentNullException.ThrowIfNull(componentType);
		ArgumentNullException.ThrowIfNull(metadataFactory);

		if (_cache.TryGetValue(componentType, out var cacheEntry)) {
			cacheEntry.UpdateAccessTime();
			_statistics.RecordHit();
			return cacheEntry.Metadata;
		}
		var lazyTask = _loadingTasks.GetOrAdd(componentType, _ => new Lazy<Task<StateFieldMetadata>>(
			() => Task.Run(() => CreateAndCacheMetadata(componentType, metadataFactory))));

		try {
			var metadata = await lazyTask.Value;
			_statistics.RecordMiss();
			return metadata;
		}
		finally {
			_loadingTasks.TryRemove(componentType, out _);
		}
	}

	/// <summary>
	/// Gets metadata synchronously if available in cache, otherwise returns null.
	/// </summary>
	/// <param name="componentType">The component type.</param>
	/// <returns>The cached metadata or null if not available.</returns>
	public StateFieldMetadata? GetIfCached(Type componentType) {
		if (_cache.TryGetValue(componentType, out var cacheEntry)) {
			cacheEntry.UpdateAccessTime();
			_statistics.RecordHit();
			return cacheEntry.Metadata;
		}

		return null;
	}

	/// <summary>
	/// Invalidates cache entries for specific types or all entries.
	/// </summary>
	/// <param name="componentTypes">Types to invalidate, or null to invalidate all.</param>
	public void Invalidate(IEnumerable<Type>? componentTypes = null) {
		if (componentTypes == null) {
			var removedCount = _cache.Count;
			_cache.Clear();
			_loadingTasks.Clear();
			_statistics.RecordInvalidation(removedCount);
		}
		else {
			var removedCount = 0;
			foreach (var type in componentTypes) {
				if (_cache.TryRemove(type, out _))
					removedCount++;
				_loadingTasks.TryRemove(type, out _);
			}
			_statistics.RecordInvalidation(removedCount);
		}
	}

	/// <summary>
	/// Gets current cache statistics.
	/// </summary>
	/// <returns>Cache performance statistics.</returns>
	public CacheStatistics GetStatistics() =>
		_statistics.CreateSnapshot();

	/// <summary>
	/// Gets detailed cache information for diagnostics.
	/// </summary>
	/// <returns>Detailed cache information.</returns>
	public CacheInfo GetCacheInfo() {
		var entries = _cache.Values.ToList();

		return new CacheInfo {
			TotalEntries = entries.Count,
			LoadingTasks = _loadingTasks.Count,
			TotalMemoryEstimate = entries.Sum(e => e.EstimatedMemoryUsage),
			OldestEntry = entries.MinBy(e => e.CreatedAt)?.CreatedAt,
			NewestEntry = entries.MaxBy(e => e.CreatedAt)?.CreatedAt,
			MostRecentlyAccessed = entries.MaxBy(e => e.LastAccessTime)?.LastAccessTime,
			LeastRecentlyAccessed = entries.MinBy(e => e.LastAccessTime)?.LastAccessTime,
			Statistics = GetStatistics()
		};
	}

	/// <summary>
	/// Performs cache maintenance including cleanup of old entries.
	/// </summary>
	public void PerformMaintenance() {
		var cutoffTime = DateTime.UtcNow.AddMinutes(-_config.MaxEntryAgeMinutes);
		var maxEntries = _config.MaxCacheSize;

		var entriesToRemove = new List<Type>();
		var allEntries = _cache.ToList();

		foreach (var kvp in allEntries)
			if (kvp.Value.CreatedAt < cutoffTime)
				entriesToRemove.Add(kvp.Key);


		bool overLimitAfterCleanup = allEntries.Count - entriesToRemove.Count > maxEntries;
		if (overLimitAfterCleanup) {
			var remainingEntries = allEntries
				.Where(kvp => !entriesToRemove.Contains(kvp.Key))
				.OrderBy(kvp => kvp.Value.LastAccessTime)
				.Take(allEntries.Count - entriesToRemove.Count - maxEntries)
				.Select(kvp => kvp.Key);

			entriesToRemove.AddRange(remainingEntries);
		}

		foreach (var type in entriesToRemove)
			_cache.TryRemove(type, out _);

		if (entriesToRemove.Count > 0)
			_statistics.RecordMaintenance(entriesToRemove.Count);
	}

	/// <summary>
	/// Creates metadata and adds it to the cache.
	/// </summary>
	/// <param name="componentType">The component type.</param>
	/// <param name="metadataFactory">Factory function to create metadata.</param>
	/// <returns>The created metadata.</returns>
	private StateFieldMetadata CreateAndCacheMetadata(Type componentType, Func<Type, StateFieldMetadata> metadataFactory) {
		var metadata = metadataFactory(componentType);
		var cacheEntry = new CacheEntry(metadata);

		_cache.TryAdd(componentType, cacheEntry);

		return metadata;
	}

	/// <summary>
	/// Performs maintenance as a timer callback.
	/// </summary>
	/// <param name="state">Timer state (unused).</param>
	private void PerformMaintenance(object? state) {
		try {
			PerformMaintenance();
		}
		catch (Exception ex) {
			// record maintenance errors but don't let them stop the timer
			_statistics.RecordMaintenanceError();

			// for now, we'll track it in statistics but continue operation
			// to prevent the maintenance timer from stopping

			// maybe add logging later?
			// _logger?.LogWarning(ex, "Cache maintenance failed but will continue");

			_ = ex; // acknowledge we're intentionally ignoring the exception to keep the timer running
		}
	}

	/// <summary>
	/// Disposes the cache and stops maintenance.
	/// </summary>
	public void Dispose() {
		_maintenanceTimer?.Dispose();
		_cache.Clear();
		_loadingTasks.Clear();
	}
}




