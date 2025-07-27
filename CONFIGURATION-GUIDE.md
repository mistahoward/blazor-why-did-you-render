# Blazor WhyDidYouRender - Configuration Guide v2.0

This guide provides comprehensive configuration instructions for WhyDidYouRender across all Blazor hosting environments.

## 🌐 Cross-Platform Overview

WhyDidYouRender v2.0 automatically adapts to your Blazor hosting environment:

- **🖥️ Blazor Server** - Full server-side tracking with HttpContext session management
- **🌐 Blazor WebAssembly** - Browser-based tracking with localStorage session management  
- **📄 Server-Side Rendering (SSR)** - Pre-render tracking with server-side optimization

## 🚀 Quick Start

### 1. Install Package

```bash
dotnet add package Blazor.WhyDidYouRender
```

### 2. Basic Configuration

**For any Blazor project:**
```csharp
builder.Services.AddWhyDidYouRender(config =>
{
    config.Enabled = true;
    config.Verbosity = TrackingVerbosity.Normal;
    config.Output = TrackingOutput.Both; // Server console AND browser console
});
```

### 3. Initialize (Choose Your Method)

**Option A: Auto-Detection (Recommended)**
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

**Option B: Environment-Specific**
```csharp
// Blazor Server
await app.Services.InitializeServerAsync();

// Blazor WebAssembly  
await host.Services.InitializeWasmAsync(jsRuntime);
```

## 🔧 Environment-Specific Setup

### Blazor Server Configuration

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add Blazor services
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Add session support (required for server-side tracking)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add WhyDidYouRender
builder.Services.AddWhyDidYouRender(config =>
{
    config.Enabled = builder.Environment.IsDevelopment();
    config.Verbosity = TrackingVerbosity.Normal;
    config.Output = TrackingOutput.Both; // Server console AND browser console
    config.TrackParameterChanges = true;
    config.TrackPerformance = true;
    config.IncludeSessionInfo = true;
});

var app = builder.Build();

// Configure middleware
app.UseSession(); // Required for server-side session tracking
app.UseStaticFiles();
app.UseRouting();

app.MapRazorPages();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

// Initialize WhyDidYouRender for Server environment
await app.Services.InitializeServerAsync();

app.Run();
```

### Blazor WebAssembly Configuration

```csharp
var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Add HTTP client
builder.Services.AddScoped(sp => new HttpClient 
{ 
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) 
});

// Add WhyDidYouRender
builder.Services.AddWhyDidYouRender(config =>
{
    config.Enabled = true; // Always enabled in WASM for development
    config.Verbosity = TrackingVerbosity.Normal;
    config.Output = TrackingOutput.BrowserConsole; // Browser console only
    config.TrackParameterChanges = true;
    config.TrackPerformance = true;
    
    // WASM-specific settings
    config.IncludeSessionInfo = true; // Uses browser storage
    config.MaxParameterChangesToLog = 5; // Reduce for performance
});

var host = builder.Build();

// Initialize WhyDidYouRender for WASM environment
await host.Services.InitializeWasmAsync(host.Services.GetRequiredService<IJSRuntime>());

await host.RunAsync();
```

### Server-Side Rendering (SSR) Configuration

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add Blazor services
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add WhyDidYouRender
builder.Services.AddWhyDidYouRender(config =>
{
    config.Enabled = builder.Environment.IsDevelopment();
    config.Verbosity = TrackingVerbosity.Minimal; // Reduced for SSR performance
    config.Output = TrackingOutput.Console; // Server console only for SSR
    config.TrackParameterChanges = false; // Disable for SSR performance
    config.TrackPerformance = true;
    config.IncludeSessionInfo = false; // Disable for SSR privacy
});

var app = builder.Build();

// Configure middleware
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Initialize WhyDidYouRender for SSR environment
app.Services.InitializeSSRServices();

app.Run();
```

## ⚙️ Configuration Properties

### Core Settings

```csharp
config.Enabled = true;                              // Enable/disable tracking
config.Verbosity = TrackingVerbosity.Normal;        // Minimal, Normal, Verbose
config.Output = TrackingOutput.Both;                // Console, BrowserConsole, Both
config.TrackParameterChanges = true;                // Track parameter changes
config.TrackPerformance = true;                     // Track render performance
config.IncludeSessionInfo = true;                   // Include session information
```

### Filtering & Performance

```csharp
// Component filtering
config.IncludeComponents = new[] { "Counter*", "*Important*" };
config.ExcludeComponents = new[] { "System.*", "*Layout*" };

// Namespace filtering  
config.IncludeNamespaces = new[] { "MyApp.Components.*" };
config.ExcludeNamespaces = new[] { "Microsoft.*", "System.*" };

// Performance settings
config.MaxParameterChangesToLog = 10;               // Limit parameter change logs
config.LogOnlyWhenParametersChange = false;         // Log all OnParametersSet calls
config.FrequentRerenderThreshold = 5.0;            // Renders per second threshold
```

### Advanced Detection

```csharp
// Unnecessary re-render detection
config.DetectUnnecessaryRerenders = true;           // Detect unnecessary re-renders
config.HighlightUnnecessaryRerenders = true;        // Highlight in browser console

// Environment control
config.AutoDetectEnvironment = true;                // Auto-detect hosting model
config.ForceHostingModel = BlazorHostingModel.Server; // Force specific model
```

## 🎯 Environment-Specific Recommendations

### Development Environment

```csharp
config.Enabled = true;
config.Verbosity = TrackingVerbosity.Verbose;
config.Output = TrackingOutput.Both;
config.TrackParameterChanges = true;
config.TrackPerformance = true;
config.DetectUnnecessaryRerenders = true;
config.HighlightUnnecessaryRerenders = true;
```

### Staging Environment

```csharp
config.Enabled = true;
config.Verbosity = TrackingVerbosity.Normal;
config.Output = TrackingOutput.Console;
config.TrackParameterChanges = true;
config.TrackPerformance = true;
config.DetectUnnecessaryRerenders = false;
config.IncludeSessionInfo = false; // Privacy
```

### Production Environment

```csharp
config.Enabled = false; // Always disable in production
```

## 🔍 Component Tracking

### Automatic Tracking (Recommended)

```csharp
@using Blazor.WhyDidYouRender.Components
@inherits TrackedComponentBase

<h3>My Component</h3>
<p>Count: @Count</p>

@code {
    [Parameter] public int Count { get; set; }
    // Tracking happens automatically!
}
```

### Selective Tracking

Only track components you're actively optimizing:

```csharp
// Track only specific components
config.IncludeComponents = new[] { "Counter", "WeatherForecast", "DataGrid*" };

// Exclude system components
config.ExcludeComponents = new[] { "Microsoft.*", "System.*", "*Layout*" };
```

## 📊 Output Configuration

### Console Output Only (Server)

```csharp
config.Output = TrackingOutput.Console;
```

### Browser Console Only (WASM)

```csharp
config.Output = TrackingOutput.BrowserConsole;
```

### Both Outputs (Hybrid)

```csharp
config.Output = TrackingOutput.Both;
```

## 🛡️ Privacy & Security

### Development vs Production

```csharp
// Development
config.IncludeSessionInfo = true;
config.IncludeUserInfo = true;
config.IncludeClientInfo = true;

// Production
config.Enabled = false; // Disable entirely
```

### GDPR Compliance

```csharp
config.IncludeUserInfo = false;        // No user identification
config.IncludeClientInfo = false;      // No IP/User-Agent
config.IncludeSessionInfo = false;     // No session tracking
```

## 🔧 Advanced Configuration

### Custom Initialization

```csharp
// Manual environment detection
var detector = serviceProvider.GetService<IHostingEnvironmentDetector>();
if (detector?.IsClientSide == true)
{
    await serviceProvider.InitializeWasmAsync(jsRuntime);
}
else
{
    await serviceProvider.InitializeServerAsync();
}
```

### Configuration from appsettings.json

```json
{
  "WhyDidYouRender": {
    "Enabled": true,
    "Verbosity": "Normal",
    "Output": "Both",
    "TrackParameterChanges": true,
    "TrackPerformance": true,
    "IncludeSessionInfo": false,
    "ExcludeComponents": ["Microsoft.*", "System.*"]
  }
}
```

```csharp
// Bind from configuration
builder.Services.AddWhyDidYouRender(builder.Configuration.GetSection("WhyDidYouRender"));
```

## 🚨 Troubleshooting

### Common Issues

1. **No output in browser console**
   - Ensure `TrackingOutput.BrowserConsole` or `TrackingOutput.Both`
   - Call `InitializeWhyDidYouRenderAsync(JSRuntime)` in a component

2. **No tracking in WASM**
   - Ensure `InitializeWasmAsync()` is called
   - Check browser console for initialization messages

3. **Performance issues**
   - Reduce `MaxParameterChangesToLog`
   - Set `LogOnlyWhenParametersChange = true`
   - Use component filtering

4. **Session errors in Server**
   - Add `app.UseSession()` middleware
   - Configure session services

### Debug Configuration

```csharp
config.Verbosity = TrackingVerbosity.Verbose;
config.Output = TrackingOutput.Both;
// Check console for detailed initialization messages
```

## 📚 Next Steps

- See [API-DOCUMENTATION.md](API-DOCUMENTATION.md) for detailed API reference
- See [README.md](README.md) for usage examples
- See [EXAMPLES-AND-BEST-PRACTICES.md](EXAMPLES-AND-BEST-PRACTICES.md) for optimization tips
