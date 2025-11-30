using System;
using System.Collections.Generic;

namespace Blazor.WhyDidYouRender.Records.StateTracking;

/// <summary>
/// Represents detailed information about the current state of thread-safe operations.
/// This record provides immutable snapshot data about threading activity and resource usage.
/// </summary>
/// <remarks>
/// ThreadingInfo provides a comprehensive view of the current threading state including
/// active components, locks, threads, and concurrency levels. This information is useful
/// for monitoring, debugging, and performance analysis of thread-safe operations.
/// </remarks>
public record ThreadingInfo
{
	/// <summary>
	/// Gets the number of components currently being tracked.
	/// </summary>
	public int TrackedComponents { get; init; }

	/// <summary>
	/// Gets the number of active locks currently held.
	/// </summary>
	public int ActiveLocks { get; init; }

	/// <summary>
	/// Gets the number of distinct threads currently involved in operations.
	/// </summary>
	public int ActiveThreads { get; init; }

	/// <summary>
	/// Gets the number of available concurrency slots.
	/// </summary>
	public int AvailableConcurrency { get; init; }

	/// <summary>
	/// Gets the maximum number of concurrent operations allowed.
	/// </summary>
	public int MaxConcurrency { get; init; }

	/// <summary>
	/// Gets the comprehensive threading statistics.
	/// </summary>
	public ThreadingStatistics Statistics { get; init; } = ThreadingStatistics.Empty();

	/// <summary>
	/// Gets the timestamp when this information was captured.
	/// </summary>
	public DateTime CapturedAt { get; init; } = DateTime.UtcNow;

	/// <summary>
	/// Gets additional context information about the threading state.
	/// </summary>
	public string? Context { get; init; }

	/// <summary>
	/// Gets the number of concurrent operations currently in use.
	/// </summary>
	public int UsedConcurrency => MaxConcurrency - AvailableConcurrency;

	/// <summary>
	/// Gets the concurrency utilization as a percentage (0.0 to 1.0).
	/// </summary>
	public double ConcurrencyUtilization => MaxConcurrency > 0 ? (double)UsedConcurrency / MaxConcurrency : 0.0;

	/// <summary>
	/// Gets the ratio of active locks to tracked components.
	/// </summary>
	public double LockToComponentRatio => TrackedComponents > 0 ? (double)ActiveLocks / TrackedComponents : 0.0;

	/// <summary>
	/// Gets the average components per active thread.
	/// </summary>
	public double ComponentsPerThread => ActiveThreads > 0 ? (double)TrackedComponents / ActiveThreads : 0.0;

	/// <summary>
	/// Gets whether the threading system is under high load.
	/// </summary>
	public bool IsUnderHighLoad =>
		ConcurrencyUtilization > 0.8
		|| // More than 80% concurrency used
		LockToComponentRatio > 2.0; // More than 2 locks per component

	/// <summary>
	/// Gets whether there are potential concurrency issues.
	/// </summary>
	public bool HasConcurrencyIssues =>
		AvailableConcurrency == 0
		|| // No available concurrency
		ActiveLocks > MaxConcurrency * 3; // Excessive locks

	/// <summary>
	/// Gets whether the threading state appears healthy.
	/// </summary>
	public bool IsHealthy => !IsUnderHighLoad && !HasConcurrencyIssues && Statistics.IsHealthy;

	/// <summary>
	/// Gets the age of this threading information.
	/// </summary>
	public TimeSpan Age => DateTime.UtcNow - CapturedAt;

	/// <summary>
	/// Gets a brief status description of the threading state.
	/// </summary>
	public string Status =>
		IsHealthy ? "Healthy"
		: IsUnderHighLoad ? "High Load"
		: HasConcurrencyIssues ? "Concurrency Issues"
		: "Degraded";

	/// <summary>
	/// Gets the threading efficiency rating.
	/// </summary>
	public string EfficiencyRating =>
		ConcurrencyUtilization switch
		{
			< 0.3 => "Underutilized",
			< 0.6 => "Optimal",
			< 0.8 => "High",
			< 0.95 => "Very High",
			_ => "Saturated",
		};

	/// <summary>
	/// Creates empty threading information.
	/// </summary>
	/// <returns>Empty ThreadingInfo instance.</returns>
	public static ThreadingInfo Empty() => new();

	/// <summary>
	/// Creates threading information with basic metrics.
	/// </summary>
	/// <param name="trackedComponents">Number of tracked components.</param>
	/// <param name="activeLocks">Number of active locks.</param>
	/// <param name="activeThreads">Number of active threads.</param>
	/// <param name="availableConcurrency">Available concurrency slots.</param>
	/// <param name="maxConcurrency">Maximum concurrency allowed.</param>
	/// <param name="statistics">Threading statistics.</param>
	/// <param name="context">Optional context information.</param>
	/// <returns>A new ThreadingInfo instance.</returns>
	public static ThreadingInfo Create(
		int trackedComponents,
		int activeLocks,
		int activeThreads,
		int availableConcurrency,
		int maxConcurrency,
		ThreadingStatistics? statistics = null,
		string? context = null
	) =>
		new()
		{
			TrackedComponents = trackedComponents,
			ActiveLocks = activeLocks,
			ActiveThreads = activeThreads,
			AvailableConcurrency = availableConcurrency,
			MaxConcurrency = maxConcurrency,
			Statistics = statistics ?? ThreadingStatistics.Empty(),
			Context = context,
		};

	/// <summary>
	/// Gets potential issues with the current threading state.
	/// </summary>
	/// <returns>A list of identified threading issues.</returns>
	public IReadOnlyList<string> GetPotentialIssues()
	{
		var issues = new List<string>();

		if (AvailableConcurrency == 0)
			issues.Add("All concurrency slots are in use - potential bottleneck");

		if (ConcurrencyUtilization > 0.9)
			issues.Add($"Very high concurrency utilization: {ConcurrencyUtilization:P1}");

		if (ActiveLocks > MaxConcurrency * 2)
			issues.Add($"High number of active locks: {ActiveLocks} (max concurrency: {MaxConcurrency})");

		if (LockToComponentRatio > 3.0)
			issues.Add($"High lock-to-component ratio: {LockToComponentRatio:F1}");

		if (ActiveThreads > Environment.ProcessorCount * 4)
			issues.Add($"High number of active threads: {ActiveThreads} (processors: {Environment.ProcessorCount})");

		if (!Statistics.IsHealthy)
			issues.Add($"Performance issues detected: {Statistics.PerformanceRating}");

		if (Age > TimeSpan.FromMinutes(5))
			issues.Add($"Threading information is stale: {Age}");

		return issues;
	}

	/// <summary>
	/// Gets a formatted summary of the threading information.
	/// </summary>
	/// <returns>A formatted string with comprehensive threading information.</returns>
	public string GetFormattedSummary()
	{
		var summary =
			$"Threading Information (Status: {Status}, Efficiency: {EfficiencyRating}):\n"
			+ $"  Captured At: {CapturedAt:HH:mm:ss.fff} (Age: {Age})\n"
			+ $"  Tracked Components: {TrackedComponents:N0}\n"
			+ $"  Active Locks: {ActiveLocks:N0}\n"
			+ $"  Active Threads: {ActiveThreads:N0}\n"
			+ $"  Concurrency: {UsedConcurrency}/{MaxConcurrency} ({ConcurrencyUtilization:P1})\n"
			+ $"  Lock/Component Ratio: {LockToComponentRatio:F2}\n"
			+ $"  Components/Thread: {ComponentsPerThread:F1}\n"
			+ $"  Performance: {Statistics.PerformanceRating}";

		if (!string.IsNullOrEmpty(Context))
		{
			summary += $"\n  Context: {Context}";
		}

		var issues = GetPotentialIssues();
		if (issues.Count > 0)
		{
			summary += $"\n  Issues ({issues.Count}):\n";
			foreach (var issue in issues)
			{
				summary += $"    • {issue}\n";
			}
		}

		return summary;
	}

	/// <summary>
	/// Creates a copy of this threading info with additional context.
	/// </summary>
	/// <param name="additionalContext">Additional context to append.</param>
	/// <returns>A new ThreadingInfo with updated context.</returns>
	public ThreadingInfo WithContext(string additionalContext)
	{
		var newContext = string.IsNullOrEmpty(Context) ? additionalContext : $"{Context}; {additionalContext}";

		return this with
		{
			Context = newContext,
		};
	}
}
