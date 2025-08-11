using Blazor.WhyDidYouRender.Extensions;
using Blazor.WhyDidYouRender.Configuration;
using Blazor.WhyDidYouRender.Core;

var builder = WebApplication.CreateBuilder(args);

// Wire .NET Aspire service defaults including OpenTelemetry (logs/traces/metrics)
builder.AddServiceDefaults();

// add session support for WhyDidYouRender
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options => {
	options.IdleTimeout = TimeSpan.FromMinutes(30);
	options.Cookie.HttpOnly = true;
	options.Cookie.IsEssential = true;
});

// add services to the container
builder.Services.AddRazorComponents()
	.AddInteractiveServerComponents();

// add background maintenance service for render tracking
builder.Services.AddHostedService<RenderTracker.SampleApp.Services.RenderTrackingMaintenanceService>();

// add WhyDidYouRender with configuration
builder.Services.AddWhyDidYouRender(config => {
	config.Enabled = true;
	config.Verbosity = TrackingVerbosity.Verbose;
	config.Output = TrackingOutput.Both; // both console and browser for testing
	config.TrackParameterChanges = true;
	config.TrackPerformance = true;
	config.IncludeSessionInfo = true;
	config.LogOnlyWhenParametersChange = false;
	config.MaxParameterChangesToLog = 5;

	// Enable Aspire/OpenTelemetry surfaces in the sample
	config.EnableOpenTelemetry = true;
	config.EnableOtelLogs = true;
	config.EnableOtelTraces = true;
	config.EnableOtelMetrics = true;
	// Optional: limit cardinality during demos
	// config.ComponentWhitelist = new(["Counter", "ComplexObjectDemo", "Home"]);

	// enable unnecessary re-render detection
	config.DetectUnnecessaryRerenders = true;
	config.HighlightUnnecessaryRerenders = true;
	config.FrequentRerenderThreshold = 3.0; // lower threshold for demo

	// enable state tracking for advanced analysis
	config.EnableStateTracking = true;
	config.AutoTrackSimpleTypes = true;
	config.MaxTrackedFieldsPerComponent = 25;
	config.LogStateChanges = true;
	config.LogDetailedStateChanges = true;
	config.TrackInheritedFields = true;
	config.MaxStateComparisonDepth = 3;
	config.EnableCollectionContentTracking = true;
	config.MaxTrackedComponents = 200;
	config.StateSnapshotCleanupIntervalMinutes = 5;
	config.MaxStateSnapshotAgeMinutes = 15;

	// SSR-specific settings (for demo - in production, be more restrictive)
	config.IncludeUserInfo = true;
	config.IncludeClientInfo = true;
	config.TrackDuringPrerendering = true;
	config.TrackDuringHydration = true;
	config.MaxConcurrentSessions = 100; // lower for demo
	config.SessionCleanupIntervalMinutes = 5; // more frequent cleanup for demo
	config.EnableSecurityMode = false; // disabled for demo

	// example filtering - exclude system components
	config.ExcludeNamespaces = ["Microsoft.*", "System.*"];
});

var app = builder.Build();

// Initialize SSR services for WhyDidYouRender
app.Services.InitializeSSRServices();

// Initialize state tracking for better performance (using previously unused methods!)
await InitializeRenderTrackingAsync(app.Services);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment()) {
	app.UseExceptionHandler("/Error", createScopeForErrors: true);
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}

app.UseHttpsRedirection();

// Enable session middleware for WhyDidYouRender
app.UseSession();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<RenderTracker.SampleApp.Component.App>()
	.AddInteractiveServerRenderMode();

// Error diagnostics endpoints removed for WASM compatibility

app.Run();

/// <summary>
/// Initializes render tracking with performance optimizations using previously unused methods.
/// </summary>
static async Task InitializeRenderTrackingAsync(IServiceProvider services) {
	try {
		var renderTracker = RenderTrackerService.Instance;

		// Use the unused InitializeStateTrackingAsync method for better startup performance!
		await renderTracker.InitializeStateTrackingAsync();
		Console.WriteLine("[WhyDidYouRender] State tracking initialized asynchronously");

		// Pre-warm the cache with common component types using the unused PreWarmStateTrackingCacheAsync method!
		var commonComponentTypes = new[]
		{
			typeof(RenderTracker.SampleApp.Components.Pages.Home),
			typeof(RenderTracker.SampleApp.Components.Pages.Counter),
			typeof(RenderTracker.SampleApp.Components.Pages.StateTrackingDemo),
			typeof(RenderTracker.SampleApp.Components.Pages.CrossPlatformDemo),
			typeof(RenderTracker.SampleApp.Components.Pages.Weather),
			typeof(RenderTracker.SampleApp.Components.Pages.Diagnostics),
			typeof(Blazor.WhyDidYouRender.Components.TrackedComponentBase)
		};

		await renderTracker.PreWarmStateTrackingCacheAsync(commonComponentTypes);
		Console.WriteLine($"[WhyDidYouRender] Cache pre-warmed with {commonComponentTypes.Length} component types");

		// Get initial diagnostics to verify everything is working
		var diagnostics = renderTracker.GetStateTrackingDiagnostics();
		if (diagnostics != null) {
			Console.WriteLine($"[WhyDidYouRender] State tracking diagnostics: Enabled={diagnostics.IsEnabled}, Initialized={diagnostics.IsInitialized}");
		}
	}
	catch (Exception ex) {
		Console.WriteLine($"[WhyDidYouRender] Warning: Failed to initialize state tracking optimizations: {ex.Message}");
	}
}
