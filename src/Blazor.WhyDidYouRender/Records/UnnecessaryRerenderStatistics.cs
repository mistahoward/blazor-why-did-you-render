namespace Blazor.WhyDidYouRender.Records;

/// <summary>
/// Statistics about unnecessary re-render detection.
/// </summary>
public record UnnecessaryRerenderStatistics {
	/// <summary>
	/// Gets or sets the number of components being tracked.
	/// </summary>
	public int TrackedComponents { get; init; }

	/// <summary>
	/// Gets or sets whether unnecessary re-render detection is enabled.
	/// </summary>
	public bool IsEnabled { get; init; }
}
