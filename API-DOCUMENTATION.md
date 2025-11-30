# Blazor WhyDidYouRender - API Documentation v3.0

This document provides comprehensive API documentation for all public classes, interfaces, and methods in the **cross-platform** Blazor WhyDidYouRender library.

## 🌐 Cross-Platform Architecture

WhyDidYouRender v3.0 builds on the cross-platform architecture and adds optional .NET Aspire/OpenTelemetry integration:

- **🖥️ Blazor Server** - Full server-side tracking with HttpContext session management
- **🌐 Blazor WebAssembly** - Browser-based tracking; no persisted session storage by default
- **📄 Server-Side Rendering (SSR)** - Pre-render tracking with server-side optimization

- **📡 .NET Aspire/OTel (Server/SSR)** - Optional logs, traces, and metrics with ActivitySource + Meter

## 📦 Namespaces

- `Blazor.WhyDidYouRender.Abstractions` - **NEW** Cross-platform service interfaces
- `Blazor.WhyDidYouRender.Components` - Component base classes
- `Blazor.WhyDidYouRender.Configuration` - Configuration classes and enums
- `Blazor.WhyDidYouRender.Core` - Core tracking services
- `Blazor.WhyDidYouRender.Extensions` - Service collection extensions
- `Blazor.WhyDidYouRender.Records` - Data transfer objects
- `Blazor.WhyDidYouRender.Services` - **NEW** Environment-specific service implementations
- `Blazor.WhyDidYouRender.Helpers` - Utility classes

- `Blazor.WhyDidYouRender.Logging` - Unified logging interfaces and implementations

## 🔧 Cross-Platform Interfaces

### IHostingEnvironmentDetector

**Namespace**: `Blazor.WhyDidYouRender.Abstractions`

Detects the current Blazor hosting environment and provides environment-specific information.

```csharp
public interface IHostingEnvironmentDetector
{
    BlazorHostingModel DetectHostingModel();
    bool IsServerSide { get; }
    bool IsClientSide { get; }
    bool HasHttpContext { get; }
    bool HasJavaScriptInterop { get; }
    string GetEnvironmentDescription();
}
```

**Implementation**: `HostingEnvironmentDetector` (automatically registered)

### ISessionContextService

**Namespace**: `Blazor.WhyDidYouRender.Abstractions`

Provides cross-platform session management with automatic environment adaptation.

```csharp
public interface ISessionContextService
{
    string GetSessionId();
}
```

**Implementations**:
- **Server**: `ServerSessionContextService` - Uses HttpContext.Session
- **WASM**: `WasmSessionContextService` - Ephemeral session (no persisted storage)

### IWhyDidYouRenderLogger

**Namespace**: `Blazor.WhyDidYouRender.Logging`

Unified, structured logger used across environments (Server, WASM, SSR, Aspire/OpenTelemetry). Prefer this over any legacy logger.

```csharp
public interface IWhyDidYouRenderLogger
{
    // Levels
    void LogDebug(string message, Dictionary<string, object?>? data = null);
    void LogInfo(string message, Dictionary<string, object?>? data = null);
    void LogWarning(string message, Dictionary<string, object?>? data = null);
    void LogError(string message, Exception? exception = null, Dictionary<string, object?>? data = null);

    // Structured events
    void LogRenderEvent(RenderEvent renderEvent);
    void LogParameterChanges(string componentName, Dictionary<string, object?> changes);
    void LogPerformance(string componentName, string method, double durationMs, Dictionary<string, object?>? metrics = null);

    // Control/log level
    bool IsEnabled(LogLevel level);
    void SetLogLevel(LogLevel level);
    LogLevel GetLogLevel();
}
```

**Implementations** (examples):
- `ConsoleWhyDidYouRenderLogger` (fallback when MEL logger unavailable)
- `ServerWhyDidYouRenderLogger`
- `WasmWhyDidYouRenderLogger`
- `CompositeWhyDidYouRenderLogger` (fans-out to multiple sinks)
- `AspireWhyDidYouRenderLogger` (OTel/Aspire aware)


### IErrorTracker

**Namespace**: `Blazor.WhyDidYouRender.Abstractions`

Provides cross-platform error tracking and reporting.

```csharp
public interface IErrorTracker
{
    bool SupportsPersistentStorage { get; }
    bool SupportsErrorReporting { get; }
    string ErrorTrackingDescription { get; }

    Task TrackErrorAsync(Exception exception, Dictionary<string, object?> context, ErrorSeverity severity, string? componentName = null, string? operation = null);
    Task TrackErrorAsync(string message, Dictionary<string, object?> context, ErrorSeverity severity, string? componentName = null, string? operation = null);
    Task<IEnumerable<TrackingError>> GetRecentErrorsAsync(int count = 50, ErrorSeverity? severity = null, string? componentName = null);
    Task<ErrorStatistics> GetErrorStatisticsAsync();
    Task ClearErrorsAsync();
    Task<int> GetErrorCountAsync();
    Task<IEnumerable<TrackingError>> GetErrorsSinceAsync(DateTime since, DateTime? until = null);
}
```

**Implementations**:
- **Server**: `ServerErrorTracker` - In-memory with console logging
- **WASM**: `WasmErrorTracker` - In-memory with browser console logging (no storage)

## 🧩 Core Components

### TrackedComponentBase

Base class for components that should be tracked for render analysis.

```csharp
public abstract class TrackedComponentBase : ComponentBase
```

#### Methods

| Method | Description | Parameters | Returns |
|--------|-------------|------------|---------|
| `OnInitializedAsync()` | Tracks component initialization | None | `Task` |
| `OnParametersSetAsync()` | Tracks parameter changes | None | `Task` |
| `OnAfterRenderAsync(bool firstRender)` | Tracks render completion | `firstRender`: Whether this is the first render | `Task` |
| `StateHasChanged()` | Tracks manual state changes | None | `void` |

#### Usage Example

```csharp
@using Blazor.WhyDidYouRender.Components
@inherits TrackedComponentBase

<h3>My Component</h3>
<p>Count: @Count</p>

@code {
    [Parameter] public int Count { get; set; }
}
```

## ⚙️ Configuration

### WhyDidYouRenderConfig

Main configuration class for the tracking system.

```csharp
public class WhyDidYouRenderConfig
```

#### Core Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Enabled` | `bool` | `true` | Enable/disable tracking |
| `Verbosity` | `TrackingVerbosity` | `Normal` | Tracking verbosity level (Minimal, Normal, Verbose) |
| `Output` | `TrackingOutput` | `Console` | Output destination (Console, BrowserConsole, Both) |
| `TrackParameterChanges` | `bool` | `true` | Track parameter changes |
| `TrackPerformance` | `bool` | `true` | Track render performance |
| `IncludeSessionInfo` | `bool` | `true` | Include session information in logs |
| `EnableStateTracking` | `bool` | `true` | Enable field-level state change tracking |
| `AutoTrackSimpleTypes` | `bool` | `true` | Auto-track simple value types (string, int, bool, etc.) |
| `MaxTrackedFieldsPerComponent` | `int` | `50` | Maximum fields to track per component |
| `LogDetailedStateChanges` | `bool` | `false` | Log detailed before/after values for state changes |
| `TrackInheritedFields` | `bool` | `true` | Track fields inherited from base classes |
| `MaxStateComparisonDepth` | `int` | `3` | Maximum depth for object comparison |
| `EnableCollectionContentTracking` | `bool` | `false` | Track changes within collection contents |
| `StateSnapshotCleanupIntervalMinutes` | `int` | `10` | Interval for cleaning up old state snapshots |
| `MaxStateSnapshotAgeMinutes` | `int` | `30` | Maximum age for state snapshots before cleanup |
| `MaxTrackedComponents` | `int` | `1000` | Maximum number of components to track simultaneously |

#### Filtering Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `IncludeComponents` | `List<string>?` | `null` | Component name patterns to include (supports wildcards) |
| `ExcludeComponents` | `List<string>?` | `null` | Component name patterns to exclude (supports wildcards) |
| `IncludeNamespaces` | `List<string>?` | `null` | Namespace patterns to include |
| `ExcludeNamespaces` | `List<string>?` | `null` | Namespace patterns to exclude |

#### Performance & Optimization Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `MaxParameterChangesToLog` | `int` | `10` | Maximum parameter changes to log per component |
| `LogOnlyWhenParametersChange` | `bool` | `false` | Log only when parameters actually change |
| `DetectUnnecessaryRerenders` | `bool` | `true` | Detect and warn about unnecessary re-renders |
| `HighlightUnnecessaryRerenders` | `bool` | `true` | Highlight unnecessary re-renders in browser console |
| `FrequentRerenderThreshold` | `double` | `5.0` | Threshold for flagging frequent re-renders (renders/second) |

#### Cross-Platform Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `AutoDetectEnvironment` | `bool` | `true` | Automatically detect hosting environment |
| `ForceHostingModel` | `BlazorHostingModel?` | `null` | Force specific hosting model (overrides detection) |

#### Cross-Platform Usage Examples

**Blazor Server:**
```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Add WhyDidYouRender
builder.Services.AddWhyDidYouRender(config =>
{
    config.Enabled = true;
    config.Verbosity = TrackingVerbosity.Normal;
    config.Output = TrackingOutput.Both; // Server console AND browser console
    config.TrackParameterChanges = true;
    config.TrackPerformance = true;
});

var app = builder.Build();

// Auto-initialize for Server environment
await app.Services.InitializeServerAsync();
```

**Blazor WebAssembly:**
```csharp
var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");

// Add WhyDidYouRender
builder.Services.AddWhyDidYouRender(config =>
{
    config.Enabled = true;
    config.Verbosity = TrackingVerbosity.Normal;
    config.Output = TrackingOutput.BrowserConsole; // Browser console only
    config.TrackParameterChanges = true;
    config.TrackPerformance = true;
});

var host = builder.Build();

// Auto-initialize for WASM environment
await host.Services.InitializeWasmAsync(host.Services.GetRequiredService<IJSRuntime>());
```

**Auto-Detection (Recommended):**
```csharp
// In any component after service registration:
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        // Automatically detects environment and initializes accordingly
        await ServiceProvider.InitializeAsync(JSRuntime);
    }
}
```

### TrackingVerbosity Enum

Defines the verbosity levels for tracking output.

```csharp
public enum TrackingVerbosity
{
    Minimal = 0,
    Normal = 1,
    Verbose = 2
}
```

### TrackingOutput Enum

Defines where tracking output is sent.

```csharp
public enum TrackingOutput
{
    Console = 0,        // Server console only
    BrowserConsole = 1, // Browser console only
    Both = 2           // Both server and browser console
}
```

## 🔧 Core Services

### RenderTrackerService

Main service responsible for tracking and reporting render events.

```csharp
public class RenderTrackerService
```

#### Methods

| Method | Description | Parameters | Returns |
|--------|-------------|------------|---------|
| `TrackRender(ComponentBase, string, bool)` | Track a render event | `component`: Component instance<br>`trigger`: Trigger method<br>`firstRender`: Is first render | `void` |
| `TrackParameterChange(ComponentBase, string, object?, object?)` | Track parameter changes | `component`: Component<br>`parameterName`: Parameter name<br>`oldValue`: Previous value<br>`newValue`: Current value | `void` |
| `GetRenderStatistics(ComponentBase)` | Get render stats for component | `component`: Component instance | `RenderStatistics?` |
| `GetAllStatistics()` | Get all render statistics | None | `IEnumerable<RenderStatistics>` |

#### Usage Example

**Automatic Tracking (Recommended):**
```csharp
@using Blazor.WhyDidYouRender.Components
@inherits TrackedComponentBase

<h3>My Component</h3>
<p>Count: @Count</p>

@code {
    [Parameter] public int Count { get; set; }
    // Tracking happens automatically - no manual calls needed!
}
```

**Manual Tracking (Advanced):**
```csharp
@inject RenderTrackerService RenderTracker

@code {
    protected override void OnAfterRender(bool firstRender)
    {
        RenderTracker.TrackRender(this, "OnAfterRender", firstRender);
        base.OnAfterRender(firstRender);
    }
}
```

### ParameterChangeDetector

Service for detecting and analyzing parameter changes.

```csharp
public class ParameterChangeDetector
```

#### Methods

| Method | Description | Parameters | Returns |
|--------|-------------|------------|---------|
| `DetectParameterChanges(ComponentBase, string)` | Detect parameter changes for a component | `component`: Component instance<br>`method`: Lifecycle method being called | `Dictionary<string, object?>?` |
| `HasMeaningfulParameterChange(object?)` | Check if a parameter change is meaningful | `change`: Parameter change data | `bool` |
| `CleanupInactiveComponents(IEnumerable<ComponentBase>)` | Clean up parameter history for inactive components | `activeComponents`: Currently active components | `void` |
| `GetTrackedComponentCount()` | Get the number of tracked components | None | `int` |
| `ClearAll()` | Clear all parameter history | None | `void` |

### PerformanceTracker

Service for tracking render performance metrics.

```csharp
public class PerformanceTracker
```

#### Methods

| Method | Description | Parameters | Returns |
|--------|-------------|------------|---------|
| `StartTracking(ComponentBase)` | Start performance tracking | `component`: Component instance | `void` |
| `StopTracking(ComponentBase)` | Stop performance tracking | `component`: Component instance | `TimeSpan` |
| `GetMetrics(ComponentBase)` | Get performance metrics | `component`: Component instance | `PerformanceMetrics?` |

## 🧠 State Tracking APIs

### TrackStateAttribute

Marks fields or properties for explicit state tracking.

```csharp
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class TrackStateAttribute : Attribute
```

#### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Description` | `string?` | `null` | Optional description for the tracked field |
| `UseCustomComparer` | `bool` | `false` | Use custom comparison logic for complex objects |
| `TrackCollectionContents` | `bool` | `false` | Track changes within collection contents |
| `MaxComparisonDepth` | `int` | `3` | Maximum depth for object comparison |

#### Usage Examples

```csharp
// Basic tracking
[TrackState]
private UserInfo user;

// With description
[TrackState("User profile data")]
private UserProfile profile;

// Collection content tracking
[TrackState(TrackCollectionContents = true)]
private List<string> items;

// Custom comparison settings
[TrackState(UseCustomComparer = true, MaxComparisonDepth = 2)]
private ComplexObject data;
```

### IgnoreStateAttribute

Excludes fields or properties from state tracking.

```csharp
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class IgnoreStateAttribute : Attribute
```

#### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Reason` | `string?` | `null` | Optional reason for ignoring the field |
| `ApplyToInheritedClasses` | `bool` | `true` | Apply exclusion to inherited classes |

#### Usage Examples

```csharp
// Basic exclusion
[IgnoreState]
private long performanceCounter;

// With reason
[IgnoreState("Changes frequently - not relevant for rendering")]
private string debugInfo;

// Don't apply to inherited classes
[IgnoreState("Internal field", ApplyToInheritedClasses = false)]
private int internalCounter;
```

### StateTrackingOptionsAttribute

Provides component-level state tracking configuration.

```csharp
[AttributeUsage(AttributeTargets.Class)]
public class StateTrackingOptionsAttribute : Attribute
```

#### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `EnableStateTracking` | `bool` | `true` | Enable/disable state tracking for this component |
| `AutoTrackSimpleTypes` | `bool` | `true` | Auto-track simple value types |
| `MaxFields` | `int` | `50` | Maximum fields to track |
| `LogStateChanges` | `bool` | `true` | Log state changes for this component |
| `MaxComparisonDepth` | `int` | `3` | Maximum comparison depth |
| `TrackInheritedFields` | `bool` | `true` | Track inherited fields |
| `Description` | `string?` | `null` | Description for this configuration |

#### Usage Examples

```csharp
// Disable state tracking
[StateTrackingOptions(EnableStateTracking = false)]
public class NoTrackingComponent : ComponentBase { }

// Custom configuration
[StateTrackingOptions(
    MaxFields = 10,
    AutoTrackSimpleTypes = false,
    LogStateChanges = true,
    Description = "Performance-critical component")]
public class OptimizedComponent : ComponentBase { }
```

## 📊 Data Models

### RenderEvent

Represents a single render event with detailed tracking information.

```csharp
public record RenderEvent
{
    public DateTime Timestamp { get; init; }
    public string ComponentName { get; init; }
    public string ComponentType { get; init; }
    public string Method { get; init; }
    public bool? FirstRender { get; init; }
    public double? DurationMs { get; init; }
    public string? SessionId { get; init; }
    public Dictionary<string, object?>? ParameterChanges { get; init; }
    public IEnumerable<StateChange>? StateChanges { get; init; }
    public bool IsUnnecessaryRerender { get; init; }
    public string? UnnecessaryRerenderReason { get; init; }
    public bool IsFrequentRerender { get; init; }
}
```

### StateChange

Represents a change in component state.

```csharp
public record StateChange
{
    public string FieldName { get; init; }
    public object? OldValue { get; init; }
    public object? NewValue { get; init; }
    public string ChangeType { get; init; }
    public string? Description { get; init; }
}
```

### StateSnapshot

Captures the state of a component at a specific point in time.

```csharp
public class StateSnapshot
{
    public Type ComponentType { get; }
    public DateTime Timestamp { get; }
    public Dictionary<string, object?> FieldValues { get; }
    public bool HasValues { get; }
}
```

#### Methods

| Method | Description | Parameters | Returns |
|--------|-------------|------------|---------|
| `Create(ComponentBase, ComponentMetadata)` | Create snapshot from component | `component`: Component instance, `metadata`: Component metadata | `StateSnapshot` |
| `GetChangesFrom(StateSnapshot?)` | Get changes from previous snapshot | `previous`: Previous snapshot | `IEnumerable<StateChange>` |

### RenderStatistics

Statistics about component render frequency.

```csharp
public record RenderStatistics
{
    public string ComponentName { get; init; }
    public int TotalRenders { get; init; }
    public int RendersLastSecond { get; init; }
    public int RendersLastMinute { get; init; }
    public double AverageRenderRate { get; init; }
    public bool IsFrequentRenderer { get; init; }
}
```

### PerformanceMetrics

Performance metrics for a component.

```csharp
public class PerformanceMetrics
{
    public string ComponentName { get; set; }
    public int TotalRenders { get; set; }
    public double TotalDurationMs { get; set; }
    public double AverageDurationMs { get; set; }
    public double MaxDurationMs { get; set; }
    public double MinDurationMs { get; set; }
    public double LastRenderDurationMs { get; set; }
    public string LastRenderMethod { get; set; }
    public DateTime LastRenderTime { get; set; }
    public string SlowestMethod { get; set; }
    public string FastestMethod { get; set; }
}
```

### OverallPerformanceStatistics

Overall performance statistics across all components.

```csharp
public record OverallPerformanceStatistics
{
    public int TotalComponents { get; init; }
    public int TotalRenders { get; init; }
    public double AverageRenderTime { get; init; }
    public double SlowestRenderTime { get; init; }
    public double FastestRenderTime { get; init; }
    public int ComponentsWithSlowRenders { get; init; }
}
```

## 🔌 Extensions

### ServiceCollectionExtensions

Extension methods for service registration and cross-platform initialization.

```csharp
public static class ServiceCollectionExtensions
```

#### Service Registration Methods

| Method | Description | Parameters | Returns |
|--------|-------------|------------|---------|
| `AddWhyDidYouRender(this IServiceCollection, IConfiguration?)` | Add with configuration binding | `services`: Service collection<br>`configuration`: Optional config section | `IServiceCollection` |
| `AddWhyDidYouRender(this IServiceCollection, Action<WhyDidYouRenderConfig>)` | Add with config action | `services`: Service collection<br>`configureOptions`: Config action | `IServiceCollection` |

#### Cross-Platform Initialization Methods

| Method | Description | Parameters | Returns |
|--------|-------------|------------|---------|
| `InitializeAsync(this IServiceProvider, IJSRuntime?)` | **Recommended** - Auto-detects environment and initializes | `serviceProvider`: Service provider<br>`jsRuntime`: Optional JS runtime | `Task` |
| `InitializeServerAsync(this IServiceProvider)` | Initialize for Blazor Server environment | `serviceProvider`: Service provider | `Task` |
| `InitializeWasmAsync(this IServiceProvider, IJSRuntime)` | Initialize for WebAssembly environment | `serviceProvider`: Service provider<br>`jsRuntime`: JS runtime | `Task` |
| `InitializeSSRServices(this IServiceProvider)` | Initialize core services (environment-agnostic) | `serviceProvider`: Service provider | `void` |
| `InitializeWhyDidYouRenderAsync(this IServiceProvider, IJSRuntime)` | Initialize browser console logging | `serviceProvider`: Service provider<br>`jsRuntime`: JS runtime | `Task` |

#### Basic Usage Examples

```csharp
// Service registration with configuration
services.AddWhyDidYouRender(config =>
{
    config.Enabled = true;
    config.Verbosity = TrackingVerbosity.Normal;
    config.Output = TrackingOutput.Both;
});

// Auto-initialization (recommended)
await serviceProvider.InitializeAsync(jsRuntime);
```

## 🛠️ Utility Classes

### BrowserConsoleLogger

Service for logging to browser console.

```csharp
public class BrowserConsoleLogger : IBrowserConsoleLogger
```

#### Methods

| Method | Description | Parameters | Returns |
|--------|-------------|------------|---------|
| `LogAsync(TrackingVerbosity, string, object?)` | Log message to console | `verbosity`: Verbosity level<br>`message`: Message<br>`data`: Additional data | `Task` |
| `LogRenderEventAsync(RenderEvent)` | Log render event | `renderEvent`: Event to log | `Task` |

### SafeExecutor

Utility for safe execution of tracking operations.

```csharp
public static class SafeExecutor
```

#### Methods

| Method | Description | Parameters | Returns |
|--------|-------------|------------|---------|
| `Execute(Action)` | Execute action safely | `action`: Action to execute | `void` |
| `Execute<T>(Func<T>)` | Execute function safely | `func`: Function to execute | `T?` |
| `ExecuteAsync(Func<Task>)` | Execute async action safely | `asyncAction`: Async action | `Task` |

## 🔍 Error Handling

### TrackingError

Represents an error that occurred during tracking.

```csharp
public record TrackingError
{
    public string ErrorId { get; init; }
    public DateTime Timestamp { get; init; }
    public string Message { get; init; }
    public string? ExceptionType { get; init; }
    public string? StackTrace { get; init; }
    public string? ComponentName { get; init; }
    public string? TrackingMethod { get; init; }
    public string? SessionId { get; init; }
    public Dictionary<string, object?> Context { get; init; }
    public ErrorSeverity Severity { get; init; }
    public bool Recovered { get; init; }
}
```

### ErrorSeverity

Severity levels for tracking errors.

```csharp
public enum ErrorSeverity
{
    Info,       // Informational - minor issues that don't affect functionality
    Warning,    // Issues that might affect tracking but don't break functionality
    Error,      // Significant issues that affect tracking functionality
    Critical    // Severe issues that might affect application stability
}
```

### ErrorStatistics

Error statistics summary.

```csharp
public record ErrorStatistics
{
    public int TotalErrors { get; init; }
    public int ErrorsLastHour { get; init; }
    public int ErrorsLast24Hours { get; init; }
    public Dictionary<string, int> CommonErrorTypes { get; init; }
    public Dictionary<string, int> ProblematicComponents { get; init; }
    public double ErrorRate { get; init; }
}
```

## 📈 Best Practices

### Performance Considerations

1. **Disable in Production**: Always disable tracking in production environments
2. **Selective Tracking**: Only track components you're actively optimizing
3. **Log Level Management**: Use appropriate log levels to reduce noise

### Configuration Recommendations

```csharp
// Development
config.Enabled = true;
config.Verbosity = TrackingVerbosity.Verbose;
config.Output = TrackingOutput.Both;
config.TrackPerformance = true;

// Staging
config.Enabled = true;
config.Verbosity = TrackingVerbosity.Normal;
config.Output = TrackingOutput.Console;

// Production
config.Enabled = false;
```

### Component Design

1. **Inherit Selectively**: Only inherit from `TrackedComponentBase` for components under investigation
2. **Parameter Stability**: Ensure parameter objects have stable references when possible
3. **State Management**: Be mindful of unnecessary state changes that trigger re-renders
