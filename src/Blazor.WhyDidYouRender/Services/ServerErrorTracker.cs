using System.Collections.Concurrent;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using Blazor.WhyDidYouRender.Abstractions;
using Blazor.WhyDidYouRender.Configuration;
using Blazor.WhyDidYouRender.Records;

namespace Blazor.WhyDidYouRender.Services;

/// <summary>
/// Server-side implementation of error tracking service.
/// </summary>
public class ServerErrorTracker : IErrorTracker {
	private readonly ConcurrentQueue<TrackingError> _errors = new();
	private readonly WhyDidYouRenderConfig _config;
	private readonly ILogger<ServerErrorTracker>? _logger;
	private readonly Lock _statsLock = new();
	private int _totalErrorCount = 0;
	private readonly JsonSerializerOptions _jsonOptions;

	/// <summary>
	/// Initializes a new instance of the <see cref="ServerErrorTracker"/> class.
	/// </summary>
	/// <param name="config">The configuration.</param>
	/// <param name="logger">The logger.</param>
	public ServerErrorTracker(WhyDidYouRenderConfig config, ILogger<ServerErrorTracker>? logger = null) {
		_config = config ?? throw new ArgumentNullException(nameof(config));
		_logger = logger;

		_jsonOptions = new JsonSerializerOptions {
			WriteIndented = false,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase
		};
	}

	/// <inheritdoc />
	public bool SupportsPersistentStorage => false; // server-side uses in-memory storage

	/// <inheritdoc />
	public bool SupportsErrorReporting => true; // can log to server console and external systems

	/// <inheritdoc />
	public string ErrorTrackingDescription => "Server-side in-memory error tracking with console logging";

	/// <inheritdoc />
	public Task TrackErrorAsync(Exception exception, Dictionary<string, object?> context, ErrorSeverity severity, string? componentName = null, string? operation = null) {
		var trackingError = new TrackingError {
			ErrorId = Guid.NewGuid().ToString("N")[..8], // match our existing format
			Message = exception.Message,
			ExceptionType = exception.GetType().Name,
			StackTrace = exception.StackTrace,
			Context = new Dictionary<string, object?>(context),
			Severity = severity,
			ComponentName = componentName,
			TrackingMethod = operation,
			Timestamp = DateTime.UtcNow
		};

		trackingError.Context["ExceptionType"] = exception.GetType().FullName;
		if (exception.InnerException != null)
			trackingError.Context["InnerException"] = exception.InnerException.Message;

		return TrackErrorInternalAsync(trackingError);
	}

	/// <inheritdoc />
	public Task TrackErrorAsync(string message, Dictionary<string, object?> context, ErrorSeverity severity, string? componentName = null, string? operation = null) {
		var trackingError = new TrackingError {
			ErrorId = Guid.NewGuid().ToString("N")[..8], // match our existing format
			Message = message,
			Context = new Dictionary<string, object?>(context),
			Severity = severity,
			ComponentName = componentName,
			TrackingMethod = operation,
			Timestamp = DateTime.UtcNow
		};

		return TrackErrorInternalAsync(trackingError);
	}

	/// <inheritdoc />
	public Task<IEnumerable<TrackingError>> GetRecentErrorsAsync(int count = 50, ErrorSeverity? severity = null, string? componentName = null) {
		var errors = _errors.ToArray().AsEnumerable();

		if (severity.HasValue)
			errors = errors.Where(e => e.Severity >= severity.Value);

		if (!string.IsNullOrEmpty(componentName))
			errors = errors.Where(e => string.Equals(e.ComponentName, componentName, StringComparison.OrdinalIgnoreCase));

		var result = errors.TakeLast(count).ToList();
		return Task.FromResult<IEnumerable<TrackingError>>(result);
	}

	/// <inheritdoc />
	public Task<ErrorStatistics> GetErrorStatisticsAsync() {
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

		var problematicComponents = errors
			.Where(e => !string.IsNullOrEmpty(e.ComponentName))
			.GroupBy(e => e.ComponentName!)
			.ToDictionary(g => g.Key, g => g.Count());

		var errorRate = errors.Length > 0 && errors.Length > 1
			? errors.Length / (now - errors.First().Timestamp).TotalMinutes
			: 0.0;

		var statistics = new ErrorStatistics {
			TotalErrors = _totalErrorCount,
			ErrorsLastHour = errorsLastHour,
			ErrorsLast24Hours = errorsLast24Hours,
			CommonErrorTypes = commonTypes,
			ProblematicComponents = problematicComponents,
			ErrorRate = errorRate
		};

		return Task.FromResult(statistics);
	}

	/// <inheritdoc />
	public Task ClearErrorsAsync() {
		while (_errors.TryDequeue(out _)) { }

		lock (_statsLock) {
			_totalErrorCount = 0;
		}

		return Task.CompletedTask;
	}

	/// <inheritdoc />
	public Task<int> GetErrorCountAsync() {
		return Task.FromResult(_totalErrorCount);
	}

	/// <inheritdoc />
	public Task<IEnumerable<TrackingError>> GetErrorsSinceAsync(DateTime since, DateTime? until = null) {
		var errors = _errors.ToArray().AsEnumerable();

		errors = errors.Where(e => e.Timestamp >= since);

		if (until.HasValue)
			errors = errors.Where(e => e.Timestamp <= until.Value);

		var result = errors.ToList();
		return Task.FromResult<IEnumerable<TrackingError>>(result);
	}

	/// <summary>
	/// Clears old error records.
	/// </summary>
	/// <param name="olderThan">Clear errors older than this timespan.</param>
	public void ClearOldErrors(TimeSpan olderThan) {
		var cutoff = DateTime.UtcNow - olderThan;
		var newQueue = new ConcurrentQueue<TrackingError>();

		while (_errors.TryDequeue(out var error))
			if (error.Timestamp >= cutoff)
				newQueue.Enqueue(error);

		while (newQueue.TryDequeue(out var error))
			_errors.Enqueue(error);
	}

	/// <summary>
	/// Internal method to track an error.
	/// </summary>
	/// <param name="trackingError">The tracking error to track.</param>
	/// <returns>A task representing the tracking operation.</returns>
	private async Task TrackErrorInternalAsync(TrackingError trackingError) {
		_errors.Enqueue(trackingError);

		lock (_statsLock) {
			_totalErrorCount++;
		}

		// Keep only the most recent 1000 errors to prevent memory issues
		int maxErrors = 1000;
		bool shouldCleanup = _errors.Count > maxErrors;
		while (shouldCleanup)
			_errors.TryDequeue(out _);

		await LogErrorAsync(trackingError);
	}

	/// <summary>
	/// Logs an error to the configured output destinations.
	/// </summary>
	/// <param name="trackingError">The tracking error to log.</param>
	/// <returns>A task representing the logging operation.</returns>
	private async Task LogErrorAsync(TrackingError trackingError) {
		try {
			var logLevel = trackingError.Severity switch {
				ErrorSeverity.Info => LogLevel.Information,
				ErrorSeverity.Warning => LogLevel.Warning,
				ErrorSeverity.Error => LogLevel.Error,
				ErrorSeverity.Critical => LogLevel.Critical,
				_ => LogLevel.Warning
			};

			_logger?.Log(logLevel,
				"[WhyDidYouRender] {Severity} {ErrorId}: {Message}{ComponentInfo}{MethodInfo}",
				trackingError.Severity,
				trackingError.ErrorId,
				trackingError.Message,
				!string.IsNullOrEmpty(trackingError.ComponentName) ? $" | Component: {trackingError.ComponentName}" : "",
				!string.IsNullOrEmpty(trackingError.TrackingMethod) ? $" | Method: {trackingError.TrackingMethod}" : "");

			var message = $"[WhyDidYouRender] {trackingError.Severity} {trackingError.ErrorId}: {trackingError.Message}";
			if (!string.IsNullOrEmpty(trackingError.ComponentName))
				message += $" | Component: {trackingError.ComponentName}";
			if (!string.IsNullOrEmpty(trackingError.TrackingMethod))
				message += $" | Method: {trackingError.TrackingMethod}";

			if (_config.Output.HasFlag(TrackingOutput.Console)) {
				Console.WriteLine(message);

				if (trackingError.Context.Count > 0) {
					var contextJson = JsonSerializer.Serialize(trackingError.Context, _jsonOptions);
					Console.WriteLine($"[WhyDidYouRender] Context: {contextJson}");
				}

				if (!string.IsNullOrEmpty(trackingError.StackTrace) && trackingError.Severity >= ErrorSeverity.Error)
					Console.WriteLine($"[WhyDidYouRender] Stack Trace: {trackingError.StackTrace}");
			}
		}
		catch (Exception ex) {
			Console.WriteLine($"[WhyDidYouRender] Failed to log error: {trackingError.Message} | Logging error: {ex.Message}");
		}
	}
}
