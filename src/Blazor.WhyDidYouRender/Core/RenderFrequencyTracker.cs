using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Blazor.WhyDidYouRender.Configuration;
using Blazor.WhyDidYouRender.Records;
using Microsoft.AspNetCore.Components;

namespace Blazor.WhyDidYouRender.Core;

/// <summary>
/// Holds render history and its associated lock for thread-safe access.
/// </summary>
internal sealed class RenderHistoryEntry
{
	public List<DateTime> History { get; } = [];
	public Lock Lock { get; } = new();
}

/// <summary>
/// Service responsible for tracking render frequency and detecting frequent re-renders.
/// </summary>
public class RenderFrequencyTracker
{
	/// <summary>
	/// Cache to store render history entries for each component.
	/// </summary>
	private readonly ConcurrentDictionary<ComponentBase, RenderHistoryEntry> _renderHistory = new();

	/// <summary>
	/// Configuration for frequency tracking.
	/// </summary>
	private readonly WhyDidYouRenderConfig _config;

	/// <summary>
	/// Initializes a new instance of the <see cref="RenderFrequencyTracker"/> class.
	/// </summary>
	/// <param name="config">The configuration for tracking.</param>
	public RenderFrequencyTracker(WhyDidYouRenderConfig config)
	{
		_config = config;
	}

	/// <summary>
	/// Tracks render frequency and determines if component is rendering too frequently.
	/// </summary>
	/// <param name="component">The component being rendered.</param>
	/// <returns>True if component is rendering frequently; otherwise, false.</returns>
	public bool TrackRenderFrequency(ComponentBase component)
	{
		var now = DateTime.UtcNow;
		var entry = _renderHistory.GetOrAdd(component, static _ => new RenderHistoryEntry());

		lock (entry.Lock)
		{
			entry.History.Add(now);

			var cutoff = now.AddSeconds(-1);
			entry.History.RemoveAll(time => time < cutoff);

			return entry.History.Count > _config.FrequentRerenderThreshold;
		}
	}

	/// <summary>
	/// Gets render statistics for a specific component.
	/// </summary>
	/// <param name="component">The component to get statistics for.</param>
	/// <returns>Render statistics for the component.</returns>
	public RenderStatistics GetRenderStatistics(ComponentBase component)
	{
		if (!_renderHistory.TryGetValue(component, out var entry))
		{
			return new RenderStatistics
			{
				ComponentName = component.GetType().Name,
				TotalRenders = 0,
				RendersLastSecond = 0,
				RendersLastMinute = 0,
				AverageRenderRate = 0.0,
				IsFrequentRenderer = false,
			};
		}

		var now = DateTime.UtcNow;
		var oneSecondAgo = now.AddSeconds(-1);
		var oneMinuteAgo = now.AddMinutes(-1);

		lock (entry.Lock)
		{
			var rendersLastSecond = entry.History.Count(t => t >= oneSecondAgo);
			var rendersLastMinute = entry.History.Count(t => t >= oneMinuteAgo);
			var totalRenders = entry.History.Count;

			var averageRate = totalRenders > 1 && entry.History.Count > 0 ? totalRenders / (now - entry.History.First()).TotalMinutes : 0.0;

			return new RenderStatistics
			{
				ComponentName = component.GetType().Name,
				TotalRenders = totalRenders,
				RendersLastSecond = rendersLastSecond,
				RendersLastMinute = rendersLastMinute,
				AverageRenderRate = averageRate,
				IsFrequentRenderer = rendersLastSecond > _config.FrequentRerenderThreshold,
			};
		}
	}

	/// <summary>
	/// Gets render statistics for all tracked components.
	/// </summary>
	/// <returns>Collection of render statistics for all components.</returns>
	public IEnumerable<RenderStatistics> GetAllRenderStatistics()
	{
		return _renderHistory.Keys.Select(GetRenderStatistics);
	}

	/// <summary>
	/// Cleans up render history for components that haven't rendered recently.
	/// </summary>
	/// <param name="olderThan">Remove history older than this timespan.</param>
	public void CleanupOldHistory(TimeSpan olderThan)
	{
		var cutoff = DateTime.UtcNow - olderThan;

		foreach (var kvp in _renderHistory.ToList())
		{
			var entry = kvp.Value;
			lock (entry.Lock)
			{
				for (int i = entry.History.Count - 1; i >= 0; i--)
				{
					if (entry.History[i] < cutoff)
					{
						entry.History.RemoveAt(i);
					}
				}

				if (entry.History.Count == 0)
				{
					_renderHistory.TryRemove(kvp.Key, out _);
				}
			}
		}
	}

	/// <summary>
	/// Gets the number of components currently being tracked.
	/// </summary>
	/// <returns>The number of tracked components.</returns>
	public int GetTrackedComponentCount() => _renderHistory.Count;

	/// <summary>
	/// Clears all render history.
	/// </summary>
	public void ClearAll() => _renderHistory.Clear();

	/// <summary>
	/// Gets components that are rendering frequently.
	/// </summary>
	/// <returns>Collection of components that exceed the frequent render threshold.</returns>
	public IEnumerable<ComponentBase> GetFrequentRenderers()
	{
		return _renderHistory.Keys.Where(component =>
		{
			var stats = GetRenderStatistics(component);
			return stats.IsFrequentRenderer;
		});
	}
}
