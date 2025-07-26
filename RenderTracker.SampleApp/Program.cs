using Blazor.WhyDidYouRender.Tracking;

var builder = WebApplication.CreateBuilder(args);

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

	// Example filtering - exclude system components
	config.ExcludeNamespaces = new List<string> { "Microsoft.*", "System.*" };

	// Example: Only track our demo components
	// config.IncludeComponents = new List<string> { "Counter", "Home", "TrackedChildComponent", "Weather" };
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment()) {
	app.UseExceptionHandler("/Error", createScopeForErrors: true);
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<RenderTracker.SampleApp.Component.App>()
	.AddInteractiveServerRenderMode();

app.Run();
