using System.Diagnostics;
using System.Diagnostics.Metrics;
using Blazor.WhyDidYouRender.Configuration;
using Blazor.WhyDidYouRender.Logging;
using Blazor.WhyDidYouRender.Records;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace Blazor.WhyDidYouRender.Tests;

/// <summary>
/// Holds captured metric measurement data (value + tags).
/// </summary>
public sealed record MetricMeasurement<T>(T Value, KeyValuePair<string, object?>[] Tags);

/// <summary>
/// Tests for OpenTelemetry/Aspire integration via AspireWhyDidYouRenderLogger.
/// Validates that ActivitySource spans and Meter metrics are emitted correctly.
/// </summary>
public sealed class AspireOpenTelemetryTests : IDisposable
{
	private sealed class DummyComponent : ComponentBase { }

	private readonly ActivityListener _activityListener;
	private readonly List<Activity> _capturedActivities = new();
	private readonly MeterListener _meterListener;
	private readonly Dictionary<string, List<MetricMeasurement<long>>> _capturedCounters = new();
	private readonly Dictionary<string, List<MetricMeasurement<double>>> _capturedHistograms = new();

	public AspireOpenTelemetryTests()
	{
		// Set up activity listener to capture spans from "Blazor.WhyDidYouRender" source
		_activityListener = new ActivityListener
		{
			ShouldListenTo = source => source.Name == "Blazor.WhyDidYouRender",
			Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
			ActivityStarted = activity => _capturedActivities.Add(activity),
		};
		ActivitySource.AddActivityListener(_activityListener);

		// Set up meter listener to capture metrics from "Blazor.WhyDidYouRender" meter
		_meterListener = new MeterListener
		{
			InstrumentPublished = (instrument, listener) =>
			{
				if (instrument.Meter.Name == "Blazor.WhyDidYouRender")
				{
					listener.EnableMeasurementEvents(instrument);
				}
			},
		};
		_meterListener.SetMeasurementEventCallback<long>(OnMeasurement);
		_meterListener.SetMeasurementEventCallback<double>(OnMeasurement);
		_meterListener.Start();
	}

	private void OnMeasurement(Instrument instrument, long measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
	{
		if (!_capturedCounters.TryGetValue(instrument.Name, out var list))
		{
			list = new();
			_capturedCounters[instrument.Name] = list;
		}
		list.Add(new MetricMeasurement<long>(measurement, tags.ToArray()));
	}

	private void OnMeasurement(Instrument instrument, double measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
	{
		if (!_capturedHistograms.TryGetValue(instrument.Name, out var list))
		{
			list = new();
			_capturedHistograms[instrument.Name] = list;
		}
		list.Add(new MetricMeasurement<double>(measurement, tags.ToArray()));
	}

	public void Dispose()
	{
		_activityListener.Dispose();
		_meterListener.Dispose();
	}

	#region Activity/Span Creation Tests

	[Fact]
	public void LogRenderEvent_WithOtelTracesEnabled_CreatesActivityWithCorrectTags()
	{
		var config = new WhyDidYouRenderConfig
		{
			EnableOpenTelemetry = true,
			EnableOtelTraces = true,
			EnableOtelMetrics = false,
		};
		var logger = new AspireWhyDidYouRenderLogger(config);
		var renderEvent = new RenderEvent
		{
			ComponentName = "TestComponent",
			ComponentType = "Test.Namespace.TestComponent",
			Method = "OnParametersSet",
			FirstRender = true,
			DurationMs = 5.5,
			IsUnnecessaryRerender = false,
			IsFrequentRerender = false,
		};

		logger.LogRenderEvent(renderEvent);

		Assert.NotEmpty(_capturedActivities);
		var activity = _capturedActivities.First(a => a.OperationName == "WhyDidYouRender.Render");
		Assert.NotNull(activity);
		Assert.Equal("render", activity.GetTagItem("wdyrl.event"));
		Assert.Equal("TestComponent", activity.GetTagItem("wdyrl.component"));
		Assert.Equal("Test.Namespace.TestComponent", activity.GetTagItem("wdyrl.component.type"));
		Assert.Equal("OnParametersSet", activity.GetTagItem("wdyrl.method"));
		Assert.Equal(true, activity.GetTagItem("wdyrl.first_render"));
		Assert.Equal(5.5, activity.GetTagItem("wdyrl.duration.ms"));
		Assert.Equal(false, activity.GetTagItem("wdyrl.unnecessary"));
		Assert.Equal(false, activity.GetTagItem("wdyrl.frequent"));
	}

	[Fact]
	public void LogRenderEvent_WithUnnecessaryRerender_IncludesReasonTag()
	{
		var config = new WhyDidYouRenderConfig
		{
			EnableOpenTelemetry = true,
			EnableOtelTraces = true,
			EnableOtelMetrics = false,
		};
		var logger = new AspireWhyDidYouRenderLogger(config);
		var renderEvent = new RenderEvent
		{
			ComponentName = "TestComponent",
			ComponentType = "Test.TestComponent",
			Method = "StateHasChanged",
			IsUnnecessaryRerender = true,
			UnnecessaryRerenderReason = "StateHasChanged called but no state changes detected",
			IsFrequentRerender = false,
		};

		logger.LogRenderEvent(renderEvent);

		var activity = _capturedActivities.Last(a => a.OperationName == "WhyDidYouRender.Render");
		Assert.NotNull(activity);
		Assert.Equal(true, activity.GetTagItem("wdyrl.unnecessary"));
		Assert.Equal("StateHasChanged called but no state changes detected", activity.GetTagItem("wdyrl.reason"));
	}

	[Fact]
	public void LogRenderEvent_WithStateChanges_IncludesStateChangeCountTag()
	{
		var config = new WhyDidYouRenderConfig
		{
			EnableOpenTelemetry = true,
			EnableOtelTraces = true,
			EnableOtelMetrics = false,
		};
		var logger = new AspireWhyDidYouRenderLogger(config);
		var renderEvent = new RenderEvent
		{
			ComponentName = "TestComponent",
			ComponentType = "Test.TestComponent",
			Method = "StateHasChanged",
			StateChanges = new List<StateChange>
			{
				new()
				{
					FieldName = "Counter",
					PreviousValue = 0,
					CurrentValue = 1,
					ChangeType = StateChangeType.Modified,
				},
				new()
				{
					FieldName = "Name",
					PreviousValue = "old",
					CurrentValue = "new",
					ChangeType = StateChangeType.Modified,
				},
			},
		};

		logger.LogRenderEvent(renderEvent);

		var activity = _capturedActivities.Last(a => a.OperationName == "WhyDidYouRender.Render");
		Assert.NotNull(activity);
		Assert.Equal(2, activity.GetTagItem("wdyrl.state.change.count"));
	}

	[Fact]
	public void LogRenderEvent_WithOtelTracesDisabled_DoesNotCreateActivity()
	{
		var config = new WhyDidYouRenderConfig
		{
			EnableOpenTelemetry = true,
			EnableOtelTraces = false,
			EnableOtelMetrics = false,
		};
		var logger = new AspireWhyDidYouRenderLogger(config);
		var initialCount = _capturedActivities.Count;
		var renderEvent = new RenderEvent
		{
			ComponentName = "TestComponent",
			ComponentType = "Test.TestComponent",
			Method = "OnParametersSet",
		};

		logger.LogRenderEvent(renderEvent);

		// No new activity should be created when traces are disabled
		Assert.Equal(initialCount, _capturedActivities.Count);
	}

	#endregion

	#region Metrics Emission Tests

	[Fact]
	public void LogRenderEvent_WithOtelMetricsEnabled_RecordsRenderCounter()
	{
		var config = new WhyDidYouRenderConfig
		{
			EnableOpenTelemetry = true,
			EnableOtelTraces = false,
			EnableOtelMetrics = true,
		};
		var logger = new AspireWhyDidYouRenderLogger(config);
		var renderEvent = new RenderEvent
		{
			ComponentName = "TestComponent",
			ComponentType = "Test.TestComponent",
			Method = "OnParametersSet",
			IsUnnecessaryRerender = false,
			IsFrequentRerender = false,
		};

		logger.LogRenderEvent(renderEvent);
		_meterListener.RecordObservableInstruments();

		Assert.True(_capturedCounters.ContainsKey("wdyrl.renders"));
		var renders = _capturedCounters["wdyrl.renders"];
		Assert.NotEmpty(renders);

		var measurement = renders.Last();
		Assert.Equal(1, measurement.Value);
		var tagDict = measurement.Tags.ToDictionary(t => t.Key, t => t.Value);
		Assert.Equal("TestComponent", tagDict["component"]);
		Assert.Equal("OnParametersSet", tagDict["method"]);
		Assert.Equal(false, tagDict["unnecessary"]);
		Assert.Equal(false, tagDict["frequent"]);
	}

	[Fact]
	public void LogRenderEvent_WithUnnecessaryRerender_RecordsUnnecessaryCounter()
	{
		var config = new WhyDidYouRenderConfig
		{
			EnableOpenTelemetry = true,
			EnableOtelTraces = false,
			EnableOtelMetrics = true,
		};
		var logger = new AspireWhyDidYouRenderLogger(config);
		var renderEvent = new RenderEvent
		{
			ComponentName = "TestComponent",
			ComponentType = "Test.TestComponent",
			Method = "StateHasChanged",
			IsUnnecessaryRerender = true,
			UnnecessaryRerenderReason = "No changes detected",
			IsFrequentRerender = false,
		};

		logger.LogRenderEvent(renderEvent);
		_meterListener.RecordObservableInstruments();

		Assert.True(_capturedCounters.ContainsKey("wdyrl.rerenders.unnecessary"));
		var unnecessary = _capturedCounters["wdyrl.rerenders.unnecessary"];
		Assert.NotEmpty(unnecessary);

		var measurement = unnecessary.Last();
		Assert.Equal(1, measurement.Value);
		var tagDict = measurement.Tags.ToDictionary(t => t.Key, t => t.Value);
		Assert.Equal("TestComponent", tagDict["component"]);
		Assert.Equal("No changes detected", tagDict["reason"]);
	}

	[Fact]
	public void LogRenderEvent_WithDuration_RecordsDurationHistogram()
	{
		var config = new WhyDidYouRenderConfig
		{
			EnableOpenTelemetry = true,
			EnableOtelTraces = false,
			EnableOtelMetrics = true,
		};
		var logger = new AspireWhyDidYouRenderLogger(config);
		var renderEvent = new RenderEvent
		{
			ComponentName = "TestComponent",
			ComponentType = "Test.TestComponent",
			Method = "OnAfterRender",
			DurationMs = 12.34,
			IsUnnecessaryRerender = false,
			IsFrequentRerender = false,
		};

		logger.LogRenderEvent(renderEvent);
		_meterListener.RecordObservableInstruments();

		Assert.True(_capturedHistograms.ContainsKey("wdyrl.render.duration.ms"));
		var durations = _capturedHistograms["wdyrl.render.duration.ms"];
		Assert.NotEmpty(durations);

		var measurement = durations.Last();
		Assert.Equal(12.34, measurement.Value);
		var tagDict = measurement.Tags.ToDictionary(t => t.Key, t => t.Value);
		Assert.Equal("TestComponent", tagDict["component"]);
		Assert.Equal("OnAfterRender", tagDict["method"]);
	}

	[Fact]
	public void LogRenderEvent_WithOtelMetricsDisabled_DoesNotRecordMetrics()
	{
		var config = new WhyDidYouRenderConfig
		{
			EnableOpenTelemetry = true,
			EnableOtelTraces = false,
			EnableOtelMetrics = false,
		};
		var logger = new AspireWhyDidYouRenderLogger(config);
		_capturedCounters.Clear();
		_capturedHistograms.Clear();

		var renderEvent = new RenderEvent
		{
			ComponentName = "TestComponent",
			ComponentType = "Test.TestComponent",
			Method = "OnParametersSet",
			DurationMs = 5.0,
		};

		logger.LogRenderEvent(renderEvent);
		_meterListener.RecordObservableInstruments();

		// No metrics should be recorded when metrics are disabled
		Assert.Empty(_capturedCounters);
		Assert.Empty(_capturedHistograms);
	}

	#endregion

	#region ComponentWhitelist Filtering Tests

	[Fact]
	public void LogRenderEvent_WithComponentWhitelist_OnlyLogsWhitelistedComponents()
	{
		var config = new WhyDidYouRenderConfig
		{
			EnableOpenTelemetry = true,
			EnableOtelTraces = true,
			EnableOtelMetrics = true,
			ComponentWhitelist = new HashSet<string> { "AllowedComponent" },
		};
		var logger = new AspireWhyDidYouRenderLogger(config);
		var initialActivityCount = _capturedActivities.Count;
		_capturedCounters.Clear();

		// This component is NOT in the whitelist
		var blockedEvent = new RenderEvent
		{
			ComponentName = "BlockedComponent",
			ComponentType = "Test.BlockedComponent",
			Method = "OnParametersSet",
		};
		logger.LogRenderEvent(blockedEvent);

		// Verify no activity or metrics for blocked component
		Assert.Equal(initialActivityCount, _capturedActivities.Count);
		Assert.Empty(_capturedCounters);

		// This component IS in the whitelist
		var allowedEvent = new RenderEvent
		{
			ComponentName = "AllowedComponent",
			ComponentType = "Test.AllowedComponent",
			Method = "OnParametersSet",
		};
		logger.LogRenderEvent(allowedEvent);
		_meterListener.RecordObservableInstruments();

		// Verify activity and metrics were recorded for allowed component
		Assert.True(_capturedActivities.Count > initialActivityCount);
		Assert.True(_capturedCounters.ContainsKey("wdyrl.renders"));
	}

	[Fact]
	public void LogRenderEvent_WithEmptyComponentWhitelist_LogsAllComponents()
	{
		var config = new WhyDidYouRenderConfig
		{
			EnableOpenTelemetry = true,
			EnableOtelTraces = true,
			EnableOtelMetrics = false,
			ComponentWhitelist = new HashSet<string>(), // Empty whitelist
		};
		var logger = new AspireWhyDidYouRenderLogger(config);
		var initialCount = _capturedActivities.Count;

		var renderEvent = new RenderEvent
		{
			ComponentName = "AnyComponent",
			ComponentType = "Test.AnyComponent",
			Method = "OnParametersSet",
		};
		logger.LogRenderEvent(renderEvent);

		// Empty whitelist should allow all components
		Assert.True(_capturedActivities.Count > initialCount);
	}

	#endregion

	#region Correlation ID Tests

	[Fact]
	public void LogRenderEvent_WithCorrelationId_IncludesCorrelationIdTag()
	{
		var config = new WhyDidYouRenderConfig
		{
			EnableOpenTelemetry = true,
			EnableOtelTraces = true,
			EnableOtelMetrics = false,
		};
		var logger = new AspireWhyDidYouRenderLogger(config);
		logger.SetCorrelationId("test-correlation-123");

		var renderEvent = new RenderEvent
		{
			ComponentName = "TestComponent",
			ComponentType = "Test.TestComponent",
			Method = "OnParametersSet",
		};
		logger.LogRenderEvent(renderEvent);

		var activity = _capturedActivities.Last(a => a.OperationName == "WhyDidYouRender.Render");
		Assert.NotNull(activity);
		Assert.Equal("test-correlation-123", activity.GetTagItem("wdyrl.correlationId"));
	}

	[Fact]
	public void CorrelationId_CanBeSetAndCleared()
	{
		var config = new WhyDidYouRenderConfig();
		var logger = new AspireWhyDidYouRenderLogger(config);

		Assert.Null(logger.GetCorrelationId());

		logger.SetCorrelationId("my-correlation-id");
		Assert.Equal("my-correlation-id", logger.GetCorrelationId());

		logger.ClearCorrelationId();
		Assert.Null(logger.GetCorrelationId());
	}

	#endregion

	#region Basic Logging Methods Tests

	[Fact]
	public void LogDebug_CreatesActivityWithMessageTag()
	{
		var config = new WhyDidYouRenderConfig { MinimumLogLevel = LogLevel.Debug };
		var logger = new AspireWhyDidYouRenderLogger(config);
		logger.SetLogLevel(LogLevel.Debug);

		logger.LogDebug("Debug message", new Dictionary<string, object?> { ["key"] = "value" });

		var activity = _capturedActivities.LastOrDefault(a => a.OperationName == "WhyDidYouRender.Debug");
		Assert.NotNull(activity);
		Assert.Equal("Debug message", activity.GetTagItem("wdyrl.message"));
		Assert.Equal("value", activity.GetTagItem("wdyrl.key"));
	}

	[Fact]
	public void LogInfo_CreatesActivityWithMessageTag()
	{
		var config = new WhyDidYouRenderConfig();
		var logger = new AspireWhyDidYouRenderLogger(config);

		logger.LogInfo("Info message", new Dictionary<string, object?> { ["detail"] = 42 });

		var activity = _capturedActivities.LastOrDefault(a => a.OperationName == "WhyDidYouRender.Info");
		Assert.NotNull(activity);
		Assert.Equal("Info message", activity.GetTagItem("wdyrl.message"));
		Assert.Equal(42, activity.GetTagItem("wdyrl.detail"));
	}

	[Fact]
	public void LogWarning_CreatesActivityWithMessageTag()
	{
		var config = new WhyDidYouRenderConfig();
		var logger = new AspireWhyDidYouRenderLogger(config);

		logger.LogWarning("Warning message");

		var activity = _capturedActivities.LastOrDefault(a => a.OperationName == "WhyDidYouRender.Warning");
		Assert.NotNull(activity);
		Assert.Equal("Warning message", activity.GetTagItem("wdyrl.message"));
	}

	[Fact]
	public void LogError_CreatesActivityWithExceptionMetadata()
	{
		var config = new WhyDidYouRenderConfig();
		var logger = new AspireWhyDidYouRenderLogger(config);
		var exception = new InvalidOperationException("Test exception");

		logger.LogError("Error message", exception);

		var activity = _capturedActivities.LastOrDefault(a => a.OperationName == "WhyDidYouRender.Error");
		Assert.NotNull(activity);
		Assert.Equal("Error message", activity.GetTagItem("wdyrl.message"));
		Assert.Equal("InvalidOperationException", activity.GetTagItem("wdyrl.exception.type"));
		Assert.Equal("Test exception", activity.GetTagItem("wdyrl.exception.message"));
	}

	[Fact]
	public void LogDebug_WithLogLevelInfo_DoesNotCreateActivity()
	{
		var config = new WhyDidYouRenderConfig();
		var logger = new AspireWhyDidYouRenderLogger(config);
		logger.SetLogLevel(LogLevel.Info); // Debug should be filtered out
		var initialCount = _capturedActivities.Count;

		logger.LogDebug("This should not be logged");

		Assert.Equal(initialCount, _capturedActivities.Count);
	}

	#endregion

	#region CompositeLogger Activity Correlation Tests

	private sealed class TestInnerLogger : WhyDidYouRenderLoggerBase
	{
		public List<RenderEvent> ReceivedEvents { get; } = new();
		public Activity? AmbientActivityDuringLog { get; private set; }

		public TestInnerLogger()
			: base(new WhyDidYouRenderConfig()) { }

		public override void LogDebug(string message, Dictionary<string, object?>? data = null) { }

		public override void LogInfo(string message, Dictionary<string, object?>? data = null) { }

		public override void LogWarning(string message, Dictionary<string, object?>? data = null) { }

		public override void LogError(string message, Exception? exception = null, Dictionary<string, object?>? data = null) { }

		public override void LogRenderEvent(RenderEvent renderEvent)
		{
			ReceivedEvents.Add(renderEvent);
			AmbientActivityDuringLog = Activity.Current;
		}
	}

	[Fact]
	public void CompositeLogger_LogRenderEvent_CreatesAmbientActivity()
	{
		var config = new WhyDidYouRenderConfig();
		var innerLogger = new TestInnerLogger();
		var compositeLogger = new CompositeWhyDidYouRenderLogger(config, innerLogger);
		var renderEvent = new RenderEvent
		{
			ComponentName = "TestComponent",
			ComponentType = "Test.TestComponent",
			Method = "OnParametersSet",
			DurationMs = 3.5,
		};

		compositeLogger.LogRenderEvent(renderEvent);

		// Verify the inner logger received the event
		Assert.Single(innerLogger.ReceivedEvents);
		Assert.Same(renderEvent, innerLogger.ReceivedEvents[0]);

		// Verify an ambient activity was available during the inner logger call
		Assert.NotNull(innerLogger.AmbientActivityDuringLog);
		Assert.Equal("WhyDidYouRender.Render", innerLogger.AmbientActivityDuringLog.OperationName);
	}

	[Fact]
	public void CompositeLogger_LogRenderEvent_SetsBasicTagsOnActivity()
	{
		var config = new WhyDidYouRenderConfig();
		var innerLogger = new TestInnerLogger();
		var compositeLogger = new CompositeWhyDidYouRenderLogger(config, innerLogger);
		var renderEvent = new RenderEvent
		{
			ComponentName = "Counter",
			ComponentType = "App.Counter",
			Method = "StateHasChanged",
			DurationMs = 7.89,
		};

		compositeLogger.LogRenderEvent(renderEvent);

		var activity = innerLogger.AmbientActivityDuringLog;
		Assert.NotNull(activity);
		Assert.Equal("render", activity.GetTagItem("wdyrl.event"));
		Assert.Equal("Counter", activity.GetTagItem("wdyrl.component"));
		Assert.Equal("StateHasChanged", activity.GetTagItem("wdyrl.method"));
		Assert.Equal(7.89, activity.GetTagItem("wdyrl.duration.ms"));
	}

	[Fact]
	public void CompositeLogger_ForwardsAllMethodsToInnerLoggers()
	{
		var config = new WhyDidYouRenderConfig();
		var inner1 = new TestInnerLogger();
		var inner2 = new TestInnerLogger();
		var compositeLogger = new CompositeWhyDidYouRenderLogger(config, inner1, inner2);
		var renderEvent = new RenderEvent
		{
			ComponentName = "TestComponent",
			ComponentType = "Test.TestComponent",
			Method = "OnParametersSet",
		};

		compositeLogger.LogRenderEvent(renderEvent);

		// Both inner loggers should receive the same event
		Assert.Single(inner1.ReceivedEvents);
		Assert.Single(inner2.ReceivedEvents);
		Assert.Same(renderEvent, inner1.ReceivedEvents[0]);
		Assert.Same(renderEvent, inner2.ReceivedEvents[0]);
	}

	[Fact]
	public void CompositeLogger_WithAspireLogger_AspireReuseAmbientActivity()
	{
		var config = new WhyDidYouRenderConfig
		{
			EnableOpenTelemetry = true,
			EnableOtelTraces = true,
			EnableOtelMetrics = false,
		};
		var aspireLogger = new AspireWhyDidYouRenderLogger(config);
		var testLogger = new TestInnerLogger();
		var compositeLogger = new CompositeWhyDidYouRenderLogger(config, testLogger, aspireLogger);
		var initialActivityCount = _capturedActivities.Count;

		var renderEvent = new RenderEvent
		{
			ComponentName = "TestComponent",
			ComponentType = "Test.TestComponent",
			Method = "OnParametersSet",
		};

		compositeLogger.LogRenderEvent(renderEvent);

		// Composite logger creates one activity, Aspire logger should reuse it (not create a new one)
		// We should see exactly one new WhyDidYouRender.Render activity
		var newActivities = _capturedActivities.Skip(initialActivityCount).Where(a => a.OperationName == "WhyDidYouRender.Render").ToList();
		Assert.Single(newActivities);
	}

	#endregion
}
