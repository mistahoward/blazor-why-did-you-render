using System;
using System.Collections.Generic;
using System.Threading;

namespace Blazor.WhyDidYouRender.Records.StateTracking;

/// <summary>
/// Represents comprehensive statistics for thread-safe state tracking operations.
/// This record provides immutable performance and operational metrics for threading analysis.
/// </summary>
/// <remarks>
/// ThreadingStatistics tracks various metrics related to thread-safe operations including
/// operation counts, success rates, timing information, and cleanup statistics. This data
/// is essential for monitoring threading performance and identifying potential issues.
/// </remarks>
public record ThreadingStatistics {
    /// <summary>
    /// Gets the total number of operations performed.
    /// </summary>
    public long TotalOperations { get; init; }

    /// <summary>
    /// Gets the number of successful operations.
    /// </summary>
    public long SuccessfulOperations { get; init; }

    /// <summary>
    /// Gets the number of failed operations.
    /// </summary>
    public long FailedOperations { get; init; }

    /// <summary>
    /// Gets the number of cleanup operations performed.
    /// </summary>
    public long CleanupOperations { get; init; }

    /// <summary>
    /// Gets the number of bulk cleanup operations performed.
    /// </summary>
    public long BulkCleanupOperations { get; init; }

    /// <summary>
    /// Gets the total time spent in operations (in ticks).
    /// </summary>
    public long TotalOperationTimeTicks { get; init; }

    /// <summary>
    /// Gets the time when statistics collection started.
    /// </summary>
    public DateTime StartTime { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the time when these statistics were captured.
    /// </summary>
    public DateTime CapturedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the failure rate as a percentage (0.0 to 1.0).
    /// </summary>
    public double FailureRate => TotalOperations > 0 ? (double)FailedOperations / TotalOperations : 0.0;

    /// <summary>
    /// Gets the success rate as a percentage (0.0 to 1.0).
    /// </summary>
    public double SuccessRate => TotalOperations > 0 ? (double)SuccessfulOperations / TotalOperations : 0.0;

    /// <summary>
    /// Gets the total operation time as a TimeSpan.
    /// </summary>
    public TimeSpan TotalOperationTime => TimeSpan.FromTicks(TotalOperationTimeTicks);

    /// <summary>
    /// Gets the average operation time for successful operations.
    /// </summary>
    public TimeSpan AverageOperationTime => SuccessfulOperations > 0
        ? TimeSpan.FromTicks(TotalOperationTimeTicks / SuccessfulOperations)
        : TimeSpan.Zero;

    /// <summary>
    /// Gets the duration of statistics collection.
    /// </summary>
    public TimeSpan CollectionDuration => CapturedAt - StartTime;

    /// <summary>
    /// Gets the average operations per second during the collection period.
    /// </summary>
    public double OperationsPerSecond => CollectionDuration.TotalSeconds > 0
        ? TotalOperations / CollectionDuration.TotalSeconds
        : 0.0;

    /// <summary>
    /// Gets the ratio of cleanup operations to total operations.
    /// </summary>
    public double CleanupRatio => TotalOperations > 0
        ? (double)(CleanupOperations + BulkCleanupOperations) / TotalOperations
        : 0.0;

    /// <summary>
    /// Gets whether the threading performance is considered healthy.
    /// </summary>
    public bool IsHealthy => FailureRate <= 0.05 && // Less than 5% failure rate
                            AverageOperationTime <= TimeSpan.FromMilliseconds(100); // Less than 100ms average

    /// <summary>
    /// Gets whether there are performance concerns.
    /// </summary>
    public bool HasPerformanceConcerns => FailureRate > 0.10 || // More than 10% failure rate
                                         AverageOperationTime > TimeSpan.FromMilliseconds(500); // More than 500ms average

    /// <summary>
    /// Gets a performance rating based on current metrics.
    /// </summary>
    public string PerformanceRating => FailureRate switch {
        <= 0.01 when AverageOperationTime <= TimeSpan.FromMilliseconds(50) => "Excellent",
        <= 0.05 when AverageOperationTime <= TimeSpan.FromMilliseconds(100) => "Good",
        <= 0.10 when AverageOperationTime <= TimeSpan.FromMilliseconds(250) => "Fair",
        <= 0.20 when AverageOperationTime <= TimeSpan.FromMilliseconds(500) => "Poor",
        _ => "Critical"
    };

    /// <summary>
    /// Creates empty threading statistics.
    /// </summary>
    /// <returns>Empty ThreadingStatistics instance.</returns>
    public static ThreadingStatistics Empty() => new();

    /// <summary>
    /// Creates threading statistics from raw values.
    /// </summary>
    /// <param name="totalOperations">Total number of operations.</param>
    /// <param name="successfulOperations">Number of successful operations.</param>
    /// <param name="failedOperations">Number of failed operations.</param>
    /// <param name="cleanupOperations">Number of cleanup operations.</param>
    /// <param name="bulkCleanupOperations">Number of bulk cleanup operations.</param>
    /// <param name="totalOperationTimeTicks">Total operation time in ticks.</param>
    /// <param name="startTime">When statistics collection started.</param>
    /// <returns>A new ThreadingStatistics instance.</returns>
    public static ThreadingStatistics Create(
        long totalOperations,
        long successfulOperations,
        long failedOperations,
        long cleanupOperations,
        long bulkCleanupOperations,
        long totalOperationTimeTicks,
        DateTime? startTime = null) => new() {
            TotalOperations = totalOperations,
            SuccessfulOperations = successfulOperations,
            FailedOperations = failedOperations,
            CleanupOperations = cleanupOperations,
            BulkCleanupOperations = bulkCleanupOperations,
            TotalOperationTimeTicks = totalOperationTimeTicks,
            StartTime = startTime ?? DateTime.UtcNow
        };

    /// <summary>
    /// Combines this statistics instance with another.
    /// </summary>
    /// <param name="other">The other statistics to combine with.</param>
    /// <returns>A new ThreadingStatistics with combined values.</returns>
    public ThreadingStatistics Combine(ThreadingStatistics other) => new() {
        TotalOperations = TotalOperations + other.TotalOperations,
        SuccessfulOperations = SuccessfulOperations + other.SuccessfulOperations,
        FailedOperations = FailedOperations + other.FailedOperations,
        CleanupOperations = CleanupOperations + other.CleanupOperations,
        BulkCleanupOperations = BulkCleanupOperations + other.BulkCleanupOperations,
        TotalOperationTimeTicks = TotalOperationTimeTicks + other.TotalOperationTimeTicks,
        StartTime = StartTime < other.StartTime ? StartTime : other.StartTime,
        CapturedAt = CapturedAt > other.CapturedAt ? CapturedAt : other.CapturedAt
    };

    /// <summary>
    /// Gets a formatted summary of the threading statistics.
    /// </summary>
    /// <returns>A formatted string with comprehensive statistics information.</returns>
    public string GetFormattedSummary() {
        return $"Threading Statistics (Performance: {PerformanceRating}):\n" +
               $"  Collection Duration: {CollectionDuration}\n" +
               $"  Total Operations: {TotalOperations:N0}\n" +
               $"  Success Rate: {SuccessRate:P2}\n" +
               $"  Failure Rate: {FailureRate:P2}\n" +
               $"  Average Operation Time: {AverageOperationTime.TotalMilliseconds:F2}ms\n" +
               $"  Operations/Second: {OperationsPerSecond:F2}\n" +
               $"  Cleanup Operations: {CleanupOperations:N0}\n" +
               $"  Bulk Cleanups: {BulkCleanupOperations:N0}\n" +
               $"  Cleanup Ratio: {CleanupRatio:P2}\n" +
               $"  Health Status: {(IsHealthy ? "Healthy" : HasPerformanceConcerns ? "Concerning" : "Degraded")}";
    }

    /// <summary>
    /// Gets key performance indicators as a dictionary.
    /// </summary>
    /// <returns>A dictionary of key performance metrics.</returns>
    public Dictionary<string, object> GetKPIs() => new() {
        ["TotalOperations"] = TotalOperations,
        ["SuccessRate"] = SuccessRate,
        ["FailureRate"] = FailureRate,
        ["AverageOperationTimeMs"] = AverageOperationTime.TotalMilliseconds,
        ["OperationsPerSecond"] = OperationsPerSecond,
        ["CleanupRatio"] = CleanupRatio,
        ["PerformanceRating"] = PerformanceRating,
        ["IsHealthy"] = IsHealthy,
        ["CollectionDurationMinutes"] = CollectionDuration.TotalMinutes
    };
}
