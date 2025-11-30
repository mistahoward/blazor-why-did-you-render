using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Blazor.WhyDidYouRender.Records.StateTracking;

/// <summary>
/// Represents comprehensive performance metrics for a specific operation in state tracking.
/// This record provides immutable performance data including timing, success rates, and failure analysis.
/// </summary>
public record OperationMetrics
{
	/// <summary>
	/// Gets the name of the operation being tracked.
	/// </summary>
	public required string OperationName { get; init; }

	/// <summary>
	/// Gets the time when the first operation was recorded.
	/// </summary>
	public DateTime FirstOperationTime { get; init; } = DateTime.MaxValue;

	/// <summary>
	/// Gets the time when the last operation was recorded.
	/// </summary>
	public DateTime LastOperationTime { get; init; } = DateTime.MinValue;

	/// <summary>
	/// Gets the total number of operations performed.
	/// </summary>
	public long TotalOperations { get; init; }

	/// <summary>
	/// Gets the number of successful operations.
	/// </summary>
	public long SuccessfulOperations { get; init; }

	/// <summary>
	/// Gets the number of failed operations.
	/// </summary>
	public long FailedOperations { get; init; }

	/// <summary>
	/// Gets the total time spent in successful operations (in ticks).
	/// </summary>
	public long TotalTimeTicks { get; init; }

	/// <summary>
	/// Gets the minimum operation time (in ticks).
	/// </summary>
	public long MinTimeTicks { get; init; } = long.MaxValue;

	/// <summary>
	/// Gets the maximum operation time (in ticks).
	/// </summary>
	public long MaxTimeTicks { get; init; }

	/// <summary>
	/// Gets the failure rate as a percentage (0.0 to 1.0).
	/// </summary>
	public double FailureRate => TotalOperations > 0 ? (double)FailedOperations / TotalOperations : 0.0;

	/// <summary>
	/// Gets the success rate as a percentage (0.0 to 1.0).
	/// </summary>
	public double SuccessRate => TotalOperations > 0 ? (double)SuccessfulOperations / TotalOperations : 0.0;

	/// <summary>
	/// Gets the average operation time for successful operations.
	/// </summary>
	public TimeSpan AverageTime => SuccessfulOperations > 0 ? TimeSpan.FromTicks(TotalTimeTicks / SuccessfulOperations) : TimeSpan.Zero;

	/// <summary>
	/// Gets the minimum operation time.
	/// </summary>
	public TimeSpan MinTime => MinTimeTicks == long.MaxValue ? TimeSpan.Zero : TimeSpan.FromTicks(MinTimeTicks);

	/// <summary>
	/// Gets the maximum operation time.
	/// </summary>
	public TimeSpan MaxTime => TimeSpan.FromTicks(MaxTimeTicks);

	/// <summary>
	/// Gets the total duration of the monitoring period.
	/// </summary>
	public TimeSpan MonitoringDuration => LastOperationTime > FirstOperationTime ? LastOperationTime - FirstOperationTime : TimeSpan.Zero;

	/// <summary>
	/// Gets the average operations per second during the monitoring period.
	/// </summary>
	public double OperationsPerSecond => MonitoringDuration.TotalSeconds > 0 ? TotalOperations / MonitoringDuration.TotalSeconds : 0.0;

	/// <summary>
	/// Gets whether this operation has performance issues based on common thresholds.
	/// </summary>
	public bool HasPerformanceIssues => FailureRate > 0.05 || AverageTime > TimeSpan.FromMilliseconds(100);

	/// <summary>
	/// Gets a formatted summary of the operation metrics.
	/// </summary>
	/// <returns>A formatted string with operation performance information.</returns>
	public string GetFormattedSummary()
	{
		return $"Operation Metrics for '{OperationName}':\n"
			+ $"  Total Operations: {TotalOperations:N0}\n"
			+ $"  Success Rate: {SuccessRate:P2}\n"
			+ $"  Failure Rate: {FailureRate:P2}\n"
			+ $"  Average Time: {AverageTime.TotalMilliseconds:F2}ms\n"
			+ $"  Min Time: {MinTime.TotalMilliseconds:F2}ms\n"
			+ $"  Max Time: {MaxTime.TotalMilliseconds:F2}ms\n"
			+ $"  Operations/sec: {OperationsPerSecond:F2}\n"
			+ $"  Monitoring Duration: {MonitoringDuration}\n"
			+ $"  Performance Issues: {(HasPerformanceIssues ? "Yes" : "No")}";
	}
}
