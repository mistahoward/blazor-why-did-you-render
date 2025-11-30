using System;
using System.Collections.Generic;
using System.Linq;

namespace Blazor.WhyDidYouRender.Records.StateTracking;

/// <summary>
/// Represents the health status of memory usage in state tracking operations.
/// This record provides immutable information about memory health and any issues detected.
/// </summary>
public record MemoryHealthStatus
{
	/// <summary>
	/// Gets the overall health severity level.
	/// </summary>
	public HealthSeverity Severity { get; init; } = HealthSeverity.Healthy;

	/// <summary>
	/// Gets the list of health issues detected.
	/// </summary>
	public IReadOnlyList<string> Issues { get; init; } = new List<string>();

	/// <summary>
	/// Gets the time when the health check was performed.
	/// </summary>
	public DateTime CheckTime { get; init; } = DateTime.UtcNow;

	/// <summary>
	/// Gets whether the memory usage is considered healthy.
	/// </summary>
	public bool IsHealthy => Severity == HealthSeverity.Healthy;

	/// <summary>
	/// Gets whether the memory usage requires immediate attention.
	/// </summary>
	public bool RequiresAttention => Severity >= HealthSeverity.Warning;

	/// <summary>
	/// Gets whether the memory usage is in a critical state.
	/// </summary>
	public bool IsCritical => Severity >= HealthSeverity.Critical;

	/// <summary>
	/// Gets the number of issues by severity level.
	/// </summary>
	public int IssueCount => Issues.Count;

	/// <summary>
	/// Creates a new healthy status with no issues.
	/// </summary>
	/// <returns>A healthy memory status.</returns>
	public static MemoryHealthStatus Healthy() => new();

	/// <summary>
	/// Creates a new status with the specified severity and issues.
	/// </summary>
	/// <param name="severity">The health severity level.</param>
	/// <param name="issues">The list of issues detected.</param>
	/// <returns>A memory health status with the specified issues.</returns>
	public static MemoryHealthStatus WithIssues(HealthSeverity severity, params string[] issues) =>
		new() { Severity = severity, Issues = issues.ToList() };

	/// <summary>
	/// Gets a formatted summary of the health status.
	/// </summary>
	/// <returns>A formatted string with health status information.</returns>
	public string GetFormattedSummary()
	{
		if (IsHealthy)
			return $"Memory Health: {Severity} (checked at {CheckTime:HH:mm:ss})";

		var issueText = Issues.Count == 1 ? "issue" : "issues";
		return $"Memory Health: {Severity} - {Issues.Count} {issueText} detected (checked at {CheckTime:HH:mm:ss})\n"
			+ string.Join("\n", Issues.Select(issue => $"  • {issue}"));
	}
}
