using System.Collections.Concurrent;
using System.Diagnostics;

using Blazor.WhyDidYouRender.Records.StateTracking;

namespace Blazor.WhyDidYouRender.Core.StateTracking;

/// <summary>
/// Monitors performance of state tracking operations and provides detailed metrics.
/// This class helps identify performance bottlenecks and optimize state tracking behavior.
/// </summary>
public class StateTrackingPerformanceMonitor {
	/// <summary>
	/// Performance metrics for different operation types.
	/// </summary>
	private readonly ConcurrentDictionary<string, MutableOperationMetrics> _operationMetrics = new();

	/// <summary>
	/// Recent operation timings for trend analysis.
	/// </summary>
	private readonly ConcurrentQueue<OperationTiming> _recentTimings = new();

	/// <summary>
	/// Maximum number of recent timings to keep.
	/// </summary>
	private const int _maxRecentTimings = 1000;

	/// <summary>
	/// Measures the execution time of an operation.
	/// </summary>
	/// <typeparam name="T">The return type of the operation.</typeparam>
	/// <param name="operationName">The name of the operation being measured.</param>
	/// <param name="operation">The operation to measure.</param>
	/// <returns>The result of the operation.</returns>
	public T MeasureOperation<T>(string operationName, Func<T> operation) {
		var stopwatch = Stopwatch.StartNew();
		var startTime = DateTime.UtcNow;

		try {
			var result = operation();
			stopwatch.Stop();

			RecordSuccessfulOperation(operationName, stopwatch.Elapsed, startTime);
			return result;
		}
		catch (Exception ex) {
			stopwatch.Stop();
			RecordFailedOperation(operationName, stopwatch.Elapsed, startTime, ex);
			throw;
		}
	}

	/// <summary>
	/// Measures the execution time of an operation without a return value.
	/// </summary>
	/// <param name="operationName">The name of the operation being measured.</param>
	/// <param name="operation">The operation to measure.</param>
	public void MeasureOperation(string operationName, Action operation) {
		MeasureOperation(operationName, () => {
			operation();
			return true;
		});
	}

	/// <summary>
	/// Records a successful operation timing.
	/// </summary>
	/// <param name="operationName">The name of the operation.</param>
	/// <param name="duration">The duration of the operation.</param>
	/// <param name="startTime">When the operation started.</param>
	private void RecordSuccessfulOperation(string operationName, TimeSpan duration, DateTime startTime) {
		var metrics = _operationMetrics.GetOrAdd(operationName, _ => new MutableOperationMetrics(operationName));
		metrics.RecordSuccess(duration);

		var timing = OperationTiming.CreateSuccess(operationName, startTime, duration);
		AddRecentTiming(timing);
	}

	/// <summary>
	/// Records a failed operation timing.
	/// </summary>
	/// <param name="operationName">The name of the operation.</param>
	/// <param name="duration">The duration of the operation before failure.</param>
	/// <param name="startTime">When the operation started.</param>
	/// <param name="exception">The exception that occurred.</param>
	private void RecordFailedOperation(string operationName, TimeSpan duration, DateTime startTime, Exception exception) {
		var metrics = _operationMetrics.GetOrAdd(operationName, _ => new MutableOperationMetrics(operationName));
		metrics.RecordFailure(duration, exception);

		var timing = OperationTiming.CreateFailure(operationName, startTime, duration, exception);
		AddRecentTiming(timing);
	}

	/// <summary>
	/// Adds a timing to the recent timings queue, maintaining size limit.
	/// </summary>
	/// <param name="timing">The timing to add.</param>
	private void AddRecentTiming(OperationTiming timing) {
		_recentTimings.Enqueue(timing);

		while (_recentTimings.Count > _maxRecentTimings)
			_recentTimings.TryDequeue(out _);
	}

	/// <summary>
	/// Gets performance metrics for all operations.
	/// </summary>
	/// <returns>A dictionary of operation metrics.</returns>
	public Dictionary<string, OperationMetrics> GetAllOperationMetrics() =>
		_operationMetrics.ToDictionary(
			kvp => kvp.Key,
			kvp => kvp.Value.CreateSnapshot());

	/// <summary>
	/// Gets recent operation timings for trend analysis.
	/// </summary>
	/// <param name="operationName">Optional operation name filter.</param>
	/// <param name="maxCount">Maximum number of timings to return.</param>
	/// <returns>Recent operation timings.</returns>
	public List<OperationTiming> GetRecentTimings(string? operationName = null, int maxCount = 100) {
		var timings = _recentTimings.ToList();

		if (!string.IsNullOrEmpty(operationName))
			timings = timings.Where(t => t.OperationName == operationName).ToList();

		return [.. timings.TakeLast(maxCount)];
	}

	/// <summary>
	/// Gets a performance summary for all operations.
	/// </summary>
	/// <returns>A comprehensive performance summary.</returns>
	public PerformanceSummary GetPerformanceSummary() {
		var allMetrics = GetAllOperationMetrics();
		var recentTimings = GetRecentTimings();

		return new PerformanceSummary {
			TotalOperations = allMetrics.Values.Sum(m => m.TotalOperations),
			TotalSuccessfulOperations = allMetrics.Values.Sum(m => m.SuccessfulOperations),
			TotalFailedOperations = allMetrics.Values.Sum(m => m.FailedOperations),
			AverageOperationTime = allMetrics.Values.Any()
				? TimeSpan.FromMilliseconds(allMetrics.Values.Average(m => m.AverageTime.TotalMilliseconds))
				: TimeSpan.Zero,
			SlowestOperation = allMetrics.Values.MaxBy(m => m.MaxTime),
			FastestOperation = allMetrics.Values.MinBy(m => m.MinTime),
			OperationMetrics = allMetrics,
			RecentTimingsCount = recentTimings.Count,
			MonitoringStartTime = allMetrics.Values.MinBy(m => m.FirstOperationTime)?.FirstOperationTime ?? DateTime.UtcNow
		};
	}

	/// <summary>
	/// Resets all performance metrics.
	/// </summary>
	public void Reset() {
		_operationMetrics.Clear();
		while (_recentTimings.TryDequeue(out _)) { }
	}
}

/// <summary>
/// Mutable implementation of operation metrics for internal tracking.
/// </summary>
internal class MutableOperationMetrics(string operationName) {
	private long _totalOperations = 0;
	private long _successfulOperations = 0;
	private long _failedOperations = 0;
	private long _totalTimeTicks = 0;
	private long _minTimeTicks = long.MaxValue;
	private long _maxTimeTicks = 0;

	public string OperationName { get; } = operationName;
	public DateTime FirstOperationTime { get; private set; } = DateTime.MaxValue;
	public DateTime LastOperationTime { get; private set; } = DateTime.MinValue;

	public void RecordSuccess(TimeSpan duration) {
		Interlocked.Increment(ref _totalOperations);
		Interlocked.Increment(ref _successfulOperations);
		Interlocked.Add(ref _totalTimeTicks, duration.Ticks);

		UpdateMinTime(duration.Ticks);
		UpdateMaxTime(duration.Ticks);
		UpdateOperationTimes();
	}

	public void RecordFailure(TimeSpan duration, Exception exception) {
		Interlocked.Increment(ref _totalOperations);
		Interlocked.Increment(ref _failedOperations);
		UpdateOperationTimes();
	}

	private void UpdateMinTime(long ticks) {
		long current, newValue;
		do {
			current = _minTimeTicks;
			newValue = Math.Min(current, ticks);
		} while (Interlocked.CompareExchange(ref _minTimeTicks, newValue, current) != current);
	}

	private void UpdateMaxTime(long ticks) {
		long current, newValue;
		do {
			current = _maxTimeTicks;
			newValue = Math.Max(current, ticks);
		} while (Interlocked.CompareExchange(ref _maxTimeTicks, newValue, current) != current);
	}

	private void UpdateOperationTimes() {
		var now = DateTime.UtcNow;
		if (FirstOperationTime == DateTime.MaxValue)
			FirstOperationTime = now;
		LastOperationTime = now;
	}

	public OperationMetrics CreateSnapshot() {
		return new OperationMetrics {
			OperationName = OperationName,
			FirstOperationTime = FirstOperationTime,
			LastOperationTime = LastOperationTime,
			TotalOperations = _totalOperations,
			SuccessfulOperations = _successfulOperations,
			FailedOperations = _failedOperations,
			TotalTimeTicks = _totalTimeTicks,
			MinTimeTicks = _minTimeTicks,
			MaxTimeTicks = _maxTimeTicks
		};
	}
}


