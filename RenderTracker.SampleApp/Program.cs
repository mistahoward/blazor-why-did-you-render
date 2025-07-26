using Blazor.WhyDidYouRender.Tracking;

var builder = WebApplication.CreateBuilder(args);

// Add session support for WhyDidYouRender
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options => {
	options.IdleTimeout = TimeSpan.FromMinutes(30);
	options.Cookie.HttpOnly = true;
	options.Cookie.IsEssential = true;
});

// Add services to the container.
builder.Services.AddRazorComponents()
	.AddInteractiveServerComponents();

// Add WhyDidYouRender with configuration
builder.Services.AddWhyDidYouRender(config => {
	config.Enabled = true;
	config.Verbosity = TrackingVerbosity.Verbose;
	config.Output = TrackingOutput.Both; // Both console and browser
	config.TrackParameterChanges = true;
	config.TrackPerformance = true;
	config.IncludeSessionInfo = true;
	config.LogOnlyWhenParametersChange = false;
	config.MaxParameterChangesToLog = 5;

	// Enable unnecessary re-render detection
	config.DetectUnnecessaryRerenders = true;
	config.HighlightUnnecessaryRerenders = true;
	config.FrequentRerenderThreshold = 3.0; // Lower threshold for demo

	// SSR-specific settings (for demo - in production, be more restrictive)
	config.IncludeUserInfo = true; // Enable for demo
	config.IncludeClientInfo = true; // Enable for demo
	config.TrackDuringPrerendering = true;
	config.TrackDuringHydration = true;
	config.MaxConcurrentSessions = 100; // Lower for demo
	config.SessionCleanupIntervalMinutes = 5; // More frequent cleanup for demo
	config.EnableSecurityMode = false; // Disabled for demo

	// Example filtering - exclude system components
	config.ExcludeNamespaces = new List<string> { "Microsoft.*", "System.*" };

	// Example: Only track our demo components
	// config.IncludeComponents = new List<string> { "Counter", "Home", "TrackedChildComponent", "Weather" };
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

app.Run();
