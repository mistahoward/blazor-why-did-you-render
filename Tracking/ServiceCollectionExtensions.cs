using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Blazor.WhyDidYouRender.Tracking;

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
		// Register the browser console logger
		services.AddScoped<BrowserConsoleLogger>();

		// Configure the tracking service
		if (configuration != null) {
			var config = new WhyDidYouRenderConfig();
			configuration.Bind(config);

			services.AddSingleton<WhyDidYouRenderConfig>(config);

			// Apply configuration to the singleton service
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
		// Register the browser console logger
		services.AddScoped<BrowserConsoleLogger>();

		// Create and configure the config
		var config = new WhyDidYouRenderConfig();
		configureOptions(config);

		services.AddSingleton<WhyDidYouRenderConfig>(config);

		// Apply configuration to the singleton service
		RenderTrackerService.Instance.Configure(config);

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
}
