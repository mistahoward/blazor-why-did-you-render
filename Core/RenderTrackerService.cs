using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Hosting;

using Blazor.WhyDidYouRender.Configuration;
using Blazor.WhyDidYouRender.Records;
using Blazor.WhyDidYouRender.Diagnostics;
using Blazor.WhyDidYouRender.Helpers;

namespace Blazor.WhyDidYouRender.Core;

/// <summary>
/// Service responsible for tracking and logging Blazor component render events for diagnostics.
/// </summary>
public class RenderTrackerService {
	/// <summary>
	/// Singleton instance of <see cref="RenderTrackerService"/>.
	/// </summary>
	private static readonly Lazy<RenderTrackerService> _instance = new(() => new RenderTrackerService());

	/// <summary>
	/// Service for detecting parameter changes.
	/// </summary>
	private readonly ParameterChangeDetector _parameterChangeDetector = new();

	/// <summary>
	/// Service for detecting unnecessary re-renders.
	/// </summary>
	private readonly UnnecessaryRerenderDetector _unnecessaryRerenderDetector;

	/// <summary>
	/// Service for tracking render performance.
	/// </summary>
	private readonly PerformanceTracker _performanceTracker;

	/// <summary>
	/// Service for tracking render frequency.
	/// </summary>
	private readonly RenderFrequencyTracker _renderFrequencyTracker;

	/// <summary>
	/// Configuration for the tracking system.
	/// </summary>
	private readonly WhyDidYouRenderConfig _config = new();

	/// <summary>
	/// Browser console logger for client-side logging.
	/// </summary>
	private IBrowserConsoleLogger? _browserLogger;

	/// <summary>
	/// Session context for tracking session-specific data.
	/// </summary>
	private readonly ConcurrentDictionary<string, SessionContext> _sessions = new();

	/// <summary>
	/// Timer for periodic cleanup of old session data.
	/// </summary>
	private Timer? _cleanupTimer;

	/// <summary>
	/// Error tracker for handling and logging errors.
	/// </summary>
	private IErrorTracker? _errorTracker;

	/// <summary>
	/// Cached JSON serializer options for performance.
	/// </summary>
	private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = false };

	/// <summary>
	/// Gets the singleton instance of the render tracker service.
	/// </summary>
	public static RenderTrackerService Instance => _instance.Value;

	/// <summary>
	/// Initializes a new instance of the <see cref="RenderTrackerService"/> class.
	/// Private to enforce singleton pattern.
	/// </summary>
	private RenderTrackerService() {
		_unnecessaryRerenderDetector = new UnnecessaryRerenderDetector(_config);
		_performanceTracker = new PerformanceTracker(_config);
		_renderFrequencyTracker = new RenderFrequencyTracker(_config);
	}

	/// <summary>
	/// Configures the tracking service with a configuration action.
	/// </summary>
	/// <param name="configureAction">Action to configure the tracking options.</param>
	public void Configure(Action<WhyDidYouRenderConfig> configureAction) {
		ArgumentNullException.ThrowIfNull(configureAction);
		configureAction(_config);
	}

	/// <summary>
	/// Initializes the service with required dependencies.
	/// </summary>
	/// <param name="browserLogger">Browser console logger for client-side logging.</param>
	/// <param name="errorTracker">Error tracker for handling errors.</param>
	/// <param name="hostEnvironment">Host environment for determining if in development.</param>
	public void Initialize(IBrowserConsoleLogger? browserLogger, IErrorTracker? errorTracker, IHostEnvironment? hostEnvironment) {
		_browserLogger = browserLogger;
		_errorTracker = errorTracker;

		if (hostEnvironment?.IsDevelopment() == true) {
			StartCleanupTimer();
		}
	}

	/// <summary>
	/// Starts timing a render operation for a component.
	/// </summary>
	/// <param name="component">The component being rendered.</param>
	public void StartRenderTiming(ComponentBase component) {
		_performanceTracker.StartRenderTiming(component);
	}

	/// <summary>
	/// Tracks a render event for the specified component.
	/// </summary>
	/// <param name="component">The component being rendered.</param>
	/// <param name="method">The lifecycle method that triggered the render.</param>
	/// <param name="firstRender">Whether this is the first render of the component.</param>
	public void TrackRender(ComponentBase component, string method, bool? firstRender = null) {
		if (!_config.Enabled) return;

		SafeExecutor.ExecuteTracking(
			component,
			"TrackRender",
			() => TrackRenderInternal(component, method, firstRender),
			null,
			_errorTracker);
	}

	/// <summary>
	/// Internal implementation of render tracking.
	/// </summary>
	/// <param name="component">The component being rendered.</param>
	/// <param name="method">The lifecycle method that triggered the render.</param>
	/// <param name="firstRender">Whether this is the first render of the component.</param>
	private void TrackRenderInternal(ComponentBase component, string method, bool? firstRender) {
		var componentType = component.GetType();
		var componentName = componentType.Name;
		var componentFullName = componentType.FullName ?? componentName;

		if (!ShouldTrackComponent(componentName, componentFullName)) return;

		var parameterChanges = _config.TrackParameterChanges
			? SafeExecutor.ExecuteTracking(
				component,
				"DetectParameterChanges",
				() => _parameterChangeDetector.DetectParameterChanges(component, method),
				null,
				_errorTracker)
			: null;

		var unnecessaryRerenderInfo = _config.DetectUnnecessaryRerenders
			? _unnecessaryRerenderDetector.DetectUnnecessaryRerender(component, method, parameterChanges, firstRender)
			: (false, null);

		if (_config.LogOnlyWhenParametersChange && method == "OnParametersSet" && parameterChanges == null) {
			return;
		}

		var isFrequentRerender = _renderFrequencyTracker.TrackRenderFrequency(component);

		var renderEvent = new RenderEvent {
			ComponentName = componentName,
			ComponentType = componentFullName,
			Method = method,
			FirstRender = firstRender,
			SessionId = _config.IncludeSessionInfo ? GetSessionId() : null,
			ParameterChanges = parameterChanges,
			DurationMs = _config.TrackPerformance ? _performanceTracker.GetAndResetRenderDuration(component, method) : null,
			IsUnnecessaryRerender = unnecessaryRerenderInfo.Item1,
			UnnecessaryRerenderReason = unnecessaryRerenderInfo.Item2,
			IsFrequentRerender = isFrequentRerender
		};

		_ = LogRenderEventAsync(renderEvent);
	}

	/// <summary>
	/// Determines whether a component should be tracked based on configuration filters.
	/// </summary>
	/// <param name="componentName">The simple component name.</param>
	/// <param name="componentFullName">The full component type name including namespace.</param>
	/// <returns>True if the component should be tracked; otherwise, false.</returns>
	private bool ShouldTrackComponent(string componentName, string componentFullName) {
		if (_config.ExcludeNamespaces?.Count > 0) {
			foreach (var pattern in _config.ExcludeNamespaces) {
				if (MatchesPattern(componentFullName, pattern)) return false;
			}
		}

		if (_config.ExcludeComponents?.Count > 0) {
			foreach (var pattern in _config.ExcludeComponents) {
				if (MatchesPattern(componentName, pattern)) return false;
			}
		}

		// Check namespace inclusions (if specified, must match at least one)
		if (_config.IncludeNamespaces?.Count > 0) {
			var matchesInclude = false;
			foreach (var pattern in _config.IncludeNamespaces) {
				if (MatchesPattern(componentFullName, pattern)) {
					matchesInclude = true;
					break;
				}
			}
			if (!matchesInclude) return false;
		}

		// Check component inclusions (if specified, must match at least one)
		if (_config.IncludeComponents?.Count > 0) {
			var matchesInclude = false;
			foreach (var pattern in _config.IncludeComponents) {
				if (MatchesPattern(componentName, pattern)) {
					matchesInclude = true;
					break;
				}
			}
			if (!matchesInclude) return false;
		}

		return true;
	}

	/// <summary>
	/// Checks if a string matches a pattern with wildcard support.
	/// </summary>
	/// <param name="input">The input string to check.</param>
	/// <param name="pattern">The pattern with optional wildcards (*).</param>
	/// <returns>True if the input matches the pattern; otherwise, false.</returns>
	private static bool MatchesPattern(string input, string pattern) {
		if (string.IsNullOrEmpty(pattern)) return false;
		if (pattern == "*") return true;
		if (!pattern.Contains('*')) return string.Equals(input, pattern, StringComparison.OrdinalIgnoreCase);

		var regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
		return Regex.IsMatch(input, regexPattern, RegexOptions.IgnoreCase);
	}

	/// <summary>
	/// Logs a render event to the configured outputs.
	/// </summary>
	/// <param name="renderEvent">The render event to log.</param>
	/// <returns>A task representing the asynchronous logging operation.</returns>
	private async Task LogRenderEventAsync(RenderEvent renderEvent) {
		try {
			if (_config.Output.HasFlag(TrackingOutput.Console)) {
				LogToConsole(renderEvent);
			}

			if (_config.Output.HasFlag(TrackingOutput.BrowserConsole) && _browserLogger != null) {
				await _browserLogger.LogRenderEventAsync(renderEvent);
			}
		}
		catch (Exception ex) {
			_errorTracker?.TrackError(ex, "LogRenderEventAsync", ErrorSeverity.Warning);
		}
	}

	/// <summary>
	/// Logs a render event to the server console.
	/// </summary>
	/// <param name="renderEvent">The render event to log.</param>
	private void LogToConsole(RenderEvent renderEvent) {
		var message = FormatConsoleMessage(renderEvent);
		Console.WriteLine(message);

		if (_config.Verbosity >= TrackingVerbosity.Verbose && renderEvent.ParameterChanges?.Count > 0) {
			LogParameterChangesToConsole(renderEvent);
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
			if (renderEvent.DurationMs.HasValue) {
				parts.Add($"({renderEvent.DurationMs:F2}ms)");
			}

			if (_config.IncludeSessionInfo && !string.IsNullOrEmpty(renderEvent.SessionId)) {
				parts.Add($"[{renderEvent.SessionId}]");
			}
		}

		if (renderEvent.IsUnnecessaryRerender) {
			parts.Add($"⚠️ UNNECESSARY: {renderEvent.UnnecessaryRerenderReason}");
		}

		if (renderEvent.IsFrequentRerender) {
			parts.Add("🔥 FREQUENT");
		}

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
				Console.WriteLine($"  {paramName}: {changeJson}");
			}
			catch {
				Console.WriteLine($"  {paramName}: [Unable to serialize]");
			}
		}
	}

	/// <summary>
	/// Gets the current session ID for tracking purposes.
	/// </summary>
	/// <returns>The session ID, or a default value if not available.</returns>
	private string GetSessionId() {
		return "default-session";
	}

	/// <summary>
	/// Starts the cleanup timer for periodic maintenance.
	/// </summary>
	private void StartCleanupTimer() {
		var interval = TimeSpan.FromMinutes(_config.SessionCleanupIntervalMinutes);
		_cleanupTimer = new Timer(
			_ => CleanupSessionSpecificData("periodic-cleanup"),
			null,
			interval,
			interval);
	}

	/// <summary>
	/// Cleans up session-specific data from component caches.
	/// </summary>
	/// <param name="sessionId">The session ID to clean up.</param>
	private void CleanupSessionSpecificData(string sessionId) {
		// Note: This is a simplified cleanup. In a real implementation,
		// you might want to track which components belong to which sessions
		// and clean them up more precisely.
		// For now, sessionId is not used as we clean up globally.
		_ = sessionId; // Acknowledge the parameter to avoid warnings

		var cleanupTimeSpan = TimeSpan.FromMinutes(_config.SessionCleanupIntervalMinutes);
		_renderFrequencyTracker.CleanupOldHistory(cleanupTimeSpan);
		_performanceTracker.CleanupInactiveComponents([]);
		_parameterChangeDetector.CleanupInactiveComponents([]);
		_unnecessaryRerenderDetector.CleanupInactiveComponents([]);
	}
}
