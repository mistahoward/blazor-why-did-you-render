using System.Text.Json;
using Microsoft.JSInterop;

using Blazor.WhyDidYouRender.Abstractions;
using Blazor.WhyDidYouRender.Configuration;
using Blazor.WhyDidYouRender.Records;

namespace Blazor.WhyDidYouRender.Services;

/// <summary>
/// WebAssembly implementation of error tracking service using browser storage.
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
	public bool SupportsPersistentStorage => _config.WasmStorage.UseLocalStorage;

	/// <inheritdoc />
	public bool SupportsErrorReporting => true; // can log to browser console

	/// <inheritdoc />
	public string ErrorTrackingDescription =>
		$"WebAssembly error tracking with browser storage (Local: {_config.WasmStorage.UseLocalStorage}, Session: {_config.WasmStorage.UseSessionStorage})";

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

		await ClearStoredErrorsAsync();
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
	/// Loads errors from browser storage into memory.
	/// </summary>
	/// <returns>A task representing the load operation.</returns>
	public async Task LoadErrorsFromStorageAsync() {
		try {
			var storageKey = GetErrorStorageKey();
			string? errorsJson = null;

			if (_config.WasmStorage.UseLocalStorage)
				errorsJson = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", storageKey);
			else if (_config.WasmStorage.UseSessionStorage)
				errorsJson = await _jsRuntime.InvokeAsync<string?>("sessionStorage.getItem", storageKey);

			if (!string.IsNullOrEmpty(errorsJson)) {
				var storedErrors = JsonSerializer.Deserialize<List<TrackingError>>(errorsJson, _jsonOptions);
				if (storedErrors != null)
					lock (_lock) {
						_memoryErrors.AddRange(storedErrors);
						_totalErrorCount = Math.Max(_totalErrorCount, _memoryErrors.Count);
					}
			}
		}
		catch (Exception ex) {
			await _jsRuntime.InvokeVoidAsync("console.warn", $"[WhyDidYouRender] Failed to load errors from storage: {ex.Message}");
		}
	}

	/// <summary>
	/// Performs automatic cleanup of old errors if configured.
	/// </summary>
	/// <returns>A task representing the cleanup operation.</returns>
	public async Task PerformErrorCleanupAsync() {
		if (!_config.WasmStorage.AutoCleanupStorage)
			return;

		try {
			var cutoff = DateTime.UtcNow.AddMinutes(-_config.WasmStorage.StorageCleanupIntervalMinutes);

			lock (_lock) {
				var errorsToKeep = _memoryErrors.Where(e => e.Timestamp >= cutoff).ToList();
				_memoryErrors.Clear();
				_memoryErrors.AddRange(errorsToKeep);
			}

			await SaveErrorsToStorageAsync();
		}
		catch (Exception ex) {
			await _jsRuntime.InvokeVoidAsync("console.warn", $"[WhyDidYouRender] Error cleanup failed: {ex.Message}");
		}
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

			bool shouldCleanup = _memoryErrors.Count > _config.WasmStorage.MaxStoredErrors;
			while (shouldCleanup)
				_memoryErrors.RemoveAt(0);
		}

		await SaveErrorsToStorageAsync();

		await LogErrorToBrowserConsoleAsync(trackingError);
	}

	/// <summary>
	/// Saves errors to browser storage.
	/// </summary>
	/// <returns>A task representing the save operation.</returns>
	private async Task SaveErrorsToStorageAsync() {
		try {
			List<TrackingError> errorsToSave;
			lock (_lock)
				errorsToSave = _memoryErrors.ToList();

			var errorsJson = JsonSerializer.Serialize(errorsToSave, _jsonOptions);

			if (errorsJson.Length > _config.WasmStorage.MaxStorageEntrySize) {
				var reducedErrors = errorsToSave.TakeLast(_config.WasmStorage.MaxStoredErrors / 2).ToList();
				errorsJson = JsonSerializer.Serialize(reducedErrors, _jsonOptions);
			}

			var storageKey = GetErrorStorageKey();

			if (_config.WasmStorage.UseLocalStorage)
				await _jsRuntime.InvokeVoidAsync("localStorage.setItem", storageKey, errorsJson);
			else if (_config.WasmStorage.UseSessionStorage)
				await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", storageKey, errorsJson);
		}
		catch (Exception ex) {
			await _jsRuntime.InvokeVoidAsync("console.warn", $"[WhyDidYouRender] Failed to save errors to storage: {ex.Message}");
		}
	}

	/// <summary>
	/// Clears stored errors from browser storage.
	/// </summary>
	/// <returns>A task representing the clear operation.</returns>
	private async Task ClearStoredErrorsAsync() {
		try {
			var storageKey = GetErrorStorageKey();

			if (_config.WasmStorage.UseLocalStorage)
				await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", storageKey);

			if (_config.WasmStorage.UseSessionStorage)
				await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", storageKey);
		}
		catch (Exception ex) {
			await _jsRuntime.InvokeVoidAsync("console.warn", $"[WhyDidYouRender] Failed to clear stored errors: {ex.Message}");
		}
	}

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
	/// Gets the storage key for errors.
	/// </summary>
	/// <returns>The storage key for errors.</returns>
	private string GetErrorStorageKey() {
		return $"{_config.WasmStorage.StorageKeyPrefix}errors";
	}
}
