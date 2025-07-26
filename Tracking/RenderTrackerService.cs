using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Blazor.WhyDidYouRender.Tracking;

/// <summary>
/// Represents a render event with detailed tracking information.
/// </summary>
public record RenderEvent {
	/// <summary>
	/// Timestamp when the render event occurred.
	/// </summary>
	public DateTime Timestamp { get; init; } = DateTime.UtcNow;

	/// <summary>
	/// Simple name of the component (e.g., "Counter").
	/// </summary>
	public string ComponentName { get; init; } = string.Empty;

	/// <summary>
	/// Full type name including namespace (e.g., "RenderTracker.SampleApp.Components.Pages.Counter").
	/// </summary>
	public string ComponentType { get; init; } = string.Empty;

	/// <summary>
	/// The lifecycle method or trigger that caused the render.
	/// </summary>
	public string Method { get; init; } = string.Empty;

	/// <summary>
	/// Indicates if this is the first render of the component.
	/// </summary>
	public bool? FirstRender { get; init; }

	/// <summary>
	/// Duration of the render operation in milliseconds.
	/// </summary>
	public double? DurationMs { get; init; }

	/// <summary>
	/// Session or connection ID for SSR scenarios.
	/// </summary>
	public string? SessionId { get; init; }

	/// <summary>
	/// Parameter changes detected during this render (if any).
	/// </summary>
	public Dictionary<string, object?>? ParameterChanges { get; init; }

	/// <summary>
	/// Indicates if this render was unnecessary (no actual changes detected).
	/// </summary>
	public bool IsUnnecessaryRerender { get; init; }

	/// <summary>
	/// Reason why this render was flagged as unnecessary.
	/// </summary>
	public string? UnnecessaryRerenderReason { get; init; }

	/// <summary>
	/// Indicates if this component is rendering frequently (potential performance issue).
	/// </summary>
	public bool IsFrequentRerender { get; init; }
}

/// <summary>
/// Service responsible for tracking and logging Blazor component render events for diagnostics.
/// </summary>
public class RenderTrackerService {
	/// <summary>
	/// Singleton instance of <see cref="RenderTrackerService"/>.
	/// </summary>
	private static readonly Lazy<RenderTrackerService> _instance = new(() => new RenderTrackerService());

	/// <summary>
	/// Cache to store previous parameter values for change detection.
	/// </summary>
	private readonly ConcurrentDictionary<object, Dictionary<string, object?>> _previousParameters = new();

	/// <summary>
	/// Cache to store render performance data.
	/// </summary>
	private readonly ConcurrentDictionary<object, Stopwatch> _renderTimers = new();

	/// <summary>
	/// Cache to store component render history for frequency analysis.
	/// </summary>
	private readonly ConcurrentDictionary<object, List<DateTime>> _renderHistory = new();

	/// <summary>
	/// Cache to store component state snapshots for change detection.
	/// </summary>
	private readonly ConcurrentDictionary<object, object?> _componentStateSnapshots = new();

	/// <summary>
	/// Configuration for the tracking service.
	/// </summary>
	private WhyDidYouRenderConfig _config = new();

	/// <summary>
	/// Browser console logger for client-side logging.
	/// </summary>
	private BrowserConsoleLogger? _browserLogger;

	/// <summary>
	/// Gets the singleton instance of the <see cref="RenderTrackerService"/>.
	/// </summary>
	public static RenderTrackerService Instance => _instance.Value;

	/// <summary>
	/// Initializes a new instance of the <see cref="RenderTrackerService"/> class.
	/// Private to enforce singleton pattern.
	/// </summary>
	private RenderTrackerService() { }

	/// <summary>
	/// Configures the tracking service with the specified options.
	/// </summary>
	/// <param name="config">The configuration to apply.</param>
	public void Configure(WhyDidYouRenderConfig config) {
		_config = config ?? throw new ArgumentNullException(nameof(config));
	}

	/// <summary>
	/// Configures the tracking service with a configuration action.
	/// </summary>
	/// <param name="configureAction">Action to configure the tracking options.</param>
	public void Configure(Action<WhyDidYouRenderConfig> configureAction) {
		if (configureAction == null) throw new ArgumentNullException(nameof(configureAction));
		configureAction(_config);
	}

	/// <summary>
	/// Sets the browser console logger for client-side logging.
	/// </summary>
	/// <param name="browserLogger">The browser console logger instance.</param>
	public void SetBrowserLogger(BrowserConsoleLogger browserLogger) {
		_browserLogger = browserLogger;
	}

	/// <summary>
	/// Gets the current configuration.
	/// </summary>
	public WhyDidYouRenderConfig GetConfig() => _config;

	/// <summary>
	/// Tracks a render event for the specified Blazor component.
	/// </summary>
	/// <param name="component">The component being rendered.</param>
	/// <param name="method">The lifecycle method or trigger causing the render.</param>
	/// <param name="firstRender">Indicates if this is the first render (optional).</param>
	public void Track(ComponentBase component, string method, bool? firstRender = null) {
		// Check if tracking is enabled
		if (!_config.Enabled) return;

		var componentType = component.GetType();
		var componentName = componentType.Name;
		var componentFullName = componentType.FullName ?? componentName;

		// Apply component filtering
		if (!ShouldTrackComponent(componentName, componentFullName)) return;

		// Detect parameter changes (respecting configuration)
		var parameterChanges = _config.TrackParameterChanges
			? DetectParameterChanges(component, method)
			: null;

		// Detect unnecessary re-renders if enabled
		var unnecessaryRerenderInfo = _config.DetectUnnecessaryRerenders
			? DetectUnnecessaryRerender(component, method, parameterChanges, firstRender)
			: (false, null);

		// Skip logging if configured to only log when parameters change and there are no changes
		if (_config.LogOnlyWhenParametersChange && method == "OnParametersSet" && parameterChanges == null) {
			return;
		}

		// Track render frequency
		var isFrequentRerender = TrackRenderFrequency(component);

		var renderEvent = new RenderEvent {
			ComponentName = componentName,
			ComponentType = componentFullName,
			Method = method,
			FirstRender = firstRender,
			SessionId = _config.IncludeSessionInfo ? GetSessionId() : null,
			ParameterChanges = parameterChanges,
			DurationMs = _config.TrackPerformance ? GetAndResetRenderDuration(component, method) : null,
			IsUnnecessaryRerender = unnecessaryRerenderInfo.Item1,
			UnnecessaryRerenderReason = unnecessaryRerenderInfo.Item2,
			IsFrequentRerender = isFrequentRerender
		};

		// Log the event (async fire-and-forget for browser logging)
		_ = LogRenderEventAsync(renderEvent);
	}

	/// <summary>
	/// Starts timing a render operation for performance tracking.
	/// </summary>
	/// <param name="component">The component being rendered.</param>
	public void StartRenderTiming(ComponentBase component) {
		var timer = Stopwatch.StartNew();
		_renderTimers.AddOrUpdate(component, timer, (_, _) => timer);
	}

	/// <summary>
	/// Determines whether a component should be tracked based on configuration filters.
	/// </summary>
	/// <param name="componentName">The simple component name.</param>
	/// <param name="componentFullName">The full component type name including namespace.</param>
	/// <returns>True if the component should be tracked; otherwise, false.</returns>
	private bool ShouldTrackComponent(string componentName, string componentFullName) {
		// Check namespace exclusions first
		if (_config.ExcludeNamespaces?.Any() == true) {
			foreach (var pattern in _config.ExcludeNamespaces) {
				if (MatchesPattern(componentFullName, pattern)) return false;
			}
		}

		// Check component exclusions
		if (_config.ExcludeComponents?.Any() == true) {
			foreach (var pattern in _config.ExcludeComponents) {
				if (MatchesPattern(componentName, pattern)) return false;
			}
		}

		// Check namespace inclusions (if specified, must match at least one)
		if (_config.IncludeNamespaces?.Any() == true) {
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
		if (_config.IncludeComponents?.Any() == true) {
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
	private bool MatchesPattern(string input, string pattern) {
		if (string.IsNullOrEmpty(pattern)) return false;
		if (pattern == "*") return true;

		// Convert wildcard pattern to regex
		var regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
		return Regex.IsMatch(input, regexPattern, RegexOptions.IgnoreCase);
	}

	/// <summary>
	/// Detects if a render is unnecessary (no actual changes).
	/// </summary>
	/// <param name="component">The component being rendered.</param>
	/// <param name="method">The method that triggered the render.</param>
	/// <param name="parameterChanges">Detected parameter changes.</param>
	/// <param name="firstRender">Whether this is the first render.</param>
	/// <returns>Tuple indicating if unnecessary and the reason.</returns>
	private (bool IsUnnecessary, string? Reason) DetectUnnecessaryRerender(
		ComponentBase component,
		string method,
		Dictionary<string, object?>? parameterChanges,
		bool? firstRender) {

		// First renders are never unnecessary
		if (firstRender == true) return (false, null);

		// Check for manual StateHasChanged with no parameter changes
		if (method == "StateHasChanged") {
			// For manual StateHasChanged, we need to check if component state actually changed
			var currentStateSnapshot = CreateComponentStateSnapshot(component);

			if (_componentStateSnapshots.TryGetValue(component, out var previousSnapshot)) {
				if (AreStatesEquivalent(previousSnapshot, currentStateSnapshot)) {
					return (true, "Manual StateHasChanged() called but component state unchanged");
				}
			}

			// Update the state snapshot
			_componentStateSnapshots[component] = currentStateSnapshot;
			return (false, null);
		}

		// Check for OnParametersSet with no meaningful changes
		if (method == "OnParametersSet") {
			if (parameterChanges == null || !parameterChanges.Any()) {
				return (true, "OnParametersSet called but no parameter changes detected");
			}

			// Check if parameter changes are functionally identical
			var hasRealChanges = false;
			foreach (var (paramName, change) in parameterChanges) {
				if (HasMeaningfulParameterChange(change)) {
					hasRealChanges = true;
					break;
				}
			}

			if (!hasRealChanges) {
				return (true, "Parameter changes detected but values are functionally identical");
			}
		}

		return (false, null);
	}

	/// <summary>
	/// Creates a snapshot of component state for comparison.
	/// </summary>
	/// <param name="component">The component to snapshot.</param>
	/// <returns>A snapshot object representing the component's current state.</returns>
	private object? CreateComponentStateSnapshot(ComponentBase component) {
		try {
			// Get all private fields that likely represent component state
			var componentType = component.GetType();
			var fields = componentType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
				.Where(f => !f.Name.StartsWith("<") && // Skip compiler-generated fields
						   !f.Name.Contains("k__BackingField") && // Skip auto-property backing fields
						   f.FieldType != typeof(RenderTrackerService)) // Skip our tracker
				.ToList();

			var snapshot = new Dictionary<string, object?>();
			foreach (var field in fields) {
				try {
					var value = field.GetValue(component);
					snapshot[field.Name] = value;
				}
				catch {
					// Skip fields that can't be read
				}
			}

			return snapshot;
		}
		catch {
			// If we can't create a snapshot, assume state changed
			return null;
		}
	}

	/// <summary>
	/// Compares two state snapshots for equivalence.
	/// </summary>
	/// <param name="previous">Previous state snapshot.</param>
	/// <param name="current">Current state snapshot.</param>
	/// <returns>True if states are equivalent; otherwise, false.</returns>
	private bool AreStatesEquivalent(object? previous, object? current) {
		if (previous == null || current == null) return false;

		try {
			// Simple JSON comparison for now - could be enhanced with deep comparison
			var previousJson = JsonSerializer.Serialize(previous);
			var currentJson = JsonSerializer.Serialize(current);
			return previousJson == currentJson;
		}
		catch {
			// If we can't compare, assume they're different
			return false;
		}
	}

	/// <summary>
	/// Determines if a parameter change is meaningful (not just reference equality).
	/// </summary>
	/// <param name="change">The parameter change data.</param>
	/// <returns>True if the change is meaningful; otherwise, false.</returns>
	private bool HasMeaningfulParameterChange(object? change) {
		try {
			if (change == null) return false;

			var changeType = change.GetType();
			var previousProp = changeType.GetProperty("Previous");
			var currentProp = changeType.GetProperty("Current");

			if (previousProp == null || currentProp == null) return true;

			var previousValue = previousProp.GetValue(change);
			var currentValue = currentProp.GetValue(change);

			// Check for reference equality (same object)
			if (ReferenceEquals(previousValue, currentValue)) return false;

			// Check for value equality
			if (Equals(previousValue, currentValue)) return false;

			// For strings, check if they're functionally the same
			if (previousValue is string prevStr && currentValue is string currStr) {
				return !string.Equals(prevStr, currStr, StringComparison.Ordinal);
			}

			// For collections, do a basic count/content check
			if (previousValue is System.Collections.ICollection prevColl &&
				currentValue is System.Collections.ICollection currColl) {
				if (prevColl.Count != currColl.Count) return true;

				// Could add deeper collection comparison here
				return true; // Assume different for now
			}

			return true; // Assume meaningful change
		}
		catch {
			return true; // If we can't determine, assume it's meaningful
		}
	}

	/// <summary>
	/// Tracks render frequency and determines if component is rendering too frequently.
	/// </summary>
	/// <param name="component">The component being rendered.</param>
	/// <returns>True if component is rendering frequently; otherwise, false.</returns>
	private bool TrackRenderFrequency(ComponentBase component) {
		var now = DateTime.UtcNow;
		var history = _renderHistory.GetOrAdd(component, _ => new List<DateTime>());

		lock (history) {
			// Add current render time
			history.Add(now);

			// Remove renders older than 1 second
			var cutoff = now.AddSeconds(-1);
			history.RemoveAll(time => time < cutoff);

			// Check if exceeding threshold
			return history.Count > _config.FrequentRerenderThreshold;
		}
	}

	/// <summary>
	/// Gets the session ID for the current request context (simplified for demo).
	/// </summary>
	/// <returns>A session identifier or null if not available.</returns>
	private string? GetSessionId() {
		// In a real implementation, you might get this from HttpContext or SignalR connection
		// For now, we'll use a simplified approach
		return $"session-{Environment.CurrentManagedThreadId}";
	}

	/// <summary>
	/// Detects parameter changes for the component.
	/// </summary>
	/// <param name="component">The component to check for parameter changes.</param>
	/// <param name="method">The method being called (to determine if we should check parameters).</param>
	/// <returns>Dictionary of parameter changes, or null if no changes or not applicable.</returns>
	private Dictionary<string, object?>? DetectParameterChanges(ComponentBase component, string method) {
		// Only check for parameter changes during OnParametersSet
		if (method != "OnParametersSet") return null;

		var componentType = component.GetType();
		var parameterProperties = componentType.GetProperties()
			.Where(p => p.GetCustomAttribute<ParameterAttribute>() != null)
			.ToList();

		if (!parameterProperties.Any()) return null;

		var currentParameters = new Dictionary<string, object?>();
		var changes = new Dictionary<string, object?>();

		// Get current parameter values
		foreach (var prop in parameterProperties) {
			try {
				var value = prop.GetValue(component);
				currentParameters[prop.Name] = value;
			}
			catch {
				// Skip properties that can't be read
				currentParameters[prop.Name] = "<unable to read>";
			}
		}

		// Compare with previous values
		if (_previousParameters.TryGetValue(component, out var previousParams)) {
			foreach (var (paramName, currentValue) in currentParameters) {
				if (previousParams.TryGetValue(paramName, out var previousValue)) {
					if (!Equals(currentValue, previousValue)) {
						changes[paramName] = new { Previous = previousValue, Current = currentValue };
					}
				}
				else {
					changes[paramName] = new { Previous = "<not set>", Current = currentValue };
				}
			}
		}
		else {
			// First time seeing this component, all parameters are "new"
			foreach (var (paramName, currentValue) in currentParameters) {
				changes[paramName] = new { Previous = "<first render>", Current = currentValue };
			}
		}

		// Store current parameters for next comparison
		_previousParameters[component] = currentParameters;

		return changes.Any() ? changes : null;
	}

	/// <summary>
	/// Gets the render duration and resets the timer for the component.
	/// </summary>
	/// <param name="component">The component being rendered.</param>
	/// <param name="method">The method being called.</param>
	/// <returns>Duration in milliseconds, or null if timing wasn't started.</returns>
	private double? GetAndResetRenderDuration(ComponentBase component, string method) {
		// Only measure duration for OnAfterRender to get the full render time
		if (method != "OnAfterRender") return null;

		if (_renderTimers.TryRemove(component, out var timer)) {
			timer.Stop();
			return timer.Elapsed.TotalMilliseconds;
		}

		return null;
	}

	/// <summary>
	/// Logs the render event using structured logging (async version).
	/// </summary>
	/// <param name="renderEvent">The render event to log.</param>
	private async Task LogRenderEventAsync(RenderEvent renderEvent) {
		// Log to console if enabled
		if (_config.Output.HasFlag(TrackingOutput.Console)) {
			LogToConsole(renderEvent);
		}

		// Log to browser console if enabled and available
		if (_config.Output.HasFlag(TrackingOutput.BrowserConsole) && _browserLogger != null) {
			try {
				await _browserLogger.LogRenderEventAsync(renderEvent);
			}
			catch {
				// Fallback to console if browser logging fails
				// Don't log the error to avoid spam
			}
		}
	}

	/// <summary>
	/// Logs the render event to the server console.
	/// </summary>
	/// <param name="renderEvent">The render event to log.</param>
	private void LogToConsole(RenderEvent renderEvent) {
		var logMessage = CreateLogMessage(renderEvent);
		Console.WriteLine(logMessage);

		// Log unnecessary re-render reason
		if (renderEvent.IsUnnecessaryRerender && !string.IsNullOrEmpty(renderEvent.UnnecessaryRerenderReason)) {
			Console.WriteLine($"[WhyDidYouRender] 💡 Optimization Tip: {renderEvent.UnnecessaryRerenderReason}");
		}

		// Log frequent re-render warning
		if (renderEvent.IsFrequentRerender) {
			Console.WriteLine("[WhyDidYouRender] 🔥 Performance Warning: This component is re-rendering frequently. Consider using ShouldRender(), reducing StateHasChanged() calls, or implementing IDisposable to unsubscribe from events.");
		}

		// Log parameter changes based on verbosity
		if (_config.Verbosity >= TrackingVerbosity.Verbose && renderEvent.ParameterChanges?.Any() == true) {
			LogParameterChangesToConsole(renderEvent);
		}
	}

	/// <summary>
	/// Creates a log message based on the current verbosity level.
	/// </summary>
	/// <param name="renderEvent">The render event to create a message for.</param>
	/// <returns>The formatted log message.</returns>
	private string CreateLogMessage(RenderEvent renderEvent) {
		var prefix = "[WhyDidYouRender]";

		// Add warning indicators for problematic renders
		if (renderEvent.IsUnnecessaryRerender) {
			prefix = "[WhyDidYouRender] ⚠️  UNNECESSARY";
		}
		else if (renderEvent.IsFrequentRerender) {
			prefix = "[WhyDidYouRender] 🔥 FREQUENT";
		}

		var message = $"{prefix} ";

		// Always include timestamp for Normal and Verbose
		if (_config.Verbosity >= TrackingVerbosity.Normal) {
			message += $"{renderEvent.Timestamp:O} | ";
		}

		// Always include component and method
		message += $"{renderEvent.ComponentName} | {renderEvent.Method}";

		// Add additional info based on verbosity
		if (_config.Verbosity >= TrackingVerbosity.Normal) {
			if (renderEvent.FirstRender.HasValue) {
				message += $" | firstRender: {renderEvent.FirstRender.Value}";
			}

			if (renderEvent.DurationMs.HasValue) {
				message += $" | duration: {renderEvent.DurationMs.Value:F2}ms";
			}

			if (renderEvent.SessionId != null) {
				message += $" | session: {renderEvent.SessionId}";
			}
		}

		return message;
	}

	/// <summary>
	/// Logs parameter changes to the console.
	/// </summary>
	/// <param name="renderEvent">The render event containing parameter changes.</param>
	private void LogParameterChangesToConsole(RenderEvent renderEvent) {
		if (renderEvent.ParameterChanges?.Any() != true) return;

		Console.WriteLine($"[WhyDidYouRender] Parameter changes for {renderEvent.ComponentName}:");

		var changeCount = 0;
		foreach (var (paramName, change) in renderEvent.ParameterChanges) {
			if (changeCount >= _config.MaxParameterChangesToLog) {
				Console.WriteLine($"  ... and {renderEvent.ParameterChanges.Count - changeCount} more changes");
				break;
			}

			try {
				var changeJson = JsonSerializer.Serialize(change, new JsonSerializerOptions { WriteIndented = false });
				Console.WriteLine($"  {paramName}: {changeJson}");
			}
			catch {
				Console.WriteLine($"  {paramName}: {change}");
			}

			changeCount++;
		}
	}
}
