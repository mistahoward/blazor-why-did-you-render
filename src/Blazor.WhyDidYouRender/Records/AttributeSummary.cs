using System;
using System.Collections.Generic;
using Blazor.WhyDidYouRender.Attributes;

namespace Blazor.WhyDidYouRender.Records;

/// <summary>
/// Represents a comprehensive summary of state tracking attributes and configuration for a component.
/// This record provides immutable information about how state tracking is configured at the component level.
/// </summary>
/// <remarks>
/// AttributeSummary aggregates all state tracking configuration for a component including:
/// - Component-level attributes like [IgnoreStateTracking] and [StateTrackingOptions]
/// - Member-level tracking configuration (auto-tracked, explicitly tracked, ignored)
/// - Summary statistics about tracking coverage
///
/// This information is used for validation, diagnostics, and optimization of state tracking behavior.
/// </remarks>
public record AttributeSummary
{
	/// <summary>
	/// Gets the component type that this summary describes.
	/// </summary>
	public required Type ComponentType { get; init; }

	/// <summary>
	/// Gets whether the component has the [IgnoreStateTracking] attribute applied.
	/// When true, state tracking is completely disabled for this component.
	/// </summary>
	public bool HasIgnoreStateTracking { get; init; }

	/// <summary>
	/// Gets the [StateTrackingOptions] attribute if present on the component.
	/// This attribute provides fine-grained control over state tracking behavior.
	/// </summary>
	public StateTrackingOptionsAttribute? StateTrackingOptions { get; init; }

	/// <summary>
	/// Gets the list of member names that are automatically tracked.
	/// These are typically simple value types that don't require explicit attributes.
	/// </summary>
	public List<string> AutoTrackedMembers { get; init; } = [];

	/// <summary>
	/// Gets the list of member names that are explicitly tracked via [TrackState] attribute.
	/// These are typically complex types that require explicit opt-in for tracking.
	/// </summary>
	public List<string> ExplicitlyTrackedMembers { get; init; } = [];

	/// <summary>
	/// Gets the list of member names that are explicitly ignored via [IgnoreState] attribute.
	/// These members will not be tracked even if they would normally be auto-tracked.
	/// </summary>
	public List<string> ExplicitlyIgnoredMembers { get; init; } = [];

	/// <summary>
	/// Gets the total number of members that will be tracked (auto + explicit).
	/// </summary>
	public int TotalTrackedMembers => AutoTrackedMembers.Count + ExplicitlyTrackedMembers.Count;

	/// <summary>
	/// Gets the total number of members analyzed for this component.
	/// </summary>
	public int TotalMembers => AutoTrackedMembers.Count + ExplicitlyTrackedMembers.Count + ExplicitlyIgnoredMembers.Count;

	/// <summary>
	/// Gets the percentage of analyzed members that will be tracked.
	/// </summary>
	public double TrackingCoverage => TotalMembers > 0 ? (double)TotalTrackedMembers / TotalMembers * 100 : 0.0;

	/// <summary>
	/// Gets whether state tracking is effectively enabled for this component.
	/// </summary>
	public bool IsStateTrackingEnabled => !HasIgnoreStateTracking && TotalTrackedMembers > 0;

	/// <summary>
	/// Gets whether this component has any explicit tracking configuration.
	/// </summary>
	public bool HasExplicitConfiguration =>
		StateTrackingOptions != null || ExplicitlyTrackedMembers.Count > 0 || ExplicitlyIgnoredMembers.Count > 0;

	/// <summary>
	/// Gets a brief description of the tracking configuration.
	/// </summary>
	public string ConfigurationSummary =>
		HasIgnoreStateTracking ? "State tracking disabled" : $"{TotalTrackedMembers} tracked members ({TrackingCoverage:F1}% coverage)";

	/// <summary>
	/// Gets a formatted summary of the attribute configuration.
	/// </summary>
	/// <returns>A formatted string with comprehensive attribute information.</returns>
	public string GetFormattedSummary()
	{
		var summary =
			$"Attribute Summary for {ComponentType.Name}:\n"
			+ $"  State Tracking Enabled: {IsStateTrackingEnabled}\n"
			+ $"  Has Ignore Attribute: {HasIgnoreStateTracking}\n"
			+ $"  Has Options Attribute: {StateTrackingOptions != null}\n"
			+ $"  Total Members: {TotalMembers}\n"
			+ $"  Tracked Members: {TotalTrackedMembers}\n"
			+ $"  Tracking Coverage: {TrackingCoverage:F1}%\n";

		if (AutoTrackedMembers.Count > 0)
		{
			summary += $"  Auto-Tracked ({AutoTrackedMembers.Count}): {string.Join(", ", AutoTrackedMembers)}\n";
		}

		if (ExplicitlyTrackedMembers.Count > 0)
		{
			summary += $"  Explicitly Tracked ({ExplicitlyTrackedMembers.Count}): {string.Join(", ", ExplicitlyTrackedMembers)}\n";
		}

		if (ExplicitlyIgnoredMembers.Count > 0)
		{
			summary += $"  Explicitly Ignored ({ExplicitlyIgnoredMembers.Count}): {string.Join(", ", ExplicitlyIgnoredMembers)}\n";
		}

		if (StateTrackingOptions != null)
		{
			summary += $"  Options: {StateTrackingOptions.Description ?? "Custom configuration"}\n";
		}

		return summary;
	}
}
