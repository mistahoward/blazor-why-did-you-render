using System;
using System.Collections.Generic;

using Blazor.WhyDidYouRender.Records;

namespace Blazor.WhyDidYouRender.Diagnostics;

/// <summary>
/// Service for tracking and reporting errors that occur during render tracking.
/// </summary>
public interface IErrorTracker {
	/// <summary>
	/// Tracks an error that occurred during render tracking.
	/// </summary>
	/// <param name="exception">The exception that occurred.</param>
	/// <param name="context">Additional context information.</param>
	/// <param name="severity">The severity level of the error.</param>
	/// <returns>The error ID for tracking purposes.</returns>
	string TrackError(Exception exception, Dictionary<string, object?>? context = null, ErrorSeverity severity = ErrorSeverity.Error);

	/// <summary>
	/// Tracks a custom error message.
	/// </summary>
	/// <param name="message">The error message.</param>
	/// <param name="context">Additional context information.</param>
	/// <param name="severity">The severity level of the error.</param>
	/// <returns>The error ID for tracking purposes.</returns>
	string TrackError(string message, Dictionary<string, object?>? context = null, ErrorSeverity severity = ErrorSeverity.Warning);

	/// <summary>
	/// Gets recent errors for diagnostics.
	/// </summary>
	/// <param name="count">Maximum number of errors to return.</param>
	/// <returns>Collection of recent errors.</returns>
	IEnumerable<TrackingError> GetRecentErrors(int count = 50);

	/// <summary>
	/// Gets error statistics.
	/// </summary>
	/// <returns>Error statistics summary.</returns>
	ErrorStatistics GetErrorStatistics();

	/// <summary>
	/// Clears old error records.
	/// </summary>
	/// <param name="olderThan">Clear errors older than this timespan.</param>
	void ClearOldErrors(TimeSpan olderThan);
}
