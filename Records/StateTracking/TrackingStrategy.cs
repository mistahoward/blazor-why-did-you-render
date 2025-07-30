namespace Blazor.WhyDidYouRender.Records.StateTracking;

/// <summary>
/// Defines the strategy for tracking a specific field or property.
/// </summary>
public enum TrackingStrategy {
    /// <summary>
    /// The field should not be tracked for state changes.
    /// This applies to static fields, readonly fields, compiler-generated fields,
    /// or fields explicitly marked with [IgnoreState].
    /// </summary>
    Skip = 0,

    /// <summary>
    /// The field should be automatically tracked using simple value comparison.
    /// This applies to simple value types like string, int, bool, DateTime, etc.
    /// </summary>
    AutoTrack = 1,

    /// <summary>
    /// The field should be tracked because it's explicitly marked with [TrackState].
    /// This applies to complex types that require explicit opt-in for tracking.
    /// </summary>
    ExplicitTrack = 2,

    /// <summary>
    /// The field should be ignored because it's explicitly marked with [IgnoreState].
    /// This takes precedence over auto-tracking for simple types.
    /// </summary>
    Ignore = 3
}

/// <summary>
/// Provides extension methods for the TrackingStrategy enum.
/// </summary>
public static class TrackingStrategyExtensions {
    /// <summary>
    /// Determines if the strategy indicates the field should be tracked.
    /// </summary>
    /// <param name="strategy">The tracking strategy.</param>
    /// <returns>True if the field should be tracked.</returns>
    public static bool ShouldTrack(this TrackingStrategy strategy) {
        return strategy == TrackingStrategy.AutoTrack || strategy == TrackingStrategy.ExplicitTrack;
    }

    /// <summary>
    /// Determines if the strategy indicates the field is explicitly controlled by attributes.
    /// </summary>
    /// <param name="strategy">The tracking strategy.</param>
    /// <returns>True if the field has explicit attribute control.</returns>
    public static bool IsExplicit(this TrackingStrategy strategy) {
        return strategy == TrackingStrategy.ExplicitTrack || strategy == TrackingStrategy.Ignore;
    }

    /// <summary>
    /// Gets a human-readable description of the tracking strategy.
    /// </summary>
    /// <param name="strategy">The tracking strategy.</param>
    /// <returns>A description of the strategy.</returns>
    public static string GetDescription(this TrackingStrategy strategy) {
        return strategy switch {
            TrackingStrategy.Skip => "Skipped (static, readonly, or compiler-generated)",
            TrackingStrategy.AutoTrack => "Auto-tracked (simple value type)",
            TrackingStrategy.ExplicitTrack => "Explicitly tracked ([TrackState] attribute)",
            TrackingStrategy.Ignore => "Explicitly ignored ([IgnoreState] attribute)",
            _ => "Unknown strategy"
        };
    }
}
