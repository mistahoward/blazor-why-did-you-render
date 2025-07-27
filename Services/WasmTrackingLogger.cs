using System.Text.Json;
using Microsoft.JSInterop;

using Blazor.WhyDidYouRender.Abstractions;
using Blazor.WhyDidYouRender.Configuration;
using Blazor.WhyDidYouRender.Records;

namespace Blazor.WhyDidYouRender.Services;

/// <summary>
/// WebAssembly implementation of tracking logger supporting browser console output only.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="WasmTrackingLogger"/> class.
/// </remarks>
/// <param name="jsRuntime">The JavaScript runtime for browser interop.</param>
/// <param name="config">The WhyDidYouRender configuration.</param>
public class WasmTrackingLogger(IJSRuntime jsRuntime, WhyDidYouRenderConfig config) : ITrackingLogger {
	private readonly IJSRuntime _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
	private readonly WhyDidYouRenderConfig _config = config ?? throw new ArgumentNullException(nameof(config));
	private bool _isInitialized = false;


	/// <inheritdoc />
	public bool SupportsServerConsole => false; // ! WASM doesn't have server console

	/// <inheritdoc />
	public bool SupportsBrowserConsole => true;

	/// <inheritdoc />
	public string LoggingDescription => "WebAssembly browser console logger";

	/// <inheritdoc />
	public async Task InitializeAsync() {
		if (_isInitialized) return;

		try {
			await _jsRuntime.InvokeVoidAsync("console.log", "[WhyDidYouRender] WASM tracking logger initialized");
			_isInitialized = true;
		}
		catch (Exception ex) {
			Console.WriteLine($"[WhyDidYouRender] Failed to initialize WASM logger: {ex.Message}");
		}
	}

	/// <inheritdoc />
	public async Task LogRenderEventAsync(RenderEvent renderEvent) {
		if (!_config.Enabled) return;

		try {
			var groupTitle = $"🔄 {renderEvent.ComponentName}.{renderEvent.Method}()";

			if (renderEvent.DurationMs.HasValue)
				groupTitle += $" ({renderEvent.DurationMs:F2}ms)";

			if (renderEvent.IsUnnecessaryRerender)
				groupTitle += " ⚠️ UNNECESSARY";

			if (renderEvent.IsFrequentRerender)
				groupTitle += " 🔥 FREQUENT";

			await _jsRuntime.InvokeVoidAsync("console.group", groupTitle);

			await LogRenderEventDetailsAsync(renderEvent);

			bool shouldLogParameterChanges = _config.TrackParameterChanges && renderEvent.ParameterChanges?.Count > 0;
			if (shouldLogParameterChanges) {
				await LogParameterChangesAsync(renderEvent.ParameterChanges!);
			}

			bool shouldLogPerformance = _config.TrackPerformance && renderEvent.DurationMs.HasValue;
			if (shouldLogPerformance)
				await LogPerformanceInfoAsync(renderEvent);

			await _jsRuntime.InvokeVoidAsync("console.groupEnd");
		}
		catch (Exception ex) {
			await _jsRuntime.InvokeVoidAsync("console.error", $"[WhyDidYouRender] Logging failed: {ex.Message}");
		}
	}

	/// <inheritdoc />
	public async Task LogMessageAsync(TrackingVerbosity verbosity, string message, object? data = null) {
		if (!_config.Enabled || verbosity < _config.Verbosity) return;

		try {
			var level = verbosity switch {
				TrackingVerbosity.Minimal => "log",
				TrackingVerbosity.Normal => "log",
				TrackingVerbosity.Verbose => "debug",
				_ => "log"
			};

			var formattedMessage = $"[WhyDidYouRender] {message}";

			if (data != null)
				await _jsRuntime.InvokeVoidAsync($"console.{level}", formattedMessage, data);
			else
				await _jsRuntime.InvokeVoidAsync($"console.{level}", formattedMessage);
		}
		catch (Exception ex) {
			await _jsRuntime.InvokeVoidAsync("console.error", $"[WhyDidYouRender] Message logging failed: {ex.Message}");
		}
	}

	/// <inheritdoc />
	public async Task LogErrorAsync(string message, Exception? exception = null, Dictionary<string, object?>? context = null) {
		try {
			var errorMessage = $"[WhyDidYouRender] ERROR: {message}";

			if (exception != null) {
				var errorData = new {
					message = exception.Message,
					type = exception.GetType().Name,
					stackTrace = exception.StackTrace,
					context
				};

				await _jsRuntime.InvokeVoidAsync("console.error", errorMessage, errorData);
			}
			else {
				if (context?.Count > 0)
					await _jsRuntime.InvokeVoidAsync("console.error", errorMessage, context);
				else
					await _jsRuntime.InvokeVoidAsync("console.error", errorMessage);
			}
		}
		catch (Exception ex) {
			await _jsRuntime.InvokeVoidAsync("console.error", $"[WhyDidYouRender] Error logging failed: {ex.Message}");
		}
	}

	/// <inheritdoc />
	public async Task LogWarningAsync(string message, Dictionary<string, object?>? context = null) {
		try {
			var warningMessage = $"[WhyDidYouRender] WARNING: {message}";

			if (context?.Count > 0)
				await _jsRuntime.InvokeVoidAsync("console.warn", warningMessage, context);
			else
				await _jsRuntime.InvokeVoidAsync("console.warn", warningMessage);
		}
		catch (Exception ex) {
			await _jsRuntime.InvokeVoidAsync("console.error", $"[WhyDidYouRender] Warning logging failed: {ex.Message}");
		}
	}

	/// <inheritdoc />
	public async Task LogParameterChangesAsync(string componentName, Dictionary<string, object?> parameterChanges) {
		if (!_config.Enabled || !_config.TrackParameterChanges || parameterChanges.Count == 0) return;

		try {
			await _jsRuntime.InvokeVoidAsync("console.group", $"📊 Parameter changes for {componentName}");

			await _jsRuntime.InvokeVoidAsync("console.table", parameterChanges);

			await _jsRuntime.InvokeVoidAsync("console.groupEnd");
		}
		catch (Exception ex) {
			await _jsRuntime.InvokeVoidAsync("console.error", $"[WhyDidYouRender] Parameter change logging failed: {ex.Message}");
		}
	}

	/// <inheritdoc />
	public async Task LogPerformanceAsync(string componentName, string method, double durationMs, Dictionary<string, object?>? additionalMetrics = null) {
		if (!_config.Enabled || !_config.TrackPerformance) return;

		try {
			var performanceData = new Dictionary<string, object?> {
				["component"] = componentName,
				["method"] = method,
				["duration"] = $"{durationMs:F2}ms"
			};

			if (additionalMetrics?.Count > 0)
				foreach (var (key, value) in additionalMetrics)
					performanceData[key] = value;

			var message = $"⚡ Performance: {componentName}.{method}()";

			await _jsRuntime.InvokeVoidAsync("console.log", message, performanceData);
		}
		catch (Exception ex) {
			await _jsRuntime.InvokeVoidAsync("console.error", $"[WhyDidYouRender] Performance logging failed: {ex.Message}");
		}
	}

	/// <summary>
	/// Logs detailed render event information to the browser console.
	/// </summary>
	/// <param name="renderEvent">The render event to log.</param>
	/// <returns>A task representing the logging operation.</returns>
	private async Task LogRenderEventDetailsAsync(RenderEvent renderEvent) {
		var details = new Dictionary<string, object?> {
			["component"] = renderEvent.ComponentName,
			["method"] = renderEvent.Method,
			["timestamp"] = renderEvent.Timestamp.ToString("HH:mm:ss.fff")
		};

		if (_config.IncludeSessionInfo && !string.IsNullOrEmpty(renderEvent.SessionId))
			details["sessionId"] = renderEvent.SessionId;

		if (renderEvent.DurationMs.HasValue)
			details["duration"] = $"{renderEvent.DurationMs:F2}ms";

		if (renderEvent.IsUnnecessaryRerender)
			details["unnecessaryReason"] = renderEvent.UnnecessaryRerenderReason;

		if (renderEvent.IsFrequentRerender)
			details["frequentRerender"] = true;

		await _jsRuntime.InvokeVoidAsync("console.table", details);
	}

	/// <summary>
	/// Logs parameter changes to the browser console.
	/// </summary>
	/// <param name="parameterChanges">The parameter changes to log.</param>
	/// <returns>A task representing the logging operation.</returns>
	private async Task LogParameterChangesAsync(Dictionary<string, object?> parameterChanges) {
		await _jsRuntime.InvokeVoidAsync("console.group", "📋 Parameter Changes");

		await _jsRuntime.InvokeVoidAsync("console.table", parameterChanges);

		foreach (var (paramName, change) in parameterChanges)
			await _jsRuntime.InvokeVoidAsync("console.log", $"  {paramName}:", change);

		await _jsRuntime.InvokeVoidAsync("console.groupEnd");
	}

	/// <summary>
	/// Logs performance information to the browser console.
	/// </summary>
	/// <param name="renderEvent">The render event containing performance information.</param>
	/// <returns>A task representing the logging operation.</returns>
	private async Task LogPerformanceInfoAsync(RenderEvent renderEvent) {
		var performanceData = new Dictionary<string, object?> {
			["duration"] = $"{renderEvent.DurationMs:F2}ms",
			["timestamp"] = renderEvent.Timestamp.ToString("HH:mm:ss.fff")
		};

		bool longerThanOneFrame = renderEvent.DurationMs > 16;
		if (longerThanOneFrame)
			performanceData["warning"] = "Render took longer than 16ms (60fps frame)";

		if (renderEvent.IsFrequentRerender)
			performanceData["frequentRerenders"] = "Component is re-rendering frequently";

		await _jsRuntime.InvokeVoidAsync("console.group", "⚡ Performance Metrics");
		await _jsRuntime.InvokeVoidAsync("console.table", performanceData);
		await _jsRuntime.InvokeVoidAsync("console.groupEnd");
	}
}
