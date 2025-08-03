using System;

namespace Blazor.WhyDidYouRender.Records.StateTracking;

/// <summary>
/// Configuration settings for stress testing state tracking operations.
/// This record provides immutable configuration for stress test scenarios and performance testing.
/// </summary>
public record StressTestConfiguration
{
    /// <summary>
    /// Gets the number of test components to create for stress testing.
    /// Default is 100 components.
    /// </summary>
    public int ComponentCount { get; init; } = 100;

    /// <summary>
    /// Gets the number of concurrent threads to use during stress testing.
    /// Default is the number of processor cores available.
    /// </summary>
    public int ConcurrentThreads { get; init; } = Environment.ProcessorCount;

    /// <summary>
    /// Gets the number of operations each thread should perform.
    /// Default is 100 operations per thread.
    /// </summary>
    public int OperationsPerThread { get; init; } = 100;

    /// <summary>
    /// Gets the delay between operations in milliseconds.
    /// Default is 0 (no delay) for maximum stress.
    /// </summary>
    public int DelayBetweenOperationsMs { get; init; } = 0;

    /// <summary>
    /// Gets the timeout for individual operations in milliseconds.
    /// Default is 5000ms (5 seconds).
    /// </summary>
    public int OperationTimeoutMs { get; init; } = 5000;

    /// <summary>
    /// Gets the maximum memory usage allowed during testing in MB.
    /// Default is 500MB.
    /// </summary>
    public int MaxMemoryUsageMB { get; init; } = 500;

    /// <summary>
    /// Gets whether to force garbage collection between test phases.
    /// Default is true for more consistent results.
    /// </summary>
    public bool ForceGCBetweenTests { get; init; } = true;

    /// <summary>
    /// Gets the total number of operations across all threads.
    /// </summary>
    public int TotalOperations => ConcurrentThreads * OperationsPerThread;

    /// <summary>
    /// Gets the estimated test duration based on operations and delays.
    /// </summary>
    public TimeSpan EstimatedDuration => TimeSpan.FromMilliseconds(
        (OperationsPerThread * DelayBetweenOperationsMs) + (TotalOperations * 10) // 10ms per operation estimate
    );

    /// <summary>
    /// Gets whether this configuration represents a high-stress scenario.
    /// </summary>
    public bool IsHighStress => ConcurrentThreads >= Environment.ProcessorCount * 2 && 
                               ComponentCount >= 500 && 
                               OperationsPerThread >= 1000;

    /// <summary>
    /// Gets predefined configuration for light stress testing.
    /// </summary>
    public static StressTestConfiguration Light => new()
    {
        ComponentCount = 50,
        ConcurrentThreads = Math.Max(1, Environment.ProcessorCount / 2),
        OperationsPerThread = 50,
        DelayBetweenOperationsMs = 1,
        MaxMemoryUsageMB = 100
    };

    /// <summary>
    /// Gets predefined configuration for moderate stress testing.
    /// </summary>
    public static StressTestConfiguration Moderate => new()
    {
        ComponentCount = 100,
        ConcurrentThreads = Environment.ProcessorCount,
        OperationsPerThread = 100,
        DelayBetweenOperationsMs = 0,
        MaxMemoryUsageMB = 250
    };

    /// <summary>
    /// Gets predefined configuration for heavy stress testing.
    /// </summary>
    public static StressTestConfiguration Heavy => new()
    {
        ComponentCount = 500,
        ConcurrentThreads = Environment.ProcessorCount * 2,
        OperationsPerThread = 500,
        DelayBetweenOperationsMs = 0,
        MaxMemoryUsageMB = 1000
    };

    /// <summary>
    /// Gets predefined configuration for extreme stress testing.
    /// </summary>
    public static StressTestConfiguration Extreme => new()
    {
        ComponentCount = 1000,
        ConcurrentThreads = Environment.ProcessorCount * 4,
        OperationsPerThread = 1000,
        DelayBetweenOperationsMs = 0,
        MaxMemoryUsageMB = 2000
    };

    /// <summary>
    /// Gets a formatted summary of the stress test configuration.
    /// </summary>
    /// <returns>A formatted string with configuration details.</returns>
    public string GetFormattedSummary()
    {
        var stressLevel = IsHighStress ? "High" : TotalOperations > 1000 ? "Moderate" : "Light";
        
        return $"Stress Test Configuration ({stressLevel} Stress):\n" +
               $"  Components: {ComponentCount:N0}\n" +
               $"  Concurrent Threads: {ConcurrentThreads}\n" +
               $"  Operations/Thread: {OperationsPerThread:N0}\n" +
               $"  Total Operations: {TotalOperations:N0}\n" +
               $"  Delay Between Ops: {DelayBetweenOperationsMs}ms\n" +
               $"  Operation Timeout: {OperationTimeoutMs}ms\n" +
               $"  Max Memory: {MaxMemoryUsageMB}MB\n" +
               $"  Force GC: {ForceGCBetweenTests}\n" +
               $"  Estimated Duration: {EstimatedDuration}";
    }
}
