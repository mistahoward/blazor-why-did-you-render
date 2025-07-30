using System.Reflection;

using Blazor.WhyDidYouRender.Helpers;
using Blazor.WhyDidYouRender.Records;

namespace Blazor.WhyDidYouRender.Attributes;

/// <summary>
/// Provides validation services for state tracking attributes.
/// This class helps ensure that attributes are correctly configured and compatible
/// with their target fields, properties, and components.
/// </summary>
internal static class AttributeValidator {
	/// <summary>
	/// Validates all state tracking attributes on a given component type.
	/// </summary>
	/// <param name="componentType">The component type to validate.</param>
	/// <returns>A collection of validation errors, empty if all attributes are valid.</returns>
	public static List<string> ValidateComponentAttributes(Type componentType) {
		List<string> errors = [];

		errors.AddRange(ValidateComponentLevelAttributes(componentType));
		errors.AddRange(ValidateFieldAndPropertyAttributes(componentType));

		return errors;
	}

	/// <summary>
	/// Validates component-level state tracking attributes.
	/// </summary>
	/// <param name="componentType">The component type to validate.</param>
	/// <returns>A collection of validation errors.</returns>
	private static List<string> ValidateComponentLevelAttributes(Type componentType) {
		var errors = new List<string>();

		var ignoreStateTracking = componentType.GetCustomAttribute<IgnoreStateTrackingAttribute>();
		var stateTrackingOptions = componentType.GetCustomAttribute<StateTrackingOptionsAttribute>();

		if (ignoreStateTracking != null && stateTrackingOptions != null) {
			if (stateTrackingOptions.EnableStateTracking == true)
				errors.Add($"Component {componentType.Name} has conflicting attributes: " +
						  "IgnoreStateTrackingAttribute and StateTrackingOptionsAttribute with EnableStateTracking=true");
		}

		if (stateTrackingOptions != null) {
			var optionsErrors = stateTrackingOptions.Validate();
			errors.AddRange(optionsErrors.Select(error => $"Component {componentType.Name}: {error}"));
		}

		return errors;
	}

	/// <summary>
	/// Validates field and property level state tracking attributes.
	/// </summary>
	/// <param name="componentType">The component type to validate.</param>
	/// <returns>A collection of validation errors.</returns>
	private static List<string> ValidateFieldAndPropertyAttributes(Type componentType) {
		var errors = new List<string>();

		var fields = componentType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
		var properties = componentType.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

		foreach (var field in fields)
			errors.AddRange(ValidateMemberAttributes(field, field.FieldType, componentType.Name));

		foreach (var property in properties)
			errors.AddRange(ValidateMemberAttributes(property, property.PropertyType, componentType.Name));

		return errors;
	}

	/// <summary>
	/// Validates attributes on a specific member and returns any validation errors.
	/// </summary>
	/// <param name="member">The member (field or property) to validate.</param>
	/// <param name="memberType">The type of the member.</param>
	/// <param name="componentName">The name of the component containing this member.</param>
	/// <returns>A list of validation error messages.</returns>
	private static List<string> ValidateMemberAttributes(MemberInfo member, Type memberType, string componentName) =>
		[.. Validate(member, memberType, componentName)];

	/// <summary>
	/// Performs detailed validation of state tracking attributes on a member.
	/// </summary>
	/// <param name="member">The member to validate.</param>
	/// <param name="memberType">The type of the member.</param>
	/// <param name="componentName">The name of the component containing this member.</param>
	/// <returns>An enumerable of validation error messages.</returns>
	/// <remarks>
	/// Validates:
	/// - Attribute combinations (TrackState vs IgnoreState)
	/// - Type compatibility with TrackState settings
	/// - Static/readonly field restrictions
	/// - Property accessibility requirements
	/// </remarks>
	private static IEnumerable<string> Validate(MemberInfo member, Type memberType, string componentName) {
		var trackState = member.GetCustomAttribute<TrackStateAttribute>();
		var ignoreState = member.GetCustomAttribute<IgnoreStateAttribute>();

		// rule 1: Cannot have both attributes.
		if (trackState != null && ignoreState != null)
			yield return $"Component {componentName}, member {member.Name}: Cannot have both TrackStateAttribute and IgnoreStateAttribute";

		// rules for when [TrackState] is present.
		if (trackState != null) {
			foreach (var error in trackState.Validate())
				yield return $"Component {componentName}, member {member.Name}: {error}";

			if (!trackState.IsValidForType(memberType))
				if (TypeHelper.IsSimpleValueType(memberType))
					yield return $"Component {componentName}, member {member.Name}: TrackStateAttribute is not needed for simple value types (they are auto-tracked)";
				else if (trackState.TrackCollectionContents && !TypeHelper.IsCollectionType(memberType))
					yield return $"Component {componentName}, member {member.Name}: TrackCollectionContents=true can only be used with collection types";
		}

		// a member is tracked if it has [TrackState] OR it's a simple type without [IgnoreState].
		bool isMemberTracked = trackState != null || (ignoreState == null && TypeHelper.IsSimpleValueType(memberType));

		if (!isMemberTracked)
			// if not tracked, none of the following rules apply.
			yield break;

		// rules for members that are being tracked (either explicitly or implicitly).
		if (member is FieldInfo field) {
			if (field.IsStatic)
				yield return $"Component {componentName}, field {member.Name}: Static fields cannot be tracked for state changes";

			if (field.IsInitOnly)
				yield return $"Component {componentName}, field {member.Name}: Readonly fields should not be tracked (they cannot change after initialization)";
		}
		else if (member is PropertyInfo property) {
			if (!property.CanRead)
				yield return $"Component {componentName}, property {member.Name}: Write-only properties cannot be tracked for state changes";

			var getMethod = property.GetGetMethod(true);
			if (getMethod?.IsStatic == true)
				yield return $"Component {componentName}, property {member.Name}: Static properties cannot be tracked for state changes";
		}
	}

	/// <summary>
	/// Gets a summary of all state tracking attributes on a component type.
	/// </summary>
	/// <param name="componentType">The component type to analyze.</param>
	/// <returns>A summary of the attribute configuration.</returns>
	public static AttributeSummary GetAttributeSummary(Type componentType) {
		var summary = new AttributeSummary {
			ComponentType = componentType,
			HasIgnoreStateTracking = componentType.GetCustomAttribute<IgnoreStateTrackingAttribute>() != null,
			StateTrackingOptions = componentType.GetCustomAttribute<StateTrackingOptionsAttribute>()
		};

		var fields = componentType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
		var properties = componentType.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

		foreach (var field in fields)
			AnalyzeMemberAttributes(field, field.FieldType, summary);

		foreach (var property in properties)
			AnalyzeMemberAttributes(property, property.PropertyType, summary);

		return summary;
	}

	/// <summary>
	/// Analyzes attributes on a specific member and updates the summary.
	/// </summary>
	/// <param name="member">The member to analyze.</param>
	/// <param name="memberType">The type of the member.</param>
	/// <param name="summary">The summary to update.</param>
	private static void AnalyzeMemberAttributes(MemberInfo member, Type memberType, AttributeSummary summary) {
		var trackState = member.GetCustomAttribute<TrackStateAttribute>();
		var ignoreState = member.GetCustomAttribute<IgnoreStateAttribute>();

		if (trackState != null)
			summary.ExplicitlyTrackedMembers.Add(member.Name);
		else if (ignoreState != null)
			summary.ExplicitlyIgnoredMembers.Add(member.Name);
		else if (TypeHelper.IsSimpleValueType(memberType))
			summary.AutoTrackedMembers.Add(member.Name);
	}

}
