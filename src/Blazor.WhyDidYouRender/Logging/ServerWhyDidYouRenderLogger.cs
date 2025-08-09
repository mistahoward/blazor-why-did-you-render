using Microsoft.Extensions.Logging;
using Blazor.WhyDidYouRender.Configuration;

namespace Blazor.WhyDidYouRender.Logging;

/// <summary>
/// Server-side logger implementation using Microsoft.Extensions.Logging.
/// </summary>
public class ServerWhyDidYouRenderLogger : WhyDidYouRenderLoggerBase {
    private readonly ILogger<ServerWhyDidYouRenderLogger> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServerWhyDidYouRenderLogger"/> class.
    /// </summary>
    /// <param name="config">The WhyDidYouRender configuration.</param>
    /// <param name="logger">The server ILogger instance.</param>
    public ServerWhyDidYouRenderLogger(WhyDidYouRenderConfig config, ILogger<ServerWhyDidYouRenderLogger> logger)
        : base(config) {
        _logger = logger;
        _currentLogLevel = config.MinimumLogLevel;
    }

    /// <summary>Writes a debug message using the server logger.</summary>

    public override void LogDebug(string message, Dictionary<string, object?>? data = null) {
        if (!IsEnabled(LogLevel.Debug)) return;
        _logger.LogDebug("[WhyDidYouRender] {Message} {@Data}", message, BuildData(data));
    }

    /// <summary>Writes an informational message using the server logger.</summary>
    /// <inheritdoc />
    public override void LogInfo(string message, Dictionary<string, object?>? data = null) {
        if (!IsEnabled(LogLevel.Info)) return;
        _logger.LogInformation("[WhyDidYouRender] {Message} {@Data}", message, BuildData(data));
    }

    /// <summary>Writes a warning message using the server logger.</summary>
    /// <inheritdoc />
    public override void LogWarning(string message, Dictionary<string, object?>? data = null) {
        if (!IsEnabled(LogLevel.Warning)) return;
        _logger.LogWarning("[WhyDidYouRender] {Message} {@Data}", message, BuildData(data));
    }

    /// <summary>Writes an error message using the server logger.</summary>
    /// <inheritdoc />
    public override void LogError(string message, Exception? exception = null, Dictionary<string, object?>? data = null) {
        if (!IsEnabled(LogLevel.Error)) return;
        if (exception is null)
            _logger.LogError("[WhyDidYouRender] {Message} {@Data}", message, BuildData(data));
        else
            _logger.LogError(exception, "[WhyDidYouRender] {Message} {@Data}", message, BuildData(data));
    }

    /// <summary>
    /// Builds a structured data dictionary including correlation info.
    /// </summary>
    /// <param name="additional">Optional additional properties.</param>
    /// <returns>A structured data dictionary.</returns>
    protected Dictionary<string, object?> BuildData(Dictionary<string, object?>? additional = null) {
        var dict = new Dictionary<string, object?>();
        if (_config.EnableCorrelationIds && !string.IsNullOrEmpty(_correlationId))
            dict["correlationId"] = _correlationId;
        if (additional != null) {
            foreach (var kv in additional)
                dict[kv.Key] = kv.Value;
        }
        return dict;
    }
}

