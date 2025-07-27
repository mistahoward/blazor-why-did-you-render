namespace Blazor.WhyDidYouRender.Configuration;

/// <summary>
/// Represents the different Blazor hosting models supported by WhyDidYouRender.
/// </summary>
public enum BlazorHostingModel
{
    /// <summary>
    /// Blazor Server hosting model.
    /// Components run on the server and UI updates are sent to the client via SignalR.
    /// </summary>
    Server = 0,

    /// <summary>
    /// Blazor WebAssembly hosting model.
    /// Components run entirely in the browser using WebAssembly.
    /// </summary>
    WebAssembly = 1,

    /// <summary>
    /// Server-Side Rendering (SSR) hosting model.
    /// Components are pre-rendered on the server and sent as static HTML.
    /// </summary>
    ServerSideRendering = 2
}
