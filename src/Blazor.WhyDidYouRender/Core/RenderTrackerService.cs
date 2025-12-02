using System.Text.Json;
using System.Text.RegularExpressions;
using Blazor.WhyDidYouRender.Abstractions;
using Blazor.WhyDidYouRender.Configuration;
using Blazor.WhyDidYouRender.Helpers;
using Blazor.WhyDidYouRender.Logging;
using Blazor.WhyDidYouRender.Records;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Hosting;

namespace Blazor.WhyDidYouRender.Core;

/// <summary>
/// Service responsible for tracking and logging Blazor component render events for diagnostics.
/// </summary>
public class RenderTrackerService
{
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
	private UnnecessaryRerenderDetector? _unnecessaryRerenderDetector;

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
	/// Flag to track if services have been initialized.
	/// </summary>
	private bool _servicesInitialized = false;

	/// <summary>
	/// Browser console logger for client-side logging.
	/// </summary>
	private IBrowserConsoleLogger? _browserLogger;

	/// <summary>
	/// Timer for periodic cleanup of old session data.
	/// </summary>
	private Timer? _cleanupTimer;

	/// <summary>
	/// Error tracker for handling and logging errors.
	/// </summary>
	private IErrorTracker? _errorTracker;

	/// <summary>
	/// Session context service for session management.
	/// </summary>
	private static ISessionContextService? _sessionContextService;

	/// <summary>
	/// Unified logger for structured logging.
	/// </summary>
	private static IWhyDidYouRenderLogger? _unifiedLogger;

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
	private RenderTrackerService()
	{
		_performanceTracker = new PerformanceTracker(_config);
		_renderFrequencyTracker = new RenderFrequencyTracker(_config);
	}

	/// <summary>
	/// Configures the tracking service with a configuration action.
	/// </summary>
	/// <param name="configureAction">Action to configure the tracking options.</param>
	public void Configure(Action<WhyDidYouRenderConfig> configureAction)
	{
		ArgumentNullException.ThrowIfNull(configureAction);
		configureAction(_config);

		// always (re)create the unnecessary rerender detector so that configuration
		// changes to state tracking and related options take effect immediately.
		_unnecessaryRerenderDetector = new UnnecessaryRerenderDetector(_config);
		_servicesInitialized = true;
	}

	/// <summary>
	/// Initializes services that depend on configuration.
	/// </summary>
	private void InitializeServices()
	{
		if (_servicesInitialized)
			return;

		_unnecessaryRerenderDetector = new UnnecessaryRerenderDetector(_config);
		_servicesInitialized = true;
	}

	/// <summary>
	/// Ensures services are initialized before use.
	/// </summary>
	private void EnsureServicesInitialized()
	{
		if (!_servicesInitialized)
			InitializeServices();
	}

	/// <summary>
	/// Initializes the service with required dependencies.
	/// </summary>
	/// <param name="browserLogger">Browser console logger for client-side logging.</param>
	/// <param name="errorTracker">Error tracker for handling errors.</param>
	/// <param name="hostEnvironment">Host environment for determining if in development.</param>
	public void Initialize(IBrowserConsoleLogger? browserLogger, IErrorTracker? errorTracker, IHostEnvironment? hostEnvironment)
	{
		_browserLogger = browserLogger;
		_errorTracker = errorTracker;

		if (hostEnvironment?.IsDevelopment() == true)
			StartCleanupTimer();
	}

	/// <summary>
	/// Sets the browser logger for client-side logging.
	/// </summary>
	/// <param name="browserLogger">The browser logger instance.</param>
	public void SetBrowserLogger(IBrowserConsoleLogger browserLogger) => _browserLogger = browserLogger;

	/// <summary>
	/// Sets the error tracker for handling errors.
	/// </summary>
	/// <param name="errorTracker">The error tracker instance.</param>
	public void SetErrorTracker(IErrorTracker errorTracker) => _errorTracker = errorTracker;

	/// <summary>
	/// Sets the session context service for session management.
	/// </summary>
	/// <param name="sessionContextService">The session context service instance.</param>
	public static void SetSessionContextService(ISessionContextService sessionContextService)
	{
		_sessionContextService = sessionContextService ?? throw new ArgumentNullException(nameof(sessionContextService));
	}

	/// <summary>
	/// Sets the host environment for environment-specific behavior.
	/// </summary>
	/// <param name="hostEnvironment">The host environment instance.</param>
	public void SetHostEnvironment(IHostEnvironment hostEnvironment)
	{
		if (hostEnvironment?.IsDevelopment() == true)
			StartCleanupTimer();
	}

	/// <summary>
	/// Sets the unified logger for structured logging when available.
	/// </summary>
	/// <param name="logger">The unified logger instance.</param>
	public static void SetUnifiedLogger(IWhyDidYouRenderLogger logger)
	{
		_unifiedLogger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	/// <summary>
	/// Starts timing a render operation for a component.
	/// </summary>
	/// <param name="component">The component being rendered.</param>
	public void StartRenderTiming(ComponentBase component) => _performanceTracker.StartRenderTiming(component);

	/// <summary>
	/// Tracks a render event for the specified component.
	/// </summary>
	/// <param name="component">The component being rendered.</param>
	/// <param name="method">The lifecycle method that triggered the render.</param>
	/// <param name="firstRender">Whether this is the first render of the component.</param>
	public void Track(ComponentBase component, string method, bool? firstRender = null) => TrackRender(component, method, firstRender);

	/// <summary>
	/// Tracks a render event for the specified component.
	/// </summary>
	/// <param name="component">The component being rendered.</param>
	/// <param name="method">The lifecycle method that triggered the render.</param>
	/// <param name="firstRender">Whether this is the first render of the component.</param>
	public void TrackRender(ComponentBase component, string method, bool? firstRender = null)
	{
		if (!_config.Enabled)
			return;

		SafeExecutor.ExecuteTracking(
			component,
			"TrackRender",
			() =>
			{
				TrackRenderInternal(component, method, firstRender);
				return true;
			},
			false,
			_errorTracker
		);
	}

	/// <summary>
	/// Internal implementation of render tracking.
	/// </summary>
	/// <param name="component">The component being rendered.</param>
	/// <param name="method">The lifecycle method that triggered the render.</param>
	/// <param name="firstRender">Whether this is the first render of the component.</param>
	private void TrackRenderInternal(ComponentBase component, string method, bool? firstRender)
	{
		EnsureServicesInitialized();

		var componentType = component.GetType();
		var componentName = componentType.Name;
		var componentFullName = componentType.FullName ?? componentName;

		if (!ShouldTrackComponent(componentName, componentFullName))
			return;

		var parameterChanges = _config.TrackParameterChanges
			? SafeExecutor.ExecuteTracking(
				component,
				"DetectParameterChanges",
				() => _parameterChangeDetector.DetectParameterChanges(component, method),
				null,
				_errorTracker
			)
			: null;

		var stateChanges =
			_config.EnableStateTracking && _config.LogStateChanges && ShouldTrackComponentState(componentName, componentFullName)
				? SafeExecutor.ExecuteTracking(component, "DetectStateChanges", () => GetStateChanges(component), null, _errorTracker)
				: null;

		var unnecessaryRerenderInfo =
			_config.DetectUnnecessaryRerenders && _unnecessaryRerenderDetector != null
				? _unnecessaryRerenderDetector.DetectUnnecessaryRerender(
					component,
					method,
					parameterChanges,
					stateChanges,
					firstRender ?? false
				)
				: (false, null);

		if (_config.LogOnlyWhenParametersChange && method == "OnParametersSet" && parameterChanges == null)
		{
			return;
		}

		var isFrequentRerender = _renderFrequencyTracker.TrackRenderFrequency(component);

		var renderEvent = new RenderEvent
		{
			ComponentName = componentName,
			ComponentType = componentFullName,
			Method = method,
			FirstRender = firstRender,
			SessionId = _config.IncludeSessionInfo ? GetSessionId() : null,
			ParameterChanges = parameterChanges,
			DurationMs = _config.TrackPerformance ? _performanceTracker.GetAndResetRenderDuration(component, method) : null,
			IsUnnecessaryRerender = unnecessaryRerenderInfo.Item1,
			UnnecessaryRerenderReason = unnecessaryRerenderInfo.Item2,
			IsFrequentRerender = isFrequentRerender,
			StateChanges = stateChanges,
		};

		_ = LogRenderEventAsync(renderEvent);
	}

	/// <summary>
	/// Gets state changes for a component using the enhanced state tracking system.
	/// </summary>
	/// <param name="component">The component to get state changes for.</param>
	/// <returns>A list of state changes, or null if state tracking is disabled.</returns>
	private List<StateChange>? GetStateChanges(ComponentBase component)
	{
		try
		{
			if (_unnecessaryRerenderDetector == null)
				return null;

			var stateTrackingInfo = _unnecessaryRerenderDetector.GetStateTrackingInfo(component);
			if (stateTrackingInfo?.IsStateTrackingEnabled != true)
				return null;

			var stateSnapshotManager = _unnecessaryRerenderDetector.GetStateSnapshotManager();
			if (stateSnapshotManager == null)
				return null;

			var (hasChanges, changes) = stateSnapshotManager.DetectStateChanges(component);
			// when state tracking is enabled for this component but no changes are detected,
			// return an empty list rather than null. This allows the unnecessary re-render
			// detector to distinguish between "no tracking" (null) and "tracked with no
			// changes" (empty list), so StateHasChanged calls without real state changes
			// can be flagged as unnecessary.
			if (!hasChanges)
				return [];

			return [.. changes];
		}
		catch (Exception ex)
		{
			if (_errorTracker != null)
				_ = _errorTracker.TrackErrorAsync(
					ex,
					new Dictionary<string, object?>
					{
						["ComponentName"] = component.GetType().Name,
						["TrackingMethod"] = "GetStateChanges",
					},
					ErrorSeverity.Error,
					component.GetType().Name,
					"GetStateChanges"
				);
			return null;
		}
	}

	/// <summary>
	/// Determines whether a component should be tracked based on configuration filters.
	/// </summary>
	/// <param name="componentName">The simple component name.</param>
	/// <param name="componentFullName">The full component type name including namespace.</param>
	/// <returns>True if the component should be tracked; otherwise, false.</returns>
	private bool ShouldTrackComponent(string componentName, string componentFullName)
	{
		if (_config.ExcludeNamespaces?.Count > 0)
			foreach (var pattern in _config.ExcludeNamespaces)
				if (MatchesPattern(componentFullName, pattern))
					return false;

		if (_config.ExcludeComponents?.Count > 0)
			foreach (var pattern in _config.ExcludeComponents)
				if (MatchesPattern(componentName, pattern))
					return false;

		// check namespace inclusions (if specified, must match at least one)
		if (_config.IncludeNamespaces?.Count > 0)
		{
			var matchesInclude = false;
			foreach (var pattern in _config.IncludeNamespaces)
				if (MatchesPattern(componentFullName, pattern))
				{
					matchesInclude = true;
					break;
				}

			if (!matchesInclude)
				return false;
		}

		// check component inclusions (if specified, must match at least one)
		if (_config.IncludeComponents?.Count > 0)
		{
			var matchesInclude = false;
			foreach (var pattern in _config.IncludeComponents)
				if (MatchesPattern(componentName, pattern))
				{
					matchesInclude = true;
					break;
				}
			if (!matchesInclude)
				return false;
		}

		return true;
	}

	/// <summary>
	/// Determines whether a component should have state tracking enabled based on configuration filters.
	/// </summary>
	/// <param name="componentName">The simple component name.</param>
	/// <param name="componentFullName">The full component type name including namespace.</param>
	/// <returns>True if state tracking should be enabled for the component; otherwise, false.</returns>
	private bool ShouldTrackComponentState(string componentName, string componentFullName)
	{
		if (!_config.EnableStateTracking)
			return false;

		// check state tracking exclusions
		if (_config.ExcludeFromStateTracking?.Count > 0)
			foreach (var pattern in _config.ExcludeFromStateTracking)
				if (MatchesPattern(componentName, pattern) || MatchesPattern(componentFullName, pattern))
					return false;

		// check state tracking inclusions (if specified, must match at least one)
		if (_config.IncludeInStateTracking?.Count > 0)
		{
			var matchesInclude = false;
			foreach (var pattern in _config.IncludeInStateTracking)
				if (MatchesPattern(componentName, pattern) || MatchesPattern(componentFullName, pattern))
				{
					matchesInclude = true;
					break;
				}
			if (!matchesInclude)
				return false;
		}

		return true;
	}

	/// <summary>
	/// Checks if a string matches a pattern with wildcard support.
	/// </summary>
	/// <param name="input">The input string to check.</param>
	/// <param name="pattern">The pattern with optional wildcards (*).</param>
	/// <returns>True if the input matches the pattern; otherwise, false.</returns>
	private static bool MatchesPattern(string input, string pattern)
	{
		if (string.IsNullOrEmpty(pattern))
			return false;
		if (pattern == "*")
			return true;
		if (!pattern.Contains('*'))
			return string.Equals(input, pattern, StringComparison.OrdinalIgnoreCase);

		var regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
		return Regex.IsMatch(input, regexPattern, RegexOptions.IgnoreCase);
	}

	/// <summary>
	/// Logs a render event to the configured outputs.
	/// </summary>
	/// <param name="renderEvent">The render event to log.</param>
	/// <returns>A task representing the asynchronous logging operation.</returns>
	private async Task LogRenderEventAsync(RenderEvent renderEvent)
	{
		try
		{
			if (_unifiedLogger != null)
			{
				_unifiedLogger.LogRenderEvent(renderEvent);
				if (_config.Output.HasFlag(TrackingOutput.BrowserConsole) && _browserLogger != null)
					await _browserLogger.LogRenderEventAsync(renderEvent);
			}
			else
			{
				// fallback to prior behavior when no unified logger is set
				if (_config.Output.HasFlag(TrackingOutput.Console))
					LogToConsole(renderEvent);
				if (_config.Output.HasFlag(TrackingOutput.BrowserConsole) && _browserLogger != null)
					await _browserLogger.LogRenderEventAsync(renderEvent);
			}
		}
		catch (Exception ex)
		{
			if (_errorTracker != null)
				_ = _errorTracker.TrackErrorAsync(
					ex,
					new Dictionary<string, object?> { ["Method"] = "LogRenderEventAsync" },
					ErrorSeverity.Warning,
					renderEvent.ComponentName,
					"LogRenderEventAsync"
				);
		}
	}

	/// <summary>
	/// Logs a render event to the server console.
	/// </summary>
	/// <param name="renderEvent">The render event to log.</param>
	private void LogToConsole(RenderEvent renderEvent)
	{
		var message = FormatConsoleMessage(renderEvent);
		if (_unifiedLogger != null)
		{
			_unifiedLogger.LogInfo(message);
		}
		else
		{
			Console.WriteLine(message);
		}

		var shouldLogParameterChanges = _config.Verbosity >= TrackingVerbosity.Verbose && renderEvent.ParameterChanges?.Count > 0;
		if (shouldLogParameterChanges)
			LogParameterChangesToConsole(renderEvent);
	}

	/// <summary>
	/// Formats a render event for console output.
	/// </summary>
	/// <param name="renderEvent">The render event to format.</param>
	/// <returns>A formatted string for console output.</returns>
	private string FormatConsoleMessage(RenderEvent renderEvent)
	{
		var parts = new List<string> { $"[WhyDidYouRender] {renderEvent.ComponentName}.{renderEvent.Method}()" };

		if (_config.Verbosity >= TrackingVerbosity.Normal)
		{
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
	private static void LogParameterChangesToConsole(RenderEvent renderEvent)
	{
		if (renderEvent.ParameterChanges?.Count > 0 != true)
			return;

		if (_unifiedLogger != null)
		{
			_unifiedLogger.LogParameterChanges(renderEvent.ComponentName, renderEvent.ParameterChanges);
			return;
		}

		Console.WriteLine("  Parameter changes:");
		foreach (var (paramName, change) in renderEvent.ParameterChanges)
		{
			try
			{
				var changeJson = JsonSerializer.Serialize(change, _jsonOptions);
				Console.WriteLine($"  {paramName}: {changeJson}");
			}
			catch
			{
				Console.WriteLine($"  {paramName}: [Unable to serialize]");
			}
		}
	}

	/// <summary>
	/// Gets the current session ID for tracking purposes.
	/// </summary>
	/// <returns>The session ID, or a default value if not available.</returns>
	private static string GetSessionId() => _sessionContextService?.GetSessionId() ?? "default-session";

	/// <summary>
	/// Starts the cleanup timer for periodic maintenance.
	/// </summary>
	private void StartCleanupTimer()
	{
		var interval = TimeSpan.FromMinutes(_config.SessionCleanupIntervalMinutes);
		_cleanupTimer = new Timer(_ => CleanupSessionSpecificData("periodic-cleanup"), null, interval, interval);
	}

	/// <summary>
	/// Cleans up session-specific data from component caches.
	/// </summary>
	/// <param name="sessionId">The session ID to clean up.</param>
	private void CleanupSessionSpecificData(string sessionId)
	{
		// TODO: implement more thorough cleanup
		// assigned to avoid compiler warning
		_ = sessionId;

		var cleanupTimeSpan = TimeSpan.FromMinutes(_config.SessionCleanupIntervalMinutes);
		_renderFrequencyTracker.CleanupOldHistory(cleanupTimeSpan);
		_performanceTracker.CleanupInactiveComponents([]);
		_parameterChangeDetector.CleanupInactiveComponents([]);
		_unnecessaryRerenderDetector?.CleanupInactiveComponents([]);
	}

	/// <summary>
	/// Gets the total number of components being tracked across all tracking systems.
	/// </summary>
	/// <returns>A dictionary with tracking counts for each system.</returns>
	public Dictionary<string, int> GetTrackedComponentCounts()
	{
		EnsureServicesInitialized();

		return new Dictionary<string, int>
		{
			["ParameterChanges"] = _parameterChangeDetector.GetTrackedComponentCount(),
			["Performance"] = _performanceTracker.GetTrackedComponentCount(),
			["RenderFrequency"] = _renderFrequencyTracker.GetTrackedComponentCount(),
			["UnnecessaryRerenders"] = _unnecessaryRerenderDetector?.GetTrackedComponentCount() ?? 0,
		};
	}

	/// <summary>
	/// Clears all tracking data from all tracking systems.
	/// </summary>
	/// <remarks>
	/// This method is useful for testing scenarios or when you need to reset all tracking state.
	/// Use with caution in production as it will lose all historical tracking data.
	/// </remarks>
	public void ClearAllTrackingData()
	{
		EnsureServicesInitialized();

		_parameterChangeDetector.ClearAll();
		_performanceTracker.ClearAll();
		_renderFrequencyTracker.ClearAll();
		_unnecessaryRerenderDetector?.ClearAll();
		_unnecessaryRerenderDetector?.ResetStateTracking();
	}

	/// <summary>
	/// Initializes state tracking components asynchronously for improved startup performance.
	/// </summary>
	/// <returns>A task representing the initialization operation.</returns>
	public async Task InitializeStateTrackingAsync()
	{
		EnsureServicesInitialized();
		if (_unnecessaryRerenderDetector != null)
			await _unnecessaryRerenderDetector.InitializeStateTrackingAsync();
	}

	/// <summary>
	/// Pre-warms the state tracking cache with common component types.
	/// </summary>
	/// <param name="componentTypes">Component types to pre-analyze.</param>
	/// <returns>A task representing the pre-warming operation.</returns>
	public async Task PreWarmStateTrackingCacheAsync(IEnumerable<Type> componentTypes)
	{
		EnsureServicesInitialized();
		if (_unnecessaryRerenderDetector != null)
		{
			await _unnecessaryRerenderDetector.PreWarmStateTrackingCacheAsync(componentTypes);
		}
	}

	/// <summary>
	/// Gets comprehensive diagnostics about the state tracking system.
	/// </summary>
	/// <returns>Diagnostic information about state tracking, or null if not available.</returns>
	public StateTrackingDiagnostics? GetStateTrackingDiagnostics()
	{
		EnsureServicesInitialized();
		return _unnecessaryRerenderDetector?.GetStateTrackingDiagnostics();
	}

	/// <summary>
	/// Performs maintenance on all tracking systems including state tracking.
	/// </summary>
	public void PerformMaintenance()
	{
		EnsureServicesInitialized();

		_unnecessaryRerenderDetector?.PerformStateTrackingMaintenance();

		var cleanupTimeSpan = TimeSpan.FromMinutes(_config.SessionCleanupIntervalMinutes);
		_renderFrequencyTracker.CleanupOldHistory(cleanupTimeSpan);
		_performanceTracker.CleanupInactiveComponents([]);
		_parameterChangeDetector.CleanupInactiveComponents([]);
		_unnecessaryRerenderDetector?.CleanupInactiveComponents([]);
	}
}
