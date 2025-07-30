namespace Blazor.WhyDidYouRender.Core.StateTracking;

/// <summary>
/// Configuration options for the state field cache.
/// </summary>
public class CacheConfiguration
{
    /// <summary>
    /// Maximum number of entries to keep in cache.
    /// </summary>
    public int MaxCacheSize { get; set; } = 1000;

    /// <summary>
    /// Maximum age of cache entries in minutes before they're eligible for removal.
    /// </summary>
    public int MaxEntryAgeMinutes { get; set; } = 60;

    /// <summary>
    /// Interval between maintenance operations in minutes.
    /// </summary>
    public int MaintenanceIntervalMinutes { get; set; } = 10;
}
