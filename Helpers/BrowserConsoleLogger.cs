using Microsoft.JSInterop;

using Blazor.WhyDidYouRender.Diagnostics;
using Blazor.WhyDidYouRender.Records;

namespace Blazor.WhyDidYouRender.Helpers;

/// <summary>
/// Service for logging render tracking information to the browser console via JavaScript interop.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="BrowserConsoleLogger"/> class.
/// </remarks>
/// <param name="jsRuntime">The JavaScript runtime for interop.</param>
public class BrowserConsoleLogger(IJSRuntime jsRuntime) : IBrowserConsoleLogger {
	private readonly IJSRuntime _jsRuntime = jsRuntime;
	private IJSObjectReference? _module = null;
	private bool _isInitialized = false;
	private IErrorTracker? _errorTracker;


	/// <summary>
	/// Sets the error tracker for handling JavaScript interop errors.
	/// </summary>
	/// <param name="errorTracker">The error tracker instance.</param>
	public void SetErrorTracker(IErrorTracker errorTracker) =>
		_errorTracker = errorTracker;

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
		// use a timeout to prevent hanging JS interop calls
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

		await SafeExecutor.ExecuteAsync(async () => {
			await LogRenderEventInternalAsync(renderEvent, cts.Token);
		}, _errorTracker, new Dictionary<string, object?> {
			["ComponentName"] = renderEvent.ComponentName,
			["Method"] = renderEvent.Method
		}, renderEvent.ComponentName, "BrowserConsoleLog");
	}

	/// <summary>
	/// Internal implementation of browser console logging.
	/// </summary>
	/// <param name="renderEvent">The render event to log.</param>
	/// <param name="cancellationToken">Cancellation token to prevent hanging calls.</param>
	private async Task LogRenderEventInternalAsync(RenderEvent renderEvent, CancellationToken cancellationToken = default) {
		if (!_isInitialized) {
			await InitializeAsync();
		}

		if (!_isInitialized) return;

		try {
			cancellationToken.ThrowIfCancellationRequested();

			var logData = new {
				timestamp = renderEvent.Timestamp.ToString("O"),
				component = renderEvent.ComponentName,
				componentType = renderEvent.ComponentType,
				method = renderEvent.Method,
				firstRender = renderEvent.FirstRender,
				duration = renderEvent.DurationMs,
				session = renderEvent.SessionId,
				parameterChanges = renderEvent.ParameterChanges,
				stateChanges = renderEvent.StateChanges
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

			await _jsRuntime.InvokeVoidAsync(consoleMethod, cancellationToken,
				$"%c{message}",
				messageStyle);

			cancellationToken.ThrowIfCancellationRequested();
			await _jsRuntime.InvokeVoidAsync("console.table", cancellationToken, logData);

			if (renderEvent.IsUnnecessaryRerender && !string.IsNullOrEmpty(renderEvent.UnnecessaryRerenderReason)) {
				cancellationToken.ThrowIfCancellationRequested();
				await _jsRuntime.InvokeVoidAsync("console.warn", cancellationToken,
					$"💡 Optimization Tip: {renderEvent.UnnecessaryRerenderReason}");
			}

			if (renderEvent.IsFrequentRerender) {
				cancellationToken.ThrowIfCancellationRequested();
				await _jsRuntime.InvokeVoidAsync("console.warn", cancellationToken,
					"🔥 Performance Warning: This component is re-rendering frequently. Consider using ShouldRender(), reducing StateHasChanged() calls, or implementing IDisposable to unsubscribe from events.");
			}

			if (renderEvent.ParameterChanges?.Count > 0) {
				cancellationToken.ThrowIfCancellationRequested();
				await _jsRuntime.InvokeVoidAsync("console.log", cancellationToken,
					"%cParameter Changes:",
					"color: #FF9800; font-weight: bold;");

				foreach (var (paramName, change) in renderEvent.ParameterChanges) {
					cancellationToken.ThrowIfCancellationRequested();
					await LogParameterChangeAsync(paramName, change, cancellationToken);
				}
			}

			if (renderEvent.StateChanges?.Count > 0) {
				cancellationToken.ThrowIfCancellationRequested();
				await _jsRuntime.InvokeVoidAsync("console.log", cancellationToken,
					"%cState Changes:",
					"color: #4CAF50; font-weight: bold;");

				await LogStateChangesAsync(renderEvent.StateChanges, cancellationToken);
			}

			cancellationToken.ThrowIfCancellationRequested();
			await _jsRuntime.InvokeVoidAsync("console.groupEnd", cancellationToken);
		}
		catch (OperationCanceledException) {
			// Expected when renders happen quickly - no action needed
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
	/// <param name="cancellationToken">Cancellation token to prevent hanging calls.</param>
	private async Task LogParameterChangeAsync(string parameterName, object? changeData, CancellationToken cancellationToken = default) {
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

					if (TypeHelper.IsComplexObject(previousValue) && TypeHelper.IsComplexObject(currentValue)) {
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
	/// Logs state changes to the browser console.
	/// </summary>
	/// <param name="stateChanges">The state changes to log.</param>
	/// <param name="cancellationToken">Cancellation token to prevent hanging calls.</param>
	private async Task LogStateChangesAsync(List<StateChange> stateChanges, CancellationToken cancellationToken = default) {
		try {
			cancellationToken.ThrowIfCancellationRequested();

			var stateChangeTable = stateChanges.ToDictionary(
				sc => sc.FieldName,
				sc => new {
					Previous = sc.PreviousValue,
					Current = sc.CurrentValue,
					Type = sc.ChangeType.ToString()
				}
			);

			await _jsRuntime.InvokeVoidAsync("console.table", cancellationToken, stateChangeTable);

			foreach (var stateChange in stateChanges) {
				cancellationToken.ThrowIfCancellationRequested();

				var changeInfo = new {
					field = stateChange.FieldName,
					previous = stateChange.PreviousValue,
					current = stateChange.CurrentValue,
					changeType = stateChange.ChangeType.ToString(),
					description = stateChange.GetFormattedDescription()
				};

				await _jsRuntime.InvokeVoidAsync("console.log", cancellationToken,
					$"%c{stateChange.FieldName}:",
					"color: #4CAF50; font-weight: bold;",
					changeInfo);
			}
		}
		catch (OperationCanceledException) {
			// Expected when renders happen quickly - no action needed
		}
		catch (Exception ex) {
			Console.WriteLine($"[WhyDidYouRender] State change logging failed: {ex.Message}");
			try {
				await _jsRuntime.InvokeVoidAsync("console.log", cancellationToken,
					"State changes: [Unable to log details]");
			}
			catch (Exception fallbackEx) {
				// Fallback logging failed - avoid infinite recursion
				Console.WriteLine($"[WhyDidYouRender] Fallback logging also failed: {fallbackEx.Message}");
			}
		}
	}



	/// <summary>
	/// Logs a simple message to the browser console.
	/// </summary>
	/// <param name="message">The message to log.</param>
	/// <param name="level">The console level (log, warn, error, etc.).</param>
	public async Task LogMessageAsync(string message, string level = "log") {
		if (!_isInitialized)
			await InitializeAsync();

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
		GC.SuppressFinalize(this);
	}
}
