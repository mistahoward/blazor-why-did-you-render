using System;

namespace Blazor.WhyDidYouRender.Records;

/// <summary>
/// Performance metrics for a component.
/// </summary>
public class PerformanceMetrics
{
	/// <summary>
	/// Gets or sets the component name.
	/// </summary>
	public string ComponentName { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the total number of renders.
	/// </summary>
	public int TotalRenders { get; set; }

	/// <summary>
	/// Gets or sets the total duration of all renders in milliseconds.
	/// </summary>
	public double TotalDurationMs { get; set; }

	/// <summary>
	/// Gets or sets the average render duration in milliseconds.
	/// </summary>
	public double AverageDurationMs { get; set; }

	/// <summary>
	/// Gets or sets the maximum render duration in milliseconds.
	/// </summary>
	public double MaxDurationMs { get; set; }

	/// <summary>
	/// Gets or sets the minimum render duration in milliseconds.
	/// </summary>
	public double MinDurationMs { get; set; }

	/// <summary>
	/// Gets or sets the duration of the last render in milliseconds.
	/// </summary>
	public double LastRenderDurationMs { get; set; }

	/// <summary>
	/// Gets or sets the method of the last render.
	/// </summary>
	public string LastRenderMethod { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the time of the last render.
	/// </summary>
	public DateTime LastRenderTime { get; set; }

	/// <summary>
	/// Gets or sets the method that had the slowest render.
	/// </summary>
	public string SlowestMethod { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the method that had the fastest render.
	/// </summary>
	public string FastestMethod { get; set; } = string.Empty;
}

/// <summary>
/// Overall performance statistics across all components.
/// </summary>
public record OverallPerformanceStatistics
{
	/// <summary>
	/// Gets or sets the total number of components tracked.
	/// </summary>
	public int TotalComponents { get; init; }

	/// <summary>
	/// Gets or sets the total number of renders across all components.
	/// </summary>
	public int TotalRenders { get; init; }

	/// <summary>
	/// Gets or sets the average render time across all components.
	/// </summary>
	public double AverageRenderTime { get; init; }

	/// <summary>
	/// Gets or sets the slowest render time recorded.
	/// </summary>
	public double SlowestRenderTime { get; init; }

	/// <summary>
	/// Gets or sets the fastest render time recorded.
	/// </summary>
	public double FastestRenderTime { get; init; }

	/// <summary>
	/// Gets or sets the number of components with slow renders.
	/// </summary>
	public int ComponentsWithSlowRenders { get; init; }
}
