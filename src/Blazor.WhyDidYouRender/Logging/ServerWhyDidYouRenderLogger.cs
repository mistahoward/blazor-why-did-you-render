using Blazor.WhyDidYouRender.Configuration;
using Microsoft.Extensions.Logging;

namespace Blazor.WhyDidYouRender.Logging;

/// <summary>
/// Server-side logger implementation using Microsoft.Extensions.Logging.
/// </summary>
public class ServerWhyDidYouRenderLogger : WhyDidYouRenderLoggerBase
{
	private readonly ILogger<ServerWhyDidYouRenderLogger> _logger;

	/// <summary>
	/// Initializes a new instance of the <see cref="ServerWhyDidYouRenderLogger"/> class.
	/// </summary>
	/// <param name="config">The WhyDidYouRender configuration.</param>
	/// <param name="logger">The server ILogger instance.</param>
	public ServerWhyDidYouRenderLogger(WhyDidYouRenderConfig config, ILogger<ServerWhyDidYouRenderLogger> logger)
		: base(config)
	{
		_logger = logger;
		_currentLogLevel = config.MinimumLogLevel;
	}

	/// <summary>Writes a debug message using the server logger.</summary>
	public override void LogDebug(string message, Dictionary<string, object?>? data = null)
	{
		if (!IsEnabled(LogLevel.Debug))
			return;
		var dict = BuildData(data);
		if (dict.Count > 0)
		{
			using var scope = _logger.BeginScope(dict);
			_logger.LogDebug("[WhyDidYouRender] {Message}", message);
		}
		else
		{
			_logger.LogDebug("[WhyDidYouRender] {Message}", message);
		}
	}

	/// <summary>Writes an informational message using the server logger.</summary>
	/// <inheritdoc />
	public override void LogInfo(string message, Dictionary<string, object?>? data = null)
	{
		if (!IsEnabled(LogLevel.Info))
			return;
		var dict = BuildData(data);
		if (dict.Count > 0)
		{
			using var scope = _logger.BeginScope(dict);
			_logger.LogInformation("[WhyDidYouRender] {Message}", message);
		}
		else
		{
			_logger.LogInformation("[WhyDidYouRender] {Message}", message);
		}
	}

	/// <summary>Writes a warning message using the server logger.</summary>
	/// <inheritdoc />
	public override void LogWarning(string message, Dictionary<string, object?>? data = null)
	{
		if (!IsEnabled(LogLevel.Warning))
			return;
		var dict = BuildData(data);
		if (dict.Count > 0)
		{
			using var scope = _logger.BeginScope(dict);
			_logger.LogWarning("[WhyDidYouRender] {Message}", message);
		}
		else
		{
			_logger.LogWarning("[WhyDidYouRender] {Message}", message);
		}
	}

	/// <summary>Writes an error message using the server logger.</summary>
	/// <inheritdoc />
	public override void LogError(string message, Exception? exception = null, Dictionary<string, object?>? data = null)
	{
		if (!IsEnabled(LogLevel.Error))
			return;
		var dict = BuildData(data);
		if (dict.Count > 0)
		{
			using var scope = _logger.BeginScope(dict);
			if (exception is null)
				_logger.LogError("[WhyDidYouRender] {Message}", message);
			else
				_logger.LogError(exception, "[WhyDidYouRender] {Message}", message);
		}
		else
		{
			if (exception is null)
				_logger.LogError("[WhyDidYouRender] {Message}", message);
			else
				_logger.LogError(exception, "[WhyDidYouRender] {Message}", message);
		}
	}

	/// <summary>
	/// Builds a structured data dictionary including correlation info.
	/// </summary>
	/// <param name="additional">Optional additional properties.</param>
	/// <returns>A structured data dictionary.</returns>
	protected Dictionary<string, object?> BuildData(Dictionary<string, object?>? additional = null)
	{
		var dict = new Dictionary<string, object?>();
		if (_config.EnableCorrelationIds && !string.IsNullOrEmpty(_correlationId))
			dict["wdyrl.correlationId"] = _correlationId;
		if (additional != null)
		{
			foreach (var kv in additional)
				dict[kv.Key.StartsWith("wdyrl.") ? kv.Key : $"wdyrl.{kv.Key}"] = kv.Value;
		}
		return dict;
	}
}
