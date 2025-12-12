using System.Diagnostics;
using Blazor.WhyDidYouRender.Configuration;

namespace Blazor.WhyDidYouRender.Logging;

/// <summary>
/// Composite logger that forwards calls to multiple IWhyDidYouRenderLogger implementations.
/// Used to emit structured ILogger logs alongside OpenTelemetry spans/metrics.
/// </summary>
public class CompositeWhyDidYouRenderLogger : WhyDidYouRenderLoggerBase
{
	private readonly IWhyDidYouRenderLogger[] _inner;
	private static readonly ActivitySource _activitySource = new("Blazor.WhyDidYouRender");

	/// <summary>
	/// Initializes a new instance of the <see cref="CompositeWhyDidYouRenderLogger"/> class.
	/// </summary>
	/// <param name="config">The WhyDidYouRender configuration.</param>
	/// <param name="inner">The array of inner logger implementations to forward calls to.</param>
	public CompositeWhyDidYouRenderLogger(WhyDidYouRenderConfig config, params IWhyDidYouRenderLogger[] inner)
		: base(config)
	{
		_inner = inner;
	}

	/// <summary>
	/// Logs a debug message by forwarding to all inner loggers.
	/// </summary>
	/// <param name="message">The debug message to log.</param>
	/// <param name="data">Optional structured data to include with the log entry.</param>
	public override void LogDebug(string message, Dictionary<string, object?>? data = null)
	{
		foreach (var l in _inner)
			l.LogDebug(message, data);
	}

	/// <summary>
	/// Logs an informational message by forwarding to all inner loggers.
	/// </summary>
	/// <param name="message">The informational message to log.</param>
	/// <param name="data">Optional structured data to include with the log entry.</param>
	public override void LogInfo(string message, Dictionary<string, object?>? data = null)
	{
		foreach (var l in _inner)
			l.LogInfo(message, data);
	}

	/// <summary>
	/// Logs a warning message by forwarding to all inner loggers.
	/// </summary>
	/// <param name="message">The warning message to log.</param>
	/// <param name="data">Optional structured data to include with the log entry.</param>
	public override void LogWarning(string message, Dictionary<string, object?>? data = null)
	{
		foreach (var l in _inner)
			l.LogWarning(message, data);
	}

	/// <summary>
	/// Logs an error message by forwarding to all inner loggers.
	/// </summary>
	/// <param name="message">The error message to log.</param>
	/// <param name="exception">Optional exception associated with the error.</param>
	/// <param name="data">Optional structured data to include with the log entry.</param>
	public override void LogError(string message, Exception? exception = null, Dictionary<string, object?>? data = null)
	{
		foreach (var l in _inner)
			l.LogError(message, exception, data);
	}

	/// <summary>
	/// Logs a render event by creating an OpenTelemetry activity and forwarding to all inner loggers.
	/// The activity enables correlation in Aspire and adds structured tags for the render event.
	/// </summary>
	/// <param name="renderEvent">The render event to log.</param>
	public override void LogRenderEvent(Records.RenderEvent renderEvent)
	{
		// Create a short-lived activity so Aspire logger can attach tags; this also enables kebab menu correlation
		// Start an Activity with our ActivitySource so Aspire treats it as wdyrl span
		using var activity = _activitySource.StartActivity("WhyDidYouRender.Render");
		if (activity == null)
		{
			foreach (var l in _inner)
				l.LogRenderEvent(renderEvent);
			return;
		}
		try
		{
			// add basic tags so the span is meaningful even if Aspire logger is disabled
			activity.SetTag("wdyrl.event", "render");
			activity.SetTag("wdyrl.component", renderEvent.ComponentName);
			activity.SetTag("wdyrl.method", renderEvent.Method);
			if (renderEvent.DurationMs.HasValue)
				activity.SetTag("wdyrl.duration.ms", renderEvent.DurationMs.Value);
			if (renderEvent.StateHasChangedCallCount > 0)
				activity.SetTag("wdyrl.statehaschanged.call.count", renderEvent.StateHasChangedCallCount);
			activity.SetTag("wdyrl.batched", renderEvent.IsBatchedRender);
			foreach (var l in _inner)
				l.LogRenderEvent(renderEvent);
		}
		finally
		{
			activity.Dispose();
		}
	}

	/// <summary>
	/// Logs parameter changes for a component by forwarding to all inner loggers.
	/// </summary>
	/// <param name="componentName">The name of the component whose parameters changed.</param>
	/// <param name="changes">Dictionary of parameter names and their new values.</param>
	public override void LogParameterChanges(string componentName, Dictionary<string, object?> changes)
	{
		foreach (var l in _inner)
			l.LogParameterChanges(componentName, changes);
	}

	/// <summary>
	/// Logs performance metrics for a component method by forwarding to all inner loggers.
	/// </summary>
	/// <param name="componentName">The name of the component.</param>
	/// <param name="method">The method being measured.</param>
	/// <param name="durationMs">The duration in milliseconds.</param>
	/// <param name="metrics">Optional additional performance metrics.</param>
	public override void LogPerformance(string componentName, string method, double durationMs, Dictionary<string, object?>? metrics = null)
	{
		foreach (var l in _inner)
			l.LogPerformance(componentName, method, durationMs, metrics);
	}

	/// <summary>
	/// Logs a state change in a component by forwarding to all inner loggers.
	/// </summary>
	/// <param name="componentName">The name of the component.</param>
	/// <param name="fieldName">The name of the field that changed.</param>
	/// <param name="previousValue">The previous value of the field.</param>
	/// <param name="currentValue">The current value of the field.</param>
	/// <param name="changeType">The type of change that occurred.</param>
	public override void LogStateChange(
		string componentName,
		string fieldName,
		object? previousValue,
		object? currentValue,
		string changeType
	)
	{
		foreach (var l in _inner)
			l.LogStateChange(componentName, fieldName, previousValue, currentValue, changeType);
	}

	/// <summary>
	/// Logs an unnecessary re-render event by forwarding to all inner loggers.
	/// </summary>
	/// <param name="componentName">The name of the component that re-rendered unnecessarily.</param>
	/// <param name="reason">The reason why the re-render was unnecessary.</param>
	/// <param name="context">Optional additional context about the unnecessary re-render.</param>
	public override void LogUnnecessaryRerender(string componentName, string reason, Dictionary<string, object?>? context = null)
	{
		foreach (var l in _inner)
			l.LogUnnecessaryRerender(componentName, reason, context);
	}

	/// <summary>
	/// Logs a frequent re-render warning by forwarding to all inner loggers.
	/// </summary>
	/// <param name="componentName">The name of the component that is re-rendering frequently.</param>
	/// <param name="renderCount">The number of renders that occurred.</param>
	/// <param name="timeSpan">The time span over which the renders occurred.</param>
	/// <param name="context">Optional additional context about the frequent re-renders.</param>
	public override void LogFrequentRerender(
		string componentName,
		int renderCount,
		TimeSpan timeSpan,
		Dictionary<string, object?>? context = null
	)
	{
		foreach (var l in _inner)
			l.LogFrequentRerender(componentName, renderCount, timeSpan, context);
	}

	/// <summary>
	/// Logs component initialization by forwarding to all inner loggers.
	/// </summary>
	/// <param name="componentName">The name of the component being initialized.</param>
	/// <param name="config">Optional configuration data for the component.</param>
	public override void LogInitialization(string componentName, Dictionary<string, object?>? config = null)
	{
		foreach (var l in _inner)
			l.LogInitialization(componentName, config);
	}

	/// <summary>
	/// Logs component disposal by forwarding to all inner loggers.
	/// </summary>
	/// <param name="componentName">The name of the component being disposed.</param>
	public override void LogDisposal(string componentName)
	{
		foreach (var l in _inner)
			l.LogDisposal(componentName);
	}

	/// <summary>
	/// Logs an exception that occurred in a component by forwarding to all inner loggers.
	/// </summary>
	/// <param name="componentName">The name of the component where the exception occurred.</param>
	/// <param name="exception">The exception that was thrown.</param>
	/// <param name="operation">The operation being performed when the exception occurred.</param>
	/// <param name="context">Optional additional context about the exception.</param>
	public override void LogException(
		string componentName,
		Exception exception,
		string operation,
		Dictionary<string, object?>? context = null
	)
	{
		foreach (var l in _inner)
			l.LogException(componentName, exception, operation, context);
	}
}
