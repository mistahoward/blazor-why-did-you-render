using System.Text.Json;

using Blazor.WhyDidYouRender.Abstractions;
using Blazor.WhyDidYouRender.Configuration;
using Blazor.WhyDidYouRender.Helpers;
using Blazor.WhyDidYouRender.Records;
using Blazor.WhyDidYouRender.Logging;

namespace Blazor.WhyDidYouRender.Services;

/// <summary>
/// Server-side implementation of tracking logger supporting both server console and browser console output.
/// </summary>
public class ServerTrackingLogger : ITrackingLogger {
	private readonly WhyDidYouRenderConfig _config;
	private readonly IBrowserConsoleLogger? _browserLogger;
	private readonly JsonSerializerOptions _jsonOptions;
	private readonly IWhyDidYouRenderLogger? _unifiedLogger;
	private bool _isInitialized = false;

	/// <summary>
	/// Initializes a new instance of the <see cref="ServerTrackingLogger"/> class.
	/// </summary>
	/// <param name="config">The WhyDidYouRender configuration.</param>
	/// <param name="browserLogger">Optional browser console logger for client-side output.</param>
	/// <param name="unifiedLogger">Optional unified logger to route server logs.</param>
	public ServerTrackingLogger(WhyDidYouRenderConfig config, IBrowserConsoleLogger? browserLogger = null, IWhyDidYouRenderLogger? unifiedLogger = null) {
		_config = config ?? throw new ArgumentNullException(nameof(config));
		_browserLogger = browserLogger;
		_unifiedLogger = unifiedLogger;

		_jsonOptions = new JsonSerializerOptions {
			WriteIndented = false,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase
		};
	}

	/// <inheritdoc />
	public bool SupportsServerConsole => true;

	/// <inheritdoc />
	public bool SupportsBrowserConsole => _browserLogger != null;

	/// <inheritdoc />
	public string LoggingDescription =>
		$"Server-side logger (Console: {SupportsServerConsole}, Browser: {SupportsBrowserConsole})";

	/// <inheritdoc />
	public Task InitializeAsync() {
		if (_isInitialized) return Task.CompletedTask;

		if (_unifiedLogger != null)
			_unifiedLogger.LogInfo("Server tracking logger initialized");
		else
			Console.WriteLine("[WhyDidYouRender] Server tracking logger initialized");
		_isInitialized = true;

		return Task.CompletedTask;
	}

	/// <inheritdoc />
	public async Task LogRenderEventAsync(RenderEvent renderEvent) {
		if (!_config.Enabled) return;

		try {
			if (_config.Output.HasFlag(TrackingOutput.Console)) {
				if (_unifiedLogger != null)
					_unifiedLogger.LogRenderEvent(renderEvent);
				else
					LogToServerConsole(renderEvent);
			}

			if (_config.Output.HasFlag(TrackingOutput.BrowserConsole) && _browserLogger != null)
				await _browserLogger.LogRenderEventAsync(renderEvent);
		}
		catch (Exception ex) {
			if (_unifiedLogger != null) _unifiedLogger.LogError("Logging failed", ex);
			else Console.WriteLine($"[WhyDidYouRender] Logging failed: {ex.Message}");
		}
	}

	/// <inheritdoc />
	public async Task LogMessageAsync(TrackingVerbosity verbosity, string message, object? data = null) {
		if (!_config.Enabled || verbosity < _config.Verbosity) return;

		try {
			var formattedMessage = $"[WhyDidYouRender] {message}";

			if (_config.Output.HasFlag(TrackingOutput.Console)) {
				if (_unifiedLogger != null) {
					var dict = data != null ? new Dictionary<string, object?> { ["data"] = data } : null;
					var level = verbosity >= TrackingVerbosity.Verbose ? LogLevel.Debug : LogLevel.Info;
					if (level == LogLevel.Debug) _unifiedLogger.LogDebug(formattedMessage, dict);
					else _unifiedLogger.LogInfo(formattedMessage, dict);
				}
				else {
					Console.WriteLine(formattedMessage);
					if (data != null) {
						var dataJson = JsonSerializer.Serialize(data, _jsonOptions);
						Console.WriteLine($"  Data: {dataJson}");
					}
				}
			}

			if (_config.Output.HasFlag(TrackingOutput.BrowserConsole) && _browserLogger != null) {
				var level = verbosity switch {
					TrackingVerbosity.Minimal => "log",
					TrackingVerbosity.Normal => "log",
					TrackingVerbosity.Verbose => "debug",
					_ => "log"
				};

				await _browserLogger.LogMessageAsync(formattedMessage, level);
			}
		}
		catch (Exception ex) {
			if (_unifiedLogger != null) _unifiedLogger.LogError("Message logging failed", ex);
			else Console.WriteLine($"[WhyDidYouRender] Message logging failed: {ex.Message}");
		}
	}

	/// <inheritdoc />
	public async Task LogErrorAsync(string message, Exception? exception = null, Dictionary<string, object?>? context = null) {
		try {
			var errorMessage = $"[WhyDidYouRender] ERROR: {message}";

			if (_config.Output.HasFlag(TrackingOutput.Console)) {
				if (_unifiedLogger != null) {
					_unifiedLogger.LogError(errorMessage, exception, context);
				}
				else {
					Console.WriteLine(errorMessage);
					if (exception != null) {
						Console.WriteLine($"  Exception: {exception.Message}");
						if (_config.Verbosity >= TrackingVerbosity.Verbose)
							Console.WriteLine($"  Stack Trace: {exception.StackTrace}");
					}
					if (context?.Count > 0) {
						var contextJson = JsonSerializer.Serialize(context, _jsonOptions);
						Console.WriteLine($"  Context: {contextJson}");
					}
				}
			}

			if (_config.Output.HasFlag(TrackingOutput.BrowserConsole) && _browserLogger != null)
				await _browserLogger.LogMessageAsync(errorMessage, "error");
		}
		catch (Exception ex) {
			if (_unifiedLogger != null) _unifiedLogger.LogError("Error logging failed", ex);
			else Console.WriteLine($"[WhyDidYouRender] Error logging failed: {ex.Message}");
		}
	}

	/// <inheritdoc />
	public async Task LogWarningAsync(string message, Dictionary<string, object?>? context = null) {
		try {
			var warningMessage = $"[WhyDidYouRender] WARNING: {message}";

			if (_config.Output.HasFlag(TrackingOutput.Console)) {
				if (_unifiedLogger != null) {
					_unifiedLogger.LogWarning(warningMessage, context);
				}
				else {
					Console.WriteLine(warningMessage);
					if (context?.Count > 0) {
						var contextJson = JsonSerializer.Serialize(context, _jsonOptions);
						Console.WriteLine($"  Context: {contextJson}");
					}
				}
			}

			if (_config.Output.HasFlag(TrackingOutput.BrowserConsole) && _browserLogger != null)
				await _browserLogger.LogMessageAsync(warningMessage, "warn");
		}
		catch (Exception ex) {
			if (_unifiedLogger != null) _unifiedLogger.LogError("Warning logging failed", ex);
			else Console.WriteLine($"[WhyDidYouRender] Warning logging failed: {ex.Message}");
		}
	}

	/// <inheritdoc />
	public async Task LogParameterChangesAsync(string componentName, Dictionary<string, object?> parameterChanges) {
		if (!_config.Enabled || !_config.TrackParameterChanges || parameterChanges.Count == 0) return;

		try {
			if (_config.Output.HasFlag(TrackingOutput.Console)) {
				if (_unifiedLogger != null) {
					_unifiedLogger.LogParameterChanges(componentName, parameterChanges);
				}
				else {
					Console.WriteLine($"[WhyDidYouRender] Parameter changes for {componentName}:");
					foreach (var (paramName, change) in parameterChanges) {
						try {
							var changeJson = JsonSerializer.Serialize(change, _jsonOptions);
							Console.WriteLine($"  {paramName}: {changeJson}");
						}
						catch {
							Console.WriteLine($"  {paramName}: [Unable to serialize]");
						}
					}
				}
			}

			if (_config.Output.HasFlag(TrackingOutput.BrowserConsole) && _browserLogger != null)
				await _browserLogger.LogMessageAsync($"Parameter changes for {componentName}", "log");
		}
		catch (Exception ex) {
			if (_unifiedLogger != null) _unifiedLogger.LogError("Parameter change logging failed", ex);
			else Console.WriteLine($"[WhyDidYouRender] Parameter change logging failed: {ex.Message}");
		}
	}

	/// <inheritdoc />
	public async Task LogPerformanceAsync(string componentName, string method, double durationMs, Dictionary<string, object?>? additionalMetrics = null) {
		if (!_config.Enabled || !_config.TrackPerformance) return;

		try {
			var performanceMessage = $"[WhyDidYouRender] Performance: {componentName}.{method}() took {durationMs:F2}ms";

			if (_config.Output.HasFlag(TrackingOutput.Console)) {
				if (_unifiedLogger != null) {
					_unifiedLogger.LogPerformance(componentName, method, durationMs, additionalMetrics);
				}
				else {
					Console.WriteLine(performanceMessage);
					if (additionalMetrics?.Count > 0) {
						var metricsJson = JsonSerializer.Serialize(additionalMetrics, _jsonOptions);
						Console.WriteLine($"  Additional metrics: {metricsJson}");
					}
				}
			}

			if (_config.Output.HasFlag(TrackingOutput.BrowserConsole) && _browserLogger != null)
				await _browserLogger.LogMessageAsync(performanceMessage, "log");
		}
		catch (Exception ex) {
			if (_unifiedLogger != null) _unifiedLogger.LogError("Performance logging failed", ex);
			else Console.WriteLine($"[WhyDidYouRender] Performance logging failed: {ex.Message}");
		}
	}

	/// <summary>
	/// Logs a render event to the server console.
	/// </summary>
	/// <param name="renderEvent">The render event to log.</param>
	private void LogToServerConsole(RenderEvent renderEvent) {
		var message = FormatConsoleMessage(renderEvent);
		Console.WriteLine(message);

		if (_config.Verbosity >= TrackingVerbosity.Verbose) {
			if (renderEvent.ParameterChanges?.Count > 0)
				LogParameterChangesToConsole(renderEvent);

			if (renderEvent.StateChanges?.Count > 0)
				LogStateChangesToConsole(renderEvent);
		}
	}

	/// <summary>
	/// Formats a render event for console output.
	/// </summary>
	/// <param name="renderEvent">The render event to format.</param>
	/// <returns>A formatted string for console output.</returns>
	private string FormatConsoleMessage(RenderEvent renderEvent) {
		var parts = new List<string> { $"[WhyDidYouRender] {renderEvent.ComponentName}.{renderEvent.Method}()" };

		if (_config.Verbosity >= TrackingVerbosity.Normal) {
			if (renderEvent.DurationMs.HasValue)
				parts.Add($"({renderEvent.DurationMs:F2}ms)");

			if (_config.IncludeSessionInfo && !string.IsNullOrEmpty(renderEvent.SessionId))
				parts.Add($"[{renderEvent.SessionId}]");
		}

		if (renderEvent.IsUnnecessaryRerender)
			parts.Add($"⚠️ UNNECESSARY: {renderEvent.UnnecessaryRerenderReason}");

		if (renderEvent.IsFrequentRerender)
			parts.Add("🔥 FREQUENT");

		return string.Join(" ", parts);
	}

	/// <summary>
	/// Logs parameter changes to the console.
	/// </summary>
	/// <param name="renderEvent">The render event containing parameter changes.</param>
	private void LogParameterChangesToConsole(RenderEvent renderEvent) {
		if (renderEvent.ParameterChanges?.Count > 0 != true) return;

		Console.WriteLine("  Parameter changes:");
		foreach (var (paramName, change) in renderEvent.ParameterChanges) {
			try {
				var changeJson = JsonSerializer.Serialize(change, _jsonOptions);
				Console.WriteLine($"    {paramName}: {changeJson}");
			}
			catch {
				Console.WriteLine($"    {paramName}: [Unable to serialize]");
			}
		}
	}

	/// <summary>
	/// Logs state changes to the console.
	/// </summary>
	/// <param name="renderEvent">The render event containing state changes.</param>
	private void LogStateChangesToConsole(RenderEvent renderEvent) {
		if (renderEvent.StateChanges?.Count > 0 != true) return;

		Console.WriteLine("  State changes:");
		foreach (var stateChange in renderEvent.StateChanges) {
			try {
				var changeInfo = new {
					Field = stateChange.FieldName,
					Previous = stateChange.PreviousValue,
					Current = stateChange.CurrentValue,
					Type = stateChange.ChangeType.ToString(),
					Description = stateChange.GetFormattedDescription()
				};
				var changeJson = JsonSerializer.Serialize(changeInfo, _jsonOptions);
				Console.WriteLine($"    {stateChange.FieldName}: {changeJson}");
			}
			catch {
				Console.WriteLine($"    {stateChange.FieldName}: [Unable to serialize]");
			}
		}
	}
}
