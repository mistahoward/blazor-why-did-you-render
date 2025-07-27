# Migration Guide: v1.x to v2.0

This guide helps you migrate from WhyDidYouRender v1.x to v2.0, which introduces cross-platform support for Blazor Server, WebAssembly, and SSR.

## 🚨 Breaking Changes Overview

### Major Changes in v2.0

1. **Cross-Platform Architecture**: New service abstraction layer
2. **Initialization Changes**: New initialization methods required
3. **Configuration Updates**: New cross-platform configuration options
4. **Removed Features**: Error diagnostics endpoint removed for WASM compatibility
5. **Package Dependencies**: Updated for cross-platform support

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

### Removed Configuration Options

```csharp
// REMOVED in v2.0 - No longer available
config.EnableDiagnosticsEndpoint = true;  // ❌ Removed for WASM compatibility
config.DiagnosticsPath = "/diagnostics";  // ❌ Removed for WASM compatibility
```

### New Configuration Options

```csharp
// NEW in v2.0 - Cross-platform options
config.AutoDetectEnvironment = true;                    // Auto-detect hosting model
config.ForceHostingModel = BlazorHostingModel.Server;   // Force specific model
config.Output = TrackingOutput.Both;                    // Both server and browser console
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

### Component Tracking (No Changes)

```csharp
// Same in both v1.x and v2.0
@using Blazor.WhyDidYouRender.Components
@inherits TrackedComponentBase

<h3>My Component</h3>
<p>Count: @Count</p>

@code {
    [Parameter] public int Count { get; set; }
}
```

## 🔍 API Changes

### Removed APIs

```csharp
// REMOVED in v2.0
app.UseWhyDidYouRenderDiagnostics();  // ❌ Diagnostics endpoint removed
```

### New APIs

```csharp
// NEW in v2.0 - Cross-platform services
IHostingEnvironmentDetector detector;
ISessionContextService sessionService;
ITrackingLogger trackingLogger;
IErrorTracker errorTracker; // Updated interface
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

## 🛠️ Step-by-Step Migration

### Step 1: Update Package

```bash
dotnet remove package Blazor.WhyDidYouRender
dotnet add package Blazor.WhyDidYouRender --version 2.0.0
```

### Step 2: Update Service Registration

```csharp
// Remove any diagnostics configuration
builder.Services.AddWhyDidYouRender(config =>
{
    // Remove these lines:
    // config.EnableDiagnosticsEndpoint = true;  // ❌
    // config.DiagnosticsPath = "/diagnostics";  // ❌
    
    // Update output configuration:
    config.Output = TrackingOutput.Both; // ✅ New option
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

### Step 5: Test and Verify

1. Build and run your application
2. Check console output for initialization messages
3. Verify tracking still works as expected
4. Test browser console output (if enabled)

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
