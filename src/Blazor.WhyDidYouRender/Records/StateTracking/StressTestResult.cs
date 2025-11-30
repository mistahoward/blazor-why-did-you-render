using System;

namespace Blazor.WhyDidYouRender.Records.StateTracking;

/// <summary>
/// Represents the results from a single stress test operation.
/// This class provides mutable information about stress test performance and outcomes.
/// </summary>
public class StressTestResult
{
	/// <summary>
	/// Gets the name of the stress test that was performed.
	/// </summary>
	public required string TestName { get; set; }

	/// <summary>
	/// Gets whether the stress test completed successfully.
	/// </summary>
	public bool Success { get; set; }

	/// <summary>
	/// Gets the total duration of the stress test.
	/// </summary>
	public TimeSpan Duration { get; set; }

	/// <summary>
	/// Gets the number of operations that were completed during the test.
	/// </summary>
	public int OperationsCompleted { get; set; }

	/// <summary>
	/// Gets the number of operations performed per second.
	/// </summary>
	public double OperationsPerSecond { get; set; }

	/// <summary>
	/// Gets the amount of memory used during the test in bytes.
	/// </summary>
	public long MemoryUsed { get; set; }

	/// <summary>
	/// Gets the error message if the test failed.
	/// </summary>
	public string? ErrorMessage { get; set; }

	/// <summary>
	/// Gets the exception that caused the test to fail, if any.
	/// </summary>
	public Exception? Exception { get; set; }

	/// <summary>
	/// Gets the start time of the stress test.
	/// </summary>
	public DateTime StartTime { get; init; } = DateTime.UtcNow;

	/// <summary>
	/// Gets the end time of the stress test.
	/// </summary>
	public DateTime EndTime => StartTime + Duration;

	/// <summary>
	/// Gets the memory used in megabytes.
	/// </summary>
	public double MemoryUsedMB => MemoryUsed / (1024.0 * 1024.0);

	/// <summary>
	/// Gets the average time per operation.
	/// </summary>
	public TimeSpan AverageTimePerOperation =>
		OperationsCompleted > 0 ? TimeSpan.FromTicks(Duration.Ticks / OperationsCompleted) : TimeSpan.Zero;

	/// <summary>
	/// Gets whether the test performance is considered good based on common thresholds.
	/// </summary>
	public bool IsGoodPerformance => Success && OperationsPerSecond > 100 && AverageTimePerOperation < TimeSpan.FromMilliseconds(10);

	/// <summary>
	/// Gets whether the test had memory issues.
	/// </summary>
	public bool HasMemoryIssues => MemoryUsedMB > 100; // More than 100MB

	/// <summary>
	/// Gets a brief status description of the test result.
	/// </summary>
	public string Status => Success ? "PASSED" : "FAILED";

	/// <summary>
	/// Gets a performance rating based on operations per second.
	/// </summary>
	public string PerformanceRating =>
		OperationsPerSecond switch
		{
			> 1000 => "Excellent",
			> 500 => "Good",
			> 100 => "Fair",
			> 10 => "Poor",
			_ => "Very Poor",
		};

	/// <summary>
	/// Creates a successful stress test result.
	/// </summary>
	/// <param name="testName">The name of the test.</param>
	/// <param name="duration">How long the test took.</param>
	/// <param name="operationsCompleted">Number of operations completed.</param>
	/// <param name="memoryUsed">Memory used during the test.</param>
	/// <returns>A successful stress test result.</returns>
	public static StressTestResult CreateSuccess(string testName, TimeSpan duration, int operationsCompleted, long memoryUsed = 0) =>
		new()
		{
			TestName = testName,
			Success = true,
			Duration = duration,
			OperationsCompleted = operationsCompleted,
			OperationsPerSecond = operationsCompleted / duration.TotalSeconds,
			MemoryUsed = memoryUsed,
		};

	/// <summary>
	/// Creates a failed stress test result.
	/// </summary>
	/// <param name="testName">The name of the test.</param>
	/// <param name="duration">How long the test ran before failing.</param>
	/// <param name="errorMessage">The error message.</param>
	/// <param name="exception">The exception that caused the failure.</param>
	/// <param name="operationsCompleted">Number of operations completed before failure.</param>
	/// <returns>A failed stress test result.</returns>
	public static StressTestResult CreateFailure(
		string testName,
		TimeSpan duration,
		string errorMessage,
		Exception? exception = null,
		int operationsCompleted = 0
	) =>
		new()
		{
			TestName = testName,
			Success = false,
			Duration = duration,
			OperationsCompleted = operationsCompleted,
			OperationsPerSecond = operationsCompleted > 0 ? operationsCompleted / duration.TotalSeconds : 0,
			ErrorMessage = errorMessage,
			Exception = exception,
		};

	/// <summary>
	/// Gets a formatted summary of the stress test result.
	/// </summary>
	/// <returns>A formatted string with test result information.</returns>
	public string GetFormattedSummary()
	{
		var result =
			$"Stress Test Result: {TestName}\n"
			+ $"  Status: {Status}\n"
			+ $"  Duration: {Duration.TotalMilliseconds:F2}ms\n"
			+ $"  Operations: {OperationsCompleted:N0}\n"
			+ $"  Ops/Second: {OperationsPerSecond:F2}\n"
			+ $"  Avg Time/Op: {AverageTimePerOperation.TotalMilliseconds:F3}ms\n"
			+ $"  Performance: {PerformanceRating}\n"
			+ $"  Memory Used: {MemoryUsedMB:F2}MB";

		if (!Success && !string.IsNullOrEmpty(ErrorMessage))
		{
			result += $"\n  Error: {ErrorMessage}";
		}

		if (HasMemoryIssues)
		{
			result += $"\n  ⚠️ High memory usage detected";
		}

		return result;
	}
}
