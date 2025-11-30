namespace Blazor.WhyDidYouRender.Records.StateTracking;

/// <summary>
/// Defines the severity levels for performance alerts in state tracking operations.
/// These values indicate the urgency and impact of performance issues detected during monitoring.
/// </summary>
public enum AlertSeverity
{
	/// <summary>
	/// Informational alert with no immediate action required.
	/// Used for reporting normal operational metrics or minor observations.
	/// These alerts are typically used for trend analysis and baseline establishment.
	/// </summary>
	Info = 0,

	/// <summary>
	/// Warning alert indicating potential issues that should be monitored.
	/// Performance is degraded but still within acceptable operational limits.
	/// These alerts suggest proactive investigation to prevent escalation.
	/// </summary>
	Warning = 1,

	/// <summary>
	/// Error alert indicating significant performance issues requiring attention.
	/// Performance is outside acceptable limits and may impact user experience.
	/// These alerts require timely investigation and corrective action.
	/// </summary>
	Error = 2,

	/// <summary>
	/// Critical alert indicating severe performance issues requiring immediate action.
	/// Performance is severely degraded and likely impacting system stability.
	/// These alerts require immediate intervention to prevent system failure.
	/// </summary>
	Critical = 3,
}
