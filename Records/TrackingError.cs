using System;
using System.Collections.Generic;

namespace Blazor.WhyDidYouRender.Records;

/// <summary>
/// Represents an error that occurred during render tracking.
/// </summary>
public record TrackingError {
	/// <summary>
	/// Gets or sets the unique identifier for this error occurrence.
	/// </summary>
	public string ErrorId { get; init; } = Guid.NewGuid().ToString("N")[..8];

	/// <summary>
	/// Gets or sets when the error occurred.
	/// </summary>
	public DateTime Timestamp { get; init; } = DateTime.UtcNow;

	/// <summary>
	/// Gets or sets the error message.
	/// </summary>
	public string Message { get; init; } = string.Empty;

	/// <summary>
	/// Gets or sets the exception type name.
	/// </summary>
	public string? ExceptionType { get; init; }

	/// <summary>
	/// Gets or sets the stack trace.
	/// </summary>
	public string? StackTrace { get; init; }

	/// <summary>
	/// Gets or sets the component that was being tracked when the error occurred.
	/// </summary>
	public string? ComponentName { get; init; }

	/// <summary>
	/// Gets or sets the tracking method that failed.
	/// </summary>
	public string? TrackingMethod { get; init; }

	/// <summary>
	/// Gets or sets the session ID when the error occurred.
	/// </summary>
	public string? SessionId { get; init; }

	/// <summary>
	/// Gets or sets additional context information.
	/// </summary>
	public Dictionary<string, object?> Context { get; init; } = new();

	/// <summary>
	/// Gets or sets the severity level of the error.
	/// </summary>
	public ErrorSeverity Severity { get; init; } = ErrorSeverity.Warning;

	/// <summary>
	/// Gets or sets whether this error has been recovered from.
	/// </summary>
	public bool Recovered { get; init; } = false;
}

/// <summary>
/// Severity levels for tracking errors.
/// </summary>
public enum ErrorSeverity {
	/// <summary>
	/// Informational - minor issues that don't affect functionality.
	/// </summary>
	Info,

	/// <summary>
	/// Warning - issues that might affect tracking but don't break functionality.
	/// </summary>
	Warning,

	/// <summary>
	/// Error - significant issues that affect tracking functionality.
	/// </summary>
	Error,

	/// <summary>
	/// Critical - severe issues that might affect application stability.
	/// </summary>
	Critical
}

/// <summary>
/// Error statistics summary.
/// </summary>
public record ErrorStatistics {
	/// <summary>
	/// Gets or sets the total number of errors tracked.
	/// </summary>
	public int TotalErrors { get; init; }

	/// <summary>
	/// Gets or sets the number of errors in the last hour.
	/// </summary>
	public int ErrorsLastHour { get; init; }

	/// <summary>
	/// Gets or sets the number of errors in the last 24 hours.
	/// </summary>
	public int ErrorsLast24Hours { get; init; }

	/// <summary>
	/// Gets or sets the most common error types.
	/// </summary>
	public Dictionary<string, int> CommonErrorTypes { get; init; } = new();

	/// <summary>
	/// Gets or sets the error rate (errors per minute).
	/// </summary>
	public double ErrorRate { get; init; }
}
