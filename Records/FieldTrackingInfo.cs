using System;
using System.Reflection;

using Blazor.WhyDidYouRender.Attributes;
using Blazor.WhyDidYouRender.Records.StateTracking;

namespace Blazor.WhyDidYouRender.Records;

/// <summary>
/// Contains tracking information for a specific field, including its tracking strategy and configuration.
/// This record provides metadata about how a field should be tracked for state changes.
/// </summary>
public class FieldTrackingInfo {
    /// <summary>
    /// Gets the field information from reflection.
    /// </summary>
    public FieldInfo FieldInfo { get; }

    /// <summary>
    /// Gets the tracking strategy for this field (auto-track, explicit, ignore, etc.).
    /// </summary>
    public TrackingStrategy Strategy { get; }

    /// <summary>
    /// Gets the [TrackState] attribute if present on this field.
    /// </summary>
    public TrackStateAttribute? TrackStateAttribute { get; }

    /// <summary>
    /// Gets the [IgnoreState] attribute if present on this field.
    /// </summary>
    public IgnoreStateAttribute? IgnoreStateAttribute { get; }

    /// <summary>
    /// Gets whether this field uses custom comparison logic.
    /// </summary>
    public bool UsesCustomComparison { get; }

    /// <summary>
    /// Gets the maximum comparison depth for complex objects.
    /// </summary>
    public int MaxComparisonDepth { get; }

    /// <summary>
    /// Gets whether collection contents should be tracked for this field.
    /// </summary>
    public bool TrackCollectionContents { get; }

    /// <summary>
    /// Gets a description of this field for diagnostic purposes.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FieldTrackingInfo"/> class.
    /// </summary>
    /// <param name="fieldInfo">The field information from reflection.</param>
    /// <param name="strategy">The tracking strategy for this field.</param>
    /// <param name="trackStateAttribute">The [TrackState] attribute if present.</param>
    /// <param name="ignoreStateAttribute">The [IgnoreState] attribute if present.</param>
    public FieldTrackingInfo(
        FieldInfo fieldInfo,
        TrackingStrategy strategy,
        TrackStateAttribute? trackStateAttribute = null,
        IgnoreStateAttribute? ignoreStateAttribute = null) {
        FieldInfo = fieldInfo ?? throw new ArgumentNullException(nameof(fieldInfo));
        Strategy = strategy;
        TrackStateAttribute = trackStateAttribute;
        IgnoreStateAttribute = ignoreStateAttribute;

        // Extract configuration from attributes
        if (trackStateAttribute != null) {
            UsesCustomComparison = trackStateAttribute.UseCustomComparer;
            MaxComparisonDepth = trackStateAttribute.MaxComparisonDepth;
            TrackCollectionContents = trackStateAttribute.TrackCollectionContents;
            Description = trackStateAttribute.Description;
        }
        else {
            UsesCustomComparison = false;
            MaxComparisonDepth = 1;
            TrackCollectionContents = false;
            Description = ignoreStateAttribute?.Reason;
        }
    }
}
