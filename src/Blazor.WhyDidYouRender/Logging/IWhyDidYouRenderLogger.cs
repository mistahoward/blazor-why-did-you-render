using Blazor.WhyDidYouRender.Records;

namespace Blazor.WhyDidYouRender.Logging;

/// <summary>
/// Abstraction for structured, environment-agnostic logging used by WhyDidYouRender.
/// Implementations may log to server consoles, browser consoles, telemetry, or other sinks.
/// </summary>
public interface IWhyDidYouRenderLogger
{
	/// <summary>
	/// Writes a debug-level message.
	/// </summary>
	/// <param name="message">The message to log.</param>
	/// <param name="data">Optional structured properties.</param>
	void LogDebug(string message, Dictionary<string, object?>? data = null);

	/// <summary>
	/// Writes an informational message.
	/// </summary>
	/// <param name="message">The message to log.</param>
	/// <param name="data">Optional structured properties.</param>
	void LogInfo(string message, Dictionary<string, object?>? data = null);

	/// <summary>
	/// Writes a warning message.
	/// </summary>
	/// <param name="message">The message to log.</param>
	/// <param name="data">Optional structured properties.</param>
	void LogWarning(string message, Dictionary<string, object?>? data = null);

	/// <summary>
	/// Writes an error message.
	/// </summary>
	/// <param name="message">The message to log.</param>
	/// <param name="exception">Optional exception to include.</param>
	/// <param name="data">Optional structured properties.</param>
	void LogError(string message, Exception? exception = null, Dictionary<string, object?>? data = null);

	/// <summary>
	/// Logs a render event with structured details.
	/// </summary>
	/// <param name="renderEvent">The render event.</param>
	void LogRenderEvent(RenderEvent renderEvent);

	/// <summary>
	/// Logs parameter changes for a component.
	/// </summary>
	/// <param name="componentName">The component name.</param>
	/// <param name="changes">Parameter changes keyed by name.</param>
	void LogParameterChanges(string componentName, Dictionary<string, object?> changes);

	/// <summary>
	/// Logs a performance measurement for a lifecycle method.
	/// </summary>
	/// <param name="componentName">The component name.</param>
	/// <param name="method">The lifecycle method.</param>
	/// <param name="durationMs">Duration in milliseconds.</param>
	/// <param name="metrics">Optional additional metrics.</param>
	void LogPerformance(string componentName, string method, double durationMs, Dictionary<string, object?>? metrics = null);

	/// <summary>
	/// Logs a state change for a component field or property.
	/// </summary>
	/// <param name="componentName">The component name.</param>
	/// <param name="fieldName">The field or property name.</param>
	/// <param name="previousValue">Previous value.</param>
	/// <param name="currentValue">Current value.</param>
	/// <param name="changeType">A textual description of the change type.</param>
	void LogStateChange(string componentName, string fieldName, object? previousValue, object? currentValue, string changeType);

	/// <summary>
	/// Logs an unnecessary re-render event and the reason.
	/// </summary>
	/// <param name="componentName">The component name.</param>
	/// <param name="reason">The reason the re-render was unnecessary.</param>
	/// <param name="context">Optional additional context.</param>
	void LogUnnecessaryRerender(string componentName, string reason, Dictionary<string, object?>? context = null);

	/// <summary>
	/// Logs a frequent re-render warning.
	/// </summary>
	/// <param name="componentName">The component name.</param>
	/// <param name="renderCount">Number of renders.</param>
	/// <param name="timeSpan">Time window measured.</param>
	/// <param name="context">Optional additional context.</param>
	void LogFrequentRerender(string componentName, int renderCount, TimeSpan timeSpan, Dictionary<string, object?>? context = null);

	/// <summary>
	/// Logs component initialization.
	/// </summary>
	/// <param name="componentName">The component name.</param>
	/// <param name="config">Optional configuration snapshot.</param>
	void LogInitialization(string componentName, Dictionary<string, object?>? config = null);

	/// <summary>
	/// Logs component disposal.
	/// </summary>
	/// <param name="componentName">The component name.</param>
	void LogDisposal(string componentName);

	/// <summary>
	/// Logs an exception related to a component operation.
	/// </summary>
	/// <param name="componentName">The component name.</param>
	/// <param name="exception">The exception to log.</param>
	/// <param name="operation">The operation where the exception occurred.</param>
	/// <param name="context">Optional additional context.</param>
	void LogException(string componentName, Exception exception, string operation, Dictionary<string, object?>? context = null);

	/// <summary>
	/// Sets a correlation identifier used to link related log entries.
	/// </summary>
	/// <param name="correlationId">The correlation identifier.</param>
	void SetCorrelationId(string correlationId);

	/// <summary>
	/// Gets the current correlation identifier, if any.
	/// </summary>
	/// <returns>The correlation identifier or null.</returns>
	string? GetCorrelationId();

	/// <summary>
	/// Clears the correlation identifier.
	/// </summary>
	void ClearCorrelationId();

	/// <summary>
	/// Indicates whether the specified level is enabled.
	/// </summary>
	/// <param name="level">The level to check.</param>
	/// <returns>True if enabled; otherwise false.</returns>
	bool IsEnabled(LogLevel level);

	/// <summary>
	/// Sets the minimum enabled log level.
	/// </summary>
	/// <param name="level">The minimum level.</param>
	void SetLogLevel(LogLevel level);

	/// <summary>
	/// Gets the current minimum enabled log level.
	/// </summary>
	/// <returns>The current level.</returns>
	LogLevel GetLogLevel();
}
