using System;

namespace Blazor.WhyDidYouRender.Records.StateTracking;

/// <summary>
/// Represents the timing information for a single operation execution.
/// This record provides immutable data about operation performance and outcome.
/// </summary>
public record OperationTiming
{
	/// <summary>
	/// Gets the name of the operation that was executed.
	/// </summary>
	public required string OperationName { get; init; }

	/// <summary>
	/// Gets the time when the operation started.
	/// </summary>
	public required DateTime StartTime { get; init; }

	/// <summary>
	/// Gets the duration of the operation execution.
	/// </summary>
	public required TimeSpan Duration { get; init; }

	/// <summary>
	/// Gets whether the operation completed successfully.
	/// </summary>
	public bool Success { get; init; }

	/// <summary>
	/// Gets the exception that occurred during operation execution, if any.
	/// </summary>
	public Exception? Exception { get; init; }

	/// <summary>
	/// Gets the time when the operation completed.
	/// </summary>
	public DateTime EndTime => StartTime + Duration;

	/// <summary>
	/// Gets the operation duration in milliseconds.
	/// </summary>
	public double DurationMs => Duration.TotalMilliseconds;

	/// <summary>
	/// Gets whether the operation is considered slow based on a threshold.
	/// </summary>
	/// <param name="threshold">The threshold for considering an operation slow.</param>
	/// <returns>True if the operation duration exceeds the threshold; otherwise, false.</returns>
	public bool IsSlow(TimeSpan threshold) => Duration > threshold;

	/// <summary>
	/// Gets whether the operation failed due to an exception.
	/// </summary>
	public bool HasException => Exception != null;

	/// <summary>
	/// Gets the exception type name if an exception occurred.
	/// </summary>
	public string? ExceptionType => Exception?.GetType().Name;

	/// <summary>
	/// Gets a brief description of the operation outcome.
	/// </summary>
	public string OutcomeDescription => Success ? $"Success ({DurationMs:F2}ms)" : $"Failed ({DurationMs:F2}ms): {ExceptionType}";

	/// <summary>
	/// Gets a formatted summary of the operation timing.
	/// </summary>
	/// <returns>A formatted string with timing information.</returns>
	public string GetFormattedSummary()
	{
		var status = Success ? "SUCCESS" : "FAILED";
		var result =
			$"[{status}] {OperationName}\n"
			+ $"  Start Time: {StartTime:HH:mm:ss.fff}\n"
			+ $"  Duration: {DurationMs:F2}ms\n"
			+ $"  End Time: {EndTime:HH:mm:ss.fff}";

		if (!Success && Exception != null)
		{
			result += $"\n  Exception: {Exception.GetType().Name}\n" + $"  Message: {Exception.Message}";
		}

		return result;
	}

	/// <summary>
	/// Creates a successful operation timing.
	/// </summary>
	/// <param name="operationName">The name of the operation.</param>
	/// <param name="startTime">When the operation started.</param>
	/// <param name="duration">How long the operation took.</param>
	/// <returns>A successful operation timing record.</returns>
	public static OperationTiming CreateSuccess(string operationName, DateTime startTime, TimeSpan duration) =>
		new()
		{
			OperationName = operationName,
			StartTime = startTime,
			Duration = duration,
			Success = true,
		};

	/// <summary>
	/// Creates a failed operation timing.
	/// </summary>
	/// <param name="operationName">The name of the operation.</param>
	/// <param name="startTime">When the operation started.</param>
	/// <param name="duration">How long the operation took before failing.</param>
	/// <param name="exception">The exception that caused the failure.</param>
	/// <returns>A failed operation timing record.</returns>
	public static OperationTiming CreateFailure(string operationName, DateTime startTime, TimeSpan duration, Exception exception) =>
		new()
		{
			OperationName = operationName,
			StartTime = startTime,
			Duration = duration,
			Success = false,
			Exception = exception,
		};
}
