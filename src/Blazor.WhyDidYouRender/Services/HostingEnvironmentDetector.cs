using Blazor.WhyDidYouRender.Abstractions;
using Blazor.WhyDidYouRender.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Blazor.WhyDidYouRender.Services;

/// <summary>
/// Server-first implementation of hosting environment detection.
/// Detects whether the application is running in Server, SSR, or WebAssembly mode.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="HostingEnvironmentDetector"/> class.
/// </remarks>
/// <param name="serviceProvider">The service provider for dependency resolution.</param>
public class HostingEnvironmentDetector(IServiceProvider serviceProvider) : IHostingEnvironmentDetector
{
	private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
	private BlazorHostingModel? _cachedHostingModel;
#if NET9_0_OR_GREATER
	private readonly Lock _lock = new();
#else
	private readonly object _lock = new();
#endif

	/// <inheritdoc />
	public BlazorHostingModel DetectHostingModel()
	{
		if (_cachedHostingModel.HasValue)
			return _cachedHostingModel.Value;

		lock (_lock)
		{
			if (_cachedHostingModel.HasValue)
				return _cachedHostingModel.Value;

			_cachedHostingModel = PerformDetection();
			return _cachedHostingModel.Value;
		}
	}

	/// <inheritdoc />
	public bool IsServerSide => DetectHostingModel() != BlazorHostingModel.WebAssembly;

	/// <inheritdoc />
	public bool IsClientSide => DetectHostingModel() == BlazorHostingModel.WebAssembly;

	/// <inheritdoc />
	public bool HasHttpContext
	{
		get
		{
			try
			{
				var httpContextAccessor = _serviceProvider.GetService<IHttpContextAccessor>();
				return httpContextAccessor?.HttpContext != null;
			}
			catch
			{
				return false;
			}
		}
	}

	/// <inheritdoc />
	public bool HasJavaScriptInterop
	{
		get
		{
			try
			{
				var jsRuntime = _serviceProvider.GetService<IJSRuntime>();
				return jsRuntime != null;
			}
			catch
			{
				return false;
			}
		}
	}

	/// <inheritdoc />
	public string GetEnvironmentDescription()
	{
		var model = DetectHostingModel();
		var details = new List<string>();

		if (HasHttpContext)
			details.Add("HttpContext available");
		if (HasJavaScriptInterop)
			details.Add("JS interop available");

		var detailsStr = details.Count > 0 ? $" ({string.Join(", ", details)})" : "";
		return $"{model}{detailsStr}";
	}

	/// <summary>
	/// Performs the actual environment detection logic.
	/// Uses a server-first approach: assumes server-side unless proven otherwise.
	/// </summary>
	/// <returns>The detected hosting model.</returns>
	private BlazorHostingModel PerformDetection()
	{
		try
		{
			// check for HttpContextAccessor with actual HttpContext (true server-side indicator)
			var httpContextAccessor = _serviceProvider.GetService<IHttpContextAccessor>();
			if (httpContextAccessor?.HttpContext != null)
			{
				// we have an actual HttpContext, this is definitely server-side
				// could be either Blazor Server or SSR
				return BlazorHostingModel.Server;
			}

			// check for JSRuntime (could be WASM or server with JS interop)
			var jsRuntime = _serviceProvider.GetService<IJSRuntime>();
			if (jsRuntime != null)
			{
				// if we have JSRuntime but no HttpContext, this is WASM
				// in server-side Blazor, we'd have both JSRuntime AND HttpContext
				return BlazorHostingModel.WebAssembly;
			}

			// fallback: if we can't determine, assume server-side
			// this library is server-first, so this is the safest default
			return BlazorHostingModel.Server;
		}
		catch (Exception ex)
		{
			// if detection fails, log and assume server-side
			Console.WriteLine($"[WhyDidYouRender] Environment detection failed: {ex.Message}. Defaulting to Server mode.");
			return BlazorHostingModel.Server;
		}
	}
}
