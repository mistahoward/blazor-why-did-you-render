using System.Collections.Generic;
using Blazor.WhyDidYouRender.Core.StateTracking;
using Blazor.WhyDidYouRender.Records.StateTracking;

namespace Blazor.WhyDidYouRender.Records;

/// <summary>
/// Diagnostic information about the state tracking system.
/// </summary>
public record StateTrackingDiagnostics
{
	/// <summary>
	/// Gets whether state tracking is enabled in configuration.
	/// </summary>
	public required bool IsEnabled { get; init; }

	/// <summary>
	/// Gets whether the lazy provider has been initialized.
	/// </summary>
	public required bool IsInitialized { get; init; }

	/// <summary>
	/// Gets whether the field analyzer has been created.
	/// </summary>
	public required bool FieldAnalyzerInitialized { get; init; }

	/// <summary>
	/// Gets whether the state comparer has been created.
	/// </summary>
	public required bool StateComparerInitialized { get; init; }

	/// <summary>
	/// Gets whether the snapshot manager has been created.
	/// </summary>
	public required bool SnapshotManagerInitialized { get; init; }

	/// <summary>
	/// Gets whether the performance monitor has been created.
	/// </summary>
	public required bool PerformanceMonitorInitialized { get; init; }

	/// <summary>
	/// Gets cache information if available.
	/// </summary>
	public CacheInfo? CacheInfo { get; init; }

	/// <summary>
	/// Gets performance summary if available.
	/// </summary>
	public PerformanceSummary? PerformanceSummary { get; init; }

	/// <summary>
	/// Gets a formatted summary of the diagnostic information.
	/// </summary>
	/// <returns>A formatted string with diagnostic details.</returns>
	public string GetFormattedSummary()
	{
		var lines = new List<string>
		{
			$"State Tracking Enabled: {IsEnabled}",
			$"Provider Initialized: {IsInitialized}",
			$"Field Analyzer: {(FieldAnalyzerInitialized ? "Initialized" : "Not Initialized")}",
			$"State Comparer: {(StateComparerInitialized ? "Initialized" : "Not Initialized")}",
			$"Snapshot Manager: {(SnapshotManagerInitialized ? "Initialized" : "Not Initialized")}",
			$"Performance Monitor: {(PerformanceMonitorInitialized ? "Initialized" : "Not Initialized")}",
		};

		if (CacheInfo != null)
		{
			lines.Add($"Cache Entries: {CacheInfo.TotalEntries}");
			lines.Add($"Cache Hit Ratio: {CacheInfo.Statistics.HitRatio:P2}");
		}

		if (PerformanceSummary != null)
		{
			lines.Add($"Total Operations: {PerformanceSummary.TotalOperations}");
			lines.Add($"Average Operation Time: {PerformanceSummary.AverageOperationTime.TotalMilliseconds:F2}ms");
		}

		return string.Join("\n", lines);
	}
}
