namespace Blazor.WhyDidYouRender.Records;

/// <summary>
/// Statistics about component render frequency.
/// </summary>
public record RenderStatistics
{
	/// <summary>
	/// Gets or sets the component name.
	/// </summary>
	public string ComponentName { get; init; } = string.Empty;

	/// <summary>
	/// Gets or sets the total number of renders tracked.
	/// </summary>
	public int TotalRenders { get; init; }

	/// <summary>
	/// Gets or sets the number of renders in the last second.
	/// </summary>
	public int RendersLastSecond { get; init; }

	/// <summary>
	/// Gets or sets the number of renders in the last minute.
	/// </summary>
	public int RendersLastMinute { get; init; }

	/// <summary>
	/// Gets or sets the average render rate (renders per minute).
	/// </summary>
	public double AverageRenderRate { get; init; }

	/// <summary>
	/// Gets or sets whether this component is considered a frequent renderer.
	/// </summary>
	public bool IsFrequentRenderer { get; init; }
}
