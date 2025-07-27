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
			["WasmStorageEnabled"] = WasmStorage.UseLocalStorage || WasmStorage.UseSessionStorage
		};
	}

	private void ValidateWasmConfiguration(List<string> errors) {
		if (WasmStorage.MaxStorageEntrySize <= 0) {
			errors.Add("WasmStorage.MaxStorageEntrySize must be greater than 0");
		}

		if (WasmStorage.MaxStoredErrors <= 0) {
			errors.Add("WasmStorage.MaxStoredErrors must be greater than 0");
		}

		if (WasmStorage.MaxStoredSessions <= 0) {
			errors.Add("WasmStorage.MaxStoredSessions must be greater than 0");
		}

		if (WasmStorage.StorageCleanupIntervalMinutes <= 0) {
			errors.Add("WasmStorage.StorageCleanupIntervalMinutes must be greater than 0");
		}

		if (string.IsNullOrWhiteSpace(WasmStorage.StorageKeyPrefix)) {
			errors.Add("WasmStorage.StorageKeyPrefix cannot be null or empty");
		}

		if (!WasmStorage.UseLocalStorage && !WasmStorage.UseSessionStorage) {
			errors.Add("At least one of WasmStorage.UseLocalStorage or WasmStorage.UseSessionStorage must be enabled");
		}

		if (WasmStorage.MaxStorageEntrySize > 5 * 1024 * 1024) { // 5MB
			errors.Add("WasmStorage.MaxStorageEntrySize is very large and may cause browser storage quota issues");
		}
	}

	private void ValidateServerConfiguration(List<string> errors) {
	}

	private void ValidateOutputConfiguration(List<string> errors, BlazorHostingModel hostingModel) {
		if (hostingModel == BlazorHostingModel.WebAssembly) {
			if (Output == TrackingOutput.Console) {
				errors.Add("TrackingOutput.Console is not supported in WebAssembly. Use BrowserConsole instead.");
			}
		}

		if (!Enum.IsDefined(typeof(TrackingOutput), Output)) {
			errors.Add($"Invalid TrackingOutput value: {Output}");
		}

		if (!Enum.IsDefined(typeof(TrackingVerbosity), Verbosity)) {
			errors.Add($"Invalid TrackingVerbosity value: {Verbosity}");
		}
	}
}
