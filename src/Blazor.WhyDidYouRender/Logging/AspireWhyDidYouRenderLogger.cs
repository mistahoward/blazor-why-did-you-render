using System.Diagnostics;
using System.Diagnostics.Metrics;

using Blazor.WhyDidYouRender.Configuration;

namespace Blazor.WhyDidYouRender.Logging;

/// <summary>
/// Logger implementation integrating with .NET Aspire/OpenTelemetry via ActivitySource and Meter.
/// </summary>
public class AspireWhyDidYouRenderLogger : WhyDidYouRenderLoggerBase {
    private readonly ActivitySource _activitySource = new("Blazor.WhyDidYouRender");
    private readonly Meter _meter = new("Blazor.WhyDidYouRender");
    private readonly Counter<long> _renderCounter;
    private readonly Counter<long> _unnecessaryCounter;
    private readonly Histogram<double> _renderDurationMs;

    /// <summary>
    /// Initializes a new instance of the <see cref="AspireWhyDidYouRenderLogger"/> class.
    /// </summary>
    /// <param name="config">The WhyDidYouRender configuration.</param>
    public AspireWhyDidYouRenderLogger(WhyDidYouRenderConfig config) : base(config) {
        _currentLogLevel = config.MinimumLogLevel;
        _renderCounter = _meter.CreateCounter<long>("wdyrl.renders", unit: "count", description: "Total renders");
        _unnecessaryCounter = _meter.CreateCounter<long>("wdyrl.rerenders.unnecessary", unit: "count", description: "Unnecessary rerenders");
        _renderDurationMs = _meter.CreateHistogram<double>("wdyrl.render.duration.ms", unit: "ms", description: "Render duration in milliseconds");
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
    /// WDYR-specific render event: emits a span and updates metrics.
    /// </summary>
    public override void LogRenderEvent(Records.RenderEvent renderEvent) {
        if (!IsEnabled(LogLevel.Info)) return;

        if (_config.ComponentWhitelist != null && _config.ComponentWhitelist.Count > 0 && !_config.ComponentWhitelist.Contains(renderEvent.ComponentName))
            return;

        Activity? activity = null;
        var createdNew = false;
        if (_config.EnableOtelTraces) {
            if (Activity.Current is Activity current) {
                activity = current; // reuse ambient activity if present (e.g., created by composite logger)
            }
            else {
                activity = _activitySource.StartActivity("WhyDidYouRender.Render");
                createdNew = activity != null;
            }
        }
        if (activity != null) {
            activity.SetTag("wdyrl.event", "render");
            activity.SetTag("wdyrl.component", renderEvent.ComponentName);
            activity.SetTag("wdyrl.component.type", renderEvent.ComponentType);
            activity.SetTag("wdyrl.method", renderEvent.Method);
            if (renderEvent.FirstRender.HasValue) activity.SetTag("wdyrl.first_render", renderEvent.FirstRender.Value);
            if (renderEvent.DurationMs.HasValue) activity.SetTag("wdyrl.duration.ms", renderEvent.DurationMs.Value);
            activity.SetTag("wdyrl.unnecessary", renderEvent.IsUnnecessaryRerender);
            activity.SetTag("wdyrl.frequent", renderEvent.IsFrequentRerender);
            if (!string.IsNullOrEmpty(renderEvent.UnnecessaryRerenderReason)) activity.SetTag("wdyrl.reason", renderEvent.UnnecessaryRerenderReason);
            if (!string.IsNullOrEmpty(_correlationId)) activity.SetTag("wdyrl.correlationId", _correlationId);
            if (renderEvent.StateChangeCount > 0) activity.SetTag("wdyrl.state.change.count", renderEvent.StateChangeCount);
        }
        if (createdNew) activity?.Dispose();

        if (_config.EnableOtelMetrics) {
            var tags = new TagList {
                { "component", renderEvent.ComponentName },
                { "method", renderEvent.Method },
                { "unnecessary", renderEvent.IsUnnecessaryRerender },
                { "frequent", renderEvent.IsFrequentRerender }
            };
            _renderCounter.Add(1, tags);
            if (renderEvent.IsUnnecessaryRerender) {
                if (!string.IsNullOrEmpty(renderEvent.UnnecessaryRerenderReason)) tags.Add("reason", renderEvent.UnnecessaryRerenderReason);
                _unnecessaryCounter.Add(1, tags);
            }
            if (renderEvent.DurationMs.HasValue)
                _renderDurationMs.Record(renderEvent.DurationMs.Value, tags);
        }
    }

    /// <summary>
    /// Writes an activity with structured data and optional exception metadata.
    /// </summary>
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
