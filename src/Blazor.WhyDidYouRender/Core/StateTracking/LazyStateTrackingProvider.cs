using Blazor.WhyDidYouRender.Configuration;
using Blazor.WhyDidYouRender.Records;
using Microsoft.AspNetCore.Components;

namespace Blazor.WhyDidYouRender.Core.StateTracking;

/// <summary>
/// Provides lazy initialization and loading of state tracking components to improve startup performance.
/// This class defers expensive operations until they're actually needed.
/// </summary>
public class LazyStateTrackingProvider
{
	/// <summary>
	/// Lazy initialization of the state field analyzer.
	/// </summary>
	private readonly Lazy<StateFieldAnalyzer> _stateFieldAnalyzer;

	/// <summary>
	/// Lazy initialization of the state comparer.
	/// </summary>
	private readonly Lazy<StateComparer> _stateComparer;

	/// <summary>
	/// Lazy initialization of the snapshot manager.
	/// </summary>
	private readonly Lazy<StateSnapshotManager> _snapshotManager;

	/// <summary>
	/// Lazy initialization of the performance monitor.
	/// </summary>
	private readonly Lazy<StateTrackingPerformanceMonitor> _performanceMonitor;

	/// <summary>
	/// Configuration for state tracking.
	/// </summary>
	private readonly WhyDidYouRenderConfig _config;

	/// <summary>
	/// Tracks whether state tracking has been initialized.
	/// </summary>
	private volatile bool _isInitialized = false;

	/// <summary>
	/// Synchronization object for initialization.
	/// </summary>
	private readonly Lock _initializationLock = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="LazyStateTrackingProvider"/> class.
	/// </summary>
	/// <param name="config">The configuration for state tracking.</param>
	public LazyStateTrackingProvider(WhyDidYouRenderConfig config)
	{
		_config = config ?? throw new ArgumentNullException(nameof(config));

		_stateFieldAnalyzer = new Lazy<StateFieldAnalyzer>(
			() => new StateFieldAnalyzer(_config),
			LazyThreadSafetyMode.ExecutionAndPublication
		);

		_stateComparer = new Lazy<StateComparer>(() => new StateComparer(), LazyThreadSafetyMode.ExecutionAndPublication);

		_snapshotManager = new Lazy<StateSnapshotManager>(
			() => new StateSnapshotManager(_stateFieldAnalyzer.Value, _stateComparer.Value, _config),
			LazyThreadSafetyMode.ExecutionAndPublication
		);

		_performanceMonitor = new Lazy<StateTrackingPerformanceMonitor>(
			() => new StateTrackingPerformanceMonitor(),
			LazyThreadSafetyMode.ExecutionAndPublication
		);
	}

	/// <summary>
	/// Gets the state field analyzer, initializing it if necessary.
	/// </summary>
	public StateFieldAnalyzer StateFieldAnalyzer
	{
		get
		{
			EnsureInitialized();
			return _stateFieldAnalyzer.Value;
		}
	}

	/// <summary>
	/// Gets the state comparer, initializing it if necessary.
	/// </summary>
	public StateComparer StateComparer
	{
		get
		{
			EnsureInitialized();
			return _stateComparer.Value;
		}
	}

	/// <summary>
	/// Gets the snapshot manager, initializing it if necessary.
	/// </summary>
	public StateSnapshotManager SnapshotManager
	{
		get
		{
			EnsureInitialized();
			return _snapshotManager.Value;
		}
	}

	/// <summary>
	/// Gets the performance monitor, initializing it if necessary.
	/// </summary>
	public StateTrackingPerformanceMonitor PerformanceMonitor
	{
		get
		{
			EnsureInitialized();
			return _performanceMonitor.Value;
		}
	}

	/// <summary>
	/// Gets whether state tracking is enabled in the configuration.
	/// </summary>
	public bool IsStateTrackingEnabled => _config.EnableStateTracking;

	/// <summary>
	/// Gets whether the state tracking components have been initialized.
	/// </summary>
	public bool IsInitialized => _isInitialized;

	/// <summary>
	/// Ensures that state tracking components are initialized.
	/// </summary>
	private void EnsureInitialized()
	{
		if (!_config.EnableStateTracking)
			throw new InvalidOperationException("State tracking is disabled in configuration");

		if (_isInitialized)
			return;

		lock (_initializationLock)
		{
			if (_isInitialized)
				return;

			// force initialization of all lazy components
			_ = _stateFieldAnalyzer.Value;
			_ = _stateComparer.Value;
			_ = _snapshotManager.Value;
			_ = _performanceMonitor.Value;

			_isInitialized = true;
		}
	}

	/// <summary>
	/// Initializes state tracking components asynchronously.
	/// This method can be called during application startup to warm up the components.
	/// </summary>
	/// <returns>A task representing the initialization operation.</returns>
	public async Task InitializeAsync()
	{
		if (!_config.EnableStateTracking)
		{
			return;
		}

		await Task.Run(() => EnsureInitialized());
	}

	/// <summary>
	/// Pre-warms the cache by analyzing common component types.
	/// </summary>
	/// <param name="componentTypes">Component types to pre-analyze.</param>
	/// <returns>A task representing the pre-warming operation.</returns>
	public async Task PreWarmCacheAsync(IEnumerable<Type> componentTypes)
	{
		if (!_config.EnableStateTracking || !_isInitialized)
		{
			return;
		}

		var analyzer = StateFieldAnalyzer;
		var tasks = componentTypes.Where(t => typeof(ComponentBase).IsAssignableFrom(t)).Select(t => analyzer.AnalyzeComponentTypeAsync(t));

		await Task.WhenAll(tasks);
	}

	/// <summary>
	/// Gets comprehensive state tracking information for diagnostics.
	/// </summary>
	/// <returns>Diagnostic information about the state tracking system.</returns>
	public StateTrackingDiagnostics GetDiagnostics()
	{
		return new StateTrackingDiagnostics
		{
			IsEnabled = _config.EnableStateTracking,
			IsInitialized = _isInitialized,
			FieldAnalyzerInitialized = _stateFieldAnalyzer.IsValueCreated,
			StateComparerInitialized = _stateComparer.IsValueCreated,
			SnapshotManagerInitialized = _snapshotManager.IsValueCreated,
			PerformanceMonitorInitialized = _performanceMonitor.IsValueCreated,
			CacheInfo = _isInitialized && _stateFieldAnalyzer.IsValueCreated ? _stateFieldAnalyzer.Value.GetCacheInfo() : null,
			PerformanceSummary =
				_isInitialized && _performanceMonitor.IsValueCreated ? _performanceMonitor.Value.GetPerformanceSummary() : null,
		};
	}

	/// <summary>
	/// Performs maintenance on all initialized components.
	/// </summary>
	public void PerformMaintenance()
	{
		if (!_isInitialized)
		{
			return;
		}

		if (_stateFieldAnalyzer.IsValueCreated)
		{
			_stateFieldAnalyzer.Value.PerformCacheMaintenance();
		}

		if (_snapshotManager.IsValueCreated)
		{
			_snapshotManager.Value.PerformCleanup();
		}
	}

	/// <summary>
	/// Resets all state tracking components to their initial state.
	/// </summary>
	public void Reset()
	{
		if (_stateFieldAnalyzer.IsValueCreated)
		{
			_stateFieldAnalyzer.Value.ClearCache();
		}

		if (_snapshotManager.IsValueCreated)
		{
			_snapshotManager.Value.ClearAllSnapshots();
		}

		if (_performanceMonitor.IsValueCreated)
		{
			_performanceMonitor.Value.Reset();
		}
	}

	/// <summary>
	/// Safely executes an operation with performance monitoring if available.
	/// </summary>
	/// <typeparam name="T">The return type of the operation.</typeparam>
	/// <param name="operationName">The name of the operation for monitoring.</param>
	/// <param name="operation">The operation to execute.</param>
	/// <returns>The result of the operation.</returns>
	public T ExecuteWithMonitoring<T>(string operationName, Func<T> operation)
	{
		if (_isInitialized && _performanceMonitor.IsValueCreated)
		{
			return _performanceMonitor.Value.MeasureOperation(operationName, operation);
		}

		return operation();
	}

	/// <summary>
	/// Safely executes an operation with performance monitoring if available.
	/// </summary>
	/// <param name="operationName">The name of the operation for monitoring.</param>
	/// <param name="operation">The operation to execute.</param>
	public void ExecuteWithMonitoring(string operationName, Action operation)
	{
		if (_isInitialized && _performanceMonitor.IsValueCreated)
		{
			_performanceMonitor.Value.MeasureOperation(operationName, operation);
		}
		else
		{
			operation();
		}
	}
}
