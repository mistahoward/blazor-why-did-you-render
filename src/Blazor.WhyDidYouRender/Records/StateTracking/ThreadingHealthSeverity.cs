using System;

namespace Blazor.WhyDidYouRender.Records.StateTracking;

/// <summary>
/// Defines the severity levels for threading health status in state tracking operations.
/// These values indicate the urgency and impact of threading issues detected during monitoring.
/// </summary>
/// <remarks>
/// ThreadingHealthSeverity provides a standardized way to categorize threading health issues
/// from normal operation to critical problems requiring immediate intervention. This helps
/// prioritize responses and automate escalation procedures based on severity levels.
/// </remarks>
public enum ThreadingHealthSeverity {
    /// <summary>
    /// Healthy status indicating normal threading operation.
    /// All threading metrics are within acceptable ranges and no issues are detected.
    /// This is the desired state for production systems.
    /// </summary>
    Healthy = 0,

    /// <summary>
    /// Warning status indicating minor threading issues that should be monitored.
    /// Threading performance may be slightly degraded but is still within operational limits.
    /// These warnings suggest proactive investigation to prevent escalation to more serious issues.
    /// Examples: Slightly elevated lock contention, moderate concurrency utilization.
    /// </summary>
    Warning = 1,

    /// <summary>
    /// Error status indicating significant threading issues requiring attention.
    /// Threading performance is outside acceptable limits and may impact system responsiveness.
    /// These errors require timely investigation and corrective action to prevent system degradation.
    /// Examples: High lock contention, frequent operation timeouts, excessive thread usage.
    /// </summary>
    Error = 2,

    /// <summary>
    /// Critical status indicating severe threading issues requiring immediate action.
    /// Threading performance is severely degraded and likely impacting system stability.
    /// These critical issues require immediate intervention to prevent system failure or deadlocks.
    /// Examples: Deadlock detection, complete concurrency saturation, system-wide lock contention.
    /// </summary>
    Critical = 3
}

/// <summary>
/// Extension methods for ThreadingHealthSeverity enum.
/// </summary>
public static class ThreadingHealthSeverityExtensions {
    /// <summary>
    /// Gets a human-readable description of the severity level.
    /// </summary>
    /// <param name="severity">The severity level.</param>
    /// <returns>A descriptive string for the severity level.</returns>
    public static string GetDescription(this ThreadingHealthSeverity severity) => severity switch {
        ThreadingHealthSeverity.Healthy => "System is operating normally with no threading issues detected",
        ThreadingHealthSeverity.Warning => "Minor threading issues detected that should be monitored",
        ThreadingHealthSeverity.Error => "Significant threading issues requiring attention and corrective action",
        ThreadingHealthSeverity.Critical => "Critical threading issues requiring immediate intervention",
        _ => "Unknown severity level"
    };

    /// <summary>
    /// Gets the recommended action for the severity level.
    /// </summary>
    /// <param name="severity">The severity level.</param>
    /// <returns>A recommended action string.</returns>
    public static string GetRecommendedAction(this ThreadingHealthSeverity severity) => severity switch {
        ThreadingHealthSeverity.Healthy => "Continue normal monitoring",
        ThreadingHealthSeverity.Warning => "Increase monitoring frequency and investigate potential causes",
        ThreadingHealthSeverity.Error => "Investigate issues promptly and implement corrective measures",
        ThreadingHealthSeverity.Critical => "Take immediate action to prevent system failure or deadlocks",
        _ => "Review system status and determine appropriate action"
    };

    /// <summary>
    /// Gets the priority level for the severity.
    /// </summary>
    /// <param name="severity">The severity level.</param>
    /// <returns>A priority description.</returns>
    public static string GetPriority(this ThreadingHealthSeverity severity) => severity switch {
        ThreadingHealthSeverity.Healthy => "Low",
        ThreadingHealthSeverity.Warning => "Medium",
        ThreadingHealthSeverity.Error => "High",
        ThreadingHealthSeverity.Critical => "Urgent",
        _ => "Unknown"
    };

    /// <summary>
    /// Gets whether this severity level requires immediate attention.
    /// </summary>
    /// <param name="severity">The severity level.</param>
    /// <returns>True if immediate attention is required; otherwise, false.</returns>
    public static bool RequiresImmediateAttention(this ThreadingHealthSeverity severity) =>
        severity >= ThreadingHealthSeverity.Error;

    /// <summary>
    /// Gets whether this severity level indicates a healthy state.
    /// </summary>
    /// <param name="severity">The severity level.</param>
    /// <returns>True if the state is healthy; otherwise, false.</returns>
    public static bool IsHealthy(this ThreadingHealthSeverity severity) =>
        severity == ThreadingHealthSeverity.Healthy;

    /// <summary>
    /// Gets whether this severity level indicates a critical state.
    /// </summary>
    /// <param name="severity">The severity level.</param>
    /// <returns>True if the state is critical; otherwise, false.</returns>
    public static bool IsCritical(this ThreadingHealthSeverity severity) =>
        severity == ThreadingHealthSeverity.Critical;

    /// <summary>
    /// Gets the color code typically associated with this severity level.
    /// </summary>
    /// <param name="severity">The severity level.</param>
    /// <returns>A color name or hex code for UI representation.</returns>
    public static string GetColorCode(this ThreadingHealthSeverity severity) => severity switch {
        ThreadingHealthSeverity.Healthy => "#28a745", // Green
        ThreadingHealthSeverity.Warning => "#ffc107", // Yellow
        ThreadingHealthSeverity.Error => "#fd7e14", // Orange
        ThreadingHealthSeverity.Critical => "#dc3545", // Red
        _ => "#6c757d" // Gray
    };

    /// <summary>
    /// Gets an emoji representation of the severity level.
    /// </summary>
    /// <param name="severity">The severity level.</param>
    /// <returns>An emoji character representing the severity.</returns>
    public static string GetEmoji(this ThreadingHealthSeverity severity) => severity switch {
        ThreadingHealthSeverity.Healthy => "✅",
        ThreadingHealthSeverity.Warning => "⚠️",
        ThreadingHealthSeverity.Error => "❌",
        ThreadingHealthSeverity.Critical => "🚨",
        _ => "❓"
    };

    /// <summary>
    /// Compares two severity levels and returns the higher one.
    /// </summary>
    /// <param name="severity1">The first severity level.</param>
    /// <param name="severity2">The second severity level.</param>
    /// <returns>The higher of the two severity levels.</returns>
    public static ThreadingHealthSeverity Max(ThreadingHealthSeverity severity1, ThreadingHealthSeverity severity2) =>
        (ThreadingHealthSeverity)Math.Max((int)severity1, (int)severity2);

    /// <summary>
    /// Compares two severity levels and returns the lower one.
    /// </summary>
    /// <param name="severity1">The first severity level.</param>
    /// <param name="severity2">The second severity level.</param>
    /// <returns>The lower of the two severity levels.</returns>
    public static ThreadingHealthSeverity Min(ThreadingHealthSeverity severity1, ThreadingHealthSeverity severity2) =>
        (ThreadingHealthSeverity)Math.Min((int)severity1, (int)severity2);
}
