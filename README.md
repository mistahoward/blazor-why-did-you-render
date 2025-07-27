# Blazor WhyDidYouRender

A powerful **cross-platform** performance monitoring and debugging tool for Blazor applications that helps identify unnecessary re-renders and optimize component performance across **Server**, **WebAssembly**, and **SSR** environments.

[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/S6S3XYOL5)

## 🌐 Cross-Platform Support

**WhyDidYouRender v2.0** now supports all Blazor hosting models:

| Environment | Support | Session Management | Console Logging | Performance Tracking |
|-------------|---------|-------------------|-----------------|---------------------|
| **🖥️ Blazor Server** | ✅ Full | HttpContext | Server + Browser | ✅ Full |
| **🌐 Blazor WASM** | ✅ Full | Browser Storage | Browser Only | ✅ Full |
| **📄 SSR** | ✅ Full | HttpContext | Server + Browser | ✅ Full |

## 🚀 Features

- **🔍 Render Tracking**: Monitor when and why your Blazor components re-render
- **📊 Performance Metrics**: Track render duration and frequency across all environments
- **🎯 Parameter Change Detection**: Identify which parameter changes trigger re-renders
- **⚡ Unnecessary Render Detection**: Find components that re-render without actual changes
- **🌐 Cross-Platform**: Works seamlessly in Server, WASM, and SSR environments
- **🛠️ Developer-Friendly**: Easy integration with existing Blazor applications
- **📱 Smart Console Logging**: Adapts to environment (server console + browser console)
- **💾 Flexible Session Management**: HttpContext (server) or Browser Storage (WASM)
- **⚙️ Auto-Detection**: Automatically detects hosting environment and adapts
- **🔧 Environment-Specific**: Optimized services for each hosting model

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
<PackageReference Include="Blazor.WhyDidYouRender" Version="2.0.0" />
```

> **📢 Version 2.0 Breaking Changes**: See [Migration Guide](#-migration-from-v1x) for upgrading from v1.x

## 🛠️ Quick Start

WhyDidYouRender **automatically detects** your hosting environment and configures itself appropriately. The same code works across all Blazor hosting models!

### 🖥️ Blazor Server / SSR Setup

```csharp
using Blazor.WhyDidYouRender.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add Blazor services
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Add WhyDidYouRender - works automatically!
builder.Services.AddWhyDidYouRender(config =>
{
    config.Enabled = true;
    config.Verbosity = TrackingVerbosity.Normal;
    config.Output = TrackingOutput.Both; // Server console AND browser console
    config.TrackParameterChanges = true;
    config.TrackPerformance = true;
});

var app = builder.Build();

// Initialize WhyDidYouRender services
app.Services.InitializeSSRServices();

app.Run();
```

### 🌐 Blazor WebAssembly Setup

```csharp
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Blazor.WhyDidYouRender.Extensions;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");

// Add WhyDidYouRender - automatically detects WASM!
builder.Services.AddWhyDidYouRender(config =>
{
    config.Enabled = true;
    config.Verbosity = TrackingVerbosity.Verbose;
    config.Output = TrackingOutput.BrowserConsole; // Browser console only in WASM
    config.TrackParameterChanges = true;
    config.TrackPerformance = true;
    // WASM storage is enabled by default
});

var host = builder.Build();

await host.Services.InitializeWasmAsync(host.Services.GetRequiredService<IJSRuntime>());

await host.RunAsync();
```

### 2. Use TrackedComponentBase (Cross-Platform)

Update your components to inherit from `TrackedComponentBase` - **works in all environments**:

```csharp
@using Blazor.WhyDidYouRender.Components
@inherits TrackedComponentBase

<h3>My Tracked Component</h3>
<p>Current count: @currentCount</p>
<p>Title: @Title</p>
<button @onclick="IncrementCount">Click me</button>

@code {
    private int currentCount = 0;

    [Parameter] public string? Title { get; set; }

    private void IncrementCount()
    {
        currentCount++;
    }
}
```

### 3. Monitor Output (Environment-Aware)

WhyDidYouRender automatically adapts its output based on your environment:

#### 🖥️ Server/SSR Environment
- **Server Console**: Detailed logging in your application console
- **Browser Console**: Real-time debugging in browser dev tools
- **Session Management**: Uses HttpContext for server-side session tracking

#### 🌐 WASM Environment
- **Browser Console**: All logging appears in browser dev tools
- **Session Management**: Uses browser localStorage/sessionStorage
- **Performance Tracking**: Client-side performance metrics

**Example Output:**
```
[WhyDidYouRender] Counter re-rendered (WASM)
├─ Trigger: StateHasChanged
├─ Duration: 1.8ms
├─ Parameters: Title (unchanged)
├─ Session: wasm-abc123def
└─ Reason: Manual state change
```

<img width="763" height="380" alt="image" src="https://github.com/user-attachments/assets/497fdcbe-75eb-4707-8ccb-4cb4ac07b1c6" />

## 🌐 Cross-Platform Features

### Automatic Environment Detection
WhyDidYouRender automatically detects your hosting environment and adapts:

```csharp
// Same configuration works everywhere!
builder.Services.AddWhyDidYouRender(config =>
{
    config.Enabled = true;
    config.Output = TrackingOutput.Both; // Adapts automatically:
    // Server: Console + Browser
    // WASM: Browser only
});
```

### Environment-Specific Services

| Service | Server Implementation | WASM Implementation |
|---------|----------------------|-------------------|
| **Session Management** | `ServerSessionContextService` | `WasmSessionContextService` |
| **Logging** | `ServerTrackingLogger` | `WasmTrackingLogger` |
| **Error Tracking** | `ServerErrorTracker` | `WasmErrorTracker` |
| **Storage** | HttpContext.Session | Browser Storage |

## 📖 Configuration

### Cross-Platform Configuration

The same configuration works across all environments, with automatic adaptation:

```csharp
builder.Services.AddWhyDidYouRender(config =>
{
    // Core settings (work everywhere)
    config.Enabled = true;
    config.Verbosity = TrackingVerbosity.Normal;
    config.TrackParameterChanges = true;
    config.TrackPerformance = true;
    config.IncludeSessionInfo = true;

    // Output adapts automatically:
    config.Output = TrackingOutput.Both;
    // Server/SSR: Console + Browser
    // WASM: Browser only (console not available)

    // Environment detection (usually auto)
    config.AutoDetectEnvironment = true;

    // WASM-specific settings (ignored in server environments)
    config.WasmStorageEnabled = true;
    config.WasmStorageOptions = new WasmStorageOptions
    {
        UseSessionStorage = false, // Use localStorage by default
        StorageKeyPrefix = "WhyDidYouRender_"
    };
});
```

### Environment-Specific Configuration

#### 🖥️ Server/SSR Optimized
```csharp
builder.Services.AddWhyDidYouRender(config =>
{
    config.Enabled = true;
    config.Output = TrackingOutput.Both; // Server console + browser
    config.Verbosity = TrackingVerbosity.Verbose;
    config.TrackPerformance = true;
    config.IncludeSessionInfo = true;
});
```

#### 🌐 WASM Optimized
```csharp
builder.Services.AddWhyDidYouRender(config =>
{
    config.Enabled = true;
    config.Output = TrackingOutput.BrowserConsole; // Browser only
    config.Verbosity = TrackingVerbosity.Normal;
    config.WasmStorageEnabled = true;
    config.TrackPerformance = true;
});
```

### Advanced Configuration

```csharp
builder.Services.AddWhyDidYouRender(config =>
{
    // Force specific environment (overrides auto-detection)
    config.ForceHostingModel = BlazorHostingModel.WebAssembly;

    // Performance tracking
    config.TrackPerformance = true;

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

## 🔄 Migration from v1.x

### Breaking Changes in v2.0

1. **Removed Error Diagnostics Endpoint** (incompatible with WASM)
   - `UseWhyDidYouRenderDiagnostics()` method removed
   - Use browser console logging instead

2. **New Initialization Methods**
   - Server: `app.Services.InitializeSSRServices()`
   - WASM: `await host.Services.InitializeWasmServices()`

3. **Configuration Changes**
   - Added `WasmStorageEnabled` and `WasmStorageOptions`
   - Added `AutoDetectEnvironment` and `ForceHostingModel`

### Migration Steps

#### From v1.x Server Setup:
```csharp
// v1.x (OLD)
builder.Services.AddWhyDidYouRender();
app.UseWhyDidYouRenderDiagnostics("/diagnostics"); // REMOVED

// v2.0 (NEW)
builder.Services.AddWhyDidYouRender(config => { /* same config */ });
app.Services.InitializeSSRServices(); // NEW
```

#### Component Changes:
```csharp
// v1.x and v2.0 - NO CHANGES NEEDED
@inherits TrackedComponentBase // Still works!
```

### New Features in v2.0
- ✅ **Full WASM Support** - Works in all Blazor hosting models
- ✅ **Automatic Environment Detection** - No manual configuration needed
- ✅ **Cross-Platform Session Management** - Adapts to environment
- ✅ **Smart Console Logging** - Server console + browser console
- ✅ **Browser Storage Support** - localStorage/sessionStorage in WASM

## 🚧 Roadmap

- **Testing Suite**: Comprehensive test coverage for cross-platform scenarios
- **Performance Profiler**: Advanced performance analysis tools
- **Custom Formatters**: Extensible output formatting
- **Real-time Dashboard**: Web-based monitoring dashboard

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details.

## 📄 License

This project is licensed under the GNU General Public License v3.0 or later - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

Inspired by the React [why-did-you-render](https://github.com/welldone-software/why-did-you-render) library.
