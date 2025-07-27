using Blazor.WhyDidYouRender.Extensions;
using Blazor.WhyDidYouRender.Configuration;

var builder = WebApplication.CreateBuilder(args);

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

	// enable unnecessary re-render detection
	config.DetectUnnecessaryRerenders = true;
	config.HighlightUnnecessaryRerenders = true;
	config.FrequentRerenderThreshold = 3.0; // lower threshold for demo

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
