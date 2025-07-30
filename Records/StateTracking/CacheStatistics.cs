using System;
using System.Collections.Generic;

namespace Blazor.WhyDidYouRender.Records.StateTracking;

/// <summary>
/// Represents comprehensive statistics for cache performance monitoring and analysis.
/// This class provides mutable performance metrics for cache hit rates, maintenance operations, and error tracking.
/// </summary>
/// <remarks>
/// CacheStatistics tracks various metrics related to cache performance including hit/miss ratios,
/// invalidation patterns, maintenance operations, and error rates. This data is essential for
/// optimizing cache configuration and identifying performance bottlenecks.
/// </remarks>
public class CacheStatistics {
    /// <summary>
    /// Gets the total number of cache hits.
    /// </summary>
    public long Hits { get; set; }

    /// <summary>
    /// Gets the total number of cache misses.
    /// </summary>
    public long Misses { get; set; }

    /// <summary>
    /// Gets the total number of cache invalidations.
    /// </summary>
    public long Invalidations { get; set; }

    /// <summary>
    /// Gets the total number of maintenance operations performed.
    /// </summary>
    public long MaintenanceOperations { get; set; }

    /// <summary>
    /// Gets the total number of entries removed during maintenance operations.
    /// </summary>
    public long EntriesRemovedByMaintenance { get; set; }

    /// <summary>
    /// Gets the total number of errors that occurred during maintenance operations.
    /// </summary>
    public long MaintenanceErrors { get; set; }

    /// <summary>
    /// Gets the time when these statistics were captured.
    /// </summary>
    public DateTime CapturedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the total number of cache operations (hits + misses).
    /// </summary>
    public long TotalOperations => Hits + Misses;

    /// <summary>
    /// Gets the cache hit rate as a percentage (0.0 to 1.0).
    /// </summary>
    public double HitRate => TotalOperations > 0 ? (double)Hits / TotalOperations : 0.0;

    /// <summary>
    /// Gets the cache miss rate as a percentage (0.0 to 1.0).
    /// </summary>
    public double MissRate => TotalOperations > 0 ? (double)Misses / TotalOperations : 0.0;

    /// <summary>
    /// Gets the invalidation rate relative to total operations.
    /// </summary>
    public double InvalidationRate => TotalOperations > 0 ? (double)Invalidations / TotalOperations : 0.0;

    /// <summary>
    /// Gets the maintenance error rate relative to maintenance operations.
    /// </summary>
    public double MaintenanceErrorRate => MaintenanceOperations > 0
        ? (double)MaintenanceErrors / MaintenanceOperations
        : 0.0;

    /// <summary>
    /// Gets the average entries removed per maintenance operation.
    /// </summary>
    public double AverageEntriesRemovedPerMaintenance => MaintenanceOperations > 0
        ? (double)EntriesRemovedByMaintenance / MaintenanceOperations
        : 0.0;

    /// <summary>
    /// Gets whether the cache performance is considered good.
    /// </summary>
    public bool IsPerformingWell => HitRate >= 0.8 && MaintenanceErrorRate <= 0.05;

    /// <summary>
    /// Gets whether the cache has performance issues.
    /// </summary>
    public bool HasPerformanceIssues => HitRate < 0.5 || MaintenanceErrorRate > 0.10;

    /// <summary>
    /// Gets the cache performance rating.
    /// </summary>
    public string PerformanceRating => HitRate switch {
        >= 0.95 => "Excellent",
        >= 0.85 => "Good",
        >= 0.70 => "Fair",
        >= 0.50 => "Poor",
        _ => "Very Poor"
    };

    /// <summary>
    /// Gets whether maintenance operations are effective.
    /// </summary>
    public bool IsMaintenanceEffective => MaintenanceOperations > 0 &&
                                         AverageEntriesRemovedPerMaintenance > 1.0 &&
                                         MaintenanceErrorRate < 0.05;

    /// <summary>
    /// Creates empty cache statistics.
    /// </summary>
    /// <returns>Empty CacheStatistics instance.</returns>
    public static CacheStatistics Empty() => new();

    /// <summary>
    /// Creates cache statistics from raw values.
    /// </summary>
    /// <param name="hits">Number of cache hits.</param>
    /// <param name="misses">Number of cache misses.</param>
    /// <param name="invalidations">Number of invalidations.</param>
    /// <param name="maintenanceOperations">Number of maintenance operations.</param>
    /// <param name="entriesRemovedByMaintenance">Entries removed during maintenance.</param>
    /// <param name="maintenanceErrors">Number of maintenance errors.</param>
    /// <returns>A new CacheStatistics instance.</returns>
    public static CacheStatistics Create(
        long hits,
        long misses,
        long invalidations = 0,
        long maintenanceOperations = 0,
        long entriesRemovedByMaintenance = 0,
        long maintenanceErrors = 0) => new() {
            Hits = hits,
            Misses = misses,
            Invalidations = invalidations,
            MaintenanceOperations = maintenanceOperations,
            EntriesRemovedByMaintenance = entriesRemovedByMaintenance,
            MaintenanceErrors = maintenanceErrors
        };

    /// <summary>
    /// Combines this statistics instance with another.
    /// </summary>
    /// <param name="other">The other statistics to combine with.</param>
    /// <returns>A new CacheStatistics with combined values.</returns>
    public CacheStatistics Combine(CacheStatistics other) => new() {
        Hits = Hits + other.Hits,
        Misses = Misses + other.Misses,
        Invalidations = Invalidations + other.Invalidations,
        MaintenanceOperations = MaintenanceOperations + other.MaintenanceOperations,
        EntriesRemovedByMaintenance = EntriesRemovedByMaintenance + other.EntriesRemovedByMaintenance,
        MaintenanceErrors = MaintenanceErrors + other.MaintenanceErrors,
        CapturedAt = CapturedAt > other.CapturedAt ? CapturedAt : other.CapturedAt
    };

    /// <summary>
    /// Gets potential issues with cache performance.
    /// </summary>
    /// <returns>A list of identified performance issues.</returns>
    public IReadOnlyList<string> GetPerformanceIssues() {
        var issues = new List<string>();

        if (HitRate < 0.5)
            issues.Add($"Low hit rate: {HitRate:P1}");

        if (InvalidationRate > 0.2)
            issues.Add($"High invalidation rate: {InvalidationRate:P1}");

        if (MaintenanceErrorRate > 0.05)
            issues.Add($"High maintenance error rate: {MaintenanceErrorRate:P1}");

        if (MaintenanceOperations > 0 && AverageEntriesRemovedPerMaintenance < 0.5)
            issues.Add($"Ineffective maintenance: {AverageEntriesRemovedPerMaintenance:F1} entries/operation");

        if (TotalOperations == 0)
            issues.Add("No cache operations recorded");

        return issues;
    }

    /// <summary>
    /// Gets a formatted summary of the cache statistics.
    /// </summary>
    /// <returns>A formatted string with comprehensive cache statistics.</returns>
    public string GetFormattedSummary() {
        var summary = $"Cache Statistics (Performance: {PerformanceRating}):\n" +
                     $"  Total Operations: {TotalOperations:N0}\n" +
                     $"  Hit Rate: {HitRate:P2}\n" +
                     $"  Miss Rate: {MissRate:P2}\n" +
                     $"  Invalidations: {Invalidations:N0} ({InvalidationRate:P2})\n" +
                     $"  Maintenance Operations: {MaintenanceOperations:N0}\n" +
                     $"  Entries Removed: {EntriesRemovedByMaintenance:N0}\n" +
                     $"  Maintenance Errors: {MaintenanceErrors:N0} ({MaintenanceErrorRate:P2})\n" +
                     $"  Avg Removed/Maintenance: {AverageEntriesRemovedPerMaintenance:F1}\n" +
                     $"  Captured At: {CapturedAt:yyyy-MM-dd HH:mm:ss}";

        var issues = GetPerformanceIssues();
        if (issues.Count > 0) {
            summary += $"\n  Issues ({issues.Count}):\n";
            foreach (var issue in issues) {
                summary += $"    • {issue}\n";
            }
        }

        return summary;
    }

    /// <summary>
    /// Gets key performance indicators as a dictionary.
    /// </summary>
    /// <returns>A dictionary of key performance metrics.</returns>
    public Dictionary<string, object> GetKPIs() => new() {
        ["TotalOperations"] = TotalOperations,
        ["HitRate"] = HitRate,
        ["MissRate"] = MissRate,
        ["InvalidationRate"] = InvalidationRate,
        ["MaintenanceErrorRate"] = MaintenanceErrorRate,
        ["PerformanceRating"] = PerformanceRating,
        ["IsPerformingWell"] = IsPerformingWell,
        ["IsMaintenanceEffective"] = IsMaintenanceEffective
    };

    /// <summary>
    /// Records a cache hit.
    /// </summary>
    public void RecordHit() => Hits++;

    /// <summary>
    /// Records a cache miss.
    /// </summary>
    public void RecordMiss() => Misses++;

    /// <summary>
    /// Records a cache invalidation.
    /// </summary>
    public void RecordInvalidation() => Invalidations++;

    /// <summary>
    /// Records multiple cache invalidations.
    /// </summary>
    /// <param name="count">The number of invalidations.</param>
    public void RecordInvalidation(int count) => Invalidations += count;

    /// <summary>
    /// Records a maintenance operation.
    /// </summary>
    /// <param name="removedEntries">The number of entries removed.</param>
    public void RecordMaintenance(int removedEntries) {
        MaintenanceOperations++;
        EntriesRemovedByMaintenance += removedEntries;
    }

    /// <summary>
    /// Records a maintenance error.
    /// </summary>
    public void RecordMaintenanceError() => MaintenanceErrors++;

    /// <summary>
    /// Creates a snapshot of the current statistics.
    /// </summary>
    /// <returns>A new CacheStatistics instance with current values.</returns>
    public CacheStatistics CreateSnapshot() => new() {
        Hits = Hits,
        Misses = Misses,
        Invalidations = Invalidations,
        MaintenanceOperations = MaintenanceOperations,
        EntriesRemovedByMaintenance = EntriesRemovedByMaintenance,
        MaintenanceErrors = MaintenanceErrors,
        CapturedAt = DateTime.UtcNow
    };

    /// <summary>
    /// Gets the hit ratio (same as HitRate for compatibility).
    /// </summary>
    public double HitRatio => HitRate;
}
