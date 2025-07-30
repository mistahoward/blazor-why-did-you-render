using System;

namespace Blazor.WhyDidYouRender.Records.StateTracking;

/// <summary>
/// Configuration settings for thread-safe state tracking operations.
/// This record provides immutable configuration for threading behavior and concurrency limits.
/// </summary>
/// <remarks>
/// ThreadingConfiguration controls how the thread-safe state tracking system manages
/// concurrent operations, lock timeouts, and threading behavior. These settings help
/// balance performance with thread safety and prevent deadlocks.
/// </remarks>
public record ThreadingConfiguration
{
    /// <summary>
    /// Gets the maximum number of concurrent state tracking operations allowed.
    /// Default is twice the number of processor cores.
    /// </summary>
    public int MaxConcurrentOperations { get; init; } = Environment.ProcessorCount * 2;

    /// <summary>
    /// Gets the timeout for lock acquisition in milliseconds.
    /// Default is 5000ms (5 seconds).
    /// </summary>
    public int LockTimeoutMs { get; init; } = 5000;

    /// <summary>
    /// Gets whether to use reader-writer locks for better concurrency.
    /// Default is true for improved read performance.
    /// </summary>
    public bool UseReaderWriterLocks { get; init; } = true;

    /// <summary>
    /// Gets the maximum time to wait for a semaphore in milliseconds.
    /// Default is 10000ms (10 seconds).
    /// </summary>
    public int SemaphoreTimeoutMs { get; init; } = 10000;

    /// <summary>
    /// Gets whether to enable deadlock detection and prevention.
    /// Default is true for safer operation.
    /// </summary>
    public bool EnableDeadlockDetection { get; init; } = true;

    /// <summary>
    /// Gets the interval for checking threading health in milliseconds.
    /// Default is 30000ms (30 seconds).
    /// </summary>
    public int HealthCheckIntervalMs { get; init; } = 30000;

    /// <summary>
    /// Gets whether to automatically cleanup stale locks.
    /// Default is true to prevent resource leaks.
    /// </summary>
    public bool AutoCleanupStaleLocks { get; init; } = true;

    /// <summary>
    /// Gets the age threshold for considering locks stale in milliseconds.
    /// Default is 300000ms (5 minutes).
    /// </summary>
    public int StaleLockThresholdMs { get; init; } = 300000;

    /// <summary>
    /// Gets the lock timeout as a TimeSpan.
    /// </summary>
    public TimeSpan LockTimeout => TimeSpan.FromMilliseconds(LockTimeoutMs);

    /// <summary>
    /// Gets the semaphore timeout as a TimeSpan.
    /// </summary>
    public TimeSpan SemaphoreTimeout => TimeSpan.FromMilliseconds(SemaphoreTimeoutMs);

    /// <summary>
    /// Gets the health check interval as a TimeSpan.
    /// </summary>
    public TimeSpan HealthCheckInterval => TimeSpan.FromMilliseconds(HealthCheckIntervalMs);

    /// <summary>
    /// Gets the stale lock threshold as a TimeSpan.
    /// </summary>
    public TimeSpan StaleLockThreshold => TimeSpan.FromMilliseconds(StaleLockThresholdMs);

    /// <summary>
    /// Gets whether this configuration represents high-concurrency settings.
    /// </summary>
    public bool IsHighConcurrency => MaxConcurrentOperations >= Environment.ProcessorCount * 4;

    /// <summary>
    /// Gets whether this configuration has aggressive timeout settings.
    /// </summary>
    public bool HasAggressiveTimeouts => LockTimeoutMs <= 1000 || SemaphoreTimeoutMs <= 2000;

    /// <summary>
    /// Gets predefined configuration for low-concurrency scenarios.
    /// </summary>
    public static ThreadingConfiguration LowConcurrency => new()
    {
        MaxConcurrentOperations = Math.Max(1, Environment.ProcessorCount / 2),
        LockTimeoutMs = 10000,
        SemaphoreTimeoutMs = 15000,
        HealthCheckIntervalMs = 60000,
        StaleLockThresholdMs = 600000 // 10 minutes
    };

    /// <summary>
    /// Gets predefined configuration for moderate-concurrency scenarios.
    /// </summary>
    public static ThreadingConfiguration ModerateConcurrency => new()
    {
        MaxConcurrentOperations = Environment.ProcessorCount * 2,
        LockTimeoutMs = 5000,
        SemaphoreTimeoutMs = 10000,
        HealthCheckIntervalMs = 30000,
        StaleLockThresholdMs = 300000 // 5 minutes
    };

    /// <summary>
    /// Gets predefined configuration for high-concurrency scenarios.
    /// </summary>
    public static ThreadingConfiguration HighConcurrency => new()
    {
        MaxConcurrentOperations = Environment.ProcessorCount * 4,
        LockTimeoutMs = 2000,
        SemaphoreTimeoutMs = 5000,
        HealthCheckIntervalMs = 15000,
        StaleLockThresholdMs = 120000 // 2 minutes
    };

    /// <summary>
    /// Gets predefined configuration for development environments.
    /// </summary>
    public static ThreadingConfiguration Development => new()
    {
        MaxConcurrentOperations = Environment.ProcessorCount,
        LockTimeoutMs = 15000, // Longer timeouts for debugging
        SemaphoreTimeoutMs = 20000,
        EnableDeadlockDetection = true,
        HealthCheckIntervalMs = 60000,
        StaleLockThresholdMs = 900000, // 15 minutes
        AutoCleanupStaleLocks = true
    };

    /// <summary>
    /// Gets predefined configuration for production environments.
    /// </summary>
    public static ThreadingConfiguration Production => new()
    {
        MaxConcurrentOperations = Environment.ProcessorCount * 3,
        LockTimeoutMs = 3000, // Shorter timeouts for responsiveness
        SemaphoreTimeoutMs = 7000,
        EnableDeadlockDetection = true,
        HealthCheckIntervalMs = 20000,
        StaleLockThresholdMs = 180000, // 3 minutes
        AutoCleanupStaleLocks = true
    };

    /// <summary>
    /// Validates the configuration settings and returns any issues found.
    /// </summary>
    /// <returns>A list of validation issues, or empty if configuration is valid.</returns>
    public IReadOnlyList<string> Validate()
    {
        var issues = new List<string>();

        if (MaxConcurrentOperations <= 0)
            issues.Add("MaxConcurrentOperations must be greater than 0");

        if (LockTimeoutMs <= 0)
            issues.Add("LockTimeoutMs must be greater than 0");

        if (SemaphoreTimeoutMs <= 0)
            issues.Add("SemaphoreTimeoutMs must be greater than 0");

        if (HealthCheckIntervalMs <= 0)
            issues.Add("HealthCheckIntervalMs must be greater than 0");

        if (StaleLockThresholdMs <= 0)
            issues.Add("StaleLockThresholdMs must be greater than 0");

        if (MaxConcurrentOperations > Environment.ProcessorCount * 10)
            issues.Add($"MaxConcurrentOperations ({MaxConcurrentOperations}) is very high compared to processor count ({Environment.ProcessorCount})");

        if (LockTimeoutMs < 100)
            issues.Add("LockTimeoutMs is very low and may cause frequent timeouts");

        if (StaleLockThresholdMs < HealthCheckIntervalMs * 2)
            issues.Add("StaleLockThresholdMs should be at least twice the HealthCheckIntervalMs");

        return issues;
    }

    /// <summary>
    /// Gets a formatted summary of the threading configuration.
    /// </summary>
    /// <returns>A formatted string with configuration details.</returns>
    public string GetFormattedSummary()
    {
        var concurrencyLevel = IsHighConcurrency ? "High" : 
                              MaxConcurrentOperations >= Environment.ProcessorCount ? "Moderate" : "Low";
        
        var timeoutProfile = HasAggressiveTimeouts ? "Aggressive" : "Conservative";

        return $"Threading Configuration ({concurrencyLevel} Concurrency, {timeoutProfile} Timeouts):\n" +
               $"  Max Concurrent Operations: {MaxConcurrentOperations}\n" +
               $"  Lock Timeout: {LockTimeout}\n" +
               $"  Semaphore Timeout: {SemaphoreTimeout}\n" +
               $"  Use Reader-Writer Locks: {UseReaderWriterLocks}\n" +
               $"  Deadlock Detection: {EnableDeadlockDetection}\n" +
               $"  Health Check Interval: {HealthCheckInterval}\n" +
               $"  Auto Cleanup Stale Locks: {AutoCleanupStaleLocks}\n" +
               $"  Stale Lock Threshold: {StaleLockThreshold}";
    }
}
