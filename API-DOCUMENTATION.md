# Blazor WhyDidYouRender - API Documentation

This document provides comprehensive API documentation for all public classes, interfaces, and methods in the Blazor WhyDidYouRender library.

## 📦 Namespaces

- `Blazor.WhyDidYouRender.Components` - Component base classes
- `Blazor.WhyDidYouRender.Configuration` - Configuration classes and enums
- `Blazor.WhyDidYouRender.Core` - Core tracking services
- `Blazor.WhyDidYouRender.Extensions` - Service collection extensions
- `Blazor.WhyDidYouRender.Records` - Data transfer objects
- `Blazor.WhyDidYouRender.Helpers` - Utility classes

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

#### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Enabled` | `bool` | `true` | Enable/disable tracking |
| `LogLevel` | `LogLevel` | `Warning` | Minimum log level for output |
| `IncludeProps` | `bool` | `true` | Track parameter changes |
| `IncludeState` | `bool` | `true` | Track state changes |
| `TrackHooks` | `bool` | `true` | Track lifecycle hooks |
| `TrackPerformance` | `bool` | `true` | Track render performance |
| `TrackAllPureComponents` | `bool` | `false` | Track all components regardless of changes |
| `LogOnDifferentValues` | `bool` | `true` | Only log when values actually change |
| `LogOwnerReasons` | `bool` | `false` | Include parent component information |
| `HotReloadMode` | `bool` | `false` | Enable hot reload optimizations |

#### Usage Example

```csharp
builder.Services.AddWhyDidYouRender(config =>
{
    config.Enabled = true;
    config.LogLevel = LogLevel.Warning;
    config.IncludeProps = true;
    config.TrackPerformance = true;
});
```

### LogLevel Enum

Defines the verbosity levels for tracking output.

```csharp
public enum LogLevel
{
    Debug = 0,
    Info = 1,
    Warning = 2,
    Error = 3
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
| `DetectChanges(ComponentBase, ParameterView, ParameterView)` | Detect parameter changes | `component`: Component<br>`previousParameters`: Old parameters<br>`currentParameters`: New parameters | `IEnumerable<ParameterChange>` |
| `HasSignificantChanges(IEnumerable<ParameterChange>)` | Check if changes are significant | `changes`: Parameter changes | `bool` |

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

## 📊 Data Models

### RenderEvent

Represents a single render event.

```csharp
public record RenderEvent
{
    public string ComponentName { get; init; }
    public string ComponentType { get; init; }
    public string Trigger { get; init; }
    public DateTime Timestamp { get; init; }
    public TimeSpan Duration { get; init; }
    public bool FirstRender { get; init; }
    public IEnumerable<ParameterChange> ParameterChanges { get; init; }
    public string SessionId { get; init; }
}
```

### ParameterChange

Represents a change in component parameters.

```csharp
public record ParameterChange
{
    public string Name { get; init; }
    public object? OldValue { get; init; }
    public object? NewValue { get; init; }
    public bool HasChanged { get; init; }
    public string ChangeType { get; init; }
}
```

### RenderStatistics

Aggregated statistics for a component.

```csharp
public record RenderStatistics
{
    public string ComponentName { get; init; }
    public string ComponentType { get; init; }
    public int TotalRenders { get; init; }
    public TimeSpan TotalDuration { get; init; }
    public TimeSpan AverageDuration { get; init; }
    public TimeSpan MinDuration { get; init; }
    public TimeSpan MaxDuration { get; init; }
    public DateTime FirstRender { get; init; }
    public DateTime LastRender { get; init; }
    public int UnnecessaryRenders { get; init; }
}
```

### PerformanceMetrics

Performance metrics for a component.

```csharp
public record PerformanceMetrics
{
    public string ComponentName { get; init; }
    public int RenderCount { get; init; }
    public TimeSpan TotalRenderTime { get; init; }
    public TimeSpan AverageRenderTime { get; init; }
    public TimeSpan LastRenderTime { get; init; }
    public double RendersPerSecond { get; init; }
}
```

## 🔌 Extensions

### ServiceCollectionExtensions

Extension methods for service registration.

```csharp
public static class ServiceCollectionExtensions
```

#### Methods

| Method | Description | Parameters | Returns |
|--------|-------------|------------|---------|
| `AddWhyDidYouRender(this IServiceCollection)` | Add with default config | `services`: Service collection | `IServiceCollection` |
| `AddWhyDidYouRender(this IServiceCollection, WhyDidYouRenderConfig)` | Add with config instance | `services`: Service collection<br>`config`: Configuration | `IServiceCollection` |
| `AddWhyDidYouRender(this IServiceCollection, Action<WhyDidYouRenderConfig>)` | Add with config action | `services`: Service collection<br>`configureOptions`: Config action | `IServiceCollection` |

#### Usage Examples

```csharp
// Default configuration
services.AddWhyDidYouRender();

// With configuration instance
var config = new WhyDidYouRenderConfig { Enabled = true };
services.AddWhyDidYouRender(config);

// With configuration action
services.AddWhyDidYouRender(config =>
{
    config.Enabled = true;
    config.LogLevel = LogLevel.Warning;
});
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
| `LogAsync(LogLevel, string, object?)` | Log message to console | `level`: Log level<br>`message`: Message<br>`data`: Additional data | `Task` |
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
    public string ErrorType { get; init; }
    public string Message { get; init; }
    public string? StackTrace { get; init; }
    public DateTime Timestamp { get; init; }
    public string ComponentName { get; init; }
    public string Operation { get; init; }
}
```

### IErrorTracker

Interface for error tracking service.

```csharp
public interface IErrorTracker
{
    void TrackError(Exception exception, string operation, string? componentName = null);
    IEnumerable<TrackingError> GetErrors();
    void ClearErrors();
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
config.LogLevel = LogLevel.Debug;
config.TrackPerformance = true;

// Staging
config.Enabled = true;
config.LogLevel = LogLevel.Warning;
config.LogOnDifferentValues = true;

// Production
config.Enabled = false;
```

### Component Design

1. **Inherit Selectively**: Only inherit from `TrackedComponentBase` for components under investigation
2. **Parameter Stability**: Ensure parameter objects have stable references when possible
3. **State Management**: Be mindful of unnecessary state changes that trigger re-renders
