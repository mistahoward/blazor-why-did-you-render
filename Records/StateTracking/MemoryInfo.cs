using System;
using System.Collections.Generic;

namespace Blazor.WhyDidYouRender.Records.StateTracking;

/// <summary>
/// Represents detailed memory information for state tracking operations.
/// This record provides comprehensive memory usage data and component distribution metrics.
/// </summary>
public record MemoryInfo
{
    /// <summary>
    /// Gets the number of active components currently being tracked.
    /// </summary>
    public int ActiveComponents { get; init; }

    /// <summary>
    /// Gets the total number of components registered for tracking (including inactive ones).
    /// </summary>
    public int TotalTrackedComponents { get; init; }

    /// <summary>
    /// Gets the estimated memory usage of tracked components in bytes.
    /// </summary>
    public long EstimatedMemoryUsage { get; init; }

    /// <summary>
    /// Gets the actual memory usage as reported by the garbage collector in bytes.
    /// </summary>
    public long ActualMemoryUsage { get; init; }

    /// <summary>
    /// Gets the distribution of components by their type.
    /// </summary>
    public IReadOnlyDictionary<Type, int> ComponentsByType { get; init; } = new Dictionary<Type, int>();

    /// <summary>
    /// Gets the memory usage statistics for this snapshot.
    /// </summary>
    public MemoryUsageStatistics Statistics { get; init; } = new();

    /// <summary>
    /// Gets the number of inactive (dead) component references.
    /// </summary>
    public int DeadReferences => TotalTrackedComponents - ActiveComponents;

    /// <summary>
    /// Gets the memory usage in megabytes.
    /// </summary>
    public double MemoryUsageMB => ActualMemoryUsage / (1024.0 * 1024.0);

    /// <summary>
    /// Gets the estimated memory usage in megabytes.
    /// </summary>
    public double EstimatedMemoryUsageMB => EstimatedMemoryUsage / (1024.0 * 1024.0);

    /// <summary>
    /// Gets the percentage of active components relative to total tracked components.
    /// </summary>
    public double ActiveComponentPercentage => TotalTrackedComponents > 0 
        ? (double)ActiveComponents / TotalTrackedComponents * 100 
        : 0;

    /// <summary>
    /// Gets the average estimated memory usage per active component in bytes.
    /// </summary>
    public double AverageMemoryPerComponent => ActiveComponents > 0 
        ? (double)EstimatedMemoryUsage / ActiveComponents 
        : 0;

    /// <summary>
    /// Gets a formatted summary of the memory information.
    /// </summary>
    /// <returns>A formatted string with memory information.</returns>
    public string GetFormattedSummary()
    {
        return $"Memory Information:\n" +
               $"  Active Components: {ActiveComponents:N0}\n" +
               $"  Total Tracked: {TotalTrackedComponents:N0}\n" +
               $"  Dead References: {DeadReferences:N0}\n" +
               $"  Active Percentage: {ActiveComponentPercentage:F1}%\n" +
               $"  Actual Memory: {MemoryUsageMB:F2} MB\n" +
               $"  Estimated Memory: {EstimatedMemoryUsageMB:F2} MB\n" +
               $"  Avg Memory/Component: {AverageMemoryPerComponent:N0} bytes\n" +
               $"  Component Types: {ComponentsByType.Count:N0}";
    }
}
