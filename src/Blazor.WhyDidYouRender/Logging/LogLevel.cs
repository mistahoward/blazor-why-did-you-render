namespace Blazor.WhyDidYouRender.Logging;

/// <summary>
/// Logging severity levels used by WhyDidYouRender.
/// </summary>
public enum LogLevel {
	/// <summary>Detailed diagnostic messages for troubleshooting.</summary>
	Debug = 0,
	/// <summary>General informational messages about normal operation.</summary>
	Info = 1,
	/// <summary>Potential issues that may require attention.</summary>
	Warning = 2,
	/// <summary>Errors that prevent an operation from completing successfully.</summary>
	Error = 3,
	/// <summary>Disables logging.</summary>
	None = 4
}
