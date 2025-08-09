using System.Diagnostics;

using Blazor.WhyDidYouRender.Configuration;

namespace Blazor.WhyDidYouRender.Logging;

/// <summary>
/// Logger implementation integrating with .NET Aspire/OpenTelemetry via ActivitySource.
/// </summary>
public class AspireWhyDidYouRenderLogger : WhyDidYouRenderLoggerBase {
    private readonly ActivitySource _activitySource = new("Blazor.WhyDidYouRender");

    /// <summary>
    /// Initializes a new instance of the <see cref="AspireWhyDidYouRenderLogger"/> class.
    /// </summary>
    /// <param name="config">The WhyDidYouRender configuration.</param>
    public AspireWhyDidYouRenderLogger(WhyDidYouRenderConfig config) : base(config) {
        _currentLogLevel = config.MinimumLogLevel;
    }

    /// <summary>Writes a debug message as an Aspire/OpenTelemetry activity.</summary>

    public override void LogDebug(string message, Dictionary<string, object?>? data = null)
        => WriteActivity("Debug", message, data);

    /// <summary>Writes an informational message as an Aspire/OpenTelemetry activity.</summary>

    public override void LogInfo(string message, Dictionary<string, object?>? data = null)
        => WriteActivity("Info", message, data);

    /// <summary>Writes a warning message as an Aspire/OpenTelemetry activity.</summary>

    public override void LogWarning(string message, Dictionary<string, object?>? data = null)
        => WriteActivity("Warning", message, data);

    /// <summary>Writes an error message as an Aspire/OpenTelemetry activity.</summary>

    public override void LogError(string message, Exception? exception = null, Dictionary<string, object?>? data = null)
        => WriteActivity("Error", message, data, exception);

    /// <summary>
    /// Writes an activity with structured data and optional exception metadata.
    /// </summary>
    /// <param name="level">The level name.</param>
    /// <param name="message">The log message.</param>
    /// <param name="data">Optional structured data.</param>
    /// <param name="ex">Optional exception.</param>
    private void WriteActivity(string level, string message, Dictionary<string, object?>? data, Exception? ex = null) {
        if (!IsEnabled(level switch { "Debug" => LogLevel.Debug, "Info" => LogLevel.Info, "Warning" => LogLevel.Warning, _ => LogLevel.Error }))
            return;

        using var activity = _activitySource.StartActivity($"WhyDidYouRender.{level}");
        if (activity == null) return;

        activity.SetTag("wdyrl.message", message);
        if (!string.IsNullOrEmpty(_correlationId))
            activity.SetTag("wdyrl.correlationId", _correlationId);
        if (data != null) {
            foreach (var kv in data)
                activity.SetTag($"wdyrl.{kv.Key}", kv.Value);
        }
        if (ex != null) {
            activity.SetTag("wdyrl.exception.type", ex.GetType().Name);
            activity.SetTag("wdyrl.exception.message", ex.Message);
        }
    }
}

