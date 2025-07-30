namespace Blazor.WhyDidYouRender.Records.StateTracking;

/// <summary>
/// Configuration settings for memory management in state tracking operations.
/// This record provides immutable configuration for memory optimization and cleanup behavior.
/// </summary>
public record MemoryManagementConfiguration
{
    /// <summary>
    /// Gets the maximum number of components to track simultaneously.
    /// Default is 1000 components.
    /// </summary>
    public int MaxTrackedComponents { get; init; } = 1000;

    /// <summary>
    /// Gets the maximum age of components in minutes before cleanup.
    /// Default is 30 minutes.
    /// </summary>
    public int MaxComponentAgeMinutes { get; init; } = 30;

    /// <summary>
    /// Gets the interval between cleanup operations in minutes.
    /// Default is 5 minutes.
    /// </summary>
    public int CleanupIntervalMinutes { get; init; } = 5;

    /// <summary>
    /// Gets the maximum memory usage in MB before warnings are triggered.
    /// Default is 100 MB.
    /// </summary>
    public long MaxMemoryUsageMB { get; init; } = 100;

    /// <summary>
    /// Gets the maximum number of dead references before cleanup is triggered.
    /// Default is 100 dead references.
    /// </summary>
    public int MaxDeadReferences { get; init; } = 100;

    /// <summary>
    /// Gets the threshold for forcing garbage collection after cleanup.
    /// Default is 50 cleaned up components.
    /// </summary>
    public int ForceGCThreshold { get; init; } = 50;

    /// <summary>
    /// Gets a configuration optimized for development environments.
    /// </summary>
    public static MemoryManagementConfiguration Development => new()
    {
        MaxTrackedComponents = 500,
        MaxComponentAgeMinutes = 15,
        CleanupIntervalMinutes = 2,
        MaxMemoryUsageMB = 50,
        MaxDeadReferences = 50,
        ForceGCThreshold = 25
    };

    /// <summary>
    /// Gets a configuration optimized for production environments.
    /// </summary>
    public static MemoryManagementConfiguration Production => new()
    {
        MaxTrackedComponents = 2000,
        MaxComponentAgeMinutes = 60,
        CleanupIntervalMinutes = 10,
        MaxMemoryUsageMB = 200,
        MaxDeadReferences = 200,
        ForceGCThreshold = 100
    };
}
