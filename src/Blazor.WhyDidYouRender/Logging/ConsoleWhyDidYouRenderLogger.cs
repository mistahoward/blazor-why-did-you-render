using Blazor.WhyDidYouRender.Configuration;

namespace Blazor.WhyDidYouRender.Logging;

/// <summary>
/// Minimal console-based logger implementation used as a baseline and fallback.
/// Replaces direct Console.WriteLine usage with structured, prefixed output.
/// </summary>
public class ConsoleWhyDidYouRenderLogger : WhyDidYouRenderLoggerBase
{
	/// <summary>
	/// Initializes a new instance of the ConsoleWhyDidYouRenderLogger class.
	/// </summary>
	/// <param name="config">The WhyDidYouRender configuration.</param>
	public ConsoleWhyDidYouRenderLogger(WhyDidYouRenderConfig config)
		: base(config) { }

	/// <summary>
	/// Logs a debug message to the console with a WhyDidYouRender prefix.
	/// </summary>
	/// <param name="message">The message to log.</param>
	/// <param name="data">Optional structured data to include.</param>
	public override void LogDebug(string message, Dictionary<string, object?>? data = null)
	{
		if (!IsEnabled(LogLevel.Debug))
			return;
		Write("DEBUG", message, data);
	}

	/// <summary>
	/// Logs an informational message to the console with a WhyDidYouRender prefix.
	/// </summary>
	/// <param name="message">The message to log.</param>
	/// <param name="data">Optional structured data to include.</param>
	public override void LogInfo(string message, Dictionary<string, object?>? data = null)
	{
		if (!IsEnabled(LogLevel.Info))
			return;
		Write("INFO", message, data);
	}

	/// <summary>
	/// Logs a warning message to the console with a WhyDidYouRender prefix.
	/// </summary>
	/// <param name="message">The message to log.</param>
	/// <param name="data">Optional structured data to include.</param>
	public override void LogWarning(string message, Dictionary<string, object?>? data = null)
	{
		if (!IsEnabled(LogLevel.Warning))
			return;
		Write("WARN", message, data);
	}

	/// <summary>
	/// Logs an error message to the console with a WhyDidYouRender prefix.
	/// </summary>
	/// <param name="message">The message to log.</param>
	/// <param name="exception">Optional exception to include.</param>
	/// <param name="data">Optional structured data to include.</param>
	public override void LogError(string message, Exception? exception = null, Dictionary<string, object?>? data = null)
	{
		if (!IsEnabled(LogLevel.Error))
			return;
		if (exception != null)
		{
			data ??= new();
			data["exceptionType"] = exception.GetType().Name;
			data["exceptionMessage"] = exception.Message;
		}
		Write("ERROR", message, data);
	}

	private void Write(string level, string message, Dictionary<string, object?>? data)
	{
		var prefix = $"[WhyDidYouRender] [{level}]";
		if (!string.IsNullOrEmpty(_correlationId))
			prefix += $" [{_correlationId}]";
		Console.WriteLine($"{prefix} {message}");

		if (data is { Count: > 0 })
		{
			foreach (var kvp in data)
			{
				Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
			}
		}
	}
}
