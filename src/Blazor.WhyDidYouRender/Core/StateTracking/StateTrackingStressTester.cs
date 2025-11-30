using System.Diagnostics;
using Blazor.WhyDidYouRender.Records.StateTracking;
using Microsoft.AspNetCore.Components;

namespace Blazor.WhyDidYouRender.Core.StateTracking;

/// <summary>
/// Stress testing utility for state tracking components, specifically designed
/// to test threading safety and performance under high load scenarios.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="StateTrackingStressTester"/> class.
/// </remarks>
/// <param name="stateTracker">The state tracker to test.</param>
/// <param name="config">Stress test configuration.</param>
public class StateTrackingStressTester(ThreadSafeStateTracker stateTracker, StressTestConfiguration? config = null)
{
	/// <summary>
	/// The thread-safe state tracker being tested.
	/// </summary>
	private readonly ThreadSafeStateTracker _stateTracker = stateTracker ?? throw new ArgumentNullException(nameof(stateTracker));

	/// <summary>
	/// Configuration for stress testing.
	/// </summary>
	private readonly StressTestConfiguration _config = config ?? new StressTestConfiguration();

	/// <summary>
	/// Tests concurrent snapshot capture operations.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Test results.</returns>
	private async Task<StressTestResult> RunConcurrentSnapshotTestAsync(CancellationToken cancellationToken)
	{
		var result = new StressTestResult { TestName = "Concurrent Snapshot Capture" };
		var stopwatch = Stopwatch.StartNew();

		try
		{
			var components = CreateTestComponents(_config.ComponentCount);
			var tasks = new List<Task>();

			for (int i = 0; i < _config.ConcurrentThreads; i++)
				tasks.Add(
					Task.Run(
						async () =>
						{
							for (int j = 0; j < _config.OperationsPerThread; j++)
							{
								var component = components[j % components.Count];
								await _stateTracker.CaptureSnapshotAsync(component, cancellationToken);

								if (_config.DelayBetweenOperationsMs > 0)
								{
									await Task.Delay(_config.DelayBetweenOperationsMs, cancellationToken);
								}
							}
						},
						cancellationToken
					)
				);

			await Task.WhenAll(tasks);

			result.Success = true;
			result.OperationsCompleted = _config.ConcurrentThreads * _config.OperationsPerThread;
		}
		catch (Exception ex)
		{
			result.Success = false;
			result.ErrorMessage = ex.Message;
		}

		stopwatch.Stop();
		result.Duration = stopwatch.Elapsed;
		result.OperationsPerSecond = result.OperationsCompleted / result.Duration.TotalSeconds;

		return result;
	}

	/// <summary>
	/// Tests concurrent state change detection operations.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Test results.</returns>
	private async Task<StressTestResult> RunConcurrentStateChangeTestAsync(CancellationToken cancellationToken)
	{
		var result = new StressTestResult { TestName = "Concurrent State Change Detection" };
		var stopwatch = Stopwatch.StartNew();

		try
		{
			var components = CreateTestComponents(_config.ComponentCount);
			var tasks = new List<Task>();

			foreach (var component in components)
				await _stateTracker.CaptureSnapshotAsync(component, cancellationToken);

			for (int i = 0; i < _config.ConcurrentThreads; i++)
				tasks.Add(
					Task.Run(
						async () =>
						{
							for (int j = 0; j < _config.OperationsPerThread; j++)
							{
								var component = components[j % components.Count];

								// modify component state
								if (component is TestComponent testComponent)
									testComponent.ModifyState();

								await _stateTracker.DetectStateChangesAsync(component, cancellationToken);

								if (_config.DelayBetweenOperationsMs > 0)
								{
									await Task.Delay(_config.DelayBetweenOperationsMs, cancellationToken);
								}
							}
						},
						cancellationToken
					)
				);

			await Task.WhenAll(tasks);

			result.Success = true;
			result.OperationsCompleted = _config.ConcurrentThreads * _config.OperationsPerThread;
		}
		catch (Exception ex)
		{
			result.Success = false;
			result.ErrorMessage = ex.Message;
		}

		stopwatch.Stop();
		result.Duration = stopwatch.Elapsed;
		result.OperationsPerSecond = result.OperationsCompleted / result.Duration.TotalSeconds;

		return result;
	}

	/// <summary>
	/// Tests mixed operations under load.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Test results.</returns>
	private async Task<StressTestResult> RunMixedOperationsTestAsync(CancellationToken cancellationToken)
	{
		var result = new StressTestResult { TestName = "Mixed Operations Under Load" };
		var stopwatch = Stopwatch.StartNew();

		try
		{
			var components = CreateTestComponents(_config.ComponentCount);
			var tasks = new List<Task>();
			var random = new Random();

			for (int i = 0; i < _config.ConcurrentThreads; i++)
			{
				tasks.Add(
					Task.Run(
						async () =>
						{
							for (int j = 0; j < _config.OperationsPerThread; j++)
							{
								var component = components[j % components.Count];
								var operationType = random.Next(3);

								switch (operationType)
								{
									case 0: // capture snapshot
										await _stateTracker.CaptureSnapshotAsync(component, cancellationToken);
										break;
									case 1: // detect state changes
										await _stateTracker.DetectStateChangesAsync(component, cancellationToken);
										break;
									case 2: // cleanup component
										_stateTracker.CleanupComponent(component);
										break;
								}

								if (_config.DelayBetweenOperationsMs > 0)
								{
									await Task.Delay(_config.DelayBetweenOperationsMs, cancellationToken);
								}
							}
						},
						cancellationToken
					)
				);
			}

			await Task.WhenAll(tasks);

			result.Success = true;
			result.OperationsCompleted = _config.ConcurrentThreads * _config.OperationsPerThread;
		}
		catch (Exception ex)
		{
			result.Success = false;
			result.ErrorMessage = ex.Message;
		}

		stopwatch.Stop();
		result.Duration = stopwatch.Elapsed;
		result.OperationsPerSecond = result.OperationsCompleted / result.Duration.TotalSeconds;

		return result;
	}

	/// <summary>
	/// Tests performance under memory pressure.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Test results.</returns>
	private async Task<StressTestResult> RunMemoryPressureTestAsync(CancellationToken cancellationToken)
	{
		var result = new StressTestResult { TestName = "Memory Pressure Test" };
		var stopwatch = Stopwatch.StartNew();

		try
		{
			var initialMemory = GC.GetTotalMemory(false);
			var components = CreateTestComponents(_config.ComponentCount * 10); // more components for memory pressure

			// perform operations that create memory pressure
			var tasks = Enumerable
				.Range(0, _config.ConcurrentThreads)
				.Select(async i =>
				{
					for (int j = 0; j < _config.OperationsPerThread; j++)
					{
						var component = components[j % components.Count];
						await _stateTracker.CaptureSnapshotAsync(component, cancellationToken);

						// create some memory pressure
						var largeArray = new byte[1024 * 1024]; // 1MB
						GC.KeepAlive(largeArray);
					}
				});

			await Task.WhenAll(tasks);

			var finalMemory = GC.GetTotalMemory(false);
			result.MemoryUsed = finalMemory - initialMemory;
			result.Success = true;
			result.OperationsCompleted = _config.ConcurrentThreads * _config.OperationsPerThread;
		}
		catch (Exception ex)
		{
			result.Success = false;
			result.ErrorMessage = ex.Message;
		}

		stopwatch.Stop();
		result.Duration = stopwatch.Elapsed;
		result.OperationsPerSecond = result.OperationsCompleted / result.Duration.TotalSeconds;

		return result;
	}

	/// <summary>
	/// Tests cleanup performance.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Test results.</returns>
	private async Task<StressTestResult> RunCleanupPerformanceTestAsync(CancellationToken cancellationToken)
	{
		var result = new StressTestResult { TestName = "Cleanup Performance Test" };
		var stopwatch = Stopwatch.StartNew();

		try
		{
			var components = CreateTestComponents(_config.ComponentCount);

			foreach (var component in components)
				await _stateTracker.CaptureSnapshotAsync(component, cancellationToken);

			var cleanedUp = _stateTracker.PerformBulkCleanup(components.Take(components.Count / 2));

			result.Success = true;
			result.OperationsCompleted = cleanedUp;
		}
		catch (Exception ex)
		{
			result.Success = false;
			result.ErrorMessage = ex.Message;
		}

		stopwatch.Stop();
		result.Duration = stopwatch.Elapsed;
		result.OperationsPerSecond = result.OperationsCompleted / result.Duration.TotalSeconds;

		return result;
	}

	/// <summary>
	/// Creates test components for stress testing.
	/// </summary>
	/// <param name="count">Number of components to create.</param>
	/// <returns>List of test components.</returns>
	private static List<ComponentBase> CreateTestComponents(int count)
	{
		var components = new List<ComponentBase>();

		for (int i = 0; i < count; i++)
			components.Add(new TestComponent { Id = i });

		return components;
	}
}
