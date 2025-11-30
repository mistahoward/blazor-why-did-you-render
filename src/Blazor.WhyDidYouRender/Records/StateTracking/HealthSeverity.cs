namespace Blazor.WhyDidYouRender.Records.StateTracking;

/// <summary>
/// Defines the severity levels for memory health status in state tracking operations.
/// These values indicate the urgency of memory-related issues detected during health checks.
/// </summary>
public enum HealthSeverity
{
	/// <summary>
	/// Memory usage is within normal parameters with no issues detected.
	/// No action required.
	/// </summary>
	Healthy = 0,

	/// <summary>
	/// Informational status with minor observations that don't require action.
	/// Used for reporting normal operational metrics.
	/// </summary>
	Info = 1,

	/// <summary>
	/// Warning level indicating potential issues that should be monitored.
	/// May require attention if the situation worsens.
	/// </summary>
	Warning = 2,

	/// <summary>
	/// Error level indicating significant issues that require attention.
	/// Performance may be impacted and corrective action is recommended.
	/// </summary>
	Error = 3,

	/// <summary>
	/// Critical level indicating severe issues requiring immediate action.
	/// System stability may be at risk and immediate intervention is required.
	/// </summary>
	Critical = 4,
}
