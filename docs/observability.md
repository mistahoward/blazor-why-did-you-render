# Observability with .NET Aspire / OpenTelemetry

This guide shows how to enable, verify, and troubleshoot WhyDidYouRender telemetry surfaces (Structured logs, Traces, and Metrics) in the .NET Aspire dashboard.

## Enable

1) Host wiring (recommended)

```csharp
var builder = WebApplication.CreateBuilder(args);

// Registers OpenTelemetry logging, tracing, metrics, OTLP exporter
builder.AddServiceDefaults();
```

2) WhyDidYouRender configuration

```csharp
builder.Services.AddWhyDidYouRender(config =>
{
    config.Enabled = true;
    // Existing settings...

    // Opt-in telemetry
    config.EnableOpenTelemetry = true;
    config.EnableOtelLogs = true;
    config.EnableOtelTraces = true;
    config.EnableOtelMetrics = true;

    // Optional: limit cardinality
    // config.ComponentWhitelist = new(["Counter", "ComplexObjectDemo"]);
});
```

3) Manual OTel wiring (if not using ServiceDefaults)

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource("Blazor.WhyDidYouRender"))
    .WithMetrics(m => m
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter("Blazor.WhyDidYouRender"));

builder.Services.AddOpenTelemetry().UseOtlpExporter();
```

## Verify in Aspire

- Structured logs
  - Resource: your server app
  - Add columns: event, component, method, duration.ms
  - Interact with UI → rows appear

- Traces
  - Search: WhyDidYouRender.Render or wdyrl.
  - Attributes include: wdyrl.component, wdyrl.method, wdyrl.duration.ms, wdyrl.unnecessary, wdyrl.frequent, wdyrl.reason
  - Kebab → "View structured logs" should show correlated entries

- Metrics
  - Search: wdyrl
  - Instruments:
    - wdyrl.renders (Counter)
    - wdyrl.rerenders.unnecessary (Counter)
    - wdyrl.render.duration.ms (Histogram)

## Troubleshooting

- Spans visible, structured logs empty
  - Clear the filter pill on Structured logs (hidden traceId filter)
  - Ensure logs are emitted while Activity.Current is active

- No spans/metrics
  - Confirm EnableOpenTelemetry and EnableOtelTraces/EnableOtelMetrics
  - Ensure AddSource("Blazor.WhyDidYouRender") and AddMeter("Blazor.WhyDidYouRender")
  - Interact with the UI to generate telemetry

- High cardinality
  - Use ComponentWhitelist to constrain emitted component names

## Notes

- Aspire/OTel support is focused on Server/SSR; WASM primarily uses browser console logging.

