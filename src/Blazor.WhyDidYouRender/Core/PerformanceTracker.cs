using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using Blazor.WhyDidYouRender.Configuration;
using Blazor.WhyDidYouRender.Records;
using Microsoft.AspNetCore.Components;

namespace Blazor.WhyDidYouRender.Core;

/// <summary>
/// Service responsible for tracking render performance metrics.
/// </summary>
public class PerformanceTracker
{
	/// <summary>
	/// Cache to store render timing information for each component.
	/// </summary>
	private readonly ConcurrentDictionary<ComponentBase, Stopwatch> _renderTimers = new();

	/// <summary>
	/// Cache to store performance metrics for each component.
	/// </summary>
	private readonly ConcurrentDictionary<ComponentBase, PerformanceMetrics> _performanceMetrics = new();

	/// <summary>
	/// Configuration for performance tracking.
	/// </summary>
	private readonly WhyDidYouRenderConfig _config;

	/// <summary>
	/// Initializes a new instance of the <see cref="PerformanceTracker"/> class.
	/// </summary>
	/// <param name="config">The configuration for tracking.</param>
	public PerformanceTracker(WhyDidYouRenderConfig config)
	{
		_config = config;
	}

	/// <summary>
	/// Starts timing a render operation for a component.
	/// </summary>
	/// <param name="component">The component to start timing.</param>
	public void StartRenderTiming(ComponentBase component)
	{
		if (!_config.TrackPerformance)
			return;

		var stopwatch = _renderTimers.GetOrAdd(component, _ => new Stopwatch());
		stopwatch.Restart();
	}

	/// <summary>
	/// Gets the render duration and resets the timer for a component.
	/// </summary>
	/// <param name="component">The component to get timing for.</param>
	/// <param name="method">The method that was being timed.</param>
	/// <returns>The render duration in milliseconds, or null if not tracked.</returns>
	public double? GetAndResetRenderDuration(ComponentBase component, string method)
	{
		if (!_config.TrackPerformance)
			return null;

		if (!_renderTimers.TryGetValue(component, out var stopwatch))
		{
			return null;
		}

		stopwatch.Stop();
		var durationMs = stopwatch.Elapsed.TotalMilliseconds;

		UpdatePerformanceMetrics(component, method, durationMs);

		return durationMs;
	}

	/// <summary>
	/// Updates performance metrics for a component.
	/// </summary>
	/// <param name="component">The component to update metrics for.</param>
	/// <param name="method">The method that was executed.</param>
	/// <param name="durationMs">The duration in milliseconds.</param>
	private void UpdatePerformanceMetrics(ComponentBase component, string method, double durationMs)
	{
		var metrics = _performanceMetrics.GetOrAdd(component, _ => new PerformanceMetrics { ComponentName = component.GetType().Name });

		lock (metrics)
		{
			metrics.TotalRenders++;
			metrics.TotalDurationMs += durationMs;
			metrics.AverageDurationMs = metrics.TotalDurationMs / metrics.TotalRenders;

			if (durationMs > metrics.MaxDurationMs)
			{
				metrics.MaxDurationMs = durationMs;
				metrics.SlowestMethod = method;
			}

			if (metrics.MinDurationMs == 0 || durationMs < metrics.MinDurationMs)
			{
				metrics.MinDurationMs = durationMs;
				metrics.FastestMethod = method;
			}

			metrics.LastRenderDurationMs = durationMs;
			metrics.LastRenderMethod = method;
			metrics.LastRenderTime = DateTime.UtcNow;
		}
	}

	/// <summary>
	/// Gets performance metrics for a specific component.
	/// </summary>
	/// <param name="component">The component to get metrics for.</param>
	/// <returns>Performance metrics for the component.</returns>
	public PerformanceMetrics? GetPerformanceMetrics(ComponentBase component)
	{
		return _performanceMetrics.TryGetValue(component, out var metrics) ? metrics : null;
	}

	/// <summary>
	/// Gets performance metrics for all tracked components.
	/// </summary>
	/// <returns>Collection of performance metrics for all components.</returns>
	public IEnumerable<PerformanceMetrics> GetAllPerformanceMetrics()
	{
		return _performanceMetrics.Values.ToList();
	}

	/// <summary>
	/// Gets components with performance issues (slow renders).
	/// </summary>
	/// <param name="thresholdMs">The threshold in milliseconds for considering a render slow.</param>
	/// <returns>Collection of components with slow renders.</returns>
	public IEnumerable<PerformanceMetrics> GetSlowComponents(double thresholdMs = 100.0)
	{
		return _performanceMetrics.Values.Where(m => m.MaxDurationMs > thresholdMs || m.AverageDurationMs > thresholdMs / 2);
	}

	/// <summary>
	/// Cleans up performance data for components that are no longer active.
	/// </summary>
	/// <param name="activeComponents">Set of currently active components.</param>
	public void CleanupInactiveComponents(IEnumerable<ComponentBase> activeComponents)
	{
		var activeSet = activeComponents.ToHashSet();

		var timersToRemove = _renderTimers.Keys.Where(key => !activeSet.Contains(key)).ToList();
		foreach (var key in timersToRemove)
		{
			_renderTimers.TryRemove(key, out _);
		}

		var metricsToRemove = _performanceMetrics.Keys.Where(key => !activeSet.Contains(key)).ToList();
		foreach (var key in metricsToRemove)
		{
			_performanceMetrics.TryRemove(key, out _);
		}
	}

	/// <summary>
	/// Gets the number of components currently being tracked.
	/// </summary>
	/// <returns>The number of tracked components.</returns>
	public int GetTrackedComponentCount() => _performanceMetrics.Count;

	/// <summary>
	/// Clears all performance data.
	/// </summary>
	public void ClearAll()
	{
		_renderTimers.Clear();
		_performanceMetrics.Clear();
	}

	/// <summary>
	/// Gets overall performance statistics.
	/// </summary>
	/// <returns>Overall performance statistics.</returns>
	public OverallPerformanceStatistics GetOverallStatistics()
	{
		var allMetrics = _performanceMetrics.Values.ToList();

		if (allMetrics.Count == 0)
		{
			return new OverallPerformanceStatistics();
		}

		return new OverallPerformanceStatistics
		{
			TotalComponents = allMetrics.Count,
			TotalRenders = allMetrics.Sum(m => m.TotalRenders),
			AverageRenderTime = allMetrics.Average(m => m.AverageDurationMs),
			SlowestRenderTime = allMetrics.Max(m => m.MaxDurationMs),
			FastestRenderTime = allMetrics.Min(m => m.MinDurationMs),
			ComponentsWithSlowRenders = allMetrics.Count(m => m.MaxDurationMs > 100.0),
		};
	}
}
