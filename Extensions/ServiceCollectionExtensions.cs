using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.JSInterop;

using Blazor.WhyDidYouRender.Abstractions;
using Blazor.WhyDidYouRender.Configuration;
using Blazor.WhyDidYouRender.Core;
using Blazor.WhyDidYouRender.Helpers;
using Blazor.WhyDidYouRender.Services;

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

		var config = new WhyDidYouRenderConfig();
		configuration?.Bind(config);

		return AddWhyDidYouRender(services, c => {
			configuration?.Bind(c);
		});
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

		var config = new WhyDidYouRenderConfig();
		configureOptions(config);

		ValidateAndAdaptConfiguration(config, services);

		services.AddSingleton(config);
		services.AddSingleton(RenderTrackerService.Instance);
		services.AddSingleton<ParameterChangeDetector>();
		services.AddSingleton<PerformanceTracker>();
		services.AddSingleton<IHostingEnvironmentDetector, HostingEnvironmentDetector>();

		RegisterEnvironmentSpecificServices(services, config);

		services.AddScoped<BrowserConsoleLogger>();

		RenderTrackerService.Instance.Configure(configureOptions);

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

		var detector = serviceProvider.GetService<IHostingEnvironmentDetector>();
		if (detector != null)
			Console.WriteLine($"[WhyDidYouRender] Environment detected: {detector.GetEnvironmentDescription()}");

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

		var trackingLogger = serviceProvider.GetService<ITrackingLogger>();
		if (trackingLogger != null) {
			Console.WriteLine("[WhyDidYouRender] Tracking logger service found, initializing...");
			await trackingLogger.InitializeAsync();
			Console.WriteLine("[WhyDidYouRender] Tracking logger initialized successfully");
		}

		if (detector?.IsClientSide == true) {
			var errorTracker = serviceProvider.GetService<IErrorTracker>();
			if (errorTracker is WasmErrorTracker wasmErrorTracker) {
				await wasmErrorTracker.LoadErrorsFromStorageAsync();
				Console.WriteLine("[WhyDidYouRender] WASM error tracker loaded from storage");
			}
		}
	}

	/// <summary>
	/// Registers environment-specific services based on the hosting environment.
	/// </summary>
	/// <param name="services">The service collection.</param>
	/// <param name="config">The WhyDidYouRender configuration.</param>
	private static void RegisterEnvironmentSpecificServices(IServiceCollection services, WhyDidYouRenderConfig config) {
		services.AddSingleton<ISessionContextService>(provider => {
			var detector = provider.GetRequiredService<IHostingEnvironmentDetector>();

			if (config.ForceHostingModel.HasValue) {
				return config.ForceHostingModel.Value switch {
					BlazorHostingModel.WebAssembly => new WasmSessionContextService(
						provider.GetRequiredService<IJSRuntime>(), config),
					_ => new ServerSessionContextService(
						provider.GetService<Microsoft.AspNetCore.Http.IHttpContextAccessor>()!, config)
				};
			}

			return detector.IsClientSide
				? new WasmSessionContextService(provider.GetRequiredService<IJSRuntime>(), config)
				: new ServerSessionContextService(provider.GetService<Microsoft.AspNetCore.Http.IHttpContextAccessor>()!, config);
		});

		services.AddSingleton<ITrackingLogger>(provider => {
			var detector = provider.GetRequiredService<IHostingEnvironmentDetector>();

			if (config.ForceHostingModel.HasValue)
				return config.ForceHostingModel.Value switch {
					BlazorHostingModel.WebAssembly => new WasmTrackingLogger(
						provider.GetRequiredService<IJSRuntime>(), config),
					_ => new ServerTrackingLogger(config, null)
				};

			return detector.IsClientSide
				? new WasmTrackingLogger(provider.GetRequiredService<IJSRuntime>(), config)
				: new ServerTrackingLogger(config, null);
		});

		services.AddSingleton<IErrorTracker>(provider => {
			var detector = provider.GetRequiredService<IHostingEnvironmentDetector>();
			var logger = provider.GetService<Microsoft.Extensions.Logging.ILogger<ServerErrorTracker>>();

			if (config.ForceHostingModel.HasValue)
				return config.ForceHostingModel.Value switch {
					BlazorHostingModel.WebAssembly => new WasmErrorTracker(
						provider.GetRequiredService<IJSRuntime>(), config),
					_ => new ServerErrorTracker(config, logger)
				};

			return detector.IsClientSide
				? new WasmErrorTracker(provider.GetRequiredService<IJSRuntime>(), config)
				: new ServerErrorTracker(config, logger);
		});

		services.AddHttpContextAccessor();
	}

	/// <summary>
	/// Validates and adapts the configuration for the current environment.
	/// </summary>
	/// <param name="config">The configuration to validate and adapt.</param>
	/// <param name="services">The service collection for environment detection.</param>
	private static void ValidateAndAdaptConfiguration(WhyDidYouRenderConfig config, IServiceCollection services) {
		try {
			if (!config.AutoDetectEnvironment && config.ForceHostingModel.HasValue) {
				var errors = config.Validate(config.ForceHostingModel.Value);
				if (errors.Count > 0) {
					Console.WriteLine($"[WhyDidYouRender] Configuration validation warnings for {config.ForceHostingModel.Value}:");
					foreach (var error in errors)
						Console.WriteLine($"  - {error}");
				}

				if (config.AdaptForEnvironment(config.ForceHostingModel.Value))
					Console.WriteLine($"[WhyDidYouRender] Configuration adapted for {config.ForceHostingModel.Value} environment");
			}
			else {
				var basicErrors = new List<string>();

				if (config.ErrorCleanupIntervalMinutes <= 0)
					basicErrors.Add("ErrorCleanupIntervalMinutes must be greater than 0");

				if (config.MaxErrorHistorySize <= 0)
					basicErrors.Add("MaxErrorHistorySize must be greater than 0");

				if (basicErrors.Count > 0) {
					Console.WriteLine("[WhyDidYouRender] Configuration validation warnings:");
					foreach (var error in basicErrors)
						Console.WriteLine($"  - {error}");
				}
			}

			var summary = config.GetConfigurationSummary();
			Console.WriteLine("[WhyDidYouRender] Configuration summary:");
			foreach (var (key, value) in summary)
				Console.WriteLine($"  {key}: {value}");
		}
		catch (Exception ex) {
			Console.WriteLine($"[WhyDidYouRender] Configuration validation failed: {ex.Message}");
		}
	}

	/// <summary>
	/// Initializes services after the service provider is built.
	/// Call this method in your application startup after building the service provider.
	/// This method works for both Server-side and WASM environments.
	/// </summary>
	/// <param name="serviceProvider">The built service provider.</param>
	public static void InitializeSSRServices(this IServiceProvider serviceProvider) {
		var tracker = RenderTrackerService.Instance;

		var detector = serviceProvider.GetService<IHostingEnvironmentDetector>();
		if (detector != null)
			Console.WriteLine($"[WhyDidYouRender] Initializing services for {detector.GetEnvironmentDescription()}...");
		else
			Console.WriteLine("[WhyDidYouRender] Initializing services (environment detection unavailable)...");

		var oldErrorTracker = serviceProvider.GetService<Diagnostics.IErrorTracker>();
		if (oldErrorTracker != null) {
			tracker.SetErrorTracker(oldErrorTracker);
			Console.WriteLine("[WhyDidYouRender] Legacy error tracker initialized");
		}

		var newErrorTracker = serviceProvider.GetService<IErrorTracker>();
		if (newErrorTracker != null)
			Console.WriteLine($"[WhyDidYouRender] New error tracker initialized: {newErrorTracker.ErrorTrackingDescription}");
		else
			Console.WriteLine("[WhyDidYouRender] ERROR: New error tracker service not found!");

		var sessionContextService = serviceProvider.GetService<ISessionContextService>();
		if (sessionContextService != null) {
			RenderTrackerService.SetSessionContextService(sessionContextService);
			Console.WriteLine($"[WhyDidYouRender] Session context service initialized: {sessionContextService.StorageDescription}");
		}
		else
			Console.WriteLine("[WhyDidYouRender] ERROR: Session context service not found!");

		var trackingLogger = serviceProvider.GetService<ITrackingLogger>();
		if (trackingLogger != null) {
			RenderTrackerService.SetTrackingLogger(trackingLogger);
			Console.WriteLine($"[WhyDidYouRender] Tracking logger initialized: {trackingLogger.LoggingDescription}");
		}
		else {
			Console.WriteLine("[WhyDidYouRender] ERROR: Tracking logger service not found!");
		}

		var hostEnvironment = serviceProvider.GetService<IHostEnvironment>();
		if (hostEnvironment != null) {
			tracker.SetHostEnvironment(hostEnvironment);
			Console.WriteLine("[WhyDidYouRender] Host environment initialized successfully");
		}
		else if (detector?.IsServerSide == true)
			Console.WriteLine("[WhyDidYouRender] WARNING: Host environment service not found in server environment!");

		Console.WriteLine("[WhyDidYouRender] Services initialization complete");
	}

	/// <summary>
	/// Initializes WhyDidYouRender specifically for WebAssembly environments.
	/// This method handles WASM-specific initialization including storage loading and cleanup.
	/// </summary>
	/// <param name="serviceProvider">The service provider.</param>
	/// <param name="jsRuntime">The JavaScript runtime.</param>
	/// <returns>A task representing the initialization.</returns>
	public static async Task InitializeWasmAsync(this IServiceProvider serviceProvider, IJSRuntime jsRuntime) {
		Console.WriteLine("[WhyDidYouRender] Initializing for WebAssembly environment...");

		serviceProvider.InitializeSSRServices();

		await serviceProvider.InitializeWhyDidYouRenderAsync(jsRuntime);

		var detector = serviceProvider.GetService<IHostingEnvironmentDetector>();
		if (detector?.IsClientSide == true) {
			var sessionService = serviceProvider.GetService<ISessionContextService>();
			if (sessionService is WasmSessionContextService wasmSession)
				Console.WriteLine("[WhyDidYouRender] WASM session service ready");

			var errorTracker = serviceProvider.GetService<IErrorTracker>();
			if (errorTracker is WasmErrorTracker wasmErrorTracker) {
				await wasmErrorTracker.LoadErrorsFromStorageAsync();
				Console.WriteLine("[WhyDidYouRender] WASM error history loaded from storage");
			}

			var config = serviceProvider.GetService<WhyDidYouRenderConfig>();
			if (config?.WasmStorage.AutoCleanupStorage == true) {
				_ = Task.Run(async () => {
					while (true) {
						await Task.Delay(TimeSpan.FromMinutes(config.WasmStorage.StorageCleanupIntervalMinutes));

						try {
							if (sessionService is WasmSessionContextService wasmSessionCleanup)
								await wasmSessionCleanup.PerformStorageCleanupAsync();

							if (errorTracker is WasmErrorTracker wasmErrorCleanup)
								await wasmErrorCleanup.PerformErrorCleanupAsync();
						}
						catch (Exception ex) {
							Console.WriteLine($"[WhyDidYouRender] Storage cleanup failed: {ex.Message}");
						}
					}
				});

				Console.WriteLine("[WhyDidYouRender] WASM storage cleanup scheduled");
			}
		}

		Console.WriteLine("[WhyDidYouRender] WebAssembly initialization complete");
	}

	/// <summary>
	/// Initializes WhyDidYouRender specifically for Server environments.
	/// This method handles server-specific initialization and optimizations.
	/// </summary>
	/// <param name="serviceProvider">The service provider.</param>
	/// <returns>A task representing the initialization.</returns>
	public static async Task InitializeServerAsync(this IServiceProvider serviceProvider) {
		Console.WriteLine("[WhyDidYouRender] Initializing for Server environment...");

		serviceProvider.InitializeSSRServices();
		var detector = serviceProvider.GetService<IHostingEnvironmentDetector>();
		if (detector?.IsServerSide == true) {
			var sessionService = serviceProvider.GetService<ISessionContextService>();
			if (sessionService is ServerSessionContextService serverSession)
				Console.WriteLine("[WhyDidYouRender] Server session service ready");

			var errorTracker = serviceProvider.GetService<IErrorTracker>();
			if (errorTracker is ServerErrorTracker serverErrorTracker)
				Console.WriteLine("[WhyDidYouRender] Server error tracker ready");

			var config = serviceProvider.GetService<WhyDidYouRenderConfig>();
			if (config != null) {
				_ = Task.Run(async () => {
					while (true) {
						await Task.Delay(TimeSpan.FromMinutes(config.ErrorCleanupIntervalMinutes));

						try {
							if (errorTracker is ServerErrorTracker serverErrorCleanup)
								serverErrorCleanup.ClearOldErrors(TimeSpan.FromMinutes(config.ErrorCleanupIntervalMinutes));
						}
						catch (Exception ex) {
							Console.WriteLine($"[WhyDidYouRender] Server error cleanup failed: {ex.Message}");
						}
					}
				});

				Console.WriteLine("[WhyDidYouRender] Server error cleanup scheduled");
			}
		}

		Console.WriteLine("[WhyDidYouRender] Server initialization complete");
	}

	/// <summary>
	/// Automatically detects the environment and initializes WhyDidYouRender accordingly.
	/// This is the recommended initialization method for most scenarios.
	/// </summary>
	/// <param name="serviceProvider">The service provider.</param>
	/// <param name="jsRuntime">The JavaScript runtime (required for WASM, optional for Server).</param>
	/// <returns>A task representing the initialization.</returns>
	public static async Task InitializeAsync(this IServiceProvider serviceProvider, IJSRuntime? jsRuntime = null) {
		var detector = serviceProvider.GetService<IHostingEnvironmentDetector>();

		if (detector == null) {
			Console.WriteLine("[WhyDidYouRender] Environment detector not found, falling back to basic initialization");
			serviceProvider.InitializeSSRServices();
			return;
		}

		Console.WriteLine($"[WhyDidYouRender] Auto-initializing for {detector.GetEnvironmentDescription()}");

		if (detector.IsClientSide) {
			if (jsRuntime == null)
				throw new ArgumentNullException(nameof(jsRuntime), "JavaScript runtime is required for WebAssembly initialization");
			await serviceProvider.InitializeWasmAsync(jsRuntime);
		}
		else {
			await serviceProvider.InitializeServerAsync();

			if (jsRuntime != null)
				await serviceProvider.InitializeWhyDidYouRenderAsync(jsRuntime);
		}
	}
}
