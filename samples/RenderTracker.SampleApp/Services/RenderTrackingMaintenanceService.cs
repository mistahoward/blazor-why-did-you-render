using Blazor.WhyDidYouRender.Core;

namespace RenderTracker.SampleApp.Services;

/// <summary>
/// Background service that performs periodic maintenance on the render tracking system.
/// This service uses the previously unused PerformMaintenance() method to keep the system optimized.
/// </summary>
public class RenderTrackingMaintenanceService : BackgroundService
{
	private readonly ILogger<RenderTrackingMaintenanceService> _logger;
	private readonly TimeSpan _maintenanceInterval;

	/// <summary>
	/// Initializes a new instance of the <see cref="RenderTrackingMaintenanceService"/> class.
	/// </summary>
	/// <param name="logger">Logger for the service.</param>
	public RenderTrackingMaintenanceService(ILogger<RenderTrackingMaintenanceService> logger)
	{
		_logger = logger;
		_maintenanceInterval = TimeSpan.FromMinutes(5); // Run maintenance every 5 minutes
	}

	/// <summary>
	/// Executes the background maintenance task.
	/// </summary>
	/// <param name="stoppingToken">Cancellation token to stop the service.</param>
	/// <returns>A task representing the background operation.</returns>
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation(
			"RenderTrackingMaintenanceService started. Maintenance will run every {Interval} minutes.",
			_maintenanceInterval.TotalMinutes
		);

		while (!stoppingToken.IsCancellationRequested)
		{
			try
			{
				await Task.Delay(_maintenanceInterval, stoppingToken);

				if (stoppingToken.IsCancellationRequested)
					break;

				await PerformMaintenanceAsync();
			}
			catch (OperationCanceledException)
			{
				// Expected when cancellation is requested
				break;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error occurred during render tracking maintenance");

				// Continue running even if maintenance fails
				await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
			}
		}

		_logger.LogInformation("RenderTrackingMaintenanceService stopped.");
	}

	/// <summary>
	/// Performs maintenance on the render tracking system using the previously unused PerformMaintenance method.
	/// </summary>
	private async Task PerformMaintenanceAsync()
	{
		try
		{
			_logger.LogDebug("Starting render tracking maintenance...");

			var renderTracker = RenderTrackerService.Instance;

			// Get tracking counts before maintenance
			var countsBefore = renderTracker.GetTrackedComponentCounts();
			var totalBefore = countsBefore.Values.Sum();

			// Use the previously unused PerformMaintenance method!
			renderTracker.PerformMaintenance();

			// Get tracking counts after maintenance
			var countsAfter = renderTracker.GetTrackedComponentCounts();
			var totalAfter = countsAfter.Values.Sum();

			// Get diagnostics to check system health
			var diagnostics = renderTracker.GetStateTrackingDiagnostics();

			_logger.LogInformation(
				"Render tracking maintenance completed. Components tracked: {Before} -> {After}. "
					+ "State tracking: Enabled={StateEnabled}, Initialized={StateInitialized}",
				totalBefore,
				totalAfter,
				diagnostics?.IsEnabled ?? false,
				diagnostics?.IsInitialized ?? false
			);

			// Log detailed breakdown if there are significant changes
			if (Math.Abs(totalBefore - totalAfter) > 10)
			{
				_logger.LogInformation("Significant change in tracked components detected:");
				foreach (var kvp in countsBefore)
				{
					var afterCount = countsAfter.GetValueOrDefault(kvp.Key, 0);
					if (kvp.Value != afterCount)
					{
						_logger.LogInformation("  {System}: {Before} -> {After}", kvp.Key, kvp.Value, afterCount);
					}
				}
			}

			// Log cache information if available
			if (diagnostics?.CacheInfo != null)
			{
				_logger.LogDebug(
					"Cache statistics: Types={CachedTypes}, Hits={CacheHits}, Misses={CacheMisses}, Hit Ratio={HitRatio:P2}",
					diagnostics.CacheInfo.TotalEntries,
					diagnostics.CacheInfo.Statistics.Hits,
					diagnostics.CacheInfo.Statistics.Misses,
					diagnostics.CacheInfo.Statistics.HitRate
				);
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to perform render tracking maintenance");
			throw;
		}
	}

	/// <summary>
	/// Performs an immediate maintenance operation (can be called externally).
	/// </summary>
	/// <returns>A task representing the maintenance operation.</returns>
	public async Task PerformImmediateMaintenanceAsync()
	{
		_logger.LogInformation("Performing immediate render tracking maintenance...");
		await PerformMaintenanceAsync();
	}

	/// <summary>
	/// Gets the current status of the maintenance service.
	/// </summary>
	/// <returns>Status information about the maintenance service.</returns>
	public MaintenanceServiceStatus GetStatus()
	{
		var renderTracker = RenderTrackerService.Instance;
		var trackingCounts = renderTracker.GetTrackedComponentCounts();
		var diagnostics = renderTracker.GetStateTrackingDiagnostics();

		return new MaintenanceServiceStatus
		{
			IsRunning = !ExecuteTask?.IsCompleted ?? false,
			MaintenanceInterval = _maintenanceInterval,
			LastMaintenanceTime = DateTime.Now, // This would be tracked in a real implementation
			TotalTrackedComponents = trackingCounts.Values.Sum(),
			TrackingCounts = trackingCounts,
			StateTrackingEnabled = diagnostics?.IsEnabled ?? false,
			StateTrackingInitialized = diagnostics?.IsInitialized ?? false,
		};
	}
}

/// <summary>
/// Status information for the maintenance service.
/// </summary>
public class MaintenanceServiceStatus
{
	/// <summary>
	/// Gets or sets whether the maintenance service is currently running.
	/// </summary>
	public bool IsRunning { get; set; }

	/// <summary>
	/// Gets or sets the maintenance interval.
	/// </summary>
	public TimeSpan MaintenanceInterval { get; set; }

	/// <summary>
	/// Gets or sets the last time maintenance was performed.
	/// </summary>
	public DateTime LastMaintenanceTime { get; set; }

	/// <summary>
	/// Gets or sets the total number of tracked components.
	/// </summary>
	public int TotalTrackedComponents { get; set; }

	/// <summary>
	/// Gets or sets the tracking counts by system.
	/// </summary>
	public Dictionary<string, int> TrackingCounts { get; set; } = new();

	/// <summary>
	/// Gets or sets whether state tracking is enabled.
	/// </summary>
	public bool StateTrackingEnabled { get; set; }

	/// <summary>
	/// Gets or sets whether state tracking is initialized.
	/// </summary>
	public bool StateTrackingInitialized { get; set; }
}
