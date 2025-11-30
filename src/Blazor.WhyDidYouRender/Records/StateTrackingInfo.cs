using System;
using System.Collections.Generic;

namespace Blazor.WhyDidYouRender.Records;

/// <summary>
/// Contains information about state tracking for a specific component.
/// </summary>
public class StateTrackingInfo
{
	/// <summary>
	/// Gets or sets the component type.
	/// </summary>
	public Type ComponentType { get; set; } = null!;

	/// <summary>
	/// Gets or sets whether state tracking is enabled for this component.
	/// </summary>
	public bool IsStateTrackingEnabled { get; set; }

	/// <summary>
	/// Gets or sets the number of fields being tracked.
	/// </summary>
	public int TrackedFieldCount { get; set; }

	/// <summary>
	/// Gets or sets the list of automatically tracked field names.
	/// </summary>
	public List<string> AutoTrackedFields { get; set; } = new();

	/// <summary>
	/// Gets or sets the list of explicitly tracked field names.
	/// </summary>
	public List<string> ExplicitlyTrackedFields { get; set; } = new();

	/// <summary>
	/// Gets or sets the list of ignored field names.
	/// </summary>
	public List<string> IgnoredFields { get; set; } = new();

	/// <summary>
	/// Gets or sets whether the component has a current state snapshot.
	/// </summary>
	public bool HasCurrentSnapshot { get; set; }

	/// <summary>
	/// Gets or sets the timestamp of the last snapshot, if any.
	/// </summary>
	public DateTime? LastSnapshotTime { get; set; }

	/// <summary>
	/// Gets a formatted summary of the state tracking information.
	/// </summary>
	/// <returns>A formatted string describing the state tracking configuration.</returns>
	public string GetFormattedSummary()
	{
		if (!IsStateTrackingEnabled)
		{
			return $"{ComponentType.Name}: State tracking disabled";
		}

		var parts = new List<string> { $"{ComponentType.Name}: {TrackedFieldCount} fields tracked" };

		if (AutoTrackedFields.Count > 0)
		{
			parts.Add($"{AutoTrackedFields.Count} auto-tracked");
		}

		if (ExplicitlyTrackedFields.Count > 0)
		{
			parts.Add($"{ExplicitlyTrackedFields.Count} explicit");
		}

		if (IgnoredFields.Count > 0)
		{
			parts.Add($"{IgnoredFields.Count} ignored");
		}

		if (HasCurrentSnapshot && LastSnapshotTime.HasValue)
		{
			var age = DateTime.UtcNow - LastSnapshotTime.Value;
			parts.Add($"Last snapshot: {age.TotalSeconds:F1}s ago");
		}

		return string.Join(", ", parts);
	}
}

/// <summary>
/// Enhanced statistics for unnecessary re-render detection including state tracking information.
/// </summary>
public class EnhancedUnnecessaryRerenderStatistics
{
	/// <summary>
	/// Gets or sets the number of components being tracked (legacy tracking).
	/// </summary>
	public int TrackedComponents { get; set; }

	/// <summary>
	/// Gets or sets whether unnecessary re-render detection is enabled.
	/// </summary>
	public bool IsEnabled { get; set; }

	/// <summary>
	/// Gets or sets whether state tracking is enabled.
	/// </summary>
	public bool IsStateTrackingEnabled { get; set; }

	/// <summary>
	/// Gets or sets the size of the field analyzer cache.
	/// </summary>
	public int FieldAnalyzerCacheSize { get; set; }

	/// <summary>
	/// Gets or sets the state tracking statistics from the snapshot manager.
	/// </summary>
	public Dictionary<string, object>? StateTrackingStatistics { get; set; }

	/// <summary>
	/// Gets a formatted summary of the enhanced statistics.
	/// </summary>
	/// <returns>A formatted string describing the statistics.</returns>
	public string GetFormattedSummary()
	{
		var parts = new List<string>
		{
			$"Unnecessary Re-render Detection: {(IsEnabled ? "Enabled" : "Disabled")}",
			$"Legacy Tracked Components: {TrackedComponents}",
			$"State Tracking: {(IsStateTrackingEnabled ? "Enabled" : "Disabled")}",
		};

		if (IsStateTrackingEnabled)
		{
			parts.Add($"Field Analyzer Cache Size: {FieldAnalyzerCacheSize}");

			if (StateTrackingStatistics != null)
			{
				foreach (var kvp in StateTrackingStatistics)
				{
					parts.Add($"{kvp.Key}: {kvp.Value}");
				}
			}
		}

		return string.Join("\n", parts);
	}
}
