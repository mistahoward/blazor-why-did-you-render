using System;
using System.Collections.Generic;
using System.Linq;

namespace Blazor.WhyDidYouRender.Records.StateTracking;

/// <summary>
/// Represents a comprehensive performance summary for all state tracking operations.
/// This record provides immutable aggregate performance data and analysis.
/// </summary>
public record PerformanceSummary
{
	/// <summary>
	/// Gets the total number of operations performed across all operation types.
	/// </summary>
	public long TotalOperations { get; init; }

	/// <summary>
	/// Gets the total number of successful operations across all operation types.
	/// </summary>
	public long TotalSuccessfulOperations { get; init; }

	/// <summary>
	/// Gets the total number of failed operations across all operation types.
	/// </summary>
	public long TotalFailedOperations { get; init; }

	/// <summary>
	/// Gets the average operation time across all successful operations.
	/// </summary>
	public TimeSpan AverageOperationTime { get; init; }

	/// <summary>
	/// Gets the operation with the slowest average time.
	/// </summary>
	public OperationMetrics? SlowestOperation { get; init; }

	/// <summary>
	/// Gets the operation with the fastest average time.
	/// </summary>
	public OperationMetrics? FastestOperation { get; init; }

	/// <summary>
	/// Gets the detailed metrics for each operation type.
	/// </summary>
	public IReadOnlyDictionary<string, OperationMetrics> OperationMetrics { get; init; } = new Dictionary<string, OperationMetrics>();

	/// <summary>
	/// Gets the number of recent operation timings available for analysis.
	/// </summary>
	public int RecentTimingsCount { get; init; }

	/// <summary>
	/// Gets the time when performance monitoring started.
	/// </summary>
	public DateTime MonitoringStartTime { get; init; }

	/// <summary>
	/// Gets the overall success rate across all operations.
	/// </summary>
	public double OverallSuccessRate => TotalOperations > 0 ? (double)TotalSuccessfulOperations / TotalOperations : 0.0;

	/// <summary>
	/// Gets the overall failure rate across all operations.
	/// </summary>
	public double OverallFailureRate => TotalOperations > 0 ? (double)TotalFailedOperations / TotalOperations : 0.0;

	/// <summary>
	/// Gets the total monitoring duration.
	/// </summary>
	public TimeSpan MonitoringDuration => DateTime.UtcNow - MonitoringStartTime;

	/// <summary>
	/// Gets the average operations per second across the monitoring period.
	/// </summary>
	public double OperationsPerSecond => MonitoringDuration.TotalSeconds > 0 ? TotalOperations / MonitoringDuration.TotalSeconds : 0.0;

	/// <summary>
	/// Gets the number of different operation types being tracked.
	/// </summary>
	public int OperationTypeCount => OperationMetrics.Count;

	/// <summary>
	/// Gets whether there are any performance issues detected.
	/// </summary>
	public bool HasPerformanceIssues =>
		OverallFailureRate > 0.05
		|| AverageOperationTime > TimeSpan.FromMilliseconds(100)
		|| OperationMetrics.Values.Any(m => m.HasPerformanceIssues);

	/// <summary>
	/// Gets the operations with the highest failure rates.
	/// </summary>
	public IEnumerable<OperationMetrics> ProblematicOperations =>
		OperationMetrics
			.Values.Where(m => m.HasPerformanceIssues)
			.OrderByDescending(m => m.FailureRate)
			.ThenByDescending(m => m.AverageTime);

	/// <summary>
	/// Gets the top performing operations by success rate and speed.
	/// </summary>
	public IEnumerable<OperationMetrics> TopPerformingOperations =>
		OperationMetrics.Values.Where(m => !m.HasPerformanceIssues).OrderByDescending(m => m.SuccessRate).ThenBy(m => m.AverageTime);

	/// <summary>
	/// Gets a formatted summary of the performance data.
	/// </summary>
	/// <returns>A formatted string with comprehensive performance information.</returns>
	public string GetFormattedSummary()
	{
		var summary =
			$"Performance Summary (Monitoring Duration: {MonitoringDuration}):\n"
			+ $"  Total Operations: {TotalOperations:N0}\n"
			+ $"  Success Rate: {OverallSuccessRate:P2}\n"
			+ $"  Failure Rate: {OverallFailureRate:P2}\n"
			+ $"  Average Time: {AverageOperationTime.TotalMilliseconds:F2}ms\n"
			+ $"  Operations/sec: {OperationsPerSecond:F2}\n"
			+ $"  Operation Types: {OperationTypeCount}\n"
			+ $"  Recent Timings: {RecentTimingsCount:N0}\n";

		if (SlowestOperation != null)
			summary +=
				$"  Slowest Operation: {SlowestOperation.OperationName} ({SlowestOperation.AverageTime.TotalMilliseconds:F2}ms avg)\n";

		if (FastestOperation != null)
			summary +=
				$"  Fastest Operation: {FastestOperation.OperationName} ({FastestOperation.AverageTime.TotalMilliseconds:F2}ms avg)\n";

		summary += $"  Performance Issues: {(HasPerformanceIssues ? "Yes" : "No")}";

		return summary;
	}

	/// <summary>
	/// Creates an empty performance summary for when no operations have been recorded.
	/// </summary>
	/// <returns>An empty performance summary.</returns>
	public static PerformanceSummary Empty() => new() { MonitoringStartTime = DateTime.UtcNow };
}
