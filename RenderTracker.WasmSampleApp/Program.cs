using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using RenderTracker.WasmSampleApp;
using Blazor.WhyDidYouRender.Extensions;
using Blazor.WhyDidYouRender.Configuration;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Add WhyDidYouRender with WASM-optimized configuration
builder.Services.AddWhyDidYouRender(config => {
	config.Enabled = true;
	config.Verbosity = TrackingVerbosity.Verbose;
	config.Output = TrackingOutput.BrowserConsole; // WASM-specific: use browser console
	config.TrackParameterChanges = true;
	config.TrackPerformance = true;
	config.IncludeSessionInfo = true;
	config.AutoDetectEnvironment = true; // Let it auto-detect WASM

	// Configure WASM-specific storage options
	config.WasmStorage.UseLocalStorage = true;
	config.WasmStorage.UseSessionStorage = true;
	config.WasmStorage.MaxStoredErrors = 50;
	config.WasmStorage.MaxStoredSessions = 5;
	config.WasmStorage.AutoCleanupStorage = true;
	config.WasmStorage.StorageCleanupIntervalMinutes = 30;
});

var app = builder.Build();

// Initialize WhyDidYouRender for WASM environment
await app.Services.InitializeWasmAsync(app.Services.GetRequiredService<Microsoft.JSInterop.IJSRuntime>());

await app.RunAsync();
