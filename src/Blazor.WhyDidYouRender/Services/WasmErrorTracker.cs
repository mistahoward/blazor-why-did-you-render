using System.Text.Json;
using Microsoft.JSInterop;

using Blazor.WhyDidYouRender.Abstractions;
using Blazor.WhyDidYouRender.Configuration;
using Blazor.WhyDidYouRender.Records;

namespace Blazor.WhyDidYouRender.Services;

/// <summary>
/// WebAssembly implementation of error tracking service (in-memory; browser console logging).
/// </summary>
public class WasmErrorTracker : IErrorTracker {
	private readonly IJSRuntime _jsRuntime;
	private readonly WhyDidYouRenderConfig _config;
	private readonly JsonSerializerOptions _jsonOptions;
	private readonly List<TrackingError> _memoryErrors = [];
	private readonly Lock _lock = new();
	private int _totalErrorCount = 0;

	/// <summary>
	/// Initializes a new instance of the <see cref="WasmErrorTracker"/> class.
	/// </summary>
	/// <param name="jsRuntime">The JavaScript runtime for browser interop.</param>
	/// <param name="config">The WhyDidYouRender configuration.</param>
	public WasmErrorTracker(IJSRuntime jsRuntime, WhyDidYouRenderConfig config) {
		_jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
		_config = config ?? throw new ArgumentNullException(nameof(config));

		_jsonOptions = new JsonSerializerOptions {
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			WriteIndented = false
		};
	}

	/// <inheritdoc />
	public bool SupportsPersistentStorage => false;

	/// <inheritdoc />
	public bool SupportsErrorReporting => true; // can log to browser console

	/// <inheritdoc />
	public string ErrorTrackingDescription =>
		"WebAssembly error tracking (in-memory only; browser console)";

	/// <inheritdoc />
	public async Task TrackErrorAsync(Exception exception, Dictionary<string, object?> context, ErrorSeverity severity, string? componentName = null, string? operation = null) {
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

		await TrackErrorInternalAsync(trackingError);
	}

	/// <inheritdoc />
	public async Task TrackErrorAsync(string message, Dictionary<string, object?> context, ErrorSeverity severity, string? componentName = null, string? operation = null) {
		var trackingError = new TrackingError {
			ErrorId = Guid.NewGuid().ToString("N")[..8], // match our existing format
			Message = message,
			Context = new Dictionary<string, object?>(context),
			Severity = severity,
			ComponentName = componentName,
			TrackingMethod = operation,
			Timestamp = DateTime.UtcNow
		};

		await TrackErrorInternalAsync(trackingError);
	}

	/// <inheritdoc />
	public Task<IEnumerable<TrackingError>> GetRecentErrorsAsync(int count = 50, ErrorSeverity? severity = null, string? componentName = null) {
		lock (_lock) {
			var errors = _memoryErrors.AsEnumerable();

			if (severity.HasValue)
				errors = errors.Where(e => e.Severity >= severity.Value);

			if (!string.IsNullOrEmpty(componentName))
				errors = errors.Where(e => string.Equals(e.ComponentName, componentName, StringComparison.OrdinalIgnoreCase));

			var result = errors.TakeLast(count).ToList();
			return Task.FromResult<IEnumerable<TrackingError>>(result);
		}
	}

	/// <inheritdoc />
	public Task<ErrorStatistics> GetErrorStatisticsAsync() {
		lock (_lock) {
			var errors = _memoryErrors.ToArray();
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
	}

	/// <inheritdoc />
	public async Task ClearErrorsAsync() {
		lock (_lock) {
			_memoryErrors.Clear();
			_totalErrorCount = 0;
		}

		await Task.CompletedTask;
	}

	/// <inheritdoc />
	public Task<int> GetErrorCountAsync() {
		return Task.FromResult(_totalErrorCount);
	}

	/// <inheritdoc />
	public Task<IEnumerable<TrackingError>> GetErrorsSinceAsync(DateTime since, DateTime? until = null) {
		lock (_lock) {
			var errors = _memoryErrors.AsEnumerable();

			errors = errors.Where(e => e.Timestamp >= since);

			if (until.HasValue)
				errors = errors.Where(e => e.Timestamp <= until.Value);

			var result = errors.ToList();
			return Task.FromResult<IEnumerable<TrackingError>>(result);
		}
	}

	/// <summary>
	/// No-op: browser storage has been removed. Errors are kept in-memory only.
	/// </summary>
	/// <returns>A task representing the (no-op) load operation.</returns>
	public async Task LoadErrorsFromStorageAsync() {
		await Task.CompletedTask;
	}

	/// <summary>
	/// No-op: browser storage cleanup removed. In-memory list is pruned during tracking.
	/// </summary>
	/// <returns>A task representing the (no-op) cleanup operation.</returns>
	public async Task PerformErrorCleanupAsync() {
		await Task.CompletedTask;
	}

	/// <summary>
	/// Internal method to track an error.
	/// </summary>
	/// <param name="trackingError">The tracking error to track.</param>
	/// <returns>A task representing the tracking operation.</returns>
	private async Task TrackErrorInternalAsync(TrackingError trackingError) {
		lock (_lock) {
			_memoryErrors.Add(trackingError);
			_totalErrorCount++;

			while (_memoryErrors.Count > _config.MaxErrorHistorySize)
				_memoryErrors.RemoveAt(0);
		}

		await LogErrorToBrowserConsoleAsync(trackingError);
	}

	/// <summary>
	/// No-op: browser storage has been removed.
	/// </summary>
	/// <returns>A completed task.</returns>
	private Task SaveErrorsToStorageAsync() => Task.CompletedTask;

	/// <summary>
	/// No-op: browser storage has been removed.
	/// </summary>
	/// <returns>A completed task.</returns>
	private Task ClearStoredErrorsAsync() => Task.CompletedTask;

	/// <summary>
	/// Logs an error to the browser console.
	/// </summary>
	/// <param name="trackingError">The tracking error to log.</param>
	/// <returns>A task representing the logging operation.</returns>
	private async Task LogErrorToBrowserConsoleAsync(TrackingError trackingError) {
		try {
			var consoleMethod = trackingError.Severity switch {
				ErrorSeverity.Info => "log",
				ErrorSeverity.Warning => "warn",
				ErrorSeverity.Error => "error",
				ErrorSeverity.Critical => "error",
				_ => "warn"
			};

			var message = $"[WhyDidYouRender] {trackingError.Severity} {trackingError.ErrorId}: {trackingError.Message}";

			var logData = new Dictionary<string, object?> {
				["errorId"] = trackingError.ErrorId,
				["severity"] = trackingError.Severity.ToString(),
				["timestamp"] = trackingError.Timestamp.ToString("HH:mm:ss.fff"),
				["context"] = trackingError.Context
			};

			if (!string.IsNullOrEmpty(trackingError.ComponentName))
				logData["component"] = trackingError.ComponentName;

			if (!string.IsNullOrEmpty(trackingError.TrackingMethod))
				logData["method"] = trackingError.TrackingMethod;

			if (!string.IsNullOrEmpty(trackingError.StackTrace))
				logData["stackTrace"] = trackingError.StackTrace;

			await _jsRuntime.InvokeVoidAsync($"console.{consoleMethod}", message, logData);
		}
		catch {
			await _jsRuntime.InvokeVoidAsync("console.error", $"[WhyDidYouRender] Error: {trackingError.Message}");
		}
	}

	/// <summary>
	/// Deprecated: storage key no longer used; keeping method for compatibility.
	/// </summary>
	/// <returns>A constant key.</returns>
	private string GetErrorStorageKey() {
		return "errors";
	}
}
