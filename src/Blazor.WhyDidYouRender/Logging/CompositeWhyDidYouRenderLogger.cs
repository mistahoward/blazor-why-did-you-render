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

	public CompositeWhyDidYouRenderLogger(WhyDidYouRenderConfig config, params IWhyDidYouRenderLogger[] inner)
		: base(config)
	{
		_inner = inner;
	}

	public override void LogDebug(string message, Dictionary<string, object?>? data = null)
	{
		foreach (var l in _inner)
			l.LogDebug(message, data);
	}

	public override void LogInfo(string message, Dictionary<string, object?>? data = null)
	{
		foreach (var l in _inner)
			l.LogInfo(message, data);
	}

	public override void LogWarning(string message, Dictionary<string, object?>? data = null)
	{
		foreach (var l in _inner)
			l.LogWarning(message, data);
	}

	public override void LogError(string message, Exception? exception = null, Dictionary<string, object?>? data = null)
	{
		foreach (var l in _inner)
			l.LogError(message, exception, data);
	}

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
			// Add basic tags so the span is meaningful even if Aspire logger is disabled
			activity.SetTag("wdyrl.event", "render");
			activity.SetTag("wdyrl.component", renderEvent.ComponentName);
			activity.SetTag("wdyrl.method", renderEvent.Method);
			if (renderEvent.DurationMs.HasValue)
				activity.SetTag("wdyrl.duration.ms", renderEvent.DurationMs.Value);
			foreach (var l in _inner)
				l.LogRenderEvent(renderEvent);
		}
		finally
		{
			activity.Dispose();
		}
	}

	public override void LogParameterChanges(string componentName, Dictionary<string, object?> changes)
	{
		foreach (var l in _inner)
			l.LogParameterChanges(componentName, changes);
	}

	public override void LogPerformance(string componentName, string method, double durationMs, Dictionary<string, object?>? metrics = null)
	{
		foreach (var l in _inner)
			l.LogPerformance(componentName, method, durationMs, metrics);
	}

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

	public override void LogUnnecessaryRerender(string componentName, string reason, Dictionary<string, object?>? context = null)
	{
		foreach (var l in _inner)
			l.LogUnnecessaryRerender(componentName, reason, context);
	}

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

	public override void LogInitialization(string componentName, Dictionary<string, object?>? config = null)
	{
		foreach (var l in _inner)
			l.LogInitialization(componentName, config);
	}

	public override void LogDisposal(string componentName)
	{
		foreach (var l in _inner)
			l.LogDisposal(componentName);
	}

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
