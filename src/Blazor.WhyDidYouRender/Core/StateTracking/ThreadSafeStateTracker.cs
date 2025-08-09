using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Blazor.WhyDidYouRender.Configuration;
using Blazor.WhyDidYouRender.Records.StateTracking;

namespace Blazor.WhyDidYouRender.Core.StateTracking;

/// <summary>
/// Thread-safe wrapper for state tracking operations, optimized for Blazor Server scenarios
/// where multiple threads may access component state simultaneously.
/// </summary>
public class ThreadSafeStateTracker {
	/// <summary>
	/// Thread-safe storage for component state snapshots.
	/// </summary>
	private readonly ConcurrentDictionary<ComponentKey, ThreadSafeSnapshotEntry> _componentSnapshots = new();

	/// <summary>
	/// Reader-writer locks for component-specific operations.
	/// </summary>
	private readonly ConcurrentDictionary<ComponentKey, ReaderWriterLockSlim> _componentLocks = new();

	/// <summary>
	/// The underlying state tracking provider.
	/// </summary>
	private readonly LazyStateTrackingProvider _stateTrackingProvider;

	/// <summary>
	/// Configuration for threading behavior.
	/// </summary>
	private readonly ThreadingConfiguration _threadingConfig;

	/// <summary>
	/// Statistics for threading operations.
	/// </summary>
	private readonly MutableThreadingStatistics _statistics = new();

	/// <summary>
	/// Semaphore to limit concurrent operations.
	/// </summary>
	private readonly SemaphoreSlim _concurrencyLimiter;

	/// <summary>
	/// Initializes a new instance of the <see cref="ThreadSafeStateTracker"/> class.
	/// </summary>
	/// <param name="stateTrackingProvider">The state tracking provider.</param>
	/// <param name="config">The configuration.</param>
	/// <param name="threadingConfig">Threading-specific configuration.</param>
	public ThreadSafeStateTracker(
		LazyStateTrackingProvider stateTrackingProvider,
		WhyDidYouRenderConfig config,
		ThreadingConfiguration? threadingConfig = null) {
		_stateTrackingProvider = stateTrackingProvider ?? throw new ArgumentNullException(nameof(stateTrackingProvider));
		_threadingConfig = threadingConfig ?? new ThreadingConfiguration();

		_concurrencyLimiter = new SemaphoreSlim(
			_threadingConfig.MaxConcurrentOperations,
			_threadingConfig.MaxConcurrentOperations);
	}

	/// <summary>
	/// Captures a state snapshot for a component in a thread-safe manner.
	/// </summary>
	/// <param name="component">The component to capture state for.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The captured state snapshot.</returns>
	public async Task<StateSnapshot?> CaptureSnapshotAsync(ComponentBase component, CancellationToken cancellationToken = default) {
		if (component == null || !_stateTrackingProvider.IsStateTrackingEnabled)
			return null;

		var componentKey = new ComponentKey(component);

		await _concurrencyLimiter.WaitAsync(cancellationToken);

		try {
			var lockSlim = GetOrCreateLock(componentKey);

			lockSlim.EnterReadLock();
			try {
				var startTime = DateTime.UtcNow;
				var snapshot = _stateTrackingProvider.SnapshotManager.CaptureSnapshot(component);
				var duration = DateTime.UtcNow - startTime;

				_statistics.RecordOperation("CaptureSnapshot", duration, true);

				var entry = new ThreadSafeSnapshotEntry {
					Snapshot = snapshot,
					ThreadId = Environment.CurrentManagedThreadId,
					CapturedAt = DateTime.UtcNow
				};

				_componentSnapshots.AddOrUpdate(componentKey, entry, (_, _) => entry);

				return snapshot;
			}
			finally {
				lockSlim.ExitReadLock();
			}
		}
		catch (Exception) {
			_statistics.RecordOperation("CaptureSnapshot", TimeSpan.Zero, false);
			return null;
		}
		finally {
			_concurrencyLimiter.Release();
		}
	}

	/// <summary>
	/// Detects state changes for a component in a thread-safe manner.
	/// </summary>
	/// <param name="component">The component to analyze.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>State change detection results.</returns>
	public async Task<StateChangeResult> DetectStateChangesAsync(ComponentBase component, CancellationToken cancellationToken = default) {
		if (component == null || !_stateTrackingProvider.IsStateTrackingEnabled)
			return new StateChangeResult { HasChanges = false };

		var componentKey = new ComponentKey(component);

		await _concurrencyLimiter.WaitAsync(cancellationToken);

		try {
			var lockSlim = GetOrCreateLock(componentKey);

			// write lock ensures exclusive access during state change detection
			lockSlim.EnterWriteLock();
			try {
				var startTime = DateTime.UtcNow;
				var (hasChanges, changes) = _stateTrackingProvider.SnapshotManager.DetectStateChanges(component);
				var duration = DateTime.UtcNow - startTime;

				_statistics.RecordOperation("DetectStateChanges", duration, true);

				return new StateChangeResult {
					HasChanges = hasChanges,
					Changes = changes.ToList(),
					DetectedAt = DateTime.UtcNow,
					ThreadId = Thread.CurrentThread.ManagedThreadId
				};
			}
			finally {
				lockSlim.ExitWriteLock();
			}
		}
		catch (Exception ex) {
			_statistics.RecordOperation("DetectStateChanges", TimeSpan.Zero, false);
			return new StateChangeResult { HasChanges = false, Error = ex.Message };
		}
		finally {
			_concurrencyLimiter.Release();
		}
	}

	/// <summary>
	/// Removes state tracking for a component when it's disposed.
	/// </summary>
	/// <param name="component">The component to clean up.</param>
	public void CleanupComponent(ComponentBase component) {
		if (component == null) return;

		var componentKey = new ComponentKey(component);

		_componentSnapshots.TryRemove(componentKey, out _);

		if (_componentLocks.TryRemove(componentKey, out var lockSlim))
			lockSlim.Dispose();

		_statistics.RecordCleanup();
	}

	/// <summary>
	/// Performs bulk cleanup of components that are no longer active.
	/// </summary>
	/// <param name="activeComponents">Currently active components.</param>
	/// <returns>Number of components cleaned up.</returns>
	public int PerformBulkCleanup(IEnumerable<ComponentBase> activeComponents) {
		var activeKeys = new HashSet<ComponentKey>(activeComponents.Select(c => new ComponentKey(c)));
		var keysToRemove = new List<ComponentKey>();

		foreach (var key in _componentSnapshots.Keys)
			if (!activeKeys.Contains(key))
				keysToRemove.Add(key);

		// remove components no longer present
		foreach (var key in keysToRemove) {
			_componentSnapshots.TryRemove(key, out _);

			if (_componentLocks.TryRemove(key, out var lockSlim))
				lockSlim.Dispose();
		}

		_statistics.RecordBulkCleanup(keysToRemove.Count);
		return keysToRemove.Count;
	}

	/// <summary>
	/// Gets threading statistics for monitoring and diagnostics.
	/// </summary>
	/// <returns>Threading performance statistics.</returns>
	public ThreadingStatistics GetStatistics() =>
		_statistics.CreateSnapshot();

	/// <summary>
	/// Gets detailed threading information.
	/// </summary>
	/// <returns>Detailed threading information.</returns>
	public ThreadingInfo GetThreadingInfo() {
		var activeThreads = _componentSnapshots.Values
			.Select(e => e.ThreadId)
			.Distinct()
			.Count();

		return new ThreadingInfo {
			TrackedComponents = _componentSnapshots.Count,
			ActiveLocks = _componentLocks.Count,
			ActiveThreads = activeThreads,
			AvailableConcurrency = _concurrencyLimiter.CurrentCount,
			MaxConcurrency = _threadingConfig.MaxConcurrentOperations,
			Statistics = GetStatistics()
		};
	}



	/// <summary>
	/// Gets or creates a reader-writer lock for a component.
	/// </summary>
	/// <param name="componentKey">The component key.</param>
	/// <returns>A reader-writer lock for the component.</returns>
	private ReaderWriterLockSlim GetOrCreateLock(ComponentKey componentKey) =>
		_componentLocks.GetOrAdd(componentKey, _ => new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion));

	/// <summary>
	/// Disposes the thread-safe state tracker.
	/// </summary>
	public void Dispose() {
		foreach (var lockSlim in _componentLocks.Values) {
			lockSlim.Dispose();
		}

		_componentLocks.Clear();
		_componentSnapshots.Clear();
		_concurrencyLimiter.Dispose();
	}
}

/// <summary>
/// Mutable implementation of threading statistics for internal tracking.
/// </summary>
internal class MutableThreadingStatistics {
	private long _totalOperations = 0;
	private long _successfulOperations = 0;
	private long _failedOperations = 0;
	private long _cleanupOperations = 0;
	private long _bulkCleanupOperations = 0;
	private long _totalOperationTime = 0;
	private readonly DateTime _startTime = DateTime.UtcNow;

	public void RecordOperation(string operationType, TimeSpan duration, bool success) {
		Interlocked.Increment(ref _totalOperations);

		if (success) {
			Interlocked.Increment(ref _successfulOperations);
			Interlocked.Add(ref _totalOperationTime, duration.Ticks);
		}
		else {
			Interlocked.Increment(ref _failedOperations);
		}
	}

	public void RecordCleanup() => Interlocked.Increment(ref _cleanupOperations);
	public void RecordBulkCleanup(int count) => Interlocked.Add(ref _bulkCleanupOperations, count);

	public ThreadingStatistics CreateSnapshot() {
		return ThreadingStatistics.Create(
			_totalOperations,
			_successfulOperations,
			_failedOperations,
			_cleanupOperations,
			_bulkCleanupOperations,
			_totalOperationTime,
			_startTime
		);
	}
}


