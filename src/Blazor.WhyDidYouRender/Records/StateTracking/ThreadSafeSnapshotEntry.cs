using System;

using Blazor.WhyDidYouRender.Core.StateTracking;

namespace Blazor.WhyDidYouRender.Records.StateTracking;

/// <summary>
/// Represents a thread-safe snapshot entry containing state snapshot data and metadata.
/// This record provides immutable information about when and where a state snapshot was captured.
/// </summary>
/// <remarks>
/// ThreadSafeSnapshotEntry is used internally by the thread-safe state tracking system to store
/// snapshots along with threading metadata. This allows the system to track which thread captured
/// the snapshot and when it was captured for debugging and analysis purposes.
/// </remarks>
public record ThreadSafeSnapshotEntry {
    /// <summary>
    /// Gets the state snapshot that was captured.
    /// </summary>
    public required StateSnapshot Snapshot { get; init; }

    /// <summary>
    /// Gets the managed thread ID of the thread that captured this snapshot.
    /// </summary>
    public int ThreadId { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when this snapshot was captured.
    /// </summary>
    public DateTime CapturedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the name of the thread that captured this snapshot, if available.
    /// </summary>
    public string? ThreadName { get; init; }

    /// <summary>
    /// Gets additional context information about the snapshot capture.
    /// </summary>
    public string? Context { get; init; }

    /// <summary>
    /// Gets the age of this snapshot since it was captured.
    /// </summary>
    public TimeSpan Age => DateTime.UtcNow - CapturedAt;

    /// <summary>
    /// Gets whether this snapshot is considered stale based on a threshold.
    /// </summary>
    /// <param name="threshold">The age threshold for considering a snapshot stale.</param>
    /// <returns>True if the snapshot is older than the threshold; otherwise, false.</returns>
    public bool IsStale(TimeSpan threshold) => Age > threshold;

    /// <summary>
    /// Gets whether this snapshot was captured by the current thread.
    /// </summary>
    public bool IsCapturedByCurrentThread => ThreadId == Environment.CurrentManagedThreadId;

    /// <summary>
    /// Gets a formatted description of when and where this snapshot was captured.
    /// </summary>
    public string CaptureDescription => string.IsNullOrEmpty(ThreadName)
        ? $"Thread {ThreadId} at {CapturedAt:HH:mm:ss.fff}"
        : $"Thread {ThreadId} ({ThreadName}) at {CapturedAt:HH:mm:ss.fff}";

    /// <summary>
    /// Creates a new ThreadSafeSnapshotEntry for the current thread.
    /// </summary>
    /// <param name="snapshot">The state snapshot to store.</param>
    /// <param name="context">Optional context information.</param>
    /// <returns>A new ThreadSafeSnapshotEntry.</returns>
    public static ThreadSafeSnapshotEntry Create(StateSnapshot snapshot, string? context = null) => new() {
        Snapshot = snapshot,
        ThreadId = Environment.CurrentManagedThreadId,
        ThreadName = System.Threading.Thread.CurrentThread.Name,
        Context = context
    };

    /// <summary>
    /// Creates a new ThreadSafeSnapshotEntry with specific thread information.
    /// </summary>
    /// <param name="snapshot">The state snapshot to store.</param>
    /// <param name="threadId">The thread ID that captured the snapshot.</param>
    /// <param name="threadName">The name of the thread that captured the snapshot.</param>
    /// <param name="capturedAt">When the snapshot was captured.</param>
    /// <param name="context">Optional context information.</param>
    /// <returns>A new ThreadSafeSnapshotEntry.</returns>
    public static ThreadSafeSnapshotEntry Create(
        StateSnapshot snapshot,
        int threadId,
        string? threadName = null,
        DateTime? capturedAt = null,
        string? context = null) => new() {
            Snapshot = snapshot,
            ThreadId = threadId,
            ThreadName = threadName,
            CapturedAt = capturedAt ?? DateTime.UtcNow,
            Context = context
        };

    /// <summary>
    /// Gets a formatted summary of this snapshot entry.
    /// </summary>
    /// <returns>A formatted string with snapshot entry information.</returns>
    public string GetFormattedSummary() {
        var summary = $"ThreadSafe Snapshot Entry:\n" +
                     $"  Captured: {CaptureDescription}\n" +
                     $"  Age: {Age}\n" +
                     $"  Current Thread: {IsCapturedByCurrentThread}";

        if (!string.IsNullOrEmpty(Context)) {
            summary += $"\n  Context: {Context}";
        }

        if (Snapshot != null) {
            summary += $"\n  Component: {Snapshot.ComponentName}\n" +
                      $"  Fields: {Snapshot.FieldSnapshots.Count}";
        }

        return summary;
    }

    /// <summary>
    /// Creates a copy of this entry with updated capture time.
    /// </summary>
    /// <returns>A new ThreadSafeSnapshotEntry with current timestamp.</returns>
    public ThreadSafeSnapshotEntry RefreshTimestamp() => this with { CapturedAt = DateTime.UtcNow };

    /// <summary>
    /// Creates a copy of this entry with additional context.
    /// </summary>
    /// <param name="additionalContext">Additional context to append.</param>
    /// <returns>A new ThreadSafeSnapshotEntry with updated context.</returns>
    public ThreadSafeSnapshotEntry WithContext(string additionalContext) {
        var newContext = string.IsNullOrEmpty(Context)
            ? additionalContext
            : $"{Context}; {additionalContext}";

        return this with { Context = newContext };
    }
}
