using System;

using Blazor.WhyDidYouRender.Core.StateTracking;

namespace Blazor.WhyDidYouRender.Records;

/// <summary>
/// Represents a cache entry with metadata and access tracking for performance monitoring.
/// This class tracks when entries are created, accessed, and provides memory usage estimates.
/// Note: This is a class rather than a record because it needs mutable LastAccessTime.
/// </summary>
public class CacheEntry {
    /// <summary>
    /// Gets the cached state field metadata for a component type.
    /// </summary>
    public StateFieldMetadata Metadata { get; }

    /// <summary>
    /// Gets the timestamp when this cache entry was created.
    /// </summary>
    public DateTime CreatedAt { get; }

    /// <summary>
    /// Gets the timestamp when this cache entry was last accessed.
    /// </summary>
    public DateTime LastAccessTime { get; private set; }

    /// <summary>
    /// Gets the estimated memory usage of this cache entry in bytes.
    /// </summary>
    public long EstimatedMemoryUsage { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheEntry"/> class.
    /// </summary>
    /// <param name="metadata">The state field metadata to cache.</param>
    public CacheEntry(StateFieldMetadata metadata) {
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        CreatedAt = DateTime.UtcNow;
        LastAccessTime = CreatedAt;
        EstimatedMemoryUsage = CalculateMemoryUsage(metadata);
    }



    /// <summary>
    /// Updates the last access time to the current UTC time.
    /// This method is called whenever the cache entry is accessed.
    /// </summary>
    public void UpdateAccessTime() {
        LastAccessTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the age of this cache entry since it was created.
    /// </summary>
    public TimeSpan Age => DateTime.UtcNow - CreatedAt;

    /// <summary>
    /// Gets the time since this cache entry was last accessed.
    /// </summary>
    public TimeSpan TimeSinceLastAccess => DateTime.UtcNow - LastAccessTime;

    /// <summary>
    /// Gets whether this cache entry is considered stale based on the specified maximum age.
    /// </summary>
    /// <param name="maxAge">The maximum age before an entry is considered stale.</param>
    /// <returns>True if the entry is older than the specified maximum age.</returns>
    public bool IsStale(TimeSpan maxAge) => Age > maxAge;

    /// <summary>
    /// Gets whether this cache entry has been recently accessed based on the specified threshold.
    /// </summary>
    /// <param name="recentThreshold">The threshold for considering an access recent.</param>
    /// <returns>True if the entry was accessed within the threshold time.</returns>
    public bool IsRecentlyAccessed(TimeSpan recentThreshold) => TimeSinceLastAccess <= recentThreshold;

    /// <summary>
    /// Calculates the estimated memory usage of the state field metadata.
    /// </summary>
    /// <param name="metadata">The metadata to calculate memory usage for.</param>
    /// <returns>The estimated memory usage in bytes.</returns>
    private static long CalculateMemoryUsage(StateFieldMetadata metadata) {
        // Rough estimation - in practice you might want more accurate calculation
        const long baseSize = 1024; // Base object overhead
        const long fieldInfoSize = 256; // Estimated size per FieldInfo

        return baseSize + (metadata.AllTrackedFields.Count * fieldInfoSize);
    }
}
