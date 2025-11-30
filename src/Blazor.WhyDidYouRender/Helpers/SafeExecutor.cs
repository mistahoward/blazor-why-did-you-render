using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

using Blazor.WhyDidYouRender.Abstractions;
using Blazor.WhyDidYouRender.Records;

namespace Blazor.WhyDidYouRender.Helpers;

/// <summary>
/// Provides safe execution of tracking operations with comprehensive error handling.
/// </summary>
public static class SafeExecutor {
	/// <summary>
	/// Executes an action safely, catching and logging any exceptions.
	/// </summary>
	/// <param name="action">The action to execute.</param>
	/// <param name="errorTracker">The error tracker for logging errors.</param>
	/// <param name="context">Additional context for error reporting.</param>
	/// <param name="componentName">The name of the component being tracked.</param>
	/// <param name="method">The tracking method being executed.</param>
	/// <returns>True if the action executed successfully; otherwise, false.</returns>
	public static bool Execute(
		Action action,
		IErrorTracker? errorTracker,
		Dictionary<string, object?>? context = null,
		string? componentName = null,
		string? method = null) {

		try {
			action();
			return true;
		}
		catch (Exception ex) {
			HandleError(ex, errorTracker, context, componentName, method);
			return false;
		}
	}

	/// <summary>
	/// Executes a function safely, catching and logging any exceptions.
	/// </summary>
	/// <typeparam name="T">The return type of the function.</typeparam>
	/// <param name="func">The function to execute.</param>
	/// <param name="defaultValue">The default value to return if an error occurs.</param>
	/// <param name="errorTracker">The error tracker for logging errors.</param>
	/// <param name="context">Additional context for error reporting.</param>
	/// <param name="componentName">The name of the component being tracked.</param>
	/// <param name="method">The tracking method being executed.</param>
	/// <returns>The result of the function, or the default value if an error occurred.</returns>
	public static T Execute<T>(
		Func<T> func,
		T defaultValue,
		IErrorTracker? errorTracker,
		Dictionary<string, object?>? context = null,
		string? componentName = null,
		string? method = null) {

		try {
			return func();
		}
		catch (Exception ex) {
			HandleError(ex, errorTracker, context, componentName, method);
			return defaultValue;
		}
	}

	/// <summary>
	/// Executes an async action safely, catching and logging any exceptions.
	/// </summary>
	/// <param name="action">The async action to execute.</param>
	/// <param name="errorTracker">The error tracker for logging errors.</param>
	/// <param name="context">Additional context for error reporting.</param>
	/// <param name="componentName">The name of the component being tracked.</param>
	/// <param name="method">The tracking method being executed.</param>
	/// <returns>True if the action executed successfully; otherwise, false.</returns>
	public static async Task<bool> ExecuteAsync(
		Func<Task> action,
		IErrorTracker? errorTracker,
		Dictionary<string, object?>? context = null,
		string? componentName = null,
		string? method = null) {

		try {
			await action();
			return true;
		}
		catch (Exception ex) {
			HandleError(ex, errorTracker, context, componentName, method);
			return false;
		}
	}

	/// <summary>
	/// Executes an async function safely, catching and logging any exceptions.
	/// </summary>
	/// <typeparam name="T">The return type of the function.</typeparam>
	/// <param name="func">The async function to execute.</param>
	/// <param name="defaultValue">The default value to return if an error occurs.</param>
	/// <param name="errorTracker">The error tracker for logging errors.</param>
	/// <param name="context">Additional context for error reporting.</param>
	/// <param name="componentName">The name of the component being tracked.</param>
	/// <param name="method">The tracking method being executed.</param>
	/// <returns>The result of the function, or the default value if an error occurred.</returns>
	public static async Task<T> ExecuteAsync<T>(
		Func<Task<T>> func,
		T defaultValue,
		IErrorTracker? errorTracker,
		Dictionary<string, object?>? context = null,
		string? componentName = null,
		string? method = null) {

		try {
			return await func();
		}
		catch (Exception ex) {
			HandleError(ex, errorTracker, context, componentName, method);
			return defaultValue;
		}
	}

	/// <summary>
	/// Executes a tracking operation with component context.
	/// </summary>
	/// <param name="component">The component being tracked.</param>
	/// <param name="trackingMethod">The tracking method name.</param>
	/// <param name="action">The tracking action to execute.</param>
	/// <param name="errorTracker">The error tracker for logging errors.</param>
	/// <returns>True if the tracking executed successfully; otherwise, false.</returns>
	public static bool ExecuteTracking(
		ComponentBase component,
		string trackingMethod,
		Action action,
		IErrorTracker? errorTracker) {

		var componentName = component?.GetType().Name ?? "Unknown";
		var context = new Dictionary<string, object?> {
			["ComponentType"] = component?.GetType().FullName,
			["TrackingMethod"] = trackingMethod
		};

		return Execute(action, errorTracker, context, componentName, trackingMethod);
	}

	/// <summary>
	/// Executes a tracking operation with component context and return value.
	/// </summary>
	/// <typeparam name="T">The return type.</typeparam>
	/// <param name="component">The component being tracked.</param>
	/// <param name="trackingMethod">The tracking method name.</param>
	/// <param name="func">The tracking function to execute.</param>
	/// <param name="defaultValue">The default value to return if an error occurs.</param>
	/// <param name="errorTracker">The error tracker for logging errors.</param>
	/// <returns>The result of the function, or the default value if an error occurred.</returns>
	public static T ExecuteTracking<T>(
		ComponentBase component,
		string trackingMethod,
		Func<T> func,
		T defaultValue,
		IErrorTracker? errorTracker) {

		var componentName = component?.GetType().Name ?? "Unknown";
		var context = new Dictionary<string, object?> {
			["ComponentType"] = component?.GetType().FullName,
			["TrackingMethod"] = trackingMethod
		};

		return Execute(func, defaultValue, errorTracker, context, componentName, trackingMethod);
	}

	/// <summary>
	/// Handles errors that occur during tracking operations.
	/// </summary>
	/// <param name="exception">The exception that occurred.</param>
	/// <param name="errorTracker">The error tracker for logging errors.</param>
	/// <param name="context">Additional context for error reporting.</param>
	/// <param name="componentName">The name of the component being tracked.</param>
	/// <param name="method">The tracking method being executed.</param>
	private static void HandleError(
		Exception exception,
		IErrorTracker? errorTracker,
		Dictionary<string, object?>? context,
		string? componentName,
		string? method) {

		var errorContext = context ?? new Dictionary<string, object?>();
		if (!string.IsNullOrEmpty(componentName)) {
			errorContext["ComponentName"] = componentName;
		}
		if (!string.IsNullOrEmpty(method)) {
			errorContext["TrackingMethod"] = method;
		}

		var severity = DetermineSeverity(exception);

		if (errorTracker != null) {
			_ = errorTracker.TrackErrorAsync(exception, errorContext, severity, componentName, method);
		}
		else {
			Console.WriteLine($"[WhyDidYouRender] Error in {method ?? "tracking"}: {exception.Message}");
		}
	}

	/// <summary>
	/// Determines the severity of an exception for error tracking.
	/// </summary>
	/// <param name="exception">The exception to evaluate.</param>
	/// <returns>The appropriate error severity level.</returns>
	private static ErrorSeverity DetermineSeverity(Exception exception) {
		return exception switch {
			ArgumentNullException => ErrorSeverity.Warning,
			ArgumentException => ErrorSeverity.Warning,
			InvalidOperationException => ErrorSeverity.Warning,
			NotSupportedException => ErrorSeverity.Info,
			TimeoutException => ErrorSeverity.Warning,
			UnauthorizedAccessException => ErrorSeverity.Error,
			OutOfMemoryException => ErrorSeverity.Critical,
			StackOverflowException => ErrorSeverity.Critical,
			_ => ErrorSeverity.Error
		};
	}

	/// <summary>
	/// Creates a recovery action that attempts to restore normal operation.
	/// </summary>
	/// <param name="recoveryAction">The recovery action to execute.</param>
	/// <param name="errorTracker">The error tracker for logging recovery attempts.</param>
	/// <param name="context">Additional context for recovery reporting.</param>
	/// <returns>True if recovery was successful; otherwise, false.</returns>
	public static bool AttemptRecovery(
		Action recoveryAction,
		IErrorTracker? errorTracker,
		Dictionary<string, object?>? context = null) {

		try {
			recoveryAction();

			if (errorTracker != null) {
				_ = errorTracker.TrackErrorAsync(
					"Recovery action completed successfully",
					context ?? new Dictionary<string, object?>(),
					ErrorSeverity.Info,
					null,
					"AttemptRecovery");
			}

			return true;
		}
		catch (Exception ex) {
			if (errorTracker != null) {
				_ = errorTracker.TrackErrorAsync(
					ex,
					context ?? new Dictionary<string, object?>(),
					ErrorSeverity.Error,
					null,
					"AttemptRecovery");
			}

			return false;
		}
	}
}
