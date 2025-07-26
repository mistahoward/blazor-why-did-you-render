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
    config.LogLevel = LogLevel.Warning;
    config.IncludeProps = true;
    config.IncludeState = true;
});

var app = builder.Build();

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

#### Step 4: Update Components
Update `Pages/Counter.razor`:
```csharp
@page "/counter"
@using Blazor.WhyDidYouRender.Components
@inherits TrackedComponentBase

<PageTitle>Counter</PageTitle>

<h1>Counter</h1>

<p role="status">Current count: @currentCount</p>

<button class="btn btn-primary" @onclick="IncrementCount">Click me</button>

@code {
    private int currentCount = 0;

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
    config.LogLevel = LogLevel.Warning;
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
    config.LogLevel = LogLevel.Warning;
});

await builder.Build().RunAsync();
```

## ⚙️ Configuration Examples

### Development Configuration
```csharp
builder.Services.AddWhyDidYouRender(config =>
{
    config.Enabled = true;
    config.LogLevel = LogLevel.Debug;
    config.IncludeProps = true;
    config.IncludeState = true;
    config.TrackHooks = true;
    config.TrackPerformance = true;
    config.HotReloadMode = true;
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
    config.LogLevel = LogLevel.Error; // Only critical issues
    config.TrackPerformance = true;
    config.LogOnDifferentValues = true;
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
            LogLevel = env.IsDevelopment() ? LogLevel.Debug : LogLevel.Warning,
            IncludeProps = true,
            IncludeState = env.IsDevelopment(),
            TrackPerformance = true,
            HotReloadMode = env.IsDevelopment()
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

### Diagnostics Endpoint Integration
```csharp
// In Program.cs, after app.UseRouting()
if (app.Environment.IsDevelopment())
{
    app.UseWhyDidYouRenderDiagnostics("/dev/renders");
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

### Issue 2: No Console Output
**Problem**: Not seeing any tracking information in browser console.

**Solutions**:
1. Check that tracking is enabled: `config.Enabled = true`
2. Verify log level: `config.LogLevel = LogLevel.Debug`
3. Ensure components inherit from `TrackedComponentBase`
4. Open browser developer tools console

### Issue 3: Too Much Logging
**Problem**: Console is flooded with tracking information.

**Solutions**:
1. Increase log level: `config.LogLevel = LogLevel.Warning`
2. Enable `LogOnDifferentValues = true`
3. Disable in production: `config.Enabled = Environment.IsDevelopment()`

### Issue 4: Performance Impact
**Problem**: Tracking is affecting application performance.

**Solutions**:
1. Disable in production environments
2. Use selective component tracking
3. Adjust tracking granularity
4. Consider using diagnostics endpoint instead of console logging

## 📊 Monitoring & Debugging

### Browser Console Usage
1. Open Developer Tools (F12)
2. Navigate to Console tab
3. Look for `[WhyDidYouRender]` messages
4. Use console filters to focus on specific components

### Diagnostics Endpoint Usage
1. Enable diagnostics endpoint in configuration
2. Navigate to `/diagnostics/renders` (or your configured path)
3. View aggregated render statistics
4. Export data for analysis

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
5. Consider enabling diagnostics endpoint for ongoing monitoring
