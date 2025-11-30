using System;
using System.Collections.Generic;
using System.Linq;

namespace Blazor.WhyDidYouRender.Records.StateTracking;

/// <summary>
/// Represents the comprehensive results from a full stress test suite execution.
/// This record provides immutable information about all stress test phases and overall performance.
/// </summary>
public record ComprehensiveStressTestResults
{
	/// <summary>
	/// Gets the time when the stress test suite started.
	/// </summary>
	public DateTime StartTime { get; init; }

	/// <summary>
	/// Gets the time when the stress test suite ended.
	/// </summary>
	public DateTime EndTime { get; init; }

	/// <summary>
	/// Gets the total duration of the entire stress test suite.
	/// </summary>
	public TimeSpan TotalDuration { get; init; }

	/// <summary>
	/// Gets whether all tests in the suite passed successfully.
	/// </summary>
	public bool OverallSuccess { get; init; }

	/// <summary>
	/// Gets the overall error message if the test suite failed.
	/// </summary>
	public string? OverallError { get; init; }

	/// <summary>
	/// Gets the results from the concurrent snapshot capture test.
	/// </summary>
	public StressTestResult? ConcurrentSnapshotTest { get; init; }

	/// <summary>
	/// Gets the results from the concurrent state change detection test.
	/// </summary>
	public StressTestResult? ConcurrentStateChangeTest { get; init; }

	/// <summary>
	/// Gets the results from the mixed operations under load test.
	/// </summary>
	public StressTestResult? MixedOperationsTest { get; init; }

	/// <summary>
	/// Gets the results from the memory pressure test.
	/// </summary>
	public StressTestResult? MemoryPressureTest { get; init; }

	/// <summary>
	/// Gets the results from the cleanup performance test.
	/// </summary>
	public StressTestResult? CleanupPerformanceTest { get; init; }

	/// <summary>
	/// Gets all individual test results in a collection.
	/// </summary>
	public IEnumerable<StressTestResult> AllTests =>
		new[] { ConcurrentSnapshotTest, ConcurrentStateChangeTest, MixedOperationsTest, MemoryPressureTest, CleanupPerformanceTest }.Where(
			t => t != null
		)!;

	/// <summary>
	/// Gets whether all individual tests completed successfully.
	/// </summary>
	public bool AllTestsSuccessful => AllTests.All(t => t.Success);

	/// <summary>
	/// Gets the number of tests that passed.
	/// </summary>
	public int PassedTestCount => AllTests.Count(t => t.Success);

	/// <summary>
	/// Gets the number of tests that failed.
	/// </summary>
	public int FailedTestCount => AllTests.Count(t => !t.Success);

	/// <summary>
	/// Gets the total number of tests executed.
	/// </summary>
	public int TotalTestCount => AllTests.Count();

	/// <summary>
	/// Gets the overall pass rate as a percentage.
	/// </summary>
	public double PassRate => TotalTestCount > 0 ? (double)PassedTestCount / TotalTestCount * 100 : 0;

	/// <summary>
	/// Gets the total number of operations performed across all tests.
	/// </summary>
	public int TotalOperations => AllTests.Sum(t => t.OperationsCompleted);

	/// <summary>
	/// Gets the average operations per second across all tests.
	/// </summary>
	public double AverageOperationsPerSecond => AllTests.Any() ? AllTests.Average(t => t.OperationsPerSecond) : 0;

	/// <summary>
	/// Gets the total memory used across all tests in bytes.
	/// </summary>
	public long TotalMemoryUsed => AllTests.Sum(t => t.MemoryUsed);

	/// <summary>
	/// Gets the total memory used in megabytes.
	/// </summary>
	public double TotalMemoryUsedMB => TotalMemoryUsed / (1024.0 * 1024.0);

	/// <summary>
	/// Gets the test with the best performance (highest ops/sec).
	/// </summary>
	public StressTestResult? BestPerformingTest => AllTests.MaxBy(t => t.OperationsPerSecond);

	/// <summary>
	/// Gets the test with the worst performance (lowest ops/sec).
	/// </summary>
	public StressTestResult? WorstPerformingTest => AllTests.MinBy(t => t.OperationsPerSecond);

	/// <summary>
	/// Gets the tests that failed.
	/// </summary>
	public IEnumerable<StressTestResult> FailedTests => AllTests.Where(t => !t.Success);

	/// <summary>
	/// Gets whether there were any memory issues during testing.
	/// </summary>
	public bool HasMemoryIssues => AllTests.Any(t => t.HasMemoryIssues);

	/// <summary>
	/// Gets the overall performance rating based on average operations per second.
	/// </summary>
	public string OverallPerformanceRating =>
		AverageOperationsPerSecond switch
		{
			> 1000 => "Excellent",
			> 500 => "Good",
			> 100 => "Fair",
			> 10 => "Poor",
			_ => "Very Poor",
		};

	/// <summary>
	/// Creates a comprehensive stress test result from individual test results.
	/// </summary>
	/// <param name="startTime">When the test suite started.</param>
	/// <param name="endTime">When the test suite ended.</param>
	/// <param name="concurrentSnapshotTest">Concurrent snapshot test result.</param>
	/// <param name="concurrentStateChangeTest">Concurrent state change test result.</param>
	/// <param name="mixedOperationsTest">Mixed operations test result.</param>
	/// <param name="memoryPressureTest">Memory pressure test result.</param>
	/// <param name="cleanupPerformanceTest">Cleanup performance test result.</param>
	/// <param name="overallError">Overall error message if any.</param>
	/// <returns>A comprehensive stress test result.</returns>
	public static ComprehensiveStressTestResults Create(
		DateTime startTime,
		DateTime endTime,
		StressTestResult? concurrentSnapshotTest = null,
		StressTestResult? concurrentStateChangeTest = null,
		StressTestResult? mixedOperationsTest = null,
		StressTestResult? memoryPressureTest = null,
		StressTestResult? cleanupPerformanceTest = null,
		string? overallError = null
	) =>
		new()
		{
			StartTime = startTime,
			EndTime = endTime,
			TotalDuration = endTime - startTime,
			ConcurrentSnapshotTest = concurrentSnapshotTest,
			ConcurrentStateChangeTest = concurrentStateChangeTest,
			MixedOperationsTest = mixedOperationsTest,
			MemoryPressureTest = memoryPressureTest,
			CleanupPerformanceTest = cleanupPerformanceTest,
			OverallError = overallError,
			OverallSuccess =
				string.IsNullOrEmpty(overallError)
				&& new[]
				{
					concurrentSnapshotTest,
					concurrentStateChangeTest,
					mixedOperationsTest,
					memoryPressureTest,
					cleanupPerformanceTest,
				}
					.Where(t => t != null)
					.All(t => t!.Success),
		};

	/// <summary>
	/// Gets a formatted summary of the comprehensive stress test results.
	/// </summary>
	/// <returns>A formatted string with comprehensive test results.</returns>
	public string GetFormattedSummary()
	{
		var summary =
			$"Comprehensive Stress Test Results:\n"
			+ $"  Overall Status: {(OverallSuccess ? "PASSED" : "FAILED")}\n"
			+ $"  Total Duration: {TotalDuration}\n"
			+ $"  Tests Passed: {PassedTestCount}/{TotalTestCount} ({PassRate:F1}%)\n"
			+ $"  Total Operations: {TotalOperations:N0}\n"
			+ $"  Avg Ops/Second: {AverageOperationsPerSecond:F2}\n"
			+ $"  Overall Performance: {OverallPerformanceRating}\n"
			+ $"  Total Memory Used: {TotalMemoryUsedMB:F2}MB\n";

		if (BestPerformingTest != null)
			summary += $"  Best Test: {BestPerformingTest.TestName} ({BestPerformingTest.OperationsPerSecond:F2} ops/sec)\n";

		if (WorstPerformingTest != null)
			summary += $"  Worst Test: {WorstPerformingTest.TestName} ({WorstPerformingTest.OperationsPerSecond:F2} ops/sec)\n";

		if (HasMemoryIssues)
			summary += $"  ⚠️ Memory issues detected during testing\n";

		if (!string.IsNullOrEmpty(OverallError))
			summary += $"  Error: {OverallError}\n";

		if (FailedTests.Any())
		{
			summary += $"  Failed Tests:\n";
			foreach (var failedTest in FailedTests)
			{
				summary += $"    • {failedTest.TestName}: {failedTest.ErrorMessage}\n";
			}
		}

		return summary;
	}
}
