using System;
using System.Collections.Generic;

using Blazor.WhyDidYouRender.Abstractions;

namespace Blazor.WhyDidYouRender.Configuration;

/// <summary>
/// Configuration options for the WhyDidYouRender tracking system.
/// </summary>
public class WhyDidYouRenderConfig {
	/// <summary>
	/// Gets or sets whether tracking is enabled.
	/// </summary>
	public bool Enabled { get; set; } = true;

	/// <summary>
	/// Gets or sets the verbosity level for tracking output.
	/// </summary>
	public TrackingVerbosity Verbosity { get; set; } = TrackingVerbosity.Normal;

	/// <summary>
	/// Gets or sets the output destinations for tracking logs.
	/// </summary>
	public TrackingOutput Output { get; set; } = TrackingOutput.Console;

	/// <summary>
	/// Gets or sets whether to track parameter changes.
	/// </summary>
	public bool TrackParameterChanges { get; set; } = true;

	/// <summary>
	/// Gets or sets whether to track render performance metrics.
	/// </summary>
	public bool TrackPerformance { get; set; } = true;

	/// <summary>
	/// Gets or sets whether to include session information in logs.
	/// </summary>
	public bool IncludeSessionInfo { get; set; } = true;

	/// <summary>
	/// Gets or sets component name patterns to include (null means include all).
	/// Supports wildcards like "Counter*" or "*Component".
	/// </summary>
	public List<string>? IncludeComponents { get; set; }

	/// <summary>
	/// Gets or sets component name patterns to exclude.
	/// Supports wildcards like "System.*" or "*Layout*".
	/// </summary>
	public List<string>? ExcludeComponents { get; set; }

	/// <summary>
	/// Gets or sets namespace patterns to include (null means include all).
	/// </summary>
	public List<string>? IncludeNamespaces { get; set; }

	/// <summary>
	/// Gets or sets namespace patterns to exclude.
	/// </summary>
	public List<string>? ExcludeNamespaces { get; set; }

	/// <summary>
	/// Gets or sets the maximum number of parameter changes to log per component.
	/// Helps prevent log spam for components with many parameters.
	/// </summary>
	public int MaxParameterChangesToLog { get; set; } = 10;

	/// <summary>
	/// Gets or sets whether to log only when parameters actually change (vs. all OnParametersSet calls).
	/// </summary>
	public bool LogOnlyWhenParametersChange { get; set; } = false;

	/// <summary>
	/// Gets or sets whether to detect and warn about unnecessary re-renders.
	/// This includes manual StateHasChanged calls that don't change anything,
	/// and parameter updates that are functionally identical.
	/// </summary>
	public bool DetectUnnecessaryRerenders { get; set; } = true;

	/// <summary>
	/// Gets or sets whether to highlight unnecessary re-renders with special styling in browser console.
	/// </summary>
	public bool HighlightUnnecessaryRerenders { get; set; } = true;

	/// <summary>
	/// Gets or sets the threshold for considering a re-render "frequent" (renders per second).
	/// Components exceeding this threshold will be flagged for potential optimization.
	/// </summary>
	public double FrequentRerenderThreshold { get; set; } = 5.0;

	/// <summary>
	/// Gets or sets whether state tracking is enabled.
	/// When enabled, the system will track changes to component fields and properties
	/// to provide more accurate unnecessary render detection.
	/// </summary>
	public bool EnableStateTracking { get; set; } = true;

	/// <summary>
	/// Gets or sets whether simple value types should be automatically tracked.
	/// When true, fields of types like string, int, bool, DateTime, etc. are tracked automatically.
	/// When false, only fields with [TrackState] attribute are tracked.
	/// </summary>
	public bool AutoTrackSimpleTypes { get; set; } = true;

	/// <summary>
	/// Gets or sets the maximum number of fields to track per component.
	/// This helps prevent performance issues with components that have many fields.
	/// </summary>
	public int MaxTrackedFieldsPerComponent { get; set; } = 50;

	/// <summary>
	/// Gets or sets whether state changes should be logged in diagnostic output.
	/// When true, state changes that trigger renders will be included in logs.
	/// </summary>
	public bool LogStateChanges { get; set; } = true;

	/// <summary>
	/// Gets or sets whether to track fields inherited from base classes.
	/// When true, fields from base component classes are included in state tracking.
	/// When false, only fields declared directly in the component are tracked.
	/// </summary>
	public bool TrackInheritedFields { get; set; } = true;

	/// <summary>
	/// Gets or sets the maximum depth for complex object comparison.
	/// This applies to fields with [TrackState(UseCustomComparer = true)].
	/// Higher values provide more accurate change detection but impact performance.
	/// </summary>
	public int MaxStateComparisonDepth { get; set; } = 3;

	/// <summary>
	/// Gets or sets whether to enable collection content tracking by default.
	/// When true, collections marked with [TrackState] will track content changes.
	/// When false, only collection reference changes are tracked by default.
	/// </summary>
	public bool EnableCollectionContentTracking { get; set; } = false;

	/// <summary>
	/// Gets or sets the interval (in minutes) for cleaning up state snapshots.
	/// Older snapshots are cleaned up to prevent memory leaks.
	/// </summary>
	public int StateSnapshotCleanupIntervalMinutes { get; set; } = 10;

	/// <summary>
	/// Gets or sets the maximum age (in minutes) for state snapshots before cleanup.
	/// Snapshots older than this will be removed during cleanup.
	/// </summary>
	public int MaxStateSnapshotAgeMinutes { get; set; } = 30;

	/// <summary>
	/// Gets or sets the maximum number of components to track simultaneously.
	/// This helps prevent memory issues in applications with many components.
	/// </summary>
	public int MaxTrackedComponents { get; set; } = 1000;

	/// <summary>
	/// Gets or sets whether to log detailed state change information.
	/// When true, individual field changes are logged with before/after values.
	/// </summary>
	public bool LogDetailedStateChanges { get; set; } = false;

	/// <summary>
	/// Gets or sets component name patterns to exclude from state tracking.
	/// Supports wildcards like "System.*" or "*Layout*".
	/// </summary>
	public List<string>? ExcludeFromStateTracking { get; set; }

	/// <summary>
	/// Gets or sets component name patterns to include in state tracking.
	/// When specified, only matching components will have state tracking enabled.
	/// </summary>
	public List<string>? IncludeInStateTracking { get; set; }

	/// <summary>
	/// Gets or sets whether to include user information in tracking data.
	/// Should be disabled in production for privacy compliance.
	/// </summary>
	public bool IncludeUserInfo { get; set; } = false;

	/// <summary>
	/// Gets or sets whether to include client information (IP, User-Agent) in tracking data.
	/// Should be disabled in production for privacy compliance.
	/// </summary>
	public bool IncludeClientInfo { get; set; } = false;

	/// <summary>
	/// Gets or sets whether to track components during prerendering.
	/// May be disabled to reduce noise in SSR scenarios.
	/// </summary>
	public bool TrackDuringPrerendering { get; set; } = true;

	/// <summary>
	/// Gets or sets whether to track components during hydration.
	/// Useful for debugging SSR hydration issues.
	/// </summary>
	public bool TrackDuringHydration { get; set; } = true;

	/// <summary>
	/// Gets or sets the maximum number of sessions to track concurrently.
	/// Helps prevent memory issues in high-traffic scenarios.
	/// </summary>
	public int MaxConcurrentSessions { get; set; } = 1000;

	/// <summary>
	/// Gets or sets the session data cleanup interval in minutes.
	/// Older session data will be cleaned up to prevent memory leaks.
	/// </summary>
	public int SessionCleanupIntervalMinutes { get; set; } = 30;

	/// <summary>
	/// Gets or sets whether to enable enhanced security mode.
	/// When enabled, all potentially sensitive data is sanitized or excluded.
	/// </summary>
	public bool EnableSecurityMode { get; set; } = false;

	/// <summary>
	/// Gets or sets whether to enable error tracking and reporting.
	/// When enabled, errors during tracking operations are logged and reported.
	/// </summary>
	public bool EnableErrorTracking { get; set; } = true;

	/// <summary>
	/// Gets or sets the maximum number of errors to keep in memory for diagnostics.
	/// Older errors are automatically cleaned up to prevent memory issues.
	/// </summary>
	public int MaxErrorHistorySize { get; set; } = 1000;

	/// <summary>
	/// Gets or sets the error cleanup interval in minutes.
	/// Errors older than this interval will be cleaned up automatically.
	/// </summary>
	public int ErrorCleanupIntervalMinutes { get; set; } = 60;

	/// <summary>
	/// Gets or sets whether to automatically detect the hosting environment.
	/// When true, the library will automatically adapt to Server/WASM/SSR environments.
	/// </summary>
	public bool AutoDetectEnvironment { get; set; } = true;

	/// <summary>
	/// Gets or sets a forced hosting model override.
	/// When set, overrides automatic environment detection.
	/// Useful for testing or special deployment scenarios.
	/// </summary>
	public BlazorHostingModel? ForceHostingModel { get; set; } = null;

	/// <summary>
	/// Gets or sets WASM-specific storage options.
	/// Only applies when running in WebAssembly environment.
	/// </summary>
	public WasmStorageOptions WasmStorage { get; set; } = new();

	/// <summary>
	/// Validates the configuration for the specified hosting environment.
	/// </summary>
	/// <param name="hostingModel">The hosting model to validate against.</param>
	/// <returns>A list of validation errors, empty if configuration is valid.</returns>
	public List<string> Validate(BlazorHostingModel hostingModel) {
		var errors = new List<string>();

		// Basic validation
		if (ErrorCleanupIntervalMinutes <= 0) {
			errors.Add("ErrorCleanupIntervalMinutes must be greater than 0");
		}

		if (MaxErrorHistorySize <= 0) {
			errors.Add("MaxErrorHistorySize must be greater than 0");
		}

		ValidateStateTrackingConfiguration(errors);

		switch (hostingModel) {
			case BlazorHostingModel.WebAssembly:
				ValidateWasmConfiguration(errors);
				break;
			case BlazorHostingModel.Server:
			case BlazorHostingModel.ServerSideRendering:
				ValidateServerConfiguration(errors);
				break;
		}

		ValidateOutputConfiguration(errors, hostingModel);

		return errors;
	}

	/// <summary>
	/// Validates the configuration and throws an exception if invalid.
	/// </summary>
	/// <param name="hostingModel">The hosting model to validate against.</param>
	/// <exception cref="InvalidOperationException">Thrown when configuration is invalid.</exception>
	public void ValidateAndThrow(BlazorHostingModel hostingModel) {
		var errors = Validate(hostingModel);
		if (errors.Count > 0) {
			throw new InvalidOperationException($"WhyDidYouRender configuration is invalid:\n{string.Join("\n", errors)}");
		}
	}

	/// <summary>
	/// Adapts the configuration for the specified hosting environment.
	/// This method automatically adjusts settings that are not supported in certain environments.
	/// </summary>
	/// <param name="hostingModel">The hosting model to adapt for.</param>
	/// <returns>True if any settings were changed, false otherwise.</returns>
	public bool AdaptForEnvironment(BlazorHostingModel hostingModel) {
		bool changed = false;

		switch (hostingModel) {
			case BlazorHostingModel.WebAssembly:
				if (Output == TrackingOutput.Console) {
					Output = TrackingOutput.BrowserConsole;
					changed = true;
				}
				else if (Output == TrackingOutput.Both) {
					Output = TrackingOutput.BrowserConsole;
					changed = true;
				}
				break;

			case BlazorHostingModel.Server:
			case BlazorHostingModel.ServerSideRendering:
				break;
		}

		return changed;
	}

	/// <summary>
	/// Gets a summary of the configuration for logging purposes.
	/// </summary>
	/// <returns>A dictionary containing configuration summary.</returns>
	public Dictionary<string, object> GetConfigurationSummary() {
		return new Dictionary<string, object> {
			["Enabled"] = Enabled,
			["Verbosity"] = Verbosity.ToString(),
			["Output"] = Output.ToString(),
			["TrackParameterChanges"] = TrackParameterChanges,
			["TrackPerformance"] = TrackPerformance,
			["IncludeSessionInfo"] = IncludeSessionInfo,
			["AutoDetectEnvironment"] = AutoDetectEnvironment,
			["ForceHostingModel"] = ForceHostingModel?.ToString() ?? "Auto",
			["WasmStorageEnabled"] = WasmStorage.UseLocalStorage || WasmStorage.UseSessionStorage,

			["EnableStateTracking"] = EnableStateTracking,
			["AutoTrackSimpleTypes"] = AutoTrackSimpleTypes,
			["MaxTrackedFieldsPerComponent"] = MaxTrackedFieldsPerComponent,
			["LogStateChanges"] = LogStateChanges,
			["TrackInheritedFields"] = TrackInheritedFields,
			["MaxStateComparisonDepth"] = MaxStateComparisonDepth,
			["EnableCollectionContentTracking"] = EnableCollectionContentTracking,
			["MaxTrackedComponents"] = MaxTrackedComponents,
			["LogDetailedStateChanges"] = LogDetailedStateChanges
		};
	}

	/// <summary>
	/// Validates the WebAssembly-specific configuration settings.
	/// </summary>
	/// <param name="errors">The list to which validation errors will be added.</param>
	/// <remarks>
	/// Performs validation checks for:
	/// <list type="bullet">
	///   <item>Storage entry size limits and boundaries</item>
	///   <item>Error and session storage limits</item>
	///   <item>Storage cleanup intervals</item>
	///   <item>Storage key prefix requirements</item>
	///   <item>Storage type selection (local/session)</item>
	///   <item>Browser storage quota considerations</item>
	/// </list>
	/// </remarks>
	private void ValidateWasmConfiguration(List<string> errors) {
		if (WasmStorage.MaxStorageEntrySize <= 0)
			errors.Add("WasmStorage.MaxStorageEntrySize must be greater than 0");

		if (WasmStorage.MaxStoredErrors <= 0)
			errors.Add("WasmStorage.MaxStoredErrors must be greater than 0");

		if (WasmStorage.MaxStoredSessions <= 0)
			errors.Add("WasmStorage.MaxStoredSessions must be greater than 0");

		if (WasmStorage.StorageCleanupIntervalMinutes <= 0)
			errors.Add("WasmStorage.StorageCleanupIntervalMinutes must be greater than 0");

		if (string.IsNullOrWhiteSpace(WasmStorage.StorageKeyPrefix))
			errors.Add("WasmStorage.StorageKeyPrefix cannot be null or empty");

		if (!WasmStorage.UseLocalStorage && !WasmStorage.UseSessionStorage)
			errors.Add("At least one of WasmStorage.UseLocalStorage or WasmStorage.UseSessionStorage must be enabled");

		if (WasmStorage.MaxStorageEntrySize > 5 * 1024 * 1024) // 5MB
			errors.Add("WasmStorage.MaxStorageEntrySize is very large and may cause browser storage quota issues");
	}

	/// <summary>
	/// Validates the Server/SSR-specific configuration settings.
	/// </summary>
	/// <param name="errors">The list to which validation errors will be added.</param>
	/// <remarks>
	/// Performs validation checks for:
	/// <list type="bullet">
	///   <item>Error tracking and cleanup settings</item>
	///   <item>Session management configuration</item>
	///   <item>Performance settings for server environments</item>
	///   <item>Threading and concurrency limits</item>
	///   <item>Memory management settings</item>
	/// </list>
	/// </remarks>
	private void ValidateServerConfiguration(List<string> errors) {
		if (EnableErrorTracking && MaxErrorHistorySize > 10000)
			errors.Add("MaxErrorHistorySize should not exceed 10,000 in server environments to avoid memory issues");

		if (EnableErrorTracking && ErrorCleanupIntervalMinutes > 1440) // 24 hours
			errors.Add("ErrorCleanupIntervalMinutes should not exceed 1440 minutes (24 hours) to prevent memory buildup");

		if (EnableStateTracking) {
			if (MaxTrackedComponents > 5000)
				errors.Add("MaxTrackedComponents should not exceed 5,000 in server environments to avoid memory issues");

			if (MaxTrackedFieldsPerComponent > 100)
				errors.Add("MaxTrackedFieldsPerComponent should not exceed 100 in server environments for optimal performance");

			if (StateSnapshotCleanupIntervalMinutes > 60)
				errors.Add("StateSnapshotCleanupIntervalMinutes should not exceed 60 minutes in server environments to prevent memory buildup");
		}

		if (IncludeSessionInfo && !EnableStateTracking && !TrackParameterChanges)
			errors.Add("IncludeSessionInfo is enabled but no tracking features are active - consider disabling for performance");

		if (Output == TrackingOutput.BrowserConsole)
			errors.Add("TrackingOutput.BrowserConsole requires browser console logger initialization in server environments");
	}

	/// <summary>
	/// Validates the state tracking configuration settings and adds any validation errors to the provided list.
	/// </summary>
	/// <param name="errors">The list to which validation errors will be added.</param>
	/// <remarks>
	/// Performs validation checks for:
	/// <list type="bullet">
	///   <item>Maximum tracked fields per component limits</item>
	///   <item>State comparison depth boundaries</item>
	///   <item>Snapshot cleanup and age intervals</item>
	///   <item>Maximum tracked components limits</item>
	/// </list>
	/// </remarks>
	private void ValidateStateTrackingConfiguration(List<string> errors) {
		if (MaxTrackedFieldsPerComponent <= 0)
			errors.Add("MaxTrackedFieldsPerComponent must be greater than 0");

		if (MaxTrackedFieldsPerComponent > 500)
			errors.Add("MaxTrackedFieldsPerComponent should not exceed 500 to avoid performance issues");

		if (MaxStateComparisonDepth < 0)
			errors.Add("MaxStateComparisonDepth cannot be negative");

		if (MaxStateComparisonDepth > 10)
			errors.Add("MaxStateComparisonDepth should not exceed 10 to avoid performance issues");

		if (StateSnapshotCleanupIntervalMinutes <= 0)
			errors.Add("StateSnapshotCleanupIntervalMinutes must be greater than 0");

		if (MaxStateSnapshotAgeMinutes <= 0)
			errors.Add("MaxStateSnapshotAgeMinutes must be greater than 0");

		if (MaxStateSnapshotAgeMinutes < StateSnapshotCleanupIntervalMinutes)
			errors.Add("MaxStateSnapshotAgeMinutes should be greater than or equal to StateSnapshotCleanupIntervalMinutes");

		if (MaxTrackedComponents <= 0)
			errors.Add("MaxTrackedComponents must be greater than 0");

		if (MaxTrackedComponents > 10000)
			errors.Add("MaxTrackedComponents should not exceed 10000 to avoid memory issues");
	}

	/// <summary>
	/// Validates the output and verbosity configuration settings.
	/// </summary>
	/// <param name="errors">The list of validation errors to append to.</param>
	/// <param name="hostingModel">The Blazor hosting model being used.</param>
	/// <remarks>
	/// Checks for:
	/// <list type="bullet">
	///  <item>Console output compatibility with WebAssembly</item>
	///  <item>Valid TrackingOutput enum values</item>
	///  <item>Valid TrackingVerbosity enum values</item>
	/// </list>
	/// </remarks>
	private void ValidateOutputConfiguration(List<string> errors, BlazorHostingModel hostingModel) {
		if (hostingModel == BlazorHostingModel.WebAssembly && (Output == TrackingOutput.Console || Output == TrackingOutput.Both))
			errors.Add("TrackingOutput.Console is not supported in WebAssembly. Use BrowserConsole instead.");

		if (!Enum.IsDefined(Output))
			errors.Add($"Invalid TrackingOutput value: {Output}");

		if (!Enum.IsDefined(Verbosity))
			errors.Add($"Invalid TrackingVerbosity value: {Verbosity}");
	}
}
