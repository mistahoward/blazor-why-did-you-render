using System;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

using Microsoft.JSInterop;
using Microsoft.Extensions.Logging;

using Blazor.WhyDidYouRender.Diagnostics;
using Blazor.WhyDidYouRender.Records;

namespace Blazor.WhyDidYouRender.Helpers;

/// <summary>
/// Service for logging render tracking information to the browser console via JavaScript interop.
/// </summary>
public class BrowserConsoleLogger : IBrowserConsoleLogger {
	private readonly IJSRuntime _jsRuntime;
	private IJSObjectReference? _module = null;
	private bool _isInitialized = false;
	private IErrorTracker? _errorTracker;

	/// <summary>
	/// Initializes a new instance of the <see cref="BrowserConsoleLogger"/> class.
	/// </summary>
	/// <param name="jsRuntime">The JavaScript runtime for interop.</param>
	public BrowserConsoleLogger(IJSRuntime jsRuntime) {
		_jsRuntime = jsRuntime;
	}

	/// <summary>
	/// Sets the error tracker for handling JavaScript interop errors.
	/// </summary>
	/// <param name="errorTracker">The error tracker instance.</param>
	public void SetErrorTracker(IErrorTracker errorTracker) {
		_errorTracker = errorTracker;
	}

	/// <summary>
	/// Initializes the browser console logger by loading the JavaScript module.
	/// </summary>
	public async Task InitializeAsync() {
		if (_isInitialized) return;

		try {
			await _jsRuntime.InvokeVoidAsync("console.log", "[WhyDidYouRender] Browser console logger initialized!");
			_isInitialized = true;
			Console.WriteLine("[WhyDidYouRender] Browser console logger successfully initialized");
		}
		catch (Exception ex) {
			Console.WriteLine($"[WhyDidYouRender] Failed to initialize browser console logger: {ex.Message}");
		}
	}

	/// <summary>
	/// Logs a render event to the browser console.
	/// </summary>
	/// <param name="renderEvent">The render event to log.</param>
	public async Task LogRenderEventAsync(RenderEvent renderEvent) {
		await SafeExecutor.ExecuteAsync(async () => {
			await LogRenderEventInternalAsync(renderEvent);
		}, _errorTracker, new Dictionary<string, object?> {
			["ComponentName"] = renderEvent.ComponentName,
			["Method"] = renderEvent.Method
		}, renderEvent.ComponentName, "BrowserConsoleLog");
	}

	/// <summary>
	/// Internal implementation of browser console logging.
	/// </summary>
	/// <param name="renderEvent">The render event to log.</param>
	private async Task LogRenderEventInternalAsync(RenderEvent renderEvent) {
		if (!_isInitialized) {
			await InitializeAsync();
		}

		if (!_isInitialized) return;

		try {
			var logData = new {
				timestamp = renderEvent.Timestamp.ToString("O"),
				component = renderEvent.ComponentName,
				componentType = renderEvent.ComponentType,
				method = renderEvent.Method,
				firstRender = renderEvent.FirstRender,
				duration = renderEvent.DurationMs,
				session = renderEvent.SessionId,
				parameterChanges = renderEvent.ParameterChanges
			};

			var message = $"🔄 WhyDidYouRender | {renderEvent.ComponentName} | {renderEvent.Method}";

			if (renderEvent.FirstRender.HasValue) {
				message += $" | firstRender: {renderEvent.FirstRender.Value}";
			}

			if (renderEvent.DurationMs.HasValue) {
				message += $" | {renderEvent.DurationMs.Value:F2}ms";
			}

			var consoleMethod = "console.groupCollapsed";
			var messageStyle = "color: #2196F3; font-weight: bold;";
			var icon = "🔄";

			if (renderEvent.IsUnnecessaryRerender) {
				consoleMethod = "console.group";
				messageStyle = "color: #FF5722; font-weight: bold; background-color: #FFEBEE; padding: 2px 4px; border-radius: 3px;";
				icon = "⚠️";
				message = $"{icon} UNNECESSARY RE-RENDER | {renderEvent.ComponentName} | {renderEvent.Method}";
			}
			else if (renderEvent.IsFrequentRerender) {
				messageStyle = "color: #FF9800; font-weight: bold;";
				icon = "🔥";
				message = $"{icon} FREQUENT RE-RENDER | {renderEvent.ComponentName} | {renderEvent.Method}";
			}

			await _jsRuntime.InvokeVoidAsync(consoleMethod,
				$"%c{message}",
				messageStyle);

			await _jsRuntime.InvokeVoidAsync("console.table", logData);

			if (renderEvent.IsUnnecessaryRerender && !string.IsNullOrEmpty(renderEvent.UnnecessaryRerenderReason)) {
				await _jsRuntime.InvokeVoidAsync("console.warn",
					$"💡 Optimization Tip: {renderEvent.UnnecessaryRerenderReason}");
			}

			if (renderEvent.IsFrequentRerender) {
				await _jsRuntime.InvokeVoidAsync("console.warn",
					"🔥 Performance Warning: This component is re-rendering frequently. Consider using ShouldRender(), reducing StateHasChanged() calls, or implementing IDisposable to unsubscribe from events.");
			}

			if (renderEvent.ParameterChanges?.Count > 0) {
				await _jsRuntime.InvokeVoidAsync("console.log",
					"%cParameter Changes:",
					"color: #FF9800; font-weight: bold;");

				foreach (var (paramName, change) in renderEvent.ParameterChanges) {
					await LogParameterChangeAsync(paramName, change);
				}
			}

			await _jsRuntime.InvokeVoidAsync("console.groupEnd");
		}
		catch (Exception ex) {
			Console.WriteLine($"[WhyDidYouRender] Browser logging failed: {ex.Message}");
		}
	}

	/// <summary>
	/// Logs a parameter change with enhanced object inspection.
	/// </summary>
	/// <param name="parameterName">The name of the parameter that changed.</param>
	/// <param name="changeData">The change data containing Previous and Current values.</param>
	private async Task LogParameterChangeAsync(string parameterName, object? changeData) {
		try {
			if (changeData != null) {
				var changeType = changeData.GetType();
				var previousProp = changeType.GetProperty("Previous");
				var currentProp = changeType.GetProperty("Current");

				if (previousProp != null && currentProp != null) {
					var previousValue = previousProp.GetValue(changeData);
					var currentValue = currentProp.GetValue(changeData);

					await _jsRuntime.InvokeVoidAsync("console.group",
						$"%c📝 {parameterName}",
						"color: #4CAF50; font-weight: bold;");

					await _jsRuntime.InvokeVoidAsync("console.log",
						"%cPrevious:",
						"color: #F44336; font-weight: bold;",
						previousValue);

					await _jsRuntime.InvokeVoidAsync("console.log",
						"%cCurrent:",
						"color: #2196F3; font-weight: bold;",
						currentValue);

					if (IsComplexObject(previousValue) && IsComplexObject(currentValue)) {
						await _jsRuntime.InvokeVoidAsync("console.log",
							"%cComparison:",
							"color: #9C27B0; font-weight: bold;");
						await _jsRuntime.InvokeVoidAsync("console.table", new {
							Previous = previousValue,
							Current = currentValue
						});
					}

					await _jsRuntime.InvokeVoidAsync("console.groupEnd");
				}
				else {
					await _jsRuntime.InvokeVoidAsync("console.log",
						$"%c{parameterName}:",
						"color: #FF9800; font-weight: bold;",
						changeData);
				}
			}
		}
		catch (Exception ex) {
			Console.WriteLine($"[WhyDidYouRender] Parameter logging failed: {ex.Message}");
			await _jsRuntime.InvokeVoidAsync("console.log",
				$"{parameterName}: {changeData}");
		}
	}

	/// <summary>
	/// Determines if a value is a complex object worth detailed inspection.
	/// </summary>
	/// <param name="value">The value to check.</param>
	/// <returns>True if the value is a complex object; otherwise, false.</returns>
	private static bool IsComplexObject(object? value) {
		if (value == null) return false;

		var type = value.GetType();

		return !type.IsPrimitive &&
			   type != typeof(string) &&
			   type != typeof(DateTime) &&
			   type != typeof(DateTimeOffset) &&
			   type != typeof(TimeSpan) &&
			   type != typeof(Guid) &&
			   !type.IsEnum;
	}

	/// <summary>
	/// Logs a simple message to the browser console.
	/// </summary>
	/// <param name="message">The message to log.</param>
	/// <param name="level">The console level (log, warn, error, etc.).</param>
	public async Task LogMessageAsync(string message, string level = "log") {
		if (!_isInitialized) {
			await InitializeAsync();
		}

		if (!_isInitialized) return;

		try {
			await _jsRuntime.InvokeVoidAsync($"console.{level}", message);
		}
		catch (Exception ex) {
			Console.WriteLine($"[WhyDidYouRender] Browser message logging failed: {ex.Message}");
		}
	}

	/// <summary>
	/// Disposes of the browser console logger resources.
	/// </summary>
	public async ValueTask DisposeAsync() {
		if (_module != null) {
			await _module.DisposeAsync();
		}
	}
}
