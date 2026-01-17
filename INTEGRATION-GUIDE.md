# Blazor WhyDidYouRender - Integration Guide

This guide provides step-by-step instructions for integrating WhyDidYouRender into new and existing Blazor applications.

## 🎯 Integration Scenarios

### Scenario 1: New Blazor Server App

#### Step 1: Create New Project
```bash
dotnet new blazorserver -n MyBlazorApp
cd MyBlazorApp
```

#### Step 2: Install Package
```bash
dotnet add package Blazor.WhyDidYouRender
```

#### Step 3: Configure Services
Update `Program.cs`:
```csharp
using Blazor.WhyDidYouRender.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Add WhyDidYouRender
builder.Services.AddWhyDidYouRender(config =>
{
    config.Enabled = builder.Environment.IsDevelopment();
    config.Verbosity = TrackingVerbosity.Normal;
    config.Output = TrackingOutput.Both;
    config.TrackParameterChanges = true;
    config.TrackPerformance = true;
});

var app = builder.Build();

// Initialize WhyDidYouRender SSR services
app.Services.InitializeSSRServices();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapRazorPages();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
```

#### Optional: Enable .NET Aspire / OpenTelemetry

```csharp
// Add Aspire service defaults
builder.AddServiceDefaults();

builder.Services.AddWhyDidYouRender(config =>
{
    config.EnableOpenTelemetry = true;
    config.EnableOtelLogs = true;
    config.EnableOtelTraces = true;
    config.EnableOtelMetrics = true;
});
```

See [docs/observability.md](docs/observability.md) for verification steps and troubleshooting.

#### Step 4: Update Components
Update `Pages/Counter.razor`:
```csharp
@page "/counter"
@using Blazor.WhyDidYouRender.Components
@using Blazor.WhyDidYouRender.Extensions
@inherits TrackedComponentBase
@inject IJSRuntime JSRuntime
@inject IServiceProvider ServiceProvider

<PageTitle>Counter</PageTitle>

<h1>Counter</h1>

<p role="status">Current count: @currentCount</p>

<button class="btn btn-primary" @onclick="IncrementCount">Click me</button>

@code {
    private int currentCount = 0;
    private bool browserLoggerInitialized = false;

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

### Scenario 2: Existing Blazor Server App

#### Step 1: Install Package
```bash
dotnet add package Blazor.WhyDidYouRender
```

#### Step 2: Add Service Registration
In your existing `Program.cs`, add the service registration:
```csharp
// Add this after your existing service registrations
builder.Services.AddWhyDidYouRender(config =>
{
    config.Enabled = builder.Environment.IsDevelopment();
    config.Verbosity = TrackingVerbosity.Normal;
    config.Output = TrackingOutput.Both;
    config.TrackParameterChanges = true;
});
```

#### Step 3: Gradually Migrate Components
Start with components you want to optimize:
```csharp
// Before
@inherits ComponentBase

// After
@using Blazor.WhyDidYouRender.Components
@inherits TrackedComponentBase
```

### Scenario 3: Blazor WebAssembly App

#### Step 1: Install Package
```bash
dotnet add package Blazor.WhyDidYouRender
```

#### Step 2: Configure Services
Update `Program.cs`:
```csharp
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Blazor.WhyDidYouRender.Extensions;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Add WhyDidYouRender
builder.Services.AddWhyDidYouRender(config =>
{
    config.Enabled = builder.HostEnvironment.IsDevelopment();
    config.Verbosity = TrackingVerbosity.Normal;
    config.Output = TrackingOutput.BrowserConsole; // WebAssembly only supports browser console
    config.TrackParameterChanges = true;
    config.TrackPerformance = true;
});

await builder.Build().RunAsync();
```

### Scenario 4: Blazor Interactive Server (.NET 8+)

#### Overview
Interactive Server mode (introduced in .NET 8) uses both prerendering and SignalR-based interactive rendering. This creates unique timing considerations for session management.

#### Step 1: Install Package
```bash
dotnet add package Blazor.WhyDidYouRender
```

#### Step 2: Configure Session Middleware
Session middleware is required for server-side tracking and **must be configured before mapping components**:

```csharp
using Blazor.WhyDidYouRender.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add session support (required for server-side session tracking)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add Razor components with Interactive Server render mode
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add WhyDidYouRender with session tracking
builder.Services.AddWhyDidYouRender(config =>
{
    config.Enabled = builder.Environment.IsDevelopment();
    config.Verbosity = TrackingVerbosity.Normal;
    config.Output = TrackingOutput.Both;
    config.TrackParameterChanges = true;
    config.TrackPerformance = true;
    
    // Session tracking configuration
    config.IncludeSessionInfo = true;          // Enable session ID tracking
    config.TrackDuringPrerendering = true;     // Track during prerender phase
    config.TrackDuringHydration = true;        // Track during hydration
});

var app = builder.Build();

// Initialize SSR services for WhyDidYouRender
app.Services.InitializeSSRServices();

// Configure middleware pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseSession();  // CRITICAL: Must be called before MapRazorComponents
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
```

#### Step 3: Update Components
Use `@rendermode InteractiveServer` directive:

```csharp
@page "/counter"
@rendermode InteractiveServer
@using Blazor.WhyDidYouRender.Components
@using Blazor.WhyDidYouRender.Attributes
@inherits TrackedComponentBase

<PageTitle>Counter</PageTitle>

<h1>Counter</h1>

<p role="status">Current count: @currentCount</p>

<button class="btn btn-primary" @onclick="IncrementCount">Click me</button>

@code {
    [TrackState]  // Enable state tracking for this field
    private int currentCount = 0;

    private void IncrementCount()
    {
        currentCount++;
    }
}
```

#### Understanding Session Timing in Interactive Server

**Important**: Interactive Server mode has a unique rendering lifecycle:

1. **Prerendering Phase** (Static SSR):
   - Component's `OnInitialized()` executes during initial HTTP request
   - Session access occurs **before HTTP response headers are sent**
   - WhyDidYouRender uses `Response.HasStarted` guard to handle this safely
   - **Session ID format**: `server-{TraceIdentifier}` (temporary, request-specific)

2. **Interactive Phase** (SignalR Circuit):
   - SignalR connection established after response is sent
   - Component rerenders with full interactivity
   - Session access works normally through SignalR connection
   - **Session ID format**: `server-{persistent-session-guid}` (persistent across requests)

#### Session ID Fallback Behavior

WhyDidYouRender automatically handles session timing:

```csharp
// During prerendering (Response.HasStarted == false):
// ✅ Session ID: "server-a1b2c3d4e5f6" (TraceIdentifier fallback)

// After circuit established:
// ✅ Session ID: "server-f47ac10b-58cc-4372-a567-0e02b2c3d479" (persistent)
```

This is **normal and expected behavior**, not an error. The session ID format changes during the component lifecycle, but tracking works correctly throughout.

#### Troubleshooting Interactive Server

**Error**: `InvalidOperationException: The session cannot be established after the response has started`

This error should not occur in v3.3.0+. If you see it:

1. Verify you're using Blazor.WhyDidYouRender v3.3.0 or later
2. Check that `UseSession()` is called in the middleware pipeline
3. Ensure `IncludeSessionInfo` configuration is not forcing session access at incorrect times

**Expected Console Output**:
```
[WhyDidYouRender] Response has started, using TraceIdentifier for session ID
[WhyDidYouRender] Tracking Counter component (OnInitialized) - Session: server-a1b2c3d4e5f6
```

This is normal during prerendering and indicates the fallback is working correctly.

## ⚙️ Configuration Examples

### Development Configuration
```csharp
builder.Services.AddWhyDidYouRender(config =>
{
    config.Enabled = true;
    config.Verbosity = TrackingVerbosity.Verbose;
    config.Output = TrackingOutput.Both;
    config.TrackParameterChanges = true;
    config.TrackPerformance = true;
    config.IncludeSessionInfo = true;
});
```

### Production Configuration
```csharp
builder.Services.AddWhyDidYouRender(config =>
{
    config.Enabled = false; // Disable in production
});
```

### Staging Configuration
```csharp
builder.Services.AddWhyDidYouRender(config =>
{
    config.Enabled = true;
    config.Verbosity = TrackingVerbosity.Minimal;
    config.Output = TrackingOutput.Console;
    config.TrackPerformance = true;
});
```

## 🔧 Advanced Integration

### Custom Configuration Provider
```csharp
public class WhyDidYouRenderConfigProvider
{
    public static WhyDidYouRenderConfig GetConfiguration(IWebHostEnvironment env)
    {
        return new WhyDidYouRenderConfig
        {
            Enabled = env.IsDevelopment() || env.IsStaging(),
            Verbosity = env.IsDevelopment() ? TrackingVerbosity.Verbose : TrackingVerbosity.Normal,
            Output = env.IsDevelopment() ? TrackingOutput.Both : TrackingOutput.Console,
            TrackParameterChanges = true,
            TrackPerformance = true,
            IncludeSessionInfo = env.IsDevelopment()
        };
    }
}

// Usage in Program.cs
builder.Services.AddWhyDidYouRender(
    WhyDidYouRenderConfigProvider.GetConfiguration(builder.Environment)
);
```

### Conditional Component Tracking
```csharp
@using Blazor.WhyDidYouRender.Components

@if (ShouldTrack)
{
    @inherits TrackedComponentBase
}
else
{
    @inherits ComponentBase
}

@code {
    private bool ShouldTrack =>
        Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
}
```



## 🎨 Component Migration Patterns

### Pattern 1: Gradual Migration
Start with high-traffic or problematic components:
```csharp
// Identify components with performance issues first
@inherits TrackedComponentBase // Add to specific components
```

### Pattern 2: Base Component Approach
Create a custom base component:
```csharp
// Create MyTrackedComponentBase.cs
public abstract class MyTrackedComponentBase : TrackedComponentBase
{
    // Add your common component logic here
}

// Use in components
@inherits MyTrackedComponentBase
```

### Pattern 3: Conditional Tracking
Use preprocessor directives for conditional tracking:
```csharp
#if DEBUG
@using Blazor.WhyDidYouRender.Components
@inherits TrackedComponentBase
#else
@inherits ComponentBase
#endif
```

## 🚨 Common Issues & Solutions

### Issue 1: Service Not Registered
**Error**: `InvalidOperationException: Unable to resolve service for type 'RenderTrackerService'`

**Solution**: Ensure you've called `AddWhyDidYouRender()` in your service registration:
```csharp
builder.Services.AddWhyDidYouRender();
```

### Issue 2: Session Timing Error (Interactive Server)
**Error**: `InvalidOperationException: The session cannot be established after the response has started`

**This issue is fixed in v3.3.0+**. If you still encounter it:

**Solutions**:
1. Update to Blazor.WhyDidYouRender v3.3.0 or later
2. Verify session middleware is configured:
   ```csharp
   builder.Services.AddSession();
   // ...
   app.UseSession();  // Must be before MapRazorComponents
   ```
3. The fix automatically handles session timing with `Response.HasStarted` guards
4. Session IDs temporarily use `TraceIdentifier` during prerendering (this is normal)

See [Scenario 4: Interactive Server](#scenario-4-blazor-interactive-server-net-8) for detailed explanation.

### Issue 3: No Console Output
**Problem**: Not seeing any tracking information in browser console.

**Solutions**:
1. Check that tracking is enabled: `config.Enabled = true`
2. Verify output is set to browser: `config.Output = TrackingOutput.Both`
3. Ensure components inherit from `TrackedComponentBase`
4. Initialize browser logging: `await ServiceProvider.InitializeWhyDidYouRenderAsync(JSRuntime)`
5. Open browser developer tools console

### Issue 4: Too Much Logging
**Problem**: Console is flooded with tracking information.

**Solutions**:
1. Reduce verbosity: `config.Verbosity = TrackingVerbosity.Minimal`
2. Disable parameter tracking: `config.TrackParameterChanges = false`
3. Disable in production: `config.Enabled = Environment.IsDevelopment()`

### Issue 5: Performance Impact
**Problem**: Tracking is affecting application performance.

**Solutions**:
1. Disable in production environments
2. Use selective component tracking
3. Adjust tracking granularity
4. Consider enabling .NET Aspire/OpenTelemetry for server-side observability

## 📊 Monitoring & Debugging

### Browser Console Usage
1. Open Developer Tools (F12)
2. Navigate to Console tab
3. Look for `[WhyDidYouRender]` messages
4. Use console filters to focus on specific components


## 🔄 Migration Checklist

- [ ] Install Blazor.WhyDidYouRender package
- [ ] Add service registration in Program.cs
- [ ] Configure environment-specific settings
- [ ] Update target components to inherit from TrackedComponentBase
- [ ] Test in development environment
- [ ] Verify console output
- [ ] Configure for staging/production environments
- [ ] Document component tracking decisions
- [ ] Train team on usage and interpretation

## 📚 Next Steps

After successful integration:
1. Monitor component render patterns
2. Identify unnecessary re-renders
3. Optimize component parameters and state management
4. Use insights to improve application performance
5. Consider enabling .NET Aspire/OpenTelemetry for ongoing monitoring
