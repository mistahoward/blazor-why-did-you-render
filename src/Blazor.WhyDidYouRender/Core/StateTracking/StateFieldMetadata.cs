using System;
using System.Collections.Generic;
using System.Reflection;
using Blazor.WhyDidYouRender.Attributes;
using Blazor.WhyDidYouRender.Records;

namespace Blazor.WhyDidYouRender.Core.StateTracking;

/// <summary>
/// Contains metadata about trackable fields for a specific component type.
/// This class caches field discovery results to avoid repeated reflection operations.
/// </summary>
public class StateFieldMetadata
{
	/// <summary>
	/// Gets the component type this metadata applies to.
	/// </summary>
	public Type ComponentType { get; }

	/// <summary>
	/// Gets the fields that are automatically tracked (simple value types).
	/// </summary>
	public IReadOnlyList<FieldTrackingInfo> AutoTrackedFields { get; }

	/// <summary>
	/// Gets the fields that are explicitly tracked via [TrackState] attribute.
	/// </summary>
	public IReadOnlyList<FieldTrackingInfo> ExplicitlyTrackedFields { get; }

	/// <summary>
	/// Gets all trackable fields (combination of auto-tracked and explicitly tracked).
	/// </summary>
	public IReadOnlyList<FieldTrackingInfo> AllTrackedFields { get; }

	/// <summary>
	/// Gets the fields that are explicitly ignored via [IgnoreState] attribute.
	/// </summary>
	public IReadOnlyList<FieldTrackingInfo> IgnoredFields { get; }

	/// <summary>
	/// Gets the total number of fields that will be tracked.
	/// </summary>
	public int TrackedFieldCount => AllTrackedFields.Count;

	/// <summary>
	/// Gets whether this component has any trackable fields.
	/// </summary>
	public bool HasTrackableFields => TrackedFieldCount > 0;

	/// <summary>
	/// Gets the component-level state tracking options, if any.
	/// </summary>
	public StateTrackingOptionsAttribute? ComponentOptions { get; }

	/// <summary>
	/// Gets whether state tracking is completely disabled for this component.
	/// </summary>
	public bool IsStateTrackingDisabled { get; }

	/// <summary>
	/// Gets the timestamp when this metadata was created.
	/// </summary>
	public DateTime CreatedAt { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="StateFieldMetadata"/> class.
	/// </summary>
	/// <param name="componentType">The component type.</param>
	/// <param name="autoTrackedFields">Fields that are automatically tracked.</param>
	/// <param name="explicitlyTrackedFields">Fields that are explicitly tracked.</param>
	/// <param name="ignoredFields">Fields that are explicitly ignored.</param>
	/// <param name="componentOptions">Component-level tracking options.</param>
	/// <param name="isStateTrackingDisabled">Whether state tracking is disabled.</param>
	public StateFieldMetadata(
		Type componentType,
		IReadOnlyList<FieldTrackingInfo> autoTrackedFields,
		IReadOnlyList<FieldTrackingInfo> explicitlyTrackedFields,
		IReadOnlyList<FieldTrackingInfo> ignoredFields,
		StateTrackingOptionsAttribute? componentOptions,
		bool isStateTrackingDisabled
	)
	{
		ComponentType = componentType;
		AutoTrackedFields = autoTrackedFields;
		ExplicitlyTrackedFields = explicitlyTrackedFields;
		IgnoredFields = ignoredFields;
		ComponentOptions = componentOptions;
		IsStateTrackingDisabled = isStateTrackingDisabled;
		CreatedAt = DateTime.UtcNow;

		var allFields = new List<FieldTrackingInfo>();
		allFields.AddRange(autoTrackedFields);
		allFields.AddRange(explicitlyTrackedFields);
		AllTrackedFields = allFields.AsReadOnly();
	}

	/// <summary>
	/// Gets tracking information for a specific field by name.
	/// </summary>
	/// <param name="fieldName">The name of the field.</param>
	/// <returns>The tracking information, or null if the field is not tracked.</returns>
	public FieldTrackingInfo? GetFieldTrackingInfo(string fieldName)
	{
		foreach (var field in AllTrackedFields)
			if (field.FieldInfo.Name == fieldName)
				return field;

		return null;
	}
}
