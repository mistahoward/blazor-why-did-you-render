namespace Blazor.WhyDidYouRender.Records;

/// <summary>
/// Represents a render event with detailed tracking information.
/// </summary>
public record RenderEvent {
	/// <summary>
	/// Timestamp when the render event occurred.
	/// </summary>
	public DateTime Timestamp { get; init; } = DateTime.UtcNow;

	/// <summary>
	/// Simple name of the component (e.g., "Counter").
	/// </summary>
	public string ComponentName { get; init; } = string.Empty;

	/// <summary>
	/// Full type name including namespace (e.g., "RenderTracker.SampleApp.Components.Pages.Counter").
	/// </summary>
	public string ComponentType { get; init; } = string.Empty;

	/// <summary>
	/// The lifecycle method or trigger that caused the render.
	/// </summary>
	public string Method { get; init; } = string.Empty;

	/// <summary>
	/// Indicates if this is the first render of the component.
	/// </summary>
	public bool? FirstRender { get; init; }

	/// <summary>
	/// Duration of the render operation in milliseconds.
	/// </summary>
	public double? DurationMs { get; init; }

	/// <summary>
	/// Session or connection ID for SSR scenarios.
	/// </summary>
	public string? SessionId { get; init; }

	/// <summary>
	/// Parameter changes detected during this render (if any).
	/// </summary>
	public Dictionary<string, object?>? ParameterChanges { get; init; }

	/// <summary>
	/// Indicates if this render was unnecessary (no actual changes detected).
	/// </summary>
	public bool IsUnnecessaryRerender { get; init; }

	/// <summary>
	/// Reason why this render was flagged as unnecessary.
	/// </summary>
	public string? UnnecessaryRerenderReason { get; init; }

	/// <summary>
	/// Indicates if this component is rendering frequently (potential performance issue).
	/// </summary>
	public bool IsFrequentRerender { get; init; }

	/// <summary>
	/// State changes detected during this render (if state tracking is enabled).
	/// </summary>
	public List<StateChange>? StateChanges { get; init; }

	/// <summary>
	/// Indicates if state changes were detected during this render.
	/// </summary>
	public bool HasStateChanges => StateChanges?.Count > 0;

	/// <summary>
	/// Number of state changes detected during this render.
	/// </summary>
	public int StateChangeCount => StateChanges?.Count ?? 0;
}
