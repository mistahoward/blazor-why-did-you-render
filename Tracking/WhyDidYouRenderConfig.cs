using System;
using System.Collections.Generic;

namespace Blazor.WhyDidYouRender.Tracking;

/// <summary>
/// Verbosity levels for render tracking output.
/// </summary>
public enum TrackingVerbosity {
	/// <summary>
	/// Minimal output - only component name and method.
	/// </summary>
	Minimal,

	/// <summary>
	/// Normal output - includes timing and session info.
	/// </summary>
	Normal,

	/// <summary>
	/// Verbose output - includes all available information including parameter changes.
	/// </summary>
	Verbose
}

/// <summary>
/// Output destinations for render tracking logs.
/// </summary>
[Flags]
public enum TrackingOutput {
	/// <summary>
	/// No output.
	/// </summary>
	None = 0,

	/// <summary>
	/// Output to server console/terminal.
	/// </summary>
	Console = 1,

	/// <summary>
	/// Output to browser devtools console.
	/// </summary>
	BrowserConsole = 2,

	/// <summary>
	/// Output to both console and browser.
	/// </summary>
	Both = Console | BrowserConsole
}

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
}
