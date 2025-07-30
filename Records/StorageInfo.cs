using System;
using System.Collections.Generic;

namespace Blazor.WhyDidYouRender.Records;

/// <summary>
/// Detailed storage information providing comprehensive insights into storage state and performance.
/// </summary>
public record StorageInfo
{
    /// <summary>
    /// Gets the total number of snapshots stored (including inactive ones).
    /// </summary>
    public required int TotalSnapshots { get; init; }

    /// <summary>
    /// Gets the number of snapshots with valid component references.
    /// </summary>
    public required int ActiveSnapshots { get; init; }

    /// <summary>
    /// Gets the estimated memory usage in bytes.
    /// </summary>
    public required long EstimatedMemoryUsage { get; init; }

    /// <summary>
    /// Gets the breakdown of snapshots by component type.
    /// </summary>
    public required Dictionary<Type, int> ComponentTypes { get; init; }

    /// <summary>
    /// Gets the number of dictionaries available in the object pool.
    /// </summary>
    public required int PooledDictionaries { get; init; }

    /// <summary>
    /// Gets the storage performance statistics.
    /// </summary>
    public required StorageStatistics Statistics { get; init; }

    /// <summary>
    /// Gets the percentage of snapshots that are still active.
    /// </summary>
    public double ActiveSnapshotRatio => TotalSnapshots > 0 ? (double)ActiveSnapshots / TotalSnapshots : 0.0;

    /// <summary>
    /// Gets the estimated memory usage per snapshot in bytes.
    /// </summary>
    public double AverageMemoryPerSnapshot => ActiveSnapshots > 0 ? (double)EstimatedMemoryUsage / ActiveSnapshots : 0.0;

    /// <summary>
    /// Gets whether the storage is approaching capacity limits.
    /// </summary>
    public bool IsNearCapacity => TotalSnapshots > 800; // Assuming max of 1000

    /// <summary>
    /// Gets a formatted summary of the storage information.
    /// </summary>
    /// <returns>A human-readable summary of storage state.</returns>
    public string GetFormattedSummary()
    {
        var lines = new List<string>
        {
            $"Storage Summary:",
            $"  Total Snapshots: {TotalSnapshots}",
            $"  Active Snapshots: {ActiveSnapshots} ({ActiveSnapshotRatio:P1})",
            $"  Estimated Memory: {EstimatedMemoryUsage:N0} bytes",
            $"  Average per Snapshot: {AverageMemoryPerSnapshot:F0} bytes",
            $"  Pooled Dictionaries: {PooledDictionaries}",
            $"  Hit Ratio: {Statistics.HitRatio:P2}",
            $"  Component Types: {ComponentTypes.Count}"
        };

        if (IsNearCapacity)
        {
            lines.Add("  ⚠️  Warning: Approaching capacity limits");
        }

        return string.Join("\n", lines);
    }
}
