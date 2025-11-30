namespace Blazor.WhyDidYouRender.Records.StateTracking;

/// <summary>
/// Defines the types of performance alerts that can be generated during state tracking operations.
/// These values categorize different kinds of performance issues detected by the monitoring system.
/// </summary>
public enum AlertType
{
	/// <summary>
	/// Alert triggered when the average operation time exceeds the configured threshold.
	/// This indicates that operations are consistently taking longer than expected.
	/// </summary>
	SlowAverageTime,

	/// <summary>
	/// Alert triggered when the maximum operation time exceeds the configured threshold.
	/// This indicates that at least one operation took significantly longer than expected,
	/// which could indicate performance spikes or blocking operations.
	/// </summary>
	SlowMaxTime,

	/// <summary>
	/// Alert triggered when the failure rate exceeds the configured threshold.
	/// This indicates that operations are failing more frequently than acceptable,
	/// which could indicate bugs, resource issues, or environmental problems.
	/// </summary>
	HighFailureRate,

	/// <summary>
	/// Alert triggered when memory usage during operations exceeds acceptable limits.
	/// This indicates potential memory leaks or inefficient memory usage patterns.
	/// </summary>
	HighMemoryUsage,

	/// <summary>
	/// Alert triggered when the number of concurrent operations exceeds safe limits.
	/// This indicates potential threading issues or resource contention.
	/// </summary>
	HighConcurrency,

	/// <summary>
	/// Alert triggered when operations are timing out more frequently than expected.
	/// This indicates potential deadlocks, network issues, or resource unavailability.
	/// </summary>
	FrequentTimeouts,

	/// <summary>
	/// Alert triggered when the system detects unusual patterns in operation performance.
	/// This is a catch-all for anomalies that don't fit other specific alert types.
	/// </summary>
	PerformanceAnomaly,
}
