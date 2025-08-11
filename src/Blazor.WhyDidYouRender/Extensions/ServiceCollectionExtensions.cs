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

		// Register new unified logger interface alongside existing ITrackingLogger for now
		services.AddSingleton<Logging.IWhyDidYouRenderLogger>(provider => {
			var detector = provider.GetRequiredService<IHostingEnvironmentDetector>();
			var hostModel = config.ForceHostingModel ?? detector.DetectHostingModel();

			if (hostModel == BlazorHostingModel.WebAssembly)
				return new Logging.WasmWhyDidYouRenderLogger(config, provider.GetRequiredService<IJSRuntime>());

			// Server/SSR paths
			var serverLogger = new Logging.ServerWhyDidYouRenderLogger(
				config,
				provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Logging.ServerWhyDidYouRenderLogger>>()
			);

			if (config.EnableOpenTelemetry) {
				var otelLogger = new Logging.AspireWhyDidYouRenderLogger(config);
				return new Logging.CompositeWhyDidYouRenderLogger(config, serverLogger, otelLogger);
			}

			return serverLogger;
		});

		RegisterEnvironmentSpecificServices(services, config);

		services.AddScoped<BrowserConsoleLogger>();
		services.AddScoped<Helpers.IBrowserConsoleLogger>(sp => sp.GetRequiredService<BrowserConsoleLogger>());

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
		LogInfo(serviceProvider, "Attempting to initialize browser logging...");

		var detector = serviceProvider.GetService<IHostingEnvironmentDetector>();
		if (detector != null)
			LogInfo(serviceProvider, $"Environment detected: {detector.GetEnvironmentDescription()}");

		var browserLogger = serviceProvider.GetService<BrowserConsoleLogger>();
		if (browserLogger != null) {
			LogInfo(serviceProvider, "Browser logger service found, initializing...");
			await browserLogger.InitializeAsync();
			RenderTrackerService.Instance.SetBrowserLogger(browserLogger);
			LogInfo(serviceProvider, "Browser logger set on tracker service");
		}
		else {
			LogWarning(serviceProvider, "Browser logger service not found in DI container");
		}

		var trackingLogger = serviceProvider.GetService<ITrackingLogger>();
		if (trackingLogger != null) {
			LogInfo(serviceProvider, "Tracking logger service found, initializing...");
			await trackingLogger.InitializeAsync();
			LogInfo(serviceProvider, "Tracking logger initialized successfully");
		}

		if (detector?.IsClientSide == true) {
			var errorTracker = serviceProvider.GetService<IErrorTracker>();
			if (errorTracker is WasmErrorTracker wasmErrorTracker) {
				await wasmErrorTracker.LoadErrorsFromStorageAsync();
				LogInfo(serviceProvider, "WASM error tracker loaded from storage");
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
						provider.GetRequiredService<IJSRuntime>(), config, provider.GetService<Logging.IWhyDidYouRenderLogger>()),
					_ => new ServerSessionContextService(
						provider.GetService<Microsoft.AspNetCore.Http.IHttpContextAccessor>()!, config, provider.GetService<Logging.IWhyDidYouRenderLogger>())
				};
			}

			return detector.IsClientSide
				? new WasmSessionContextService(provider.GetRequiredService<IJSRuntime>(), config, provider.GetService<Logging.IWhyDidYouRenderLogger>())
				: new ServerSessionContextService(provider.GetService<Microsoft.AspNetCore.Http.IHttpContextAccessor>()!, config, provider.GetService<Logging.IWhyDidYouRenderLogger>());
		});

		services.AddSingleton<ITrackingLogger>(provider => {
			var detector = provider.GetRequiredService<IHostingEnvironmentDetector>();

			if (config.ForceHostingModel.HasValue)
				return config.ForceHostingModel.Value switch {
					BlazorHostingModel.WebAssembly => new WasmTrackingLogger(
						provider.GetRequiredService<IJSRuntime>(), config),
					_ => new ServerTrackingLogger(config, null, provider.GetService<Logging.IWhyDidYouRenderLogger>())
				};

			return detector.IsClientSide
				? new WasmTrackingLogger(provider.GetRequiredService<IJSRuntime>(), config)
				: new ServerTrackingLogger(config, null, provider.GetService<Logging.IWhyDidYouRenderLogger>());
		});

		services.AddSingleton<IErrorTracker>(provider => {
			var detector = provider.GetRequiredService<IHostingEnvironmentDetector>();
			var logger = provider.GetService<Microsoft.Extensions.Logging.ILogger<ServerErrorTracker>>();

			if (config.ForceHostingModel.HasValue)
				return config.ForceHostingModel.Value switch {
					BlazorHostingModel.WebAssembly => new WasmErrorTracker(
						provider.GetRequiredService<IJSRuntime>(), config),
					_ => new ServerErrorTracker(config, logger, provider.GetService<Logging.IWhyDidYouRenderLogger>())
				};

			return detector.IsClientSide
				? new WasmErrorTracker(provider.GetRequiredService<IJSRuntime>(), config)
				: new ServerErrorTracker(config, logger, provider.GetService<Logging.IWhyDidYouRenderLogger>());
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
					LogWarning(services, $"Configuration validation warnings for {config.ForceHostingModel.Value}:");
					foreach (var error in errors)
						LogWarning(services, $"  - {error}");
				}

				if (config.AdaptForEnvironment(config.ForceHostingModel.Value))
					LogInfo(services, $"Configuration adapted for {config.ForceHostingModel.Value} environment");
			}
			else {
				var basicErrors = new List<string>();

				if (config.ErrorCleanupIntervalMinutes <= 0)
					basicErrors.Add("ErrorCleanupIntervalMinutes must be greater than 0");

				if (config.MaxErrorHistorySize <= 0)
					basicErrors.Add("MaxErrorHistorySize must be greater than 0");

				if (basicErrors.Count > 0) {
					LogWarning(services, "Configuration validation warnings:");
					foreach (var error in basicErrors)
						LogWarning(services, $"  - {error}");
				}
			}

			var summary = config.GetConfigurationSummary();
			LogInfo(services, "Configuration summary:");
			foreach (var (key, value) in summary)
				LogInfo(services, $"  {key}: {value}");
		}
		catch (Exception ex) {
			LogError(services, "Configuration validation failed", ex);
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
			LogInfo(serviceProvider, $"Initializing services for {detector.GetEnvironmentDescription()}...");
		else
			LogInfo(serviceProvider, "Initializing services (environment detection unavailable)...");

		var oldErrorTracker = serviceProvider.GetService<Diagnostics.IErrorTracker>();
		if (oldErrorTracker != null) {
			tracker.SetErrorTracker(oldErrorTracker);
			LogInfo(serviceProvider, "Legacy error tracker initialized");
		}

		var newErrorTracker = serviceProvider.GetService<IErrorTracker>();
		if (newErrorTracker != null)
			LogInfo(serviceProvider, $"New error tracker initialized: {newErrorTracker.ErrorTrackingDescription}");
		else
			LogError(serviceProvider, "New error tracker service not found!");

		var sessionContextService = serviceProvider.GetService<ISessionContextService>();
		if (sessionContextService != null) {
			RenderTrackerService.SetSessionContextService(sessionContextService);
			LogInfo(serviceProvider, $"Session context service initialized: {sessionContextService.StorageDescription}");
		}
		else
			LogError(serviceProvider, "Session context service not found!");

		var trackingLogger = serviceProvider.GetService<ITrackingLogger>();
		if (trackingLogger != null) {
			RenderTrackerService.SetTrackingLogger(trackingLogger);
			LogInfo(serviceProvider, $"Tracking logger initialized: {trackingLogger.LoggingDescription}");
		}

		var unified = serviceProvider.GetService<Logging.IWhyDidYouRenderLogger>();
		if (unified != null) RenderTrackerService.SetUnifiedLogger(unified);
		else {
			LogError(serviceProvider, "Tracking logger service not found!");
		}

		var hostEnvironment = serviceProvider.GetService<IHostEnvironment>();
		if (hostEnvironment != null) {
			tracker.SetHostEnvironment(hostEnvironment);
			LogInfo(serviceProvider, "Host environment initialized successfully");
		}
		else if (detector?.IsServerSide == true)
			LogWarning(serviceProvider, "Host environment service not found in server environment!");

		LogInfo(serviceProvider, "Services initialization complete");
	}

	/// <summary>
	/// Initializes WhyDidYouRender specifically for WebAssembly environments.
	/// This method handles WASM-specific initialization including storage loading and cleanup.
	/// </summary>
	/// <param name="serviceProvider">The service provider.</param>
	/// <param name="jsRuntime">The JavaScript runtime.</param>
	/// <returns>A task representing the initialization.</returns>
	public static async Task InitializeWasmAsync(this IServiceProvider serviceProvider, IJSRuntime jsRuntime) {
		LogInfo(serviceProvider, "Initializing for WebAssembly environment...");

		serviceProvider.InitializeSSRServices();

		await serviceProvider.InitializeWhyDidYouRenderAsync(jsRuntime);

		var detector = serviceProvider.GetService<IHostingEnvironmentDetector>();
		if (detector?.IsClientSide == true) {
			var sessionService = serviceProvider.GetService<ISessionContextService>();
			if (sessionService is WasmSessionContextService wasmSession)
				LogInfo(serviceProvider, "WASM session service ready");

			var errorTracker = serviceProvider.GetService<IErrorTracker>();
			if (errorTracker is WasmErrorTracker wasmErrorTracker) {
				await wasmErrorTracker.LoadErrorsFromStorageAsync();
				LogInfo(serviceProvider, "WASM error history loaded from storage");
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
							LogError(serviceProvider, "Storage cleanup failed", ex);
						}
					}
				});

				LogInfo(serviceProvider, "WASM storage cleanup scheduled");
			}
		}

		LogInfo(serviceProvider, "WebAssembly initialization complete");
	}

	/// <summary>
	/// Initializes WhyDidYouRender specifically for Server environments.
	/// This method handles server-specific initialization and optimizations.
	/// </summary>
	/// <param name="serviceProvider">The service provider.</param>
	/// <returns>A task representing the initialization.</returns>
	public static Task InitializeServerAsync(this IServiceProvider serviceProvider) {
		LogInfo(serviceProvider, "Initializing for Server environment...");

		serviceProvider.InitializeSSRServices();
		var detector = serviceProvider.GetService<IHostingEnvironmentDetector>();
		if (detector?.IsServerSide == true) {
			var sessionService = serviceProvider.GetService<ISessionContextService>();
			if (sessionService is ServerSessionContextService serverSession)
				LogInfo(serviceProvider, "Server session service ready");

			var errorTracker = serviceProvider.GetService<IErrorTracker>();
			if (errorTracker is ServerErrorTracker serverErrorTracker)
				LogInfo(serviceProvider, "Server error tracker ready");

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
							LogError(serviceProvider, "Server error cleanup failed", ex);
						}
					}
				});

				LogInfo(serviceProvider, "Server error cleanup scheduled");
			}
		}

		LogInfo(serviceProvider, "Server initialization complete");
		return Task.CompletedTask;
	}

	/// <summary>
	/// Automatically detects the environment and initializes WhyDidYouRender accordingly.
	/// This is the recommended initialization method for most scenarios.
	/// </summary>
	/// <param name="serviceProvider">The service provider.</param>
	/// <param name="jsRuntime">The JavaScript runtime (required for WASM, optional for Server).</param>
	/// <returns>A task representing the initialization.</returns>
	public static Task InitializeAsync(this IServiceProvider serviceProvider, IJSRuntime? jsRuntime = null) {
		var detector = serviceProvider.GetService<IHostingEnvironmentDetector>();

		if (detector == null) {
			LogWarning(serviceProvider, "Environment detector not found, falling back to basic initialization");
			serviceProvider.InitializeSSRServices();
			return Task.CompletedTask;
		}

		LogInfo(serviceProvider, $"Auto-initializing for {detector.GetEnvironmentDescription()}");

		if (detector.IsClientSide) {
			if (jsRuntime == null)
				throw new ArgumentNullException(nameof(jsRuntime), "JavaScript runtime is required for WebAssembly initialization");
			return serviceProvider.InitializeWasmAsync(jsRuntime);
		}
		else {
			var serverTask = serviceProvider.InitializeServerAsync();
			if (jsRuntime != null)
				return serverTask.ContinueWith(_ => serviceProvider.InitializeWhyDidYouRenderAsync(jsRuntime)).Unwrap();
			return serverTask;
		}
	}

	// Unified logging helpers with safe fallback
	private static void LogInfo(IServiceProvider sp, string message, Dictionary<string, object?>? data = null) {
		var logger = sp.GetService<Logging.IWhyDidYouRenderLogger>();
		if (logger != null) logger.LogInfo(message, data);
		else Console.WriteLine($"[WhyDidYouRender] {message}");
	}

	private static void LogWarning(IServiceProvider sp, string message, Dictionary<string, object?>? data = null) {
		var logger = sp.GetService<Logging.IWhyDidYouRenderLogger>();
		if (logger != null) logger.LogWarning(message, data);
		else Console.WriteLine($"[WhyDidYouRender] WARNING: {message}");
	}

	private static void LogError(IServiceProvider sp, string message, Exception? exception = null, Dictionary<string, object?>? data = null) {
		var logger = sp.GetService<Logging.IWhyDidYouRenderLogger>();
		if (logger != null) logger.LogError(message, exception, data);
		else Console.WriteLine(exception == null
			? $"[WhyDidYouRender] ERROR: {message}"
			: $"[WhyDidYouRender] ERROR: {message} | {exception.Message}");
	}

	// Overloads for early-stage logging where only IServiceCollection is available
	private static void LogInfo(IServiceCollection services, string message, Dictionary<string, object?>? data = null) {
		using var sp = services.BuildServiceProvider();
		LogInfo(sp, message, data);
	}

	private static void LogWarning(IServiceCollection services, string message, Dictionary<string, object?>? data = null) {
		using var sp = services.BuildServiceProvider();
		LogWarning(sp, message, data);
	}

	private static void LogError(IServiceCollection services, string message, Exception? exception = null, Dictionary<string, object?>? data = null) {
		using var sp = services.BuildServiceProvider();
		LogError(sp, message, exception, data);
	}


}
