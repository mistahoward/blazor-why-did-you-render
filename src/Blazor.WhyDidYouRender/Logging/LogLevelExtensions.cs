namespace Blazor.WhyDidYouRender.Logging;

/// <summary>
/// Extension methods for LogLevel.
/// </summary>
public static class LogLevelExtensions
{
	/// <summary>
	/// Determines whether the current level meets or exceeds the specified minimum.
	/// </summary>
	/// <param name="current">The current level.</param>
	/// <param name="minimum">The minimum enabled level.</param>
	/// <returns>True if current is enabled; otherwise false.</returns>
	public static bool IsEnabled(this LogLevel current, LogLevel minimum) => current >= minimum;

	/// <summary>
	/// Converts the level to a string.
	/// </summary>
	/// <param name="level">The level.</param>
	/// <returns>A string representation of the level.</returns>
	public static string ToString(this LogLevel level) =>
		level switch
		{
			LogLevel.Debug => "Debug",
			LogLevel.Info => "Info",
			LogLevel.Warning => "Warning",
			LogLevel.Error => "Error",
			LogLevel.None => "None",
			_ => "Unknown",
		};
}
