# Blazor WhyDidYouRender - Examples & Best Practices

This guide provides practical examples and best practices for using WhyDidYouRender effectively in your Blazor applications.

## 🎯 Common Use Cases

### 1. Identifying Unnecessary Re-renders

**Problem**: Component re-renders even when data hasn't changed.

**Example Component**:
```csharp
@using Blazor.WhyDidYouRender.Components
@inherits TrackedComponentBase

<div class="user-card">
    <h3>@User.Name</h3>
    <p>@User.Email</p>
    <p>Last login: @User.LastLogin.ToString("g")</p>
</div>

@code {
    [Parameter] public UserModel User { get; set; } = new();
    [Parameter] public bool IsSelected { get; set; }
}
```

**Console Output**:
```
[WhyDidYouRender] UserCard re-rendered
├─ Trigger: OnParametersSet
├─ Duration: 1.2ms
├─ Parameters: 
│  ├─ User: UserModel#123 (unchanged)
│  └─ IsSelected: false (unchanged)
└─ Reason: ⚠️ Unnecessary render - no changes detected
```

**Solution**: Check parent component for object recreation:
```csharp
// ❌ Bad: Creates new object every render
<UserCard User="@(new UserModel { Name = userName })" />

// ✅ Good: Stable object reference
<UserCard User="@currentUser" />
```

### 2. Parameter Change Analysis

**Problem**: Understanding which parameter changes trigger re-renders.

**Example Component**:
```csharp
@inherits TrackedComponentBase

<div class="product-card">
    <h3>@Product.Name</h3>
    <p>Price: @Product.Price.ToString("C")</p>
    <p>Stock: @Product.Stock</p>
    <button disabled="@(!Product.InStock)">Add to Cart</button>
</div>

@code {
    [Parameter] public ProductModel Product { get; set; } = new();
    [Parameter] public string Currency { get; set; } = "USD";
    [Parameter] public EventCallback<ProductModel> OnAddToCart { get; set; }
}
```

**Console Output**:
```
[WhyDidYouRender] ProductCard re-rendered
├─ Trigger: OnParametersSet
├─ Duration: 2.1ms
├─ Parameters: 
│  ├─ Product: ProductModel#456
│  │  ├─ Name: "Widget" (unchanged)
│  │  ├─ Price: 19.99 → 24.99 (changed)
│  │  └─ Stock: 10 (unchanged)
│  ├─ Currency: "USD" (unchanged)
│  └─ OnAddToCart: EventCallback (unchanged)
└─ Reason: ✅ Legitimate render - price changed
```

### 3. Performance Bottleneck Detection

**Problem**: Slow-rendering components affecting user experience.

**Example Component**:
```csharp
@inherits TrackedComponentBase

<div class="data-grid">
    @foreach (var item in Items)
    {
        <div class="grid-row">
            @foreach (var column in Columns)
            {
                <div class="grid-cell">
                    @GetCellValue(item, column)
                </div>
            }
        </div>
    }
</div>

@code {
    [Parameter] public IEnumerable<DataItem> Items { get; set; } = [];
    [Parameter] public IEnumerable<ColumnDefinition> Columns { get; set; } = [];
    
    private string GetCellValue(DataItem item, ColumnDefinition column)
    {
        // Complex calculation
        return item.GetValue(column.PropertyName)?.ToString() ?? "";
    }
}
```

**Console Output**:
```
[WhyDidYouRender] DataGrid re-rendered
├─ Trigger: OnParametersSet
├─ Duration: ⚠️ 45.2ms (slow)
├─ Performance:
│  ├─ Render Count: 12
│  ├─ Average Duration: 38.7ms
│  └─ Renders/Second: 0.8
└─ Reason: ⚠️ Performance concern - consider optimization
```

## 🛠️ Optimization Strategies

### 1. Stable Object References

**Problem**: Creating new objects in parent components.

```csharp
// ❌ Bad: Creates new objects every render
@foreach (var item in items)
{
    <ItemComponent 
        Data="@(new ItemViewModel { Id = item.Id, Name = item.Name })"
        Options="@(new { ShowDetails = true, Theme = "dark" })" />
}

// ✅ Good: Stable references
@foreach (var item in itemViewModels)
{
    <ItemComponent 
        Data="@item"
        Options="@defaultOptions" />
}

@code {
    private List<ItemViewModel> itemViewModels = new();
    private readonly object defaultOptions = new { ShowDetails = true, Theme = "dark" };
    
    protected override void OnParametersSet()
    {
        // Only recreate when source data changes
        if (items != previousItems)
        {
            itemViewModels = items.Select(i => new ItemViewModel 
            { 
                Id = i.Id, 
                Name = i.Name 
            }).ToList();
            previousItems = items;
        }
    }
}
```

### 2. Memoization for Expensive Calculations

**Problem**: Recalculating expensive values on every render.

```csharp
@inherits TrackedComponentBase

<div class="chart">
    <svg>
        @foreach (var point in ChartPoints)
        {
            <circle cx="@point.X" cy="@point.Y" r="3" />
        }
    </svg>
</div>

@code {
    [Parameter] public IEnumerable<DataPoint> Data { get; set; } = [];
    [Parameter] public ChartOptions Options { get; set; } = new();
    
    private IEnumerable<DataPoint> previousData = [];
    private ChartOptions previousOptions = new();
    private List<ChartPoint> cachedChartPoints = new();
    
    private List<ChartPoint> ChartPoints
    {
        get
        {
            // Only recalculate if data or options changed
            if (!Data.SequenceEqual(previousData) || !Options.Equals(previousOptions))
            {
                cachedChartPoints = CalculateChartPoints(Data, Options);
                previousData = Data;
                previousOptions = Options;
            }
            return cachedChartPoints;
        }
    }
    
    private List<ChartPoint> CalculateChartPoints(IEnumerable<DataPoint> data, ChartOptions options)
    {
        // Expensive calculation here
        return data.Select(d => new ChartPoint 
        { 
            X = d.Value * options.ScaleX, 
            Y = d.Timestamp.Ticks * options.ScaleY 
        }).ToList();
    }
}
```

### 3. Conditional Rendering Optimization

**Problem**: Rendering expensive content when not visible.

```csharp
@inherits TrackedComponentBase

<div class="expandable-section">
    <button @onclick="ToggleExpanded">
        @(IsExpanded ? "Collapse" : "Expand") Details
    </button>
    
    @if (IsExpanded)
    {
        <div class="details">
            <ExpensiveComponent Data="@DetailData" />
        </div>
    }
</div>

@code {
    [Parameter] public bool IsExpanded { get; set; }
    [Parameter] public ComplexData DetailData { get; set; } = new();
    
    private void ToggleExpanded()
    {
        IsExpanded = !IsExpanded;
    }
}
```

**Console Output**:
```
[WhyDidYouRender] ExpandableSection re-rendered
├─ Trigger: StateHasChanged
├─ Duration: 1.1ms
├─ Parameters: 
│  ├─ IsExpanded: false → true (changed)
│  └─ DetailData: ComplexData#789 (unchanged)
└─ Reason: ✅ Legitimate render - expansion state changed

[WhyDidYouRender] ExpensiveComponent re-rendered
├─ Trigger: OnInitialized
├─ Duration: 23.4ms
└─ Reason: ✅ First render - component just became visible
```

## 📊 Configuration Best Practices

### Environment-Specific Configuration

```csharp
public static class WhyDidYouRenderConfiguration
{
    public static void Configure(IServiceCollection services, IWebHostEnvironment environment)
    {
        services.AddWhyDidYouRender(config =>
        {
            switch (environment.EnvironmentName.ToLower())
            {
                case "development":
                    ConfigureDevelopment(config);
                    break;
                case "staging":
                    ConfigureStaging(config);
                    break;
                case "production":
                    ConfigureProduction(config);
                    break;
                default:
                    ConfigureDefault(config);
                    break;
            }
        });
    }
    
    private static void ConfigureDevelopment(WhyDidYouRenderConfig config)
    {
        config.Enabled = true;
        config.Verbosity = TrackingVerbosity.Verbose;
        config.Output = TrackingOutput.Both;
        config.TrackParameterChanges = true;
        config.TrackPerformance = true;
        config.EnableStateTracking = true;
        config.MinimumLogLevel = Blazor.WhyDidYouRender.Logging.LogLevel.Debug;
        // Optional: enable Aspire/OTel in dev
        config.EnableOpenTelemetry = true;
        config.EnableOtelLogs = true;
        config.EnableOtelTraces = true;
        config.EnableOtelMetrics = true;
    }

    private static void ConfigureStaging(WhyDidYouRenderConfig config)
    {
        config.Enabled = true;
        config.Verbosity = TrackingVerbosity.Normal;
        config.Output = TrackingOutput.Console;
        config.TrackPerformance = true;
        config.MinimumLogLevel = Blazor.WhyDidYouRender.Logging.LogLevel.Warning;
    }

    private static void ConfigureProduction(WhyDidYouRenderConfig config)
    {
        config.Enabled = false; // Completely disabled in production by default
    }

    private static void ConfigureDefault(WhyDidYouRenderConfig config)
    {
        config.Enabled = true;
        config.Verbosity = TrackingVerbosity.Normal;
        config.Output = TrackingOutput.Console;
        config.MinimumLogLevel = Blazor.WhyDidYouRender.Logging.LogLevel.Warning;
    }
}
```

### Selective Component Tracking

```csharp
// Create a custom attribute for marking components to track
[AttributeUsage(AttributeTargets.Class)]
public class TrackRenderingAttribute : Attribute
{
    public string Reason { get; set; } = "";
}

// Use on components you want to optimize
[TrackRendering(Reason = "Performance bottleneck in user dashboard")]
public partial class UserDashboard : TrackedComponentBase
{
    // Component implementation
}

// Create a base class for conditional tracking
public abstract class ConditionallyTrackedComponentBase : ComponentBase
{
    protected override void OnInitialized()
    {
        var shouldTrack = GetType().GetCustomAttribute<TrackRenderingAttribute>() != null;
        
        if (shouldTrack && ShouldEnableTracking())
        {
            // Enable tracking for this component
            EnableTracking();
        }
        
        base.OnInitialized();
    }
    
    private bool ShouldEnableTracking()
    {
        // Check environment, feature flags, etc.
        return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
    }
    
    private void EnableTracking()
    {
        // Implementation to enable tracking
    }
}
```

## 🔍 Debugging Workflows

### 1. Performance Investigation Workflow

1. **Enable Debug Logging**:
```csharp
config.LogLevel = LogLevel.Debug;
config.TrackPerformance = true;
```

2. **Identify Slow Components**:
Look for warnings about render duration > 10ms

3. **Analyze Parameter Changes**:
Check which parameters are changing unnecessarily

4. **Optimize Based on Findings**:
- Stabilize object references
- Add memoization
- Implement conditional rendering

### 2. Memory Leak Detection

```csharp
@inherits TrackedComponentBase
@implements IDisposable

<div class="real-time-component">
    <p>Current value: @currentValue</p>
</div>

@code {
    private Timer? timer;
    private int currentValue;
    
    protected override void OnInitialized()
    {
        timer = new Timer(UpdateValue, null, 0, 1000);
    }
    
    private void UpdateValue(object? state)
    {
        currentValue++;
        InvokeAsync(StateHasChanged); // This will be tracked
    }
    
    public void Dispose()
    {
        timer?.Dispose();
    }
}
```

**Console Output**:
```
[WhyDidYouRender] RealTimeComponent re-rendered
├─ Trigger: StateHasChanged
├─ Duration: 0.8ms
├─ Performance:
│  ├─ Render Count: 45 (in 45 seconds)
│  └─ Renders/Second: 1.0
└─ Reason: ✅ Expected - timer-based updates
```

## 🎨 Advanced Patterns

### Custom Tracking for Third-Party Components

```csharp
// Wrapper component for tracking third-party components
@inherits TrackedComponentBase

<ThirdPartyComponent @attributes="AllAttributes" />

@code {
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AllAttributes { get; set; }
    
    protected override void OnParametersSet()
    {
        // Custom parameter change detection for third-party component
        base.OnParametersSet();
    }
}
```

### Batch Render Tracking

```csharp
public class BatchRenderTracker
{
    private readonly List<RenderEvent> batchedEvents = new();
    private readonly Timer flushTimer;
    
    public BatchRenderTracker()
    {
        flushTimer = new Timer(FlushEvents, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
    }
    
    public void TrackRender(RenderEvent renderEvent)
    {
        lock (batchedEvents)
        {
            batchedEvents.Add(renderEvent);
        }
    }
    
    private void FlushEvents(object? state)
    {
        lock (batchedEvents)
        {
            if (batchedEvents.Count > 0)
            {
                // Process batched events
                ProcessBatch(batchedEvents.ToList());
                batchedEvents.Clear();
            }
        }
    }
    
    private void ProcessBatch(List<RenderEvent> events)
    {
        // Analyze patterns, detect issues, etc.
        var frequentRenderers = events
            .GroupBy(e => e.ComponentName)
            .Where(g => g.Count() > 10)
            .Select(g => new { Component = g.Key, Count = g.Count() });
            
        foreach (var renderer in frequentRenderers)
        {
            Console.WriteLine($"⚠️ {renderer.Component} rendered {renderer.Count} times in batch");
        }
    }
}
```
