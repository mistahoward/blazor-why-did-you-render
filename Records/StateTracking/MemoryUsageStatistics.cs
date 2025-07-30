using System;

namespace Blazor.WhyDidYouRender.Records.StateTracking;

/// <summary>
/// Represents immutable statistics about memory usage in state tracking operations.
/// This record provides comprehensive metrics for memory management analysis.
/// </summary>
public record MemoryUsageStatistics
{
    /// <summary>
    /// Gets the total number of components registered for tracking.
    /// </summary>
    public long ComponentsRegistered { get; init; }

    /// <summary>
    /// Gets the total number of components unregistered from tracking.
    /// </summary>
    public long ComponentsUnregistered { get; init; }

    /// <summary>
    /// Gets the total number of components cleaned up during maintenance operations.
    /// </summary>
    public long ComponentsCleanedUp { get; init; }

    /// <summary>
    /// Gets the total number of cleanup operations performed.
    /// </summary>
    public long CleanupOperations { get; init; }

    /// <summary>
    /// Gets the total number of garbage collection operations triggered.
    /// </summary>
    public long GarbageCollections { get; init; }

    /// <summary>
    /// Gets the total amount of memory freed through cleanup operations (in bytes).
    /// </summary>
    public long TotalMemoryFreed { get; init; }

    /// <summary>
    /// Gets the net number of components currently being tracked.
    /// </summary>
    public long ActiveComponents => ComponentsRegistered - ComponentsUnregistered - ComponentsCleanedUp;

    /// <summary>
    /// Gets the cleanup efficiency as a percentage.
    /// </summary>
    public double CleanupEfficiency => CleanupOperations > 0 
        ? (double)ComponentsCleanedUp / CleanupOperations * 100 
        : 0;

    /// <summary>
    /// Gets the average memory freed per garbage collection (in bytes).
    /// </summary>
    public double AverageMemoryFreedPerGC => GarbageCollections > 0 
        ? (double)TotalMemoryFreed / GarbageCollections 
        : 0;

    /// <summary>
    /// Gets a formatted summary of the memory usage statistics.
    /// </summary>
    /// <returns>A formatted string with memory usage information.</returns>
    public string GetFormattedSummary()
    {
        return $"Memory Usage Statistics:\n" +
               $"  Components Registered: {ComponentsRegistered:N0}\n" +
               $"  Components Unregistered: {ComponentsUnregistered:N0}\n" +
               $"  Components Cleaned Up: {ComponentsCleanedUp:N0}\n" +
               $"  Active Components: {ActiveComponents:N0}\n" +
               $"  Cleanup Operations: {CleanupOperations:N0}\n" +
               $"  Garbage Collections: {GarbageCollections:N0}\n" +
               $"  Total Memory Freed: {TotalMemoryFreed:N0} bytes\n" +
               $"  Cleanup Efficiency: {CleanupEfficiency:F1}%\n" +
               $"  Avg Memory/GC: {AverageMemoryFreedPerGC:N0} bytes";
    }
}
