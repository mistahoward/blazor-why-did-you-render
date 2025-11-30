namespace Blazor.WhyDidYouRender.Configuration;

/// <summary>
/// Verbosity levels for render tracking output.
/// </summary>
public enum TrackingVerbosity
{
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
	Verbose,
}
