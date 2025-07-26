using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.JSInterop;

using Blazor.WhyDidYouRender.Configuration;
using Blazor.WhyDidYouRender.Core;
using Blazor.WhyDidYouRender.Diagnostics;
using Blazor.WhyDidYouRender.Helpers;

namespace Blazor.WhyDidYouRender.Extensions;

/// <summary>
/// Extension methods for configuring WhyDidYouRender services.
/// </summary>
public static class ServiceCollectionExtensions {
	/// <summary>
	/// Adds WhyDidYouRender tracking services to the service collection.
	/// </summary>
	/// <param name="services">The service collection.</param>
	/// <param name="configuration">Optional configuration section for WhyDidYouRender settings.</param>
	/// <returns>The service collection for chaining.</returns>
	public static IServiceCollection AddWhyDidYouRender(
		this IServiceCollection services,
		IConfiguration? configuration = null) {
		services.AddHttpContextAccessor();
		services.AddSingleton<ISessionContextService, SessionContextService>();

		services.AddSingleton<IErrorTracker, ErrorTracker>();

		services.AddScoped<BrowserConsoleLogger>();

		if (configuration != null) {
			var config = new WhyDidYouRenderConfig();
			configuration.Bind(config);

			services.AddSingleton<WhyDidYouRenderConfig>(config);

			RenderTrackerService.Instance.Configure(config);
		}

		return services;
	}

	/// <summary>
	/// Adds WhyDidYouRender tracking services with custom configuration.
	/// </summary>
	/// <param name="services">The service collection.</param>
	/// <param name="configureOptions">Action to configure the tracking options.</param>
	/// <returns>The service collection for chaining.</returns>
	public static IServiceCollection AddWhyDidYouRender(
		this IServiceCollection services,
		Action<WhyDidYouRenderConfig> configureOptions) {
		services.AddHttpContextAccessor();
		services.AddSingleton<ISessionContextService, SessionContextService>();

		services.AddSingleton<IErrorTracker, ErrorTracker>();

		services.AddScoped<BrowserConsoleLogger>();

		var config = new WhyDidYouRenderConfig();
		configureOptions(config);

		services.AddSingleton<WhyDidYouRenderConfig>(config);

		RenderTrackerService.Instance.Configure(config);

		ConfigureSSRServices(services);

		return services;
	}

	/// <summary>
	/// Initializes WhyDidYouRender with browser console logging support.
	/// Call this method in a component's OnAfterRenderAsync to enable browser logging.
	/// </summary>
	/// <param name="serviceProvider">The service provider.</param>
	/// <param name="jsRuntime">The JavaScript runtime.</param>
	/// <returns>A task representing the initialization.</returns>
	public static async Task InitializeWhyDidYouRenderAsync(
		this IServiceProvider serviceProvider,
		IJSRuntime jsRuntime) {
		Console.WriteLine("[WhyDidYouRender] Attempting to initialize browser logging...");

		var browserLogger = serviceProvider.GetService<BrowserConsoleLogger>();
		if (browserLogger != null) {
			Console.WriteLine("[WhyDidYouRender] Browser logger service found, initializing...");
			await browserLogger.InitializeAsync();
			RenderTrackerService.Instance.SetBrowserLogger(browserLogger);
			Console.WriteLine("[WhyDidYouRender] Browser logger set on tracker service");
		}
		else {
			Console.WriteLine("[WhyDidYouRender] Browser logger service not found in DI container");
		}
	}

	/// <summary>
	/// Configures SSR-specific services for the render tracker.
	/// </summary>
	/// <param name="services">The service collection.</param>
	private static void ConfigureSSRServices(IServiceCollection services) {
		// This will be called during service registration to set up SSR services
		// The actual configuration happens when the service provider is built
	}

	/// <summary>
	/// Initializes SSR services after the service provider is built.
	/// Call this method in your application startup after building the service provider.
	/// </summary>
	/// <param name="serviceProvider">The built service provider.</param>
	public static void InitializeSSRServices(this IServiceProvider serviceProvider) {
		var tracker = RenderTrackerService.Instance;

		Console.WriteLine("[WhyDidYouRender] Initializing SSR services...");

		var errorTracker = serviceProvider.GetService<IErrorTracker>();
		if (errorTracker != null) {
			tracker.SetErrorTracker(errorTracker);
			Console.WriteLine("[WhyDidYouRender] Error tracker initialized successfully");
		}
		else {
			Console.WriteLine("[WhyDidYouRender] ERROR: Error tracker service not found!");
		}

		var sessionContextService = serviceProvider.GetService<ISessionContextService>();
		if (sessionContextService != null) {
			tracker.SetSessionContextService(sessionContextService);
			Console.WriteLine("[WhyDidYouRender] Session context service initialized successfully");
		}
		else {
			Console.WriteLine("[WhyDidYouRender] ERROR: Session context service not found!");
		}

		var hostEnvironment = serviceProvider.GetService<IHostEnvironment>();
		if (hostEnvironment != null) {
			tracker.SetHostEnvironment(hostEnvironment);
			Console.WriteLine("[WhyDidYouRender] Host environment initialized successfully");
		}
		else {
			Console.WriteLine("[WhyDidYouRender] ERROR: Host environment service not found!");
		}

		Console.WriteLine("[WhyDidYouRender] SSR services initialization complete");
	}
}
