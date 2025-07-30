using System;

namespace Blazor.WhyDidYouRender.Records.StateTracking;

/// <summary>
/// Contains tracking information for a component in the memory management system.
/// This record provides immutable information about component registration and access patterns.
/// </summary>
public record ComponentTrackingInfo
{
    /// <summary>
    /// Gets the type of the tracked component.
    /// </summary>
    public required Type ComponentType { get; init; }

    /// <summary>
    /// Gets the time when the component was registered for tracking.
    /// </summary>
    public required DateTime RegistrationTime { get; init; }

    /// <summary>
    /// Gets the time when the component was last accessed.
    /// </summary>
    public DateTime LastAccessTime { get; init; }

    /// <summary>
    /// Gets the estimated memory usage of the component in bytes.
    /// </summary>
    public long EstimatedMemoryUsage { get; init; }

    /// <summary>
    /// Gets the age of the component since registration.
    /// </summary>
    public TimeSpan Age => DateTime.UtcNow - RegistrationTime;

    /// <summary>
    /// Gets the time since the component was last accessed.
    /// </summary>
    public TimeSpan TimeSinceLastAccess => DateTime.UtcNow - LastAccessTime;

    /// <summary>
    /// Gets whether the component is considered stale based on the last access time.
    /// </summary>
    /// <param name="maxAge">The maximum age before a component is considered stale.</param>
    /// <returns>True if the component is stale; otherwise, false.</returns>
    public bool IsStale(TimeSpan maxAge) => TimeSinceLastAccess > maxAge;
}
