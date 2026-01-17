using Blazor.WhyDidYouRender.Configuration;
using Blazor.WhyDidYouRender.Core;
using Blazor.WhyDidYouRender.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add session support for WhyDidYouRender - CRITICAL for Interactive Server mode
// This reproduces the issue: session middleware is configured, but session access
// during OnInitialized() in prerendering will fail with "The session cannot be
// established after the response has started" without the fix.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
	options.IdleTimeout = TimeSpan.FromMinutes(30);
	options.Cookie.HttpOnly = true;
	options.Cookie.IsEssential = true;
});

// Add services to the container - Interactive Server mode
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

// Add WhyDidYouRender with session tracking enabled (reproduces the bug)
builder.Services.AddWhyDidYouRender(config =>
{
	config.Enabled = true;
	config.Verbosity = TrackingVerbosity.Verbose;
	config.Output = TrackingOutput.Both; // Both console and browser for testing
	config.TrackParameterChanges = true;
	config.TrackPerformance = true;

	// CRITICAL: IncludeSessionInfo = true triggers session access during prerendering
	// This was causing "The session cannot be established after the response has started" errors
	// The fix in ServerSessionContextService now handles this with Response.HasStarted guard
	config.IncludeSessionInfo = true;

	// Enable state tracking
	config.EnableStateTracking = true;
	config.AutoTrackSimpleTypes = true;
	config.LogStateChanges = true;
	config.LogDetailedStateChanges = true;
	config.TrackInheritedFields = true;
	config.MaxStateComparisonDepth = 3;
	config.EnableCollectionContentTracking = true;

	// Track during prerendering - this is where the bug manifests
	// During prerendering, OnInitialized() is called before Response.HasStarted
	// and session access would throw an exception
	config.TrackDuringPrerendering = true;
	config.TrackDuringHydration = true;

	// Session-related settings
	config.IncludeUserInfo = true;
	config.IncludeClientInfo = true;
	config.MaxConcurrentSessions = 100;
	config.SessionCleanupIntervalMinutes = 5;

	// Filtering
	config.ExcludeNamespaces = ["Microsoft.*", "System.*"];
});

var app = builder.Build();

// Initialize SSR services for WhyDidYouRender
app.Services.InitializeSSRServices();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Error", createScopeForErrors: true);
	app.UseHsts();
}

app.UseHttpsRedirection();

// CRITICAL: Enable session middleware BEFORE MapRazorComponents
// This was already correct in the original issue - the problem was the timing
// of session ACCESS, not the middleware configuration
app.UseSession();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<RenderTracker.InteractiveServerNet10.Components.App>().AddInteractiveServerRenderMode();

Console.WriteLine("=================================================================");
Console.WriteLine("RenderTracker.InteractiveServerNet10 - Session Timing Test");
Console.WriteLine("=================================================================");
Console.WriteLine("This sample demonstrates the session timing fix in Interactive");
Console.WriteLine("Server mode. Previously, accessing session during OnInitialized()");
Console.WriteLine("would throw: 'The session cannot be established after the response");
Console.WriteLine("has started.'");
Console.WriteLine("");
Console.WriteLine("The fix adds Response.HasStarted check and fallback to TraceIdentifier.");
Console.WriteLine("=================================================================");

app.Run();
