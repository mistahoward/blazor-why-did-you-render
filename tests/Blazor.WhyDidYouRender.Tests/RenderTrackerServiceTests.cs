using Blazor.WhyDidYouRender.Abstractions;
using Blazor.WhyDidYouRender.Attributes;
using Blazor.WhyDidYouRender.Configuration;
using Blazor.WhyDidYouRender.Core;
using Blazor.WhyDidYouRender.Helpers;
using Blazor.WhyDidYouRender.Logging;
using Blazor.WhyDidYouRender.Records;
using Microsoft.AspNetCore.Components;

namespace Blazor.WhyDidYouRender.Tests;

public class RenderTrackerServiceTests
{
	private sealed class DummyComponent : ComponentBase { }

	private sealed class ParameterComponent : ComponentBase
	{
		[Parameter]
		public string? Text { get; set; }
	}

	private sealed class StatefulComponent : ComponentBase
	{
		// auto-tracked simple field used to exercise state tracking end-to-end.
		private int _counter;

		public int Counter => _counter;

		public void Increment() => _counter++;
	}

	private sealed class MixedStateComponent : ComponentBase
	{
		// auto-tracked simple field (tracked by default when AutoTrackSimpleTypes is enabled).
		private int _autoTrackedCounter;

		// explicitly tracked complex field with collection content tracking.
		[TrackState(TrackCollectionContents = true)]
		private readonly List<string> _trackedItems = [];

		// would normally be auto-tracked, but explicitly ignored.
		[IgnoreState]
		private int _ignoredCounter;

		// Has both TrackState and IgnoreState; IgnoreState should win and exclude it.
		[TrackState(TrackCollectionContents = true)]
		[IgnoreState]
		private readonly List<string> _ignoredItemsWithTrackState = [];

		public void MutateTracked()
		{
			_autoTrackedCounter++;
			_trackedItems.Add("tracked");
		}

		public void MutateIgnored()
		{
			_ignoredCounter++;
			_ignoredItemsWithTrackState.Add("ignored");
		}
	}

	private sealed class CollectionStateComponent : ComponentBase
	{
		[TrackState(TrackCollectionContents = true)]
		private readonly List<int> _values = [];

		public void AddValue(int value) => _values.Add(value);
	}

	private sealed class TestLogger : IWhyDidYouRenderLogger
	{
		public List<RenderEvent> RenderEvents { get; } = new();
		public List<string> InfoMessages { get; } = new();
		public LogLevel CurrentLevel { get; private set; } = LogLevel.Info;
		public string? CorrelationId { get; private set; } = null;

		public void LogDebug(string message, Dictionary<string, object?>? data = null) { }

		public void LogInfo(string message, Dictionary<string, object?>? data = null)
		{
			InfoMessages.Add(message);
		}

		public void LogWarning(string message, Dictionary<string, object?>? data = null) { }

		public void LogError(string message, Exception? exception = null, Dictionary<string, object?>? data = null) { }

		public void LogRenderEvent(RenderEvent renderEvent)
		{
			RenderEvents.Add(renderEvent);
		}

		public void LogParameterChanges(string componentName, Dictionary<string, object?> changes) { }

		public void LogPerformance(string componentName, string method, double durationMs, Dictionary<string, object?>? metrics = null) { }

		public void LogStateChange(
			string componentName,
			string fieldName,
			object? previousValue,
			object? currentValue,
			string changeType
		) { }

		public void LogUnnecessaryRerender(string componentName, string reason, Dictionary<string, object?>? context = null) { }

		public void LogFrequentRerender(
			string componentName,
			int renderCount,
			TimeSpan timeSpan,
			Dictionary<string, object?>? context = null
		) { }

		public void LogInitialization(string componentName, Dictionary<string, object?>? config = null) { }

		public void LogDisposal(string componentName) { }

		public void LogException(
			string componentName,
			Exception exception,
			string operation,
			Dictionary<string, object?>? context = null
		) { }

		public void SetCorrelationId(string correlationId)
		{
			CorrelationId = correlationId;
		}

		public string? GetCorrelationId() => CorrelationId;

		public void ClearCorrelationId()
		{
			CorrelationId = null;
		}

		public bool IsEnabled(LogLevel level) => level >= CurrentLevel;

		public void SetLogLevel(LogLevel level)
		{
			CurrentLevel = level;
		}

		public LogLevel GetLogLevel() => CurrentLevel;
	}

	private sealed class TestBrowserConsoleLogger : IBrowserConsoleLogger
	{
		public List<RenderEvent> RenderEvents { get; } = new();
		public bool InitializeCalled { get; private set; }
		public IErrorTracker? ErrorTracker { get; private set; }

		public void SetErrorTracker(IErrorTracker errorTracker)
		{
			ErrorTracker = errorTracker;
		}

		public Task InitializeAsync()
		{
			InitializeCalled = true;
			return Task.CompletedTask;
		}

		public Task LogRenderEventAsync(RenderEvent renderEvent)
		{
			RenderEvents.Add(renderEvent);
			return Task.CompletedTask;
		}

		public Task LogMessageAsync(string message, string level = "log") => Task.CompletedTask;

		public ValueTask DisposeAsync() => ValueTask.CompletedTask;
	}

	[Fact]
	public void TrackRender_WhenDisabled_DoesNotLog()
	{
		var tracker = RenderTrackerService.Instance;
		var logger = new TestLogger();
		RenderTrackerService.SetUnifiedLogger(logger);

		tracker.Configure(cfg =>
		{
			cfg.Enabled = false;
			cfg.Output = TrackingOutput.Console;
			cfg.TrackParameterChanges = true;
			cfg.TrackPerformance = false;
			cfg.IncludeSessionInfo = false;
			cfg.DetectUnnecessaryRerenders = false;
			cfg.EnableStateTracking = false;
			cfg.LogStateChanges = false;
		});

		tracker.ClearAllTrackingData();

		var component = new DummyComponent();
		tracker.TrackRender(component, "OnParametersSet", false);

		Assert.Empty(logger.RenderEvents);
	}

	[Fact]
	public void TrackRender_WithMinimalConfig_LogsBasicRenderEvent()
	{
		var tracker = RenderTrackerService.Instance;
		var logger = new TestLogger();
		RenderTrackerService.SetUnifiedLogger(logger);

		tracker.Configure(cfg =>
		{
			cfg.Enabled = true;
			cfg.Output = TrackingOutput.Console;
			cfg.TrackParameterChanges = false;
			cfg.TrackPerformance = false;
			cfg.IncludeSessionInfo = false;
			cfg.DetectUnnecessaryRerenders = false;
			cfg.EnableStateTracking = false;
			cfg.LogStateChanges = false;
			cfg.FrequentRerenderThreshold = 5.0; // use default threshold explicitly to avoid test cross-talk
		});

		tracker.ClearAllTrackingData();

		var component = new DummyComponent();
		tracker.TrackRender(component, "OnParametersSet", false);

		var renderEvent = Assert.Single(logger.RenderEvents);
		Assert.Equal(nameof(DummyComponent), renderEvent.ComponentName);
		Assert.Equal(typeof(DummyComponent).FullName, renderEvent.ComponentType);
		Assert.Equal("OnParametersSet", renderEvent.Method);
		Assert.False(renderEvent.FirstRender);
		Assert.Null(renderEvent.ParameterChanges);
		Assert.Null(renderEvent.StateChanges);
		Assert.False(renderEvent.IsUnnecessaryRerender);
		Assert.False(renderEvent.IsFrequentRerender);
		Assert.Null(renderEvent.SessionId);
		Assert.Null(renderEvent.DurationMs);
	}

	[Fact]
	public void TrackRender_LogOnlyWhenParametersChange_SuppressesOnParametersSetWithoutChanges()
	{
		var tracker = RenderTrackerService.Instance;
		var logger = new TestLogger();
		RenderTrackerService.SetUnifiedLogger(logger);

		tracker.Configure(cfg =>
		{
			cfg.Enabled = true;
			cfg.Output = TrackingOutput.Console;
			cfg.TrackParameterChanges = true;
			cfg.LogOnlyWhenParametersChange = true;
			cfg.TrackPerformance = false;
			cfg.IncludeSessionInfo = false;
			cfg.DetectUnnecessaryRerenders = false;
			cfg.EnableStateTracking = false;
			cfg.LogStateChanges = false;
		});

		tracker.ClearAllTrackingData();

		// parameterComponent has a [Parameter] property that remains null, so
		// DetectParameterChanges returns null and, with LogOnlyWhenParametersChange
		// enabled, OnParametersSet should not be logged at all.
		var component = new ParameterComponent();
		tracker.TrackRender(component, "OnParametersSet", false);

		Assert.Empty(logger.RenderEvents);
	}

	[Fact]
	public void TrackRender_WithBrowserOutput_LogsToUnifiedLoggerAndBrowserLogger()
	{
		var tracker = RenderTrackerService.Instance;
		var logger = new TestLogger();
		var browserLogger = new TestBrowserConsoleLogger();
		RenderTrackerService.SetUnifiedLogger(logger);

		tracker.Configure(cfg =>
		{
			cfg.Enabled = true;
			cfg.Output = TrackingOutput.BrowserConsole;
			cfg.TrackParameterChanges = false;
			cfg.TrackPerformance = false;
			cfg.IncludeSessionInfo = false;
			cfg.DetectUnnecessaryRerenders = false;
			cfg.EnableStateTracking = false;
			cfg.LogStateChanges = false;
		});

		tracker.ClearAllTrackingData();
		tracker.SetBrowserLogger(browserLogger);

		var component = new DummyComponent();
		tracker.TrackRender(component, "OnParametersSet", false);

		var loggerEvent = Assert.Single(logger.RenderEvents);
		var browserEvent = Assert.Single(browserLogger.RenderEvents);
		Assert.Same(loggerEvent, browserEvent);
		Assert.Equal(nameof(DummyComponent), loggerEvent.ComponentName);
		Assert.Equal("OnParametersSet", loggerEvent.Method);
	}

	[Fact]
	public void TrackRender_StateHasChanged_WithNoStateChanges_LogsSameUnnecessaryRerenderEvent_ToUnifiedAndBrowserLoggers()
	{
		var tracker = RenderTrackerService.Instance;
		var logger = new TestLogger();
		var browserLogger = new TestBrowserConsoleLogger();
		RenderTrackerService.SetUnifiedLogger(logger);

		tracker.Configure(cfg =>
		{
			cfg.Enabled = true;
			cfg.Output = TrackingOutput.BrowserConsole;
			cfg.TrackParameterChanges = false;
			cfg.TrackPerformance = false;
			cfg.IncludeSessionInfo = false;
			cfg.DetectUnnecessaryRerenders = true;
			cfg.EnableStateTracking = true;
			cfg.LogStateChanges = true;
			cfg.LogDetailedStateChanges = false;
		});

		tracker.ClearAllTrackingData();
		tracker.SetBrowserLogger(browserLogger);

		var component = new StatefulComponent();

		// first render primes the state snapshot for this component.
		tracker.TrackRender(component, "OnParametersSet", false);

		// second render via StateHasChanged without any state mutation should be
		// considered unnecessary when state tracking is enabled.
		tracker.TrackRender(component, "StateHasChanged", false);

		Assert.NotEmpty(logger.RenderEvents);
		Assert.NotEmpty(browserLogger.RenderEvents);

		var loggerEvent = logger.RenderEvents[^1];
		var browserEvent = browserLogger.RenderEvents[^1];

		Assert.Same(loggerEvent, browserEvent);
		Assert.Equal(nameof(StatefulComponent), loggerEvent.ComponentName);
		Assert.Equal("StateHasChanged", loggerEvent.Method);
		Assert.True(loggerEvent.IsUnnecessaryRerender);
		Assert.Equal("StateHasChanged called but no state changes detected", loggerEvent.UnnecessaryRerenderReason);
		Assert.Equal(loggerEvent.IsUnnecessaryRerender, browserEvent.IsUnnecessaryRerender);
		Assert.Equal(loggerEvent.UnnecessaryRerenderReason, browserEvent.UnnecessaryRerenderReason);
	}

	[Fact]
	public void TrackRender_StateHasChanged_WithNoStateChanges_IsMarkedUnnecessary_WhenStateTrackingEnabled()
	{
		var tracker = RenderTrackerService.Instance;
		var logger = new TestLogger();
		RenderTrackerService.SetUnifiedLogger(logger);

		tracker.Configure(cfg =>
		{
			cfg.Enabled = true;
			cfg.Output = TrackingOutput.Console;
			cfg.TrackParameterChanges = false;
			cfg.TrackPerformance = false;
			cfg.IncludeSessionInfo = false;
			cfg.DetectUnnecessaryRerenders = true;
			cfg.EnableStateTracking = true;
			cfg.LogStateChanges = true;
			cfg.LogDetailedStateChanges = false;
		});

		tracker.ClearAllTrackingData();

		var component = new StatefulComponent();

		// first render primes the state snapshot for this component.
		tracker.TrackRender(component, "OnParametersSet", false);

		// second render via StateHasChanged without any state mutation should be
		// considered unnecessary when state tracking is enabled.
		tracker.TrackRender(component, "StateHasChanged", false);

		var renderEvent = Assert.Single(logger.RenderEvents);
		Assert.Equal(nameof(StatefulComponent), renderEvent.ComponentName);
		Assert.Equal("StateHasChanged", renderEvent.Method);
		Assert.True(renderEvent.IsUnnecessaryRerender);
		Assert.Equal("StateHasChanged called but no state changes detected", renderEvent.UnnecessaryRerenderReason);
		Assert.NotNull(renderEvent.StateChanges);
		Assert.Empty(renderEvent.StateChanges);
	}

	[Fact]
	public void TrackRender_StateHasChanged_WithStateChanges_IsNotUnnecessary_AndIncludesStateChanges()
	{
		var tracker = RenderTrackerService.Instance;
		var logger = new TestLogger();
		RenderTrackerService.SetUnifiedLogger(logger);

		tracker.Configure(cfg =>
		{
			cfg.Enabled = true;
			cfg.Output = TrackingOutput.Console;
			cfg.TrackParameterChanges = false;
			cfg.TrackPerformance = false;
			cfg.IncludeSessionInfo = false;
			cfg.DetectUnnecessaryRerenders = true;
			cfg.EnableStateTracking = true;
			cfg.LogStateChanges = true;
			cfg.LogDetailedStateChanges = true;
		});

		tracker.ClearAllTrackingData();

		var component = new StatefulComponent();

		// baseline snapshot with initial counter value
		tracker.TrackRender(component, "OnParametersSet", false);

		// mutate state before calling StateHasChanged so that the snapshot manager
		// detects a real state change.
		component.Increment();
		tracker.TrackRender(component, "StateHasChanged", false);

		var renderEvent = Assert.Single(logger.RenderEvents);
		Assert.Equal(nameof(StatefulComponent), renderEvent.ComponentName);
		Assert.Equal("StateHasChanged", renderEvent.Method);
		Assert.False(renderEvent.IsUnnecessaryRerender);
		Assert.NotNull(renderEvent.UnnecessaryRerenderReason);
		Assert.Contains("State changes detected", renderEvent.UnnecessaryRerenderReason);
		Assert.NotNull(renderEvent.StateChanges);
		Assert.NotEmpty(renderEvent.StateChanges);
	}

	[Fact]
	public void TrackRender_StateHasChanged_MixedTrackAndIgnoreState_OnlyTrackedFieldsAppearInStateChanges()
	{
		var tracker = RenderTrackerService.Instance;
		var logger = new TestLogger();
		RenderTrackerService.SetUnifiedLogger(logger);

		tracker.Configure(cfg =>
		{
			cfg.Enabled = true;
			cfg.Output = TrackingOutput.Console;
			cfg.TrackParameterChanges = false;
			cfg.TrackPerformance = false;
			cfg.IncludeSessionInfo = false;
			cfg.DetectUnnecessaryRerenders = true;
			cfg.EnableStateTracking = true;
			cfg.LogStateChanges = true;
			cfg.LogDetailedStateChanges = true;
		});

		tracker.ClearAllTrackingData();

		var component = new MixedStateComponent();

		// prime the snapshot with initial values.
		tracker.TrackRender(component, "OnParametersSet", false);

		// mutate both tracked and ignored fields. Only the tracked ones should
		// appear in the resulting StateChanges list.
		component.MutateTracked();
		component.MutateIgnored();
		tracker.TrackRender(component, "StateHasChanged", false);

		Assert.NotEmpty(logger.RenderEvents);
		var renderEvent = logger.RenderEvents[^1];

		Assert.Equal(nameof(MixedStateComponent), renderEvent.ComponentName);
		Assert.Equal("StateHasChanged", renderEvent.Method);
		Assert.NotNull(renderEvent.StateChanges);
		var fieldNames = renderEvent.StateChanges!.Select(c => c.FieldName).ToList();

		// tracked fields should be present.
		Assert.Contains("_autoTrackedCounter", fieldNames);
		Assert.Contains("_trackedItems", fieldNames);

		// ignored fields should not appear in StateChanges.
		Assert.DoesNotContain("_ignoredCounter", fieldNames);
		Assert.DoesNotContain("_ignoredItemsWithTrackState", fieldNames);
	}

	[Fact]
	public void TrackRender_StateHasChanged_CollectionWithTrackCollectionContents_NoContentChange_ProducesNoStateChanges()
	{
		var tracker = RenderTrackerService.Instance;
		var logger = new TestLogger();
		RenderTrackerService.SetUnifiedLogger(logger);

		tracker.Configure(cfg =>
		{
			cfg.Enabled = true;
			cfg.Output = TrackingOutput.Console;
			cfg.TrackParameterChanges = false;
			cfg.TrackPerformance = false;
			cfg.IncludeSessionInfo = false;
			cfg.DetectUnnecessaryRerenders = true;
			cfg.EnableStateTracking = true;
			cfg.LogStateChanges = true;
			cfg.LogDetailedStateChanges = true;
		});

		tracker.ClearAllTrackingData();

		var component = new CollectionStateComponent();
		component.AddValue(1);

		// baseline snapshot with initial collection contents
		tracker.TrackRender(component, "OnParametersSet", false);

		// call StateHasChanged without mutating the collection; with
		// TrackCollectionContents enabled we should treat this as having no
		// state changes.
		tracker.TrackRender(component, "StateHasChanged", false);

		Assert.NotEmpty(logger.RenderEvents);
		var renderEvent = logger.RenderEvents[^1];
		Assert.Equal(nameof(CollectionStateComponent), renderEvent.ComponentName);
		Assert.Equal("StateHasChanged", renderEvent.Method);
		Assert.NotNull(renderEvent.StateChanges);
		Assert.Empty(renderEvent.StateChanges);
	}

	[Fact]
	public void TrackRender_FrequentRerenders_FlaggedAndLoggedToUnifiedAndBrowserLoggers()
	{
		var tracker = RenderTrackerService.Instance;
		var logger = new TestLogger();
		var browserLogger = new TestBrowserConsoleLogger();
		RenderTrackerService.SetUnifiedLogger(logger);

		tracker.Configure(cfg =>
		{
			cfg.Enabled = true;
			cfg.Output = TrackingOutput.BrowserConsole;
			cfg.TrackParameterChanges = false;
			cfg.TrackPerformance = false;
			cfg.IncludeSessionInfo = false;
			cfg.DetectUnnecessaryRerenders = false;
			cfg.EnableStateTracking = false;
			cfg.LogStateChanges = false;
			cfg.FrequentRerenderThreshold = 0.1; // extremely low threshold to make the test deterministic
		});

		tracker.ClearAllTrackingData();
		tracker.SetBrowserLogger(browserLogger);

		var component = new DummyComponent();

		// simulate a burst of renders that should exceed the configured threshold
		for (var i = 0; i < 5; i++)
		{
			tracker.TrackRender(component, "OnParametersSet", i == 0);
		}

		Assert.NotEmpty(logger.RenderEvents);
		Assert.NotEmpty(browserLogger.RenderEvents);

		var loggerEvent = logger.RenderEvents[^1];
		var browserEvent = browserLogger.RenderEvents[^1];

		Assert.Same(loggerEvent, browserEvent);
		Assert.Equal(nameof(DummyComponent), loggerEvent.ComponentName);
		Assert.Equal("OnParametersSet", loggerEvent.Method);
		Assert.True(loggerEvent.IsFrequentRerender);
		Assert.True(browserEvent.IsFrequentRerender);
	}

	#region StateHasChanged Batching Detection Tests

	[Fact]
	public void RenderEvent_IsBatchedRender_ReturnsTrueWhenStateHasChangedCallCountGreaterThanOne()
	{
		var renderEvent = new RenderEvent
		{
			ComponentName = "TestComponent",
			ComponentType = "Test.TestComponent",
			Method = "OnAfterRender",
			StateHasChangedCallCount = 5,
		};

		Assert.True(renderEvent.IsBatchedRender);
		Assert.Equal(5, renderEvent.StateHasChangedCallCount);
	}

	[Fact]
	public void RenderEvent_IsBatchedRender_ReturnsFalseWhenStateHasChangedCallCountIsOne()
	{
		var renderEvent = new RenderEvent
		{
			ComponentName = "TestComponent",
			ComponentType = "Test.TestComponent",
			Method = "OnAfterRender",
			StateHasChangedCallCount = 1,
		};

		Assert.False(renderEvent.IsBatchedRender);
	}

	[Fact]
	public void RenderEvent_IsBatchedRender_ReturnsFalseWhenStateHasChangedCallCountIsZero()
	{
		var renderEvent = new RenderEvent
		{
			ComponentName = "TestComponent",
			ComponentType = "Test.TestComponent",
			Method = "OnAfterRender",
			StateHasChangedCallCount = 0,
		};

		Assert.False(renderEvent.IsBatchedRender);
	}

	[Fact]
	public void TrackRender_WithStateHasChangedCallCount_IncludesCountInRenderEvent()
	{
		var tracker = RenderTrackerService.Instance;
		var logger = new TestLogger();
		RenderTrackerService.SetUnifiedLogger(logger);

		tracker.Configure(cfg =>
		{
			cfg.Enabled = true;
			cfg.Output = TrackingOutput.Console;
			cfg.TrackParameterChanges = false;
			cfg.TrackPerformance = false;
			cfg.IncludeSessionInfo = false;
			cfg.DetectUnnecessaryRerenders = false;
			cfg.EnableStateTracking = false;
			cfg.LogStateChanges = false;
		});

		tracker.ClearAllTrackingData();

		var component = new DummyComponent();
		// Simulate OnAfterRender with 3 StateHasChanged calls that were batched
		tracker.TrackRender(component, "OnAfterRender", false, stateHasChangedCallCount: 3);

		var renderEvent = Assert.Single(logger.RenderEvents);
		Assert.Equal(nameof(DummyComponent), renderEvent.ComponentName);
		Assert.Equal("OnAfterRender", renderEvent.Method);
		Assert.Equal(3, renderEvent.StateHasChangedCallCount);
		Assert.True(renderEvent.IsBatchedRender);
	}

	[Fact]
	public void TrackRender_WithSingleStateHasChangedCall_IsNotBatched()
	{
		var tracker = RenderTrackerService.Instance;
		var logger = new TestLogger();
		RenderTrackerService.SetUnifiedLogger(logger);

		tracker.Configure(cfg =>
		{
			cfg.Enabled = true;
			cfg.Output = TrackingOutput.Console;
			cfg.TrackParameterChanges = false;
			cfg.TrackPerformance = false;
			cfg.IncludeSessionInfo = false;
			cfg.DetectUnnecessaryRerenders = false;
			cfg.EnableStateTracking = false;
			cfg.LogStateChanges = false;
		});

		tracker.ClearAllTrackingData();

		var component = new DummyComponent();
		tracker.TrackRender(component, "OnAfterRender", false, stateHasChangedCallCount: 1);

		var renderEvent = Assert.Single(logger.RenderEvents);
		Assert.Equal(1, renderEvent.StateHasChangedCallCount);
		Assert.False(renderEvent.IsBatchedRender);
	}

	[Fact]
	public void TrackRender_BatchedRender_LoggedToUnifiedAndBrowserLoggers()
	{
		var tracker = RenderTrackerService.Instance;
		var logger = new TestLogger();
		var browserLogger = new TestBrowserConsoleLogger();
		RenderTrackerService.SetUnifiedLogger(logger);

		tracker.Configure(cfg =>
		{
			cfg.Enabled = true;
			cfg.Output = TrackingOutput.BrowserConsole;
			cfg.TrackParameterChanges = false;
			cfg.TrackPerformance = false;
			cfg.IncludeSessionInfo = false;
			cfg.DetectUnnecessaryRerenders = false;
			cfg.EnableStateTracking = false;
			cfg.LogStateChanges = false;
		});

		tracker.ClearAllTrackingData();
		tracker.SetBrowserLogger(browserLogger);

		var component = new DummyComponent();
		tracker.TrackRender(component, "OnAfterRender", false, stateHasChangedCallCount: 5);

		var loggerEvent = Assert.Single(logger.RenderEvents);
		var browserEvent = Assert.Single(browserLogger.RenderEvents);

		Assert.Same(loggerEvent, browserEvent);
		Assert.Equal(5, loggerEvent.StateHasChangedCallCount);
		Assert.True(loggerEvent.IsBatchedRender);
		Assert.Equal(loggerEvent.StateHasChangedCallCount, browserEvent.StateHasChangedCallCount);
		Assert.Equal(loggerEvent.IsBatchedRender, browserEvent.IsBatchedRender);
	}

	#endregion
}
