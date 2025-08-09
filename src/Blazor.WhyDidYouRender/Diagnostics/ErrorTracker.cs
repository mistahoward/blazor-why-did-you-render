using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.Json;

using Microsoft.Extensions.Logging;
using MEL = Microsoft.Extensions.Logging;

using Blazor.WhyDidYouRender.Configuration;
using Blazor.WhyDidYouRender.Logging;
using Blazor.WhyDidYouRender.Records;

namespace Blazor.WhyDidYouRender.Diagnostics;

/// <summary>
/// Default implementation of error tracking service.
/// </summary>
public class ErrorTracker : IErrorTracker {
	private readonly ConcurrentQueue<TrackingError> _errors = new();
	private readonly WhyDidYouRenderConfig _config;
	private readonly ILogger<ErrorTracker>? _logger;
	private readonly IWhyDidYouRenderLogger? _unifiedLogger;
	private readonly object _statsLock = new();
	private int _totalErrorCount = 0;

	/// <summary>
	/// Initializes a new instance of the <see cref="ErrorTracker"/> class.
	/// </summary>
	/// <param name="config">The configuration.</param>
	/// <param name="logger">The traditional logger.</param>
	/// <param name="unifiedLogger">Optional unified logger for structured console output.</param>
	public ErrorTracker(WhyDidYouRenderConfig config, ILogger<ErrorTracker>? logger = null, IWhyDidYouRenderLogger? unifiedLogger = null) {
		_config = config;
		_logger = logger;
		_unifiedLogger = unifiedLogger;
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

		while (newQueue.TryDequeue(out var error)) {
			_errors.Enqueue(error);
		}
	}

	private string TrackErrorInternal(TrackingError error) {
		_errors.Enqueue(error);

		lock (_statsLock) {
			_totalErrorCount++;
		}

		while (_errors.Count > 1000) {
			_errors.TryDequeue(out _);
		}

		LogError(error);

		return error.ErrorId;
	}

	private void LogError(TrackingError error) {
		try {
			var logLevel = error.Severity switch {
				ErrorSeverity.Info => MEL.LogLevel.Information,
				ErrorSeverity.Warning => MEL.LogLevel.Warning,
				ErrorSeverity.Error => MEL.LogLevel.Error,
				ErrorSeverity.Critical => MEL.LogLevel.Critical,
				_ => MEL.LogLevel.Warning
			};

			var message = $"[WhyDidYouRender] {error.Severity} {error.ErrorId}: {error.Message}";

			if (!string.IsNullOrEmpty(error.ComponentName)) {
				message += $" | Component: {error.ComponentName}";
			}

			if (!string.IsNullOrEmpty(error.TrackingMethod)) {
				message += $" | Method: {error.TrackingMethod}";
			}

			_logger?.Log(logLevel, message);

			if (_config.Output == TrackingOutput.Console || _config.Output == TrackingOutput.Both) {
				if (_unifiedLogger != null) {
					var data = error.Context.Any() ? new Dictionary<string, object?>(error.Context) : new();
					if (!string.IsNullOrEmpty(error.StackTrace) && error.Severity >= ErrorSeverity.Error)
						data["stackTrace"] = error.StackTrace;
					_unifiedLogger.LogError(message, null, data);
				}
				else {
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
		}
		catch (Exception ex) {
			if (_unifiedLogger != null) _unifiedLogger.LogError($"Failed to log error: {error.Message}", ex);
			else Console.WriteLine($"[WhyDidYouRender] Failed to log error: {error.Message}");
		}
	}
}
