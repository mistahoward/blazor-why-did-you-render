using System;

namespace Blazor.WhyDidYouRender.Configuration;

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
