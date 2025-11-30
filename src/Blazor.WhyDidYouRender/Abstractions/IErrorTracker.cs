using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Blazor.WhyDidYouRender.Records;

namespace Blazor.WhyDidYouRender.Abstractions;

/// <summary>
/// Service for tracking and managing errors across different Blazor hosting environments.
/// </summary>
public interface IErrorTracker
{
	/// <summary>
	/// Gets whether this error tracker supports persistent error storage.
	/// </summary>
	bool SupportsPersistentStorage { get; }

	/// <summary>
	/// Gets whether this error tracker supports error reporting endpoints.
	/// </summary>
	bool SupportsErrorReporting { get; }

	/// <summary>
	/// Gets a description of the error tracking capabilities.
	/// </summary>
	string ErrorTrackingDescription { get; }

	/// <summary>
	/// Tracks an error with the specified context and severity.
	/// </summary>
	/// <param name="exception">The exception that occurred.</param>
	/// <param name="context">Additional context information about the error.</param>
	/// <param name="severity">The severity level of the error.</param>
	/// <param name="componentName">Optional name of the component where the error occurred.</param>
	/// <param name="operation">Optional name of the operation that was being performed.</param>
	/// <returns>A task representing the error tracking operation.</returns>
	Task TrackErrorAsync(
		Exception exception,
		Dictionary<string, object?> context,
		ErrorSeverity severity,
		string? componentName = null,
		string? operation = null
	);

	/// <summary>
	/// Tracks an error with a custom message.
	/// </summary>
	/// <param name="message">The error message.</param>
	/// <param name="context">Additional context information about the error.</param>
	/// <param name="severity">The severity level of the error.</param>
	/// <param name="componentName">Optional name of the component where the error occurred.</param>
	/// <param name="operation">Optional name of the operation that was being performed.</param>
	/// <returns>A task representing the error tracking operation.</returns>
	Task TrackErrorAsync(
		string message,
		Dictionary<string, object?> context,
		ErrorSeverity severity,
		string? componentName = null,
		string? operation = null
	);

	/// <summary>
	/// Gets recent errors based on the specified criteria.
	/// </summary>
	/// <param name="count">The maximum number of errors to retrieve.</param>
	/// <param name="severity">Optional minimum severity level to filter by.</param>
	/// <param name="componentName">Optional component name to filter by.</param>
	/// <returns>A collection of recent errors.</returns>
	Task<IEnumerable<TrackingError>> GetRecentErrorsAsync(int count = 50, ErrorSeverity? severity = null, string? componentName = null);

	/// <summary>
	/// Gets error statistics for the current session.
	/// </summary>
	/// <returns>Error statistics information.</returns>
	Task<ErrorStatistics> GetErrorStatisticsAsync();

	/// <summary>
	/// Clears all tracked errors.
	/// </summary>
	/// <returns>A task representing the clear operation.</returns>
	Task ClearErrorsAsync();

	/// <summary>
	/// Gets the total number of errors tracked.
	/// </summary>
	/// <returns>The total error count.</returns>
	Task<int> GetErrorCountAsync();

	/// <summary>
	/// Gets errors that occurred within the specified time range.
	/// </summary>
	/// <param name="since">The start time to filter from.</param>
	/// <param name="until">Optional end time to filter to.</param>
	/// <returns>A collection of errors within the time range.</returns>
	Task<IEnumerable<TrackingError>> GetErrorsSinceAsync(DateTime since, DateTime? until = null);
}
