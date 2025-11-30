using System;

namespace Blazor.WhyDidYouRender.Records;

/// <summary>
/// Represents the results of a comprehensive state tracking benchmark.
/// This record provides immutable performance measurement data.
/// </summary>
public record BenchmarkResults
{
	/// <summary>
	/// Gets the time taken for field analysis operations.
	/// </summary>
	public required TimeSpan FieldAnalysisTime { get; init; }

	/// <summary>
	/// Gets the time taken for snapshot creation operations.
	/// </summary>
	public required TimeSpan SnapshotCreationTime { get; init; }

	/// <summary>
	/// Gets the time taken for state comparison operations.
	/// </summary>
	public required TimeSpan StateComparisonTime { get; init; }

	/// <summary>
	/// Gets the time taken for change detection operations.
	/// </summary>
	public required TimeSpan ChangeDetectionTime { get; init; }

	/// <summary>
	/// Gets the total time for all operations.
	/// </summary>
	public required TimeSpan TotalTime { get; init; }

	/// <summary>
	/// Gets a formatted summary of the benchmark results.
	/// </summary>
	/// <returns>A formatted string with benchmark results.</returns>
	public string GetFormattedSummary()
	{
		return $"State Tracking Benchmark Results:\n"
			+ $"  Field Analysis: {FieldAnalysisTime.TotalMilliseconds:F2}ms\n"
			+ $"  Snapshot Creation: {SnapshotCreationTime.TotalMilliseconds:F2}ms\n"
			+ $"  State Comparison: {StateComparisonTime.TotalMilliseconds:F2}ms\n"
			+ $"  Change Detection: {ChangeDetectionTime.TotalMilliseconds:F2}ms\n"
			+ $"  Total Time: {TotalTime.TotalMilliseconds:F2}ms";
	}
}
