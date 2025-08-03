using Blazor.WhyDidYouRender.Configuration;

namespace Blazor.WhyDidYouRender.Abstractions;

/// <summary>
/// Interface for detecting the current Blazor hosting environment.
/// Provides environment-specific information for service registration and configuration.
/// </summary>
public interface IHostingEnvironmentDetector
{
    /// <summary>
    /// Detects the current Blazor hosting model.
    /// </summary>
    /// <returns>The detected hosting model.</returns>
    BlazorHostingModel DetectHostingModel();

    /// <summary>
    /// Gets a value indicating whether the current environment is server-side.
    /// This includes both Blazor Server and SSR environments.
    /// </summary>
    bool IsServerSide { get; }

    /// <summary>
    /// Gets a value indicating whether the current environment is client-side.
    /// This includes Blazor WebAssembly environments.
    /// </summary>
    bool IsClientSide { get; }

    /// <summary>
    /// Gets a human-readable description of the current environment.
    /// Useful for logging and debugging purposes.
    /// </summary>
    /// <returns>A description of the current hosting environment.</returns>
    string GetEnvironmentDescription();

    /// <summary>
    /// Gets a value indicating whether HttpContext is available in the current environment.
    /// </summary>
    bool HasHttpContext { get; }

    /// <summary>
    /// Gets a value indicating whether JavaScript interop is available in the current environment.
    /// </summary>
    bool HasJavaScriptInterop { get; }
}
