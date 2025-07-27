# Blazor WhyDidYouRender

A powerful performance monitoring and debugging tool for Blazor applications that helps identify unnecessary re-renders and optimize component performance.

## 🚀 Features

- **🔍 Render Tracking**: Monitor when and why your Blazor components re-render
- **📊 Performance Metrics**: Track render duration and frequency
- **🎯 Parameter Change Detection**: Identify which parameter changes trigger re-renders
- **⚡ Unnecessary Render Detection**: Find components that re-render without actual changes
- **🛠️ Developer-Friendly**: Easy integration with existing Blazor applications
- **📱 Browser Console Logging**: Real-time debugging information in browser dev tools
- **⚙️ Configurable**: Flexible configuration options for different environments
- **🔧 Diagnostics Endpoint**: Optional HTTP endpoint for advanced monitoring

## 📦 Installation

### Package Manager Console
```powershell
Install-Package Blazor.WhyDidYouRender
```

### .NET CLI
```bash
dotnet add package Blazor.WhyDidYouRender
```

### PackageReference
```xml
<PackageReference Include="Blazor.WhyDidYouRender" Version="1.0.0" />
```

## 🛠️ Quick Start

### 1. Register Services

Add WhyDidYouRender to your service collection in `Program.cs`:

```csharp
using Blazor.WhyDidYouRender.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add Blazor services
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

// Initialize WhyDidYouRender SSR services
app.Services.InitializeSSRServices();

// Configure pipeline...
app.Run();
```

### 2. Inherit from TrackedComponentBase

Update your components to inherit from `TrackedComponentBase`:

```csharp
@using Blazor.WhyDidYouRender.Components
@using Blazor.WhyDidYouRender.Extensions
@inherits TrackedComponentBase
@inject IJSRuntime JSRuntime
@inject IServiceProvider ServiceProvider

<h3>My Tracked Component</h3>
<p>Current count: @currentCount</p>
<button @onclick="IncrementCount">Click me</button>

@code {
    private int currentCount = 0;
    private bool browserLoggerInitialized = false;

    [Parameter] public string? Title { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !browserLoggerInitialized)
        {
            // Initialize WhyDidYouRender browser logging
            await ServiceProvider.InitializeWhyDidYouRenderAsync(JSRuntime);
            browserLoggerInitialized = true;
        }
        await base.OnAfterRenderAsync(firstRender);
    }

    private void IncrementCount()
    {
        currentCount++;
    }
}
```

### 3. Monitor in Browser Console

Open your browser's developer tools and watch the console for render tracking information:

```
[WhyDidYouRender] Counter re-rendered
├─ Trigger: StateHasChanged
├─ Duration: 2.3ms
├─ Parameters: Title (unchanged)
└─ Reason: Manual state change
```

## 📖 Configuration

### Basic Configuration

```csharp
builder.Services.AddWhyDidYouRender(config =>
{
    // Enable/disable tracking
    config.Enabled = true;

    // Set logging verbosity (Minimal, Normal, Verbose)
    config.Verbosity = TrackingVerbosity.Normal;

    // Set output destination (Console, BrowserConsole, Both)
    config.Output = TrackingOutput.Both;

    // Track parameter changes
    config.TrackParameterChanges = true;

    // Track performance metrics
    config.TrackPerformance = true;

    // Include session information
    config.IncludeSessionInfo = true;
});
```

### Advanced Configuration

```csharp
builder.Services.AddWhyDidYouRender(config =>
{
    // Performance tracking
    config.TrackPerformance = true;

    // Track parameter changes
    config.TrackParameterChanges = true;

    // Include session information in logs
    config.IncludeSessionInfo = true;

    // Set verbosity level
    config.Verbosity = TrackingVerbosity.Verbose;

    // Output to both server console and browser console
    config.Output = TrackingOutput.Both;
});
```

### Environment-Specific Configuration

```csharp
builder.Services.AddWhyDidYouRender(config =>
{
    if (builder.Environment.IsDevelopment())
    {
        config.Enabled = true;
        config.Verbosity = TrackingVerbosity.Verbose;
        config.Output = TrackingOutput.Both;
        config.TrackParameterChanges = true;
        config.TrackPerformance = true;
    }
    else if (builder.Environment.IsStaging())
    {
        config.Enabled = true;
        config.Verbosity = TrackingVerbosity.Normal;
        config.Output = TrackingOutput.Console;
    }
    else
    {
        config.Enabled = false; // Disable in production
    }
});
```

## 🎯 Usage Patterns

### Component Inheritance

The recommended approach is to inherit from `TrackedComponentBase`:

```csharp
@inherits TrackedComponentBase

@code {
    // Your component logic here
}
```

### Manual Tracking (Advanced)

For existing components that can't inherit from `TrackedComponentBase`, you can use manual tracking:

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

## 📊 Understanding the Output

### Console Log Format

```
[WhyDidYouRender] ComponentName re-rendered
├─ Trigger: OnParametersSet
├─ Duration: 1.2ms
├─ Parameters: 
│  ├─ Title: "Old Value" → "New Value" (changed)
│  └─ Count: 5 (unchanged)
├─ Performance: 
│  ├─ Render Count: 3
│  └─ Average Duration: 1.8ms
└─ Reason: Parameter change detected
```

### Log Levels

- **Debug**: All render events and detailed information
- **Info**: Normal render events with basic information
- **Warning**: Potentially unnecessary re-renders
- **Error**: Performance issues and problems

## 🔧 Diagnostics Endpoint

Enable the optional diagnostics endpoint for advanced monitoring:

```csharp
// In Program.cs
app.UseWhyDidYouRenderDiagnostics("/diagnostics/renders");
```

Access diagnostics at: `https://yourapp.com/diagnostics/renders`

## 🎨 Best Practices

### 1. Use in Development Only
```csharp
config.Enabled = builder.Environment.IsDevelopment();
```

### 2. Focus on Important Events
```csharp
config.Verbosity = TrackingVerbosity.Normal;
config.TrackParameterChanges = true;
```

### 3. Selective Component Tracking
Only track components you're optimizing:
```csharp
// Only inherit from TrackedComponentBase for components under investigation
@inherits TrackedComponentBase
```

### 4. Parameter Optimization
Use the insights to optimize parameter passing:
```csharp
// Before: Creates new object every render
<ChildComponent Data="@(new { Count = count })" />

// After: Stable reference
<ChildComponent Data="@stableDataObject" />
```

## 🚧 Roadmap

- **Testing Suite**: Comprehensive test coverage (waiting for .NET 9.0 compatibility)
- **Performance Profiler**: Advanced performance analysis tools
- **Custom Formatters**: Extensible output formatting

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details.

## 📄 License

This project is licensed under the GNU General Public License v3.0 or later - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

Inspired by the React [why-did-you-render](https://github.com/welldone-software/why-did-you-render) library.
