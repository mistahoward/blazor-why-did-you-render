using Microsoft.JSInterop;
using Blazor.WhyDidYouRender.Configuration;

namespace Blazor.WhyDidYouRender.Logging;

/// <summary>
/// WebAssembly logger implementation that writes to the browser console via JS interop.
/// </summary>
public class WasmWhyDidYouRenderLogger : WhyDidYouRenderLoggerBase {
    private readonly IJSRuntime _js;

    /// <summary>
    /// Initializes a new instance of the <see cref="WasmWhyDidYouRenderLogger"/> class.
    /// </summary>
    /// <param name="config">The WhyDidYouRender configuration.</param>
    /// <param name="js">The JS runtime for console logging.</param>
    public WasmWhyDidYouRenderLogger(WhyDidYouRenderConfig config, IJSRuntime js)
    : base(config) {
        _js = js;
        _currentLogLevel = config.MinimumLogLevel;
    }


    /// <summary>Writes a debug message to the browser console.</summary>

    public override void LogDebug(string message, Dictionary<string, object?>? data = null) {
        if (!IsEnabled(LogLevel.Debug)) return;
        _ = _js.InvokeVoidAsync("console.debug", Format(message, "Debug"), BuildData(data));
    }

    /// <summary>Writes an informational message to the browser console.</summary>
    /// <inheritdoc />
    public override void LogInfo(string message, Dictionary<string, object?>? data = null) {
        if (!IsEnabled(LogLevel.Info)) return;
        _ = _js.InvokeVoidAsync("console.log", Format(message, "Info"), BuildData(data));
    }

    /// <summary>Writes a warning message to the browser console.</summary>

    /// <summary>Writes a warning message to the browser console.</summary>
    /// <inheritdoc />
    public override void LogWarning(string message, Dictionary<string, object?>? data = null) {
        if (!IsEnabled(LogLevel.Warning)) return;
        _ = _js.InvokeVoidAsync("console.warn", Format(message, "Warning"), BuildData(data));
    }

    /// <summary>Writes an error message to the browser console.</summary>
    /// <inheritdoc />
    public override void LogError(string message, Exception? exception = null, Dictionary<string, object?>? data = null) {
        if (!IsEnabled(LogLevel.Error)) return;
        var d = BuildData(data);
        if (exception != null) {
            d["exceptionType"] = exception.GetType().Name;
            d["exceptionMessage"] = exception.Message;
        }
        _ = _js.InvokeVoidAsync("console.error", Format(message, "Error"), d);
    }

    private Dictionary<string, object?> BuildData(Dictionary<string, object?>? additional) {
        var d = new Dictionary<string, object?>();
        if (_config.EnableCorrelationIds && !string.IsNullOrEmpty(_correlationId))
            d["correlationId"] = _correlationId;
        if (additional != null) {
            foreach (var kv in additional)
                d[kv.Key] = kv.Value;
        }
        return d;
    }

    /// <summary>
    /// Formats a message with WhyDidYouRender prefix and correlation id.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="level">The level text.</param>
    /// <returns>The formatted message.</returns>
    private string Format(string message, string level) {
        var prefix = $"[WhyDidYouRender] [{level}]";
        if (!string.IsNullOrEmpty(_correlationId)) prefix += $" [{_correlationId}]";
        return $"{prefix} {message}";
    }
}

