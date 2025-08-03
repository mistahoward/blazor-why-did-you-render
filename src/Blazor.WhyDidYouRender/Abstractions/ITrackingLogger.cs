using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Blazor.WhyDidYouRender.Configuration;
using Blazor.WhyDidYouRender.Records;

namespace Blazor.WhyDidYouRender.Abstractions;

/// <summary>
/// Service for logging render tracking information across different Blazor hosting environments.
/// </summary>
public interface ITrackingLogger {
	/// <summary>
	/// Gets whether this logger supports server console output.
	/// </summary>
	bool SupportsServerConsole { get; }

	/// <summary>
	/// Gets whether this logger supports browser console output.
	/// </summary>
	bool SupportsBrowserConsole { get; }

	/// <summary>
	/// Gets a description of the logging capabilities.
	/// </summary>
	string LoggingDescription { get; }

	/// <summary>
	/// Initializes the tracking logger.
	/// </summary>
	/// <returns>A task representing the initialization operation.</returns>
	Task InitializeAsync();

	/// <summary>
	/// Logs a render event with the specified verbosity level.
	/// </summary>
	/// <param name="renderEvent">The render event to log.</param>
	/// <returns>A task representing the logging operation.</returns>
	Task LogRenderEventAsync(RenderEvent renderEvent);

	/// <summary>
	/// Logs a simple message with the specified verbosity level.
	/// </summary>
	/// <param name="verbosity">The verbosity level for the message.</param>
	/// <param name="message">The message to log.</param>
	/// <param name="data">Optional additional data to include with the message.</param>
	/// <returns>A task representing the logging operation.</returns>
	Task LogMessageAsync(TrackingVerbosity verbosity, string message, object? data = null);

	/// <summary>
	/// Logs an error message.
	/// </summary>
	/// <param name="message">The error message to log.</param>
	/// <param name="exception">Optional exception associated with the error.</param>
	/// <param name="context">Optional context information.</param>
	/// <returns>A task representing the logging operation.</returns>
	Task LogErrorAsync(string message, Exception? exception = null, Dictionary<string, object?>? context = null);

	/// <summary>
	/// Logs a warning message.
	/// </summary>
	/// <param name="message">The warning message to log.</param>
	/// <param name="context">Optional context information.</param>
	/// <returns>A task representing the logging operation.</returns>
	Task LogWarningAsync(string message, Dictionary<string, object?>? context = null);

	/// <summary>
	/// Logs parameter changes for a component.
	/// </summary>
	/// <param name="componentName">The name of the component.</param>
	/// <param name="parameterChanges">The parameter changes to log.</param>
	/// <returns>A task representing the logging operation.</returns>
	Task LogParameterChangesAsync(string componentName, Dictionary<string, object?> parameterChanges);

	/// <summary>
	/// Logs performance metrics for a component render.
	/// </summary>
	/// <param name="componentName">The name of the component.</param>
	/// <param name="method">The lifecycle method that was called.</param>
	/// <param name="durationMs">The duration of the operation in milliseconds.</param>
	/// <param name="additionalMetrics">Optional additional performance metrics.</param>
	/// <returns>A task representing the logging operation.</returns>
	Task LogPerformanceAsync(string componentName, string method, double durationMs, Dictionary<string, object?>? additionalMetrics = null);
}
