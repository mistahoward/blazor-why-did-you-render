using Blazor.WhyDidYouRender.Configuration;
using Blazor.WhyDidYouRender.Records;
using Microsoft.AspNetCore.Components;

namespace Blazor.WhyDidYouRender.Core.StateTracking;

/// <summary>
/// Manages state snapshots for component instances, handling creation, storage, and cleanup.
/// This class provides efficient snapshot management with memory cleanup to prevent leaks.
/// </summary>
public class StateSnapshotManager
{
	/// <summary>
	/// Optimized storage for state snapshots with memory management and performance tracking.
	/// </summary>
	private readonly OptimizedSnapshotStorage _optimizedStorage;

	/// <summary>
	/// The field analyzer used to get component metadata.
	/// </summary>
	private readonly StateFieldAnalyzer _fieldAnalyzer;

	/// <summary>
	/// The state comparer used for detailed field comparison.
	/// </summary>
	private readonly StateComparer _stateComparer;

	/// <summary>
	/// Configuration for state tracking behavior.
	/// </summary>
	private readonly WhyDidYouRenderConfig _config;

	/// <summary>
	/// Timestamp of the last cleanup operation.
	/// </summary>
	private DateTime _lastCleanup = DateTime.UtcNow;

	/// <summary>
	/// Initializes a new instance of the <see cref="StateSnapshotManager"/> class.
	/// </summary>
	/// <param name="fieldAnalyzer">The field analyzer for component metadata.</param>
	/// <param name="stateComparer">The state comparer for detailed field comparison.</param>
	/// <param name="config">The configuration for state tracking.</param>
	public StateSnapshotManager(StateFieldAnalyzer fieldAnalyzer, StateComparer stateComparer, WhyDidYouRenderConfig config)
	{
		_fieldAnalyzer = fieldAnalyzer ?? throw new ArgumentNullException(nameof(fieldAnalyzer));
		_stateComparer = stateComparer ?? throw new ArgumentNullException(nameof(stateComparer));
		_config = config ?? throw new ArgumentNullException(nameof(config));

		var storageConfig = new StorageConfiguration
		{
			MaxSnapshots = _config.MaxTrackedComponents,
			MaxSnapshotAgeMinutes = _config.MaxStateSnapshotAgeMinutes,
			UseObjectPooling = true,
		};
		_optimizedStorage = new OptimizedSnapshotStorage(storageConfig);
	}

	/// <summary>
	/// Captures a new state snapshot for the specified component.
	/// </summary>
	/// <param name="component">The component to capture state from.</param>
	/// <returns>The captured state snapshot.</returns>
	public StateSnapshot CaptureSnapshot(ComponentBase component)
	{
		ArgumentNullException.ThrowIfNull(component);

		if (!_config.EnableStateTracking)
			return new StateSnapshot(component.GetType(), new Dictionary<string, object?>());

		try
		{
			var metadata = _fieldAnalyzer.AnalyzeComponentType(component.GetType());
			var snapshot = StateSnapshot.Create(component, metadata);

			_optimizedStorage.StoreSnapshot(component, snapshot);
			PerformPeriodicCleanup();

			return snapshot;
		}
		catch (Exception)
		{
			return new StateSnapshot(component.GetType(), new Dictionary<string, object?>());
		}
	}

	/// <summary>
	/// Gets the current state snapshot for a component, or null if none exists.
	/// </summary>
	/// <param name="component">The component to get the snapshot for.</param>
	/// <returns>The current snapshot, or null if none exists.</returns>
	public StateSnapshot? GetCurrentSnapshot(ComponentBase component)
	{
		ArgumentNullException.ThrowIfNull(component);

		return _optimizedStorage.GetSnapshot(component);
	}

	/// <summary>
	/// Compares the current state of a component with its previous snapshot and returns changes.
	/// </summary>
	/// <param name="component">The component to analyze.</param>
	/// <returns>A tuple containing whether changes occurred and the list of changes.</returns>
	public (bool HasChanges, IEnumerable<StateChange> Changes) DetectStateChanges(ComponentBase component)
	{
		ArgumentNullException.ThrowIfNull(component);

		if (!_config.EnableStateTracking)
			return (false, Enumerable.Empty<StateChange>());

		try
		{
			var previousSnapshot = GetCurrentSnapshot(component);

			// capture new snapshot WITHOUT storing it yet
			var metadata = _fieldAnalyzer.AnalyzeComponentType(component.GetType());
			var currentSnapshot = StateSnapshot.Create(component, metadata);

			if (previousSnapshot == null)
			{
				// first time tracking this component - store the snapshot
				_optimizedStorage.StoreSnapshot(component, currentSnapshot);
				PerformPeriodicCleanup();

				return (currentSnapshot.HasValues, currentSnapshot.GetChangesFrom(null));
			}

			var comparisonResult = _stateComparer.GetDetailedComparison(previousSnapshot, currentSnapshot, metadata);
			var hasChanges = comparisonResult.HasChanges;
			var changes = hasChanges ? currentSnapshot.GetChangesFrom(previousSnapshot) : Enumerable.Empty<StateChange>();

			// store the new snapshot for next comparison
			_optimizedStorage.StoreSnapshot(component, currentSnapshot);
			PerformPeriodicCleanup();

			return (hasChanges, changes);
		}
		catch (Exception)
		{
			// if detection fails, assume no changes
			return (false, Enumerable.Empty<StateChange>());
		}
	}

	/// <summary>
	/// Clears all stored snapshots. Useful for testing or memory management.
	/// </summary>
	public void ClearAllSnapshots() => _optimizedStorage.Clear();

	/// <summary>
	/// Gets statistics about the snapshot manager for diagnostic purposes.
	/// </summary>
	/// <returns>A dictionary containing diagnostic information.</returns>
	public Dictionary<string, object> GetStatistics()
	{
		var storageInfo = _optimizedStorage.GetStorageInfo();
		var storageStats = _optimizedStorage.GetStatistics();

		return new Dictionary<string, object>
		{
			["TrackedComponentCount"] = storageInfo.ActiveSnapshots,
			["TotalSnapshots"] = storageInfo.TotalSnapshots,
			["EstimatedMemoryUsage"] = storageInfo.EstimatedMemoryUsage,
			["StorageHitRatio"] = storageStats.HitRatio,
			["PooledDictionaries"] = storageInfo.PooledDictionaries,
			["LastCleanupTime"] = _lastCleanup,
			["IsStateTrackingEnabled"] = _config.EnableStateTracking,
			["FieldAnalyzerCacheSize"] = _fieldAnalyzer.GetCacheSize(),
		};
	}

	/// <summary>
	/// Performs cleanup of snapshots for components that may no longer be active.
	/// This is called periodically to prevent memory leaks.
	/// </summary>
	public void PerformCleanup()
	{
		_optimizedStorage.PerformCleanup();
		_lastCleanup = DateTime.UtcNow;
	}

	/// <summary>
	/// Performs periodic cleanup if enough time has passed since the last cleanup.
	/// </summary>
	private void PerformPeriodicCleanup()
	{
		var timeSinceLastCleanup = DateTime.UtcNow - _lastCleanup;
		var storageInfo = _optimizedStorage.GetStorageInfo();

		if (timeSinceLastCleanup.TotalMinutes > 10 || storageInfo.TotalSnapshots > 1000)
			PerformCleanup();
	}
}
