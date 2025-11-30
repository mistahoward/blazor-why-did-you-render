using Blazor.WhyDidYouRender.Configuration;

namespace Blazor.WhyDidYouRender.Logging;

/// <summary>
/// Base class providing shared behavior for WhyDidYouRender logger implementations.
/// </summary>
public abstract class WhyDidYouRenderLoggerBase : IWhyDidYouRenderLogger
{
	/// <summary>
	/// The WhyDidYouRender configuration.
	/// </summary>
	protected readonly WhyDidYouRenderConfig _config;

	/// <summary>
	/// The current correlation identifier.
	/// </summary>
	protected string? _correlationId;

	/// <summary>
	/// The current minimum log level.
	/// </summary>
	protected LogLevel _currentLogLevel;

	/// <summary>
	/// Initializes a new instance of the <see cref="WhyDidYouRenderLoggerBase"/> class.
	/// </summary>
	/// <param name="config">The logger configuration.</param>
	protected WhyDidYouRenderLoggerBase(WhyDidYouRenderConfig config)
	{
		_config = config ?? throw new ArgumentNullException(nameof(config));
		_currentLogLevel = LogLevel.Info;
	}

	/// <summary>
	/// Basic logging methods implemented by derived types.
	/// </summary>
	/// <inheritdoc />
	public abstract void LogDebug(string message, Dictionary<string, object?>? data = null);

	/// <inheritdoc />
	public abstract void LogInfo(string message, Dictionary<string, object?>? data = null);

	/// <inheritdoc />
	public abstract void LogWarning(string message, Dictionary<string, object?>? data = null);

	/// <inheritdoc />
	public abstract void LogError(string message, Exception? exception = null, Dictionary<string, object?>? data = null);

	/// <summary>
	/// Structured logging helpers with sensible defaults.
	/// </summary>
	/// <inheritdoc />
	public virtual void LogRenderEvent(Records.RenderEvent renderEvent)
	{
		var data = new Dictionary<string, object?>
		{
			["event"] = "render",
			["component"] = renderEvent.ComponentName,
			["method"] = renderEvent.Method,
			["timestamp"] = renderEvent.Timestamp,
			["duration.ms"] = renderEvent.DurationMs,
			["unnecessary"] = renderEvent.IsUnnecessaryRerender,
			["frequent"] = renderEvent.IsFrequentRerender,
		};

		if (renderEvent.IsUnnecessaryRerender)
			data["reason"] = renderEvent.UnnecessaryRerenderReason;

		LogInfo($"Render event: {renderEvent.ComponentName}.{renderEvent.Method}()", data);
	}

	/// <inheritdoc />
	public virtual void LogParameterChanges(string componentName, Dictionary<string, object?> changes)
	{
		var data = new Dictionary<string, object?>
		{
			["event"] = "param-change",
			["component"] = componentName,
			["parameterChanges"] = changes,
		};
		LogInfo($"Parameter changes for {componentName}", data);
	}

	/// <inheritdoc />
	public virtual void LogPerformance(string componentName, string method, double durationMs, Dictionary<string, object?>? metrics = null)
	{
		var data = new Dictionary<string, object?>
		{
			["event"] = "performance",
			["component"] = componentName,
			["method"] = method,
			["duration.ms"] = durationMs,
		};

		if (metrics != null)
		{
			foreach (var kvp in metrics)
				data[kvp.Key] = kvp.Value;
		}

		LogInfo($"Performance: {componentName}.{method}() took {durationMs:F2}ms", data);
	}

	/// <inheritdoc />
	public virtual void LogStateChange(
		string componentName,
		string fieldName,
		object? previousValue,
		object? currentValue,
		string changeType
	)
	{
		var data = new Dictionary<string, object?>
		{
			["event"] = "state-change",
			["component"] = componentName,
			["field"] = fieldName,
			["previous"] = previousValue,
			["current"] = currentValue,
			["changeType"] = changeType,
		};
		LogInfo($"State change: {componentName}.{fieldName}", data);
	}

	/// <inheritdoc />
	public virtual void LogUnnecessaryRerender(string componentName, string reason, Dictionary<string, object?>? context = null)
	{
		var data = new Dictionary<string, object?>
		{
			["event"] = "unnecessary-rerender",
			["component"] = componentName,
			["reason"] = reason,
		};
		if (context != null)
		{
			foreach (var kvp in context)
				data[kvp.Key] = kvp.Value;
		}
		LogWarning($"Unnecessary rerender: {componentName} - {reason}", data);
	}

	/// <inheritdoc />
	public virtual void LogFrequentRerender(
		string componentName,
		int renderCount,
		TimeSpan timeSpan,
		Dictionary<string, object?>? context = null
	)
	{
		var data = new Dictionary<string, object?>
		{
			["event"] = "frequent-rerender",
			["component"] = componentName,
			["renderCount"] = renderCount,
			["timeSpan.ms"] = timeSpan.TotalMilliseconds,
			["rendersPerSecond"] = renderCount / timeSpan.TotalSeconds,
		};
		if (context != null)
		{
			foreach (var kvp in context)
				data[kvp.Key] = kvp.Value;
		}
		LogWarning($"Frequent rerender: {componentName} - {renderCount} renders in {timeSpan.TotalSeconds:F1}s", data);
	}

	/// <inheritdoc />
	public virtual void LogInitialization(string componentName, Dictionary<string, object?>? config = null)
	{
		var data = new Dictionary<string, object?> { ["componentName"] = componentName };
		if (config != null)
		{
			foreach (var kvp in config)
				data[kvp.Key] = kvp.Value;
		}
		LogInfo($"Component initialized: {componentName}", data);
	}

	/// <inheritdoc />
	public virtual void LogDisposal(string componentName)
	{
		LogInfo($"Component disposed: {componentName}", new Dictionary<string, object?> { ["componentName"] = componentName });
	}

	/// <inheritdoc />
	public virtual void LogException(
		string componentName,
		Exception exception,
		string operation,
		Dictionary<string, object?>? context = null
	)
	{
		var data = new Dictionary<string, object?>
		{
			["componentName"] = componentName,
			["operation"] = operation,
			["exceptionType"] = exception.GetType().Name,
			["exceptionMessage"] = exception.Message,
		};
		if (context != null)
		{
			foreach (var kvp in context)
				data[kvp.Key] = kvp.Value;
		}
		LogError($"Exception in {componentName} during {operation}", exception, data);
	}

	/// <summary>
	/// Correlation ID support for linking related log entries.
	/// </summary>
	/// <inheritdoc />
	public virtual void SetCorrelationId(string correlationId) => _correlationId = correlationId;

	/// <inheritdoc />
	public virtual string? GetCorrelationId() => _correlationId;

	/// <inheritdoc />
	public virtual void ClearCorrelationId() => _correlationId = null;

	/// <summary>
	/// Logging configuration helpers.
	/// </summary>
	/// <inheritdoc />
	public virtual bool IsEnabled(LogLevel level) => level.IsEnabled(_currentLogLevel);

	/// <inheritdoc />
	public virtual void SetLogLevel(LogLevel level) => _currentLogLevel = level;

	/// <inheritdoc />
	public virtual LogLevel GetLogLevel() => _currentLogLevel;
}
