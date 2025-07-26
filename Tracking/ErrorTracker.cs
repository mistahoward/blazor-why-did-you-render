using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Blazor.WhyDidYouRender.Tracking;

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
/// Service for tracking and reporting errors that occur during render tracking.
/// </summary>
public interface IErrorTracker {
	/// <summary>
	/// Tracks an error that occurred during render tracking.
	/// </summary>
	/// <param name="exception">The exception that occurred.</param>
	/// <param name="context">Additional context information.</param>
	/// <param name="severity">The severity level of the error.</param>
	/// <returns>The error ID for tracking purposes.</returns>
	string TrackError(Exception exception, Dictionary<string, object?>? context = null, ErrorSeverity severity = ErrorSeverity.Error);

	/// <summary>
	/// Tracks a custom error message.
	/// </summary>
	/// <param name="message">The error message.</param>
	/// <param name="context">Additional context information.</param>
	/// <param name="severity">The severity level of the error.</param>
	/// <returns>The error ID for tracking purposes.</returns>
	string TrackError(string message, Dictionary<string, object?>? context = null, ErrorSeverity severity = ErrorSeverity.Warning);

	/// <summary>
	/// Gets recent errors for diagnostics.
	/// </summary>
	/// <param name="count">Maximum number of errors to return.</param>
	/// <returns>Collection of recent errors.</returns>
	IEnumerable<TrackingError> GetRecentErrors(int count = 50);

	/// <summary>
	/// Gets error statistics.
	/// </summary>
	/// <returns>Error statistics summary.</returns>
	ErrorStatistics GetErrorStatistics();

	/// <summary>
	/// Clears old error records.
	/// </summary>
	/// <param name="olderThan">Clear errors older than this timespan.</param>
	void ClearOldErrors(TimeSpan olderThan);
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

/// <summary>
/// Default implementation of error tracking service.
/// </summary>
public class ErrorTracker : IErrorTracker {
	private readonly ConcurrentQueue<TrackingError> _errors = new();
	private readonly WhyDidYouRenderConfig _config;
	private readonly ILogger<ErrorTracker>? _logger;
	private readonly object _statsLock = new();
	private int _totalErrorCount = 0;

	/// <summary>
	/// Initializes a new instance of the <see cref="ErrorTracker"/> class.
	/// </summary>
	/// <param name="config">The configuration.</param>
	/// <param name="logger">The logger.</param>
	public ErrorTracker(WhyDidYouRenderConfig config, ILogger<ErrorTracker>? logger = null) {
		_config = config;
		_logger = logger;
	}

	/// <inheritdoc />
	public string TrackError(Exception exception, Dictionary<string, object?>? context = null, ErrorSeverity severity = ErrorSeverity.Error) {
		var error = new TrackingError {
			Message = exception.Message,
			ExceptionType = exception.GetType().Name,
			StackTrace = exception.StackTrace,
			Context = context ?? new Dictionary<string, object?>(),
			Severity = severity,
			ComponentName = context?.GetValueOrDefault("ComponentName")?.ToString(),
			TrackingMethod = context?.GetValueOrDefault("TrackingMethod")?.ToString(),
			SessionId = context?.GetValueOrDefault("SessionId")?.ToString()
		};

		return TrackErrorInternal(error);
	}

	/// <inheritdoc />
	public string TrackError(string message, Dictionary<string, object?>? context = null, ErrorSeverity severity = ErrorSeverity.Warning) {
		var error = new TrackingError {
			Message = message,
			Context = context ?? new Dictionary<string, object?>(),
			Severity = severity,
			ComponentName = context?.GetValueOrDefault("ComponentName")?.ToString(),
			TrackingMethod = context?.GetValueOrDefault("TrackingMethod")?.ToString(),
			SessionId = context?.GetValueOrDefault("SessionId")?.ToString()
		};

		return TrackErrorInternal(error);
	}

	/// <inheritdoc />
	public IEnumerable<TrackingError> GetRecentErrors(int count = 50) {
		return _errors.TakeLast(count);
	}

	/// <inheritdoc />
	public ErrorStatistics GetErrorStatistics() {
		var errors = _errors.ToArray();
		var now = DateTime.UtcNow;
		var oneHourAgo = now.AddHours(-1);
		var oneDayAgo = now.AddDays(-1);

		var errorsLastHour = errors.Count(e => e.Timestamp >= oneHourAgo);
		var errorsLast24Hours = errors.Count(e => e.Timestamp >= oneDayAgo);

		var commonTypes = errors
			.Where(e => !string.IsNullOrEmpty(e.ExceptionType))
			.GroupBy(e => e.ExceptionType!)
			.ToDictionary(g => g.Key, g => g.Count());

		var errorRate = errors.Length > 0 && errors.Length > 1
			? errors.Length / (now - errors.First().Timestamp).TotalMinutes
			: 0.0;

		return new ErrorStatistics {
			TotalErrors = _totalErrorCount,
			ErrorsLastHour = errorsLastHour,
			ErrorsLast24Hours = errorsLast24Hours,
			CommonErrorTypes = commonTypes,
			ErrorRate = errorRate
		};
	}

	/// <inheritdoc />
	public void ClearOldErrors(TimeSpan olderThan) {
		var cutoff = DateTime.UtcNow - olderThan;
		var newQueue = new ConcurrentQueue<TrackingError>();

		while (_errors.TryDequeue(out var error)) {
			if (error.Timestamp >= cutoff) {
				newQueue.Enqueue(error);
			}
		}

		// Replace the queue contents
		while (newQueue.TryDequeue(out var error)) {
			_errors.Enqueue(error);
		}
	}

	private string TrackErrorInternal(TrackingError error) {
		// Add to error queue
		_errors.Enqueue(error);
		
		// Increment total count
		lock (_statsLock) {
			_totalErrorCount++;
		}

		// Limit queue size to prevent memory issues
		while (_errors.Count > 1000) {
			_errors.TryDequeue(out _);
		}

		// Log the error
		LogError(error);

		return error.ErrorId;
	}

	private void LogError(TrackingError error) {
		try {
			var logLevel = error.Severity switch {
				ErrorSeverity.Info => LogLevel.Information,
				ErrorSeverity.Warning => LogLevel.Warning,
				ErrorSeverity.Error => LogLevel.Error,
				ErrorSeverity.Critical => LogLevel.Critical,
				_ => LogLevel.Warning
			};

			var message = $"[WhyDidYouRender] {error.Severity} {error.ErrorId}: {error.Message}";
			
			if (!string.IsNullOrEmpty(error.ComponentName)) {
				message += $" | Component: {error.ComponentName}";
			}
			
			if (!string.IsNullOrEmpty(error.TrackingMethod)) {
				message += $" | Method: {error.TrackingMethod}";
			}

			// Log to configured logger if available
			_logger?.Log(logLevel, message);

			// Also log to console for visibility
			if (_config.Output == TrackingOutput.Console || _config.Output == TrackingOutput.Both) {
				var consoleMessage = $"{message}";
				if (error.Context.Any()) {
					consoleMessage += $" | Context: {JsonSerializer.Serialize(error.Context)}";
				}
				
				Console.WriteLine(consoleMessage);
				
				if (!string.IsNullOrEmpty(error.StackTrace) && error.Severity >= ErrorSeverity.Error) {
					Console.WriteLine($"[WhyDidYouRender] Stack Trace: {error.StackTrace}");
				}
			}
		}
		catch {
			// Never let error logging itself cause issues
			Console.WriteLine($"[WhyDidYouRender] Failed to log error: {error.Message}");
		}
	}
}
