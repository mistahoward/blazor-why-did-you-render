using System;
using System.Threading.Tasks;

using Blazor.WhyDidYouRender.Diagnostics;
using Blazor.WhyDidYouRender.Records;

namespace Blazor.WhyDidYouRender.Helpers;

/// <summary>
/// Interface for logging render tracking information to the browser console.
/// </summary>
public interface IBrowserConsoleLogger : IAsyncDisposable {
	/// <summary>
	/// Sets the error tracker for handling JavaScript interop errors.
	/// </summary>
	/// <param name="errorTracker">The error tracker instance.</param>
	void SetErrorTracker(IErrorTracker errorTracker);

	/// <summary>
	/// Initializes the browser console logger.
	/// </summary>
	/// <returns>A task representing the initialization operation.</returns>
	Task InitializeAsync();

	/// <summary>
	/// Logs a render event to the browser console.
	/// </summary>
	/// <param name="renderEvent">The render event to log.</param>
	/// <returns>A task representing the logging operation.</returns>
	Task LogRenderEventAsync(RenderEvent renderEvent);

	/// <summary>
	/// Logs a simple message to the browser console.
	/// </summary>
	/// <param name="message">The message to log.</param>
	/// <param name="level">The console level (log, warn, error, etc.).</param>
	/// <returns>A task representing the logging operation.</returns>
	Task LogMessageAsync(string message, string level = "log");
}
