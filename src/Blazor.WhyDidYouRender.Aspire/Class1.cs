using Microsoft.Extensions.DependencyInjection;
using Blazor.WhyDidYouRender.Configuration;

namespace Blazor.WhyDidYouRender.Aspire;

/// <summary>
/// Minimal Aspire integration helpers for WhyDidYouRender.
/// Enables OpenTelemetry (traces + metrics) and forwards to the core AddWhyDidYouRender setup.
/// </summary>
public static class AspireExtensions
{
    /// <summary>
    /// Adds WhyDidYouRender with Aspire/OpenTelemetry enabled.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional additional configuration.</param>
    public static IServiceCollection AddWhyDidYouRenderAspire(this IServiceCollection services, Action<WhyDidYouRenderConfig>? configure = null)
    {
        return Blazor.WhyDidYouRender.Extensions.ServiceCollectionExtensions.AddWhyDidYouRender(services, c =>
        {
            c.EnableOpenTelemetry = true;
            c.EnableOtelTraces = true;
            c.EnableOtelMetrics = true;
            configure?.Invoke(c);
        });
    }
}
