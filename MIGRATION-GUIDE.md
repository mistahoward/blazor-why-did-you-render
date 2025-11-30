# Migration Guide: v1.x to v2.0+

This guide helps you migrate from WhyDidYouRender v1.x to v2.0+, which introduces cross-platform support for Blazor Server, WebAssembly, and SSR, plus advanced state tracking capabilities.

## Upgrading from 2.x to 3.0

3.0 introduces optional .NET Aspire/OpenTelemetry integration and an internal logging refactor. Defaults are unchanged unless you opt in.

- Breaking changes: None by default
- Opt-in features:
  - EnableOpenTelemetry = true
  - EnableOtelLogs = true, EnableOtelTraces = true, EnableOtelMetrics = true
- Host wiring:
  - Prefer builder.AddServiceDefaults(); or manually add AddSource("Blazor.WhyDidYouRender") and AddMeter("Blazor.WhyDidYouRender") and an OTLP exporter
- What you’ll see in Aspire:
  - Traces: WhyDidYouRender.Render spans with wdyrl.* attributes
  - Metrics: wdyrl.renders, wdyrl.rerenders.unnecessary, wdyrl.render.duration.ms
  - Structured logs: correlated to traces via Activity.Current

### Package Update (3.0)

```xml
<PackageReference Include="Blazor.WhyDidYouRender" Version="3.0.0" />
```

```bash
# Update via CLI
dotnet add package Blazor.WhyDidYouRender --version 3.0.0
```


## WIP: Breaking changes — unified async error tracking and logging

This upcoming release removes legacy diagnostics/logging types and consolidates on the async error tracker and unified logger.

### Removed
- Blazor.WhyDidYouRender.Diagnostics.IErrorTracker (sync)
- Diagnostics/ErrorTracker and Diagnostics/ErrorTrackerAdapter
- Abstractions/ITrackingLogger and Services/ServerTrackingLogger, Services/WasmTrackingLogger

### Use instead
- Abstractions.IErrorTracker (async): TrackErrorAsync, GetRecentErrorsAsync, GetErrorStatisticsAsync, etc.
- Logging.IWhyDidYouRenderLogger (unified logger used by Console/Aspire/Composite variants)

### Code migration examples

Before (legacy sync tracker):
```csharp
using Blazor.WhyDidYouRender.Diagnostics;

void Trigger()
{
    var tracker = Services.GetService<IErrorTracker>();
    tracker?.TrackError("Something went wrong", new Dictionary<string, object?>());
}
```

After (async tracker + await):
```csharp
using Blazor.WhyDidYouRender.Abstractions;
using Blazor.WhyDidYouRender.Records;

async Task Trigger()
{
    var tracker = Services.GetService<IErrorTracker>();
    if (tracker != null)
        await tracker.TrackErrorAsync("Something went wrong", new Dictionary<string, object?>(), ErrorSeverity.Warning, componentName: "Home", operation: "Trigger");
}
```

### DI and initialization
- Keep using builder.Services.AddWhyDidYouRender(...)
- No manual wiring of legacy loggers is needed; the library initializes RenderTrackerService with the async IErrorTracker automatically.
- If you previously referenced ITrackingLogger directly, migrate to Logging.IWhyDidYouRenderLogger.

Notes: This section is a WIP during active development. See CHANGELOG.md (Unreleased) for the latest details.


## 🚨 Breaking Changes Overview

### Major Changes in v2.0

1. **Cross-Platform Architecture**: New service abstraction layer
2. **Initialization Changes**: New initialization methods required
3. **Configuration Updates**: New cross-platform configuration options
4. **State Tracking System**: New field-level change detection capabilities
5. **Removed Features**: Error diagnostics endpoint removed for WASM compatibility
6. **Package Dependencies**: Updated for cross-platform support

## 📦 Package Update

### Update Package Reference

```xml
<!-- Before (v1.x) -->
<PackageReference Include="Blazor.WhyDidYouRender" Version="1.0.1" />

<!-- After (v2.0) -->
<PackageReference Include="Blazor.WhyDidYouRender" Version="2.0.0" />
```

```bash
# Update via CLI
dotnet add package Blazor.WhyDidYouRender --version 2.0.0
```

## 🔧 Service Registration Changes

### Before (v1.x) - Server Only

```csharp
// v1.x - Server only
builder.Services.AddWhyDidYouRender(config =>
{
    config.Enabled = true;
    config.Verbosity = TrackingVerbosity.Normal;
    config.Output = TrackingOutput.Console;
});

var app = builder.Build();

// No explicit initialization required
app.Run();
```

### After (v2.0) - Cross-Platform

```csharp
// v2.0 - Works in all environments
builder.Services.AddWhyDidYouRender(config =>
{
    config.Enabled = true;
    config.Verbosity = TrackingVerbosity.Normal;
    config.Output = TrackingOutput.Both; // New: Both server and browser console
});

var app = builder.Build();

// NEW: Explicit initialization required
await app.Services.InitializeServerAsync(); // For Server/SSR
// OR
// await host.Services.InitializeWasmAsync(jsRuntime); // For WASM

app.Run();
```

## 🚀 Initialization Changes

### v1.x Initialization (Automatic)

```csharp
// v1.x - No explicit initialization needed
// Services were automatically initialized
```

### v2.0 Initialization (Explicit)

**Option 1: Auto-Detection (Recommended)**
```csharp
// In any component after service registration:
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        await ServiceProvider.InitializeAsync(JSRuntime);
    }
}
```

**Option 2: Environment-Specific**
```csharp
// Blazor Server
await app.Services.InitializeServerAsync();

// Blazor WebAssembly
await host.Services.InitializeWasmAsync(jsRuntime);

// SSR
app.Services.InitializeSSRServices();
```

## ⚙️ Configuration Migration


### New Configuration Options

```csharp
// NEW in v2.0+ - Cross-platform options
config.AutoDetectEnvironment = true;                    // Auto-detect hosting model
config.ForceHostingModel = BlazorHostingModel.Server;   // Force specific model
config.Output = TrackingOutput.Both;                    // Both server and browser console

// NEW in v2.1+ - State tracking options
config.EnableStateTracking = true;                      // Enable field-level tracking
config.AutoTrackSimpleTypes = true;                     // Auto-track simple value types
config.MaxTrackedFieldsPerComponent = 50;               // Limit fields per component
config.LogDetailedStateChanges = false;                 // Log before/after values
config.TrackInheritedFields = true;                     // Track inherited fields
config.MaxStateComparisonDepth = 3;                     // Object comparison depth
config.EnableCollectionContentTracking = false;         // Track collection contents
config.StateSnapshotCleanupIntervalMinutes = 10;        // Cleanup interval
config.MaxStateSnapshotAgeMinutes = 30;                 // Max snapshot age
config.MaxTrackedComponents = 1000;                     // Max tracked components
```

### Updated Configuration Properties

```csharp
// v1.x
config.Output = TrackingOutput.Console; // Only server console

// v2.0 - Enhanced options
config.Output = TrackingOutput.Console;        // Server console only
config.Output = TrackingOutput.BrowserConsole; // Browser console only (NEW)
config.Output = TrackingOutput.Both;           // Both consoles (NEW)
```

## 🧩 Component Changes

### Component Tracking (Enhanced in v2.1+)

```csharp
// v1.x - Basic tracking
@using Blazor.WhyDidYouRender.Components
@inherits TrackedComponentBase

<h3>My Component</h3>
<p>Count: @Count</p>

@code {
    [Parameter] public int Count { get; set; }
}
```

```csharp
// v2.1+ - Enhanced with state tracking
@using Blazor.WhyDidYouRender.Components
@using Blazor.WhyDidYouRender.Attributes
@inherits TrackedComponentBase

<h3>My Component</h3>
<p>Count: @Count</p>
<p>User: @user?.Name</p>

@code {
    // Simple types are auto-tracked (no attribute needed)
    private int internalCounter = 0;
    private string message = "Hello World";

    // Complex types need [TrackState] attribute
    [TrackState]
    private UserInfo? user = new() { Name = "John", Email = "john@example.com" };

    // Performance-sensitive fields can be ignored
    [IgnoreState("Changes frequently")]
    private long performanceMetric = 0;

    [Parameter] public int Count { get; set; }
}
```

### New State Tracking Attributes (v2.1+)

#### TrackState Attribute
```csharp
// Basic usage
[TrackState]
private ComplexObject data;

// With options
[TrackState("User profile data", UseCustomComparer = true, MaxComparisonDepth = 2)]
private UserProfile profile;

// Collection tracking
[TrackState(TrackCollectionContents = true)]
private List<string> items;
```

#### IgnoreState Attribute
```csharp
// Basic exclusion
[IgnoreState]
private long performanceCounter;

// With reason
[IgnoreState("Debug information - not relevant for rendering")]
private string debugInfo;
```

#### StateTrackingOptions Attribute
```csharp
// Component-level configuration
@attribute [StateTrackingOptions(
    EnableStateTracking = true,
    MaxFields = 20,
    AutoTrackSimpleTypes = false,
    LogStateChanges = true)]
@inherits TrackedComponentBase
```

## 🔍 API Changes


### New/Updated APIs

```csharp
// v2.0 introduced cross-platform services
IHostingEnvironmentDetector detector;
ISessionContextService sessionService;

IErrorTracker errorTracker;       // updated interface

// v3.0 adds the unified structured logger used by the composite/Aspire path
Blazor.WhyDidYouRender.Logging.IWhyDidYouRenderLogger unifiedLogger;
```

## 📊 Environment-Specific Migration

### Blazor Server Migration

```csharp
// v1.x Server setup
builder.Services.AddWhyDidYouRender();
var app = builder.Build();
app.UseWhyDidYouRenderDiagnostics(); // ❌ Removed
app.Run();

// v2.0 Server setup
builder.Services.AddSession(); // Add if not present
builder.Services.AddWhyDidYouRender(config =>
{
    config.Output = TrackingOutput.Both; // Server + browser console
});
var app = builder.Build();
app.UseSession(); // Add if not present
await app.Services.InitializeServerAsync(); // ✅ Required
app.Run();
```

### New: Blazor WebAssembly Support

```csharp
// NEW in v2.0 - WASM support
var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddWhyDidYouRender(config =>
{
    config.Output = TrackingOutput.BrowserConsole; // Browser only
    config.MaxParameterChangesToLog = 5; // Optimize for WASM
});

var host = builder.Build();
await host.Services.InitializeWasmAsync(
    host.Services.GetRequiredService<IJSRuntime>()
);
await host.RunAsync();
```
### WASM storage removed (v3.0)

- Change: Browser storage (localStorage/sessionStorage) support has been removed. Session IDs are ephemeral (in-memory) only.
- Action: Remove any `config.WasmStorage` usage from your code. No replacement needed.
- For durable correlation, prefer .NET Aspire/OpenTelemetry logs/traces instead of browser storage.


## 🛠️ Step-by-Step Migration

### Step 1: Update Package

```bash
dotnet remove package Blazor.WhyDidYouRender
dotnet add package Blazor.WhyDidYouRender --version 2.0.0
```

### Step 2: Update Service Registration

```csharp
// Update service registration
builder.Services.AddWhyDidYouRender(config =>
{
    // Example: adjust output configuration
    config.Output = TrackingOutput.Both;
});
```

### Step 3: Add Initialization

```csharp
// Add after app.Build()
var app = builder.Build();

// Choose one:
await app.Services.InitializeServerAsync();     // For Server
// OR
await app.Services.InitializeAsync(jsRuntime);  // Auto-detection
```

### Step 4: Remove Diagnostics Middleware

```csharp
// Remove this line:
// app.UseWhyDidYouRenderDiagnostics(); // ❌ No longer available
```

### Step 5: Enable State Tracking (Optional - v2.1+)

```csharp
// Add state tracking configuration
builder.Services.AddWhyDidYouRender(config =>
{
    // Existing configuration...

    // NEW: Enable state tracking
    config.EnableStateTracking = true;
    config.AutoTrackSimpleTypes = true;
    config.MaxTrackedFieldsPerComponent = 50;
    config.LogDetailedStateChanges = false; // Set to true for debugging
});
```

### Step 6: Update Components for State Tracking (Optional)

```csharp
// Add state tracking attributes to existing components
@using Blazor.WhyDidYouRender.Attributes
@inherits TrackedComponentBase

@code {
    // Simple types are auto-tracked (no changes needed)
    private int counter = 0;
    private string message = "Hello";

    // Add [TrackState] to complex objects
    [TrackState]
    private UserData userData = new();

    // Add [IgnoreState] to performance-sensitive fields
    [IgnoreState("Performance counter")]
    private long renderTime = 0;
}
```

### Step 7: Test and Verify

1. Build and run your application
2. Check console output for initialization messages
3. Verify tracking still works as expected
4. Test browser console output (if enabled)
5. Verify state tracking output (if enabled)

## 🚨 Common Migration Issues

### Issue 1: No Tracking Output

**Problem**: No tracking output after migration

**Solution**: Add proper initialization
```csharp
// Add this after app.Build()
await app.Services.InitializeServerAsync();
```

### Issue 2: Browser Console Not Working

**Problem**: Browser console logging not working

**Solution**: Update output configuration and add component initialization
```csharp
// 1. Update configuration
config.Output = TrackingOutput.Both;

// 2. Add component initialization
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        await ServiceProvider.InitializeWhyDidYouRenderAsync(JSRuntime);
    }
}
```

### Issue 3: Session Errors

**Problem**: Session-related errors in Server mode

**Solution**: Add session services and middleware
```csharp
// Add session services
builder.Services.AddSession();

// Add session middleware
app.UseSession();
```

### Issue 4: Compilation Errors

**Problem**: Compilation errors after update

**Solution**: Remove references to removed APIs
```csharp
// Remove these:
// app.UseWhyDidYouRenderDiagnostics();
// config.EnableDiagnosticsEndpoint = true;
// config.DiagnosticsPath = "/diagnostics";
```

### Issue 5: State Tracking Not Working (v2.1+)

**Problem**: State changes not being detected or logged

**Solution**: Verify state tracking configuration
```csharp
// 1. Enable state tracking in configuration
config.EnableStateTracking = true;
config.AutoTrackSimpleTypes = true;

// 2. Add [TrackState] to complex objects
[TrackState]
private UserData userData;

// 3. Ensure component inherits from TrackedComponentBase
@inherits TrackedComponentBase
```

### Issue 6: Performance Issues with State Tracking

**Problem**: Application performance degraded after enabling state tracking

**Solution**: Optimize state tracking settings
```csharp
config.EnableStateTracking = true;
config.MaxTrackedFieldsPerComponent = 25; // Reduce from default 50
config.EnableCollectionContentTracking = false; // Disable expensive collection tracking
config.MaxStateComparisonDepth = 1; // Reduce comparison depth
config.LogDetailedStateChanges = false; // Disable detailed logging

// Use [IgnoreState] on frequently changing fields
[IgnoreState("Performance counter")]
private long performanceMetric;
```

### Issue 7: Too Many State Change Logs

**Problem**: Console flooded with state change messages

**Solution**: Reduce logging verbosity
```csharp
config.LogDetailedStateChanges = false;
config.Verbosity = TrackingVerbosity.Minimal;

// Or use component-level configuration
@attribute [StateTrackingOptions(LogStateChanges = false)]
```

## 📈 Benefits of v2.0

### New Features

1. **Cross-Platform Support**: Works in Server, WASM, and SSR
2. **Browser Console Logging**: Rich browser console output
3. **Automatic Environment Detection**: Adapts to hosting environment
4. **Improved Performance**: Optimized for different environments
5. **Better Error Handling**: Environment-specific error tracking

### Performance Improvements

1. **WASM Optimization**: Reduced payload and optimized for browser
2. **Server Optimization**: Better session management and logging
3. **Memory Management**: Improved cleanup and resource management

## 🔗 Additional Resources

- [Configuration Guide](CONFIGURATION-GUIDE.md) - Comprehensive configuration options
- [API Documentation](API-DOCUMENTATION.md) - Complete API reference
- [Examples and Best Practices](EXAMPLES-AND-BEST-PRACTICES.md) - Usage examples

## 💬 Support

If you encounter issues during migration:

1. Check the [troubleshooting section](CONFIGURATION-GUIDE.md#troubleshooting) in the Configuration Guide
2. Review the [examples](EXAMPLES-AND-BEST-PRACTICES.md) for your specific scenario
3. Ensure you're using the correct initialization method for your environment

## 📝 Summary

v2.0 is a major release that adds cross-platform support while maintaining the same core functionality. The main changes are:

- ✅ **Add explicit initialization** after service registration
- ✅ **Remove diagnostics endpoint** configuration and middleware
- ✅ **Update output configuration** to use new options
- ✅ **Add session support** for Server environments (if not present)

The migration is straightforward and maintains backward compatibility for component usage.
