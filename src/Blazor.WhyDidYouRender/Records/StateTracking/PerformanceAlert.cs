using System;

namespace Blazor.WhyDidYouRender.Records.StateTracking;

/// <summary>
/// Represents a performance alert generated when operation metrics exceed configured thresholds.
/// This record provides immutable information about performance issues detected during monitoring.
/// </summary>
public record PerformanceAlert
{
	/// <summary>
	/// Gets the type of performance alert that was triggered.
	/// </summary>
	public required AlertType Type { get; init; }

	/// <summary>
	/// Gets the name of the operation that triggered the alert.
	/// </summary>
	public required string OperationName { get; init; }

	/// <summary>
	/// Gets the detailed message describing the performance issue.
	/// </summary>
	public required string Message { get; init; }

	/// <summary>
	/// Gets the severity level of the performance alert.
	/// </summary>
	public required AlertSeverity Severity { get; init; }

	/// <summary>
	/// Gets the timestamp when the alert was generated.
	/// </summary>
	public DateTime Timestamp { get; init; } = DateTime.UtcNow;

	/// <summary>
	/// Gets the actual metric value that triggered the alert.
	/// </summary>
	public object? ActualValue { get; init; }

	/// <summary>
	/// Gets the threshold value that was exceeded.
	/// </summary>
	public object? ThresholdValue { get; init; }

	/// <summary>
	/// Gets additional context information about the alert.
	/// </summary>
	public string? Context { get; init; }

	/// <summary>
	/// Gets whether this alert indicates a critical performance issue.
	/// </summary>
	public bool IsCritical => Severity >= AlertSeverity.Error;

	/// <summary>
	/// Gets whether this alert requires immediate attention.
	/// </summary>
	public bool RequiresImmediateAttention => Severity >= AlertSeverity.Critical;

	/// <summary>
	/// Gets the age of the alert since it was generated.
	/// </summary>
	public TimeSpan Age => DateTime.UtcNow - Timestamp;

	/// <summary>
	/// Gets a brief summary of the alert for display purposes.
	/// </summary>
	public string Summary => $"[{Severity}] {Type}: {OperationName}";

	/// <summary>
	/// Creates a slow average time alert.
	/// </summary>
	/// <param name="operationName">The name of the operation.</param>
	/// <param name="actualTime">The actual average time.</param>
	/// <param name="thresholdTime">The threshold that was exceeded.</param>
	/// <param name="severity">The severity of the alert.</param>
	/// <returns>A performance alert for slow average time.</returns>
	public static PerformanceAlert SlowAverageTime(
		string operationName,
		TimeSpan actualTime,
		TimeSpan thresholdTime,
		AlertSeverity severity = AlertSeverity.Warning
	) =>
		new()
		{
			Type = AlertType.SlowAverageTime,
			OperationName = operationName,
			Message = $"Average time ({actualTime.TotalMilliseconds:F2}ms) exceeds threshold ({thresholdTime.TotalMilliseconds:F2}ms)",
			Severity = severity,
			ActualValue = actualTime,
			ThresholdValue = thresholdTime,
		};

	/// <summary>
	/// Creates a slow maximum time alert.
	/// </summary>
	/// <param name="operationName">The name of the operation.</param>
	/// <param name="actualTime">The actual maximum time.</param>
	/// <param name="thresholdTime">The threshold that was exceeded.</param>
	/// <param name="severity">The severity of the alert.</param>
	/// <returns>A performance alert for slow maximum time.</returns>
	public static PerformanceAlert SlowMaxTime(
		string operationName,
		TimeSpan actualTime,
		TimeSpan thresholdTime,
		AlertSeverity severity = AlertSeverity.Error
	) =>
		new()
		{
			Type = AlertType.SlowMaxTime,
			OperationName = operationName,
			Message = $"Maximum time ({actualTime.TotalMilliseconds:F2}ms) exceeds threshold ({thresholdTime.TotalMilliseconds:F2}ms)",
			Severity = severity,
			ActualValue = actualTime,
			ThresholdValue = thresholdTime,
		};

	/// <summary>
	/// Creates a high failure rate alert.
	/// </summary>
	/// <param name="operationName">The name of the operation.</param>
	/// <param name="actualRate">The actual failure rate.</param>
	/// <param name="thresholdRate">The threshold that was exceeded.</param>
	/// <param name="severity">The severity of the alert.</param>
	/// <returns>A performance alert for high failure rate.</returns>
	public static PerformanceAlert HighFailureRate(
		string operationName,
		double actualRate,
		double thresholdRate,
		AlertSeverity severity = AlertSeverity.Error
	) =>
		new()
		{
			Type = AlertType.HighFailureRate,
			OperationName = operationName,
			Message = $"Failure rate ({actualRate:P2}) exceeds threshold ({thresholdRate:P2})",
			Severity = severity,
			ActualValue = actualRate,
			ThresholdValue = thresholdRate,
		};

	/// <summary>
	/// Gets a formatted summary of the performance alert.
	/// </summary>
	/// <returns>A formatted string with alert information.</returns>
	public string GetFormattedSummary()
	{
		var result =
			$"Performance Alert [{Severity}]:\n"
			+ $"  Operation: {OperationName}\n"
			+ $"  Type: {Type}\n"
			+ $"  Message: {Message}\n"
			+ $"  Timestamp: {Timestamp:yyyy-MM-dd HH:mm:ss}\n"
			+ $"  Age: {Age}";

		if (ActualValue != null && ThresholdValue != null)
		{
			result += $"\n  Actual Value: {ActualValue}\n" + $"  Threshold: {ThresholdValue}";
		}

		if (!string.IsNullOrEmpty(Context))
		{
			result += $"\n  Context: {Context}";
		}

		return result;
	}
}
