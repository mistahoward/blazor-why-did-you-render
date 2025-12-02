using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Blazor.WhyDidYouRender.Configuration;
using Blazor.WhyDidYouRender.Core;
using Blazor.WhyDidYouRender.Helpers;
using Blazor.WhyDidYouRender.Logging;
using Blazor.WhyDidYouRender.Records;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Xunit;

namespace Blazor.WhyDidYouRender.Tests;

public sealed class LoggerParityTests
{
	private sealed class DummyComponent : ComponentBase { }

	private sealed class TestStructuredLogger : WhyDidYouRenderLoggerBase
	{
		public string? LastMessage { get; private set; }
		public Dictionary<string, object?>? LastData { get; private set; }
		public string? LastWarningMessage { get; private set; }
		public Dictionary<string, object?>? LastWarningData { get; private set; }

		public TestStructuredLogger()
			: base(new WhyDidYouRenderConfig()) { }

		public override void LogDebug(string message, Dictionary<string, object?>? data = null) { }

		public override void LogInfo(string message, Dictionary<string, object?>? data = null)
		{
			LastMessage = message;
			LastData = data;
		}

		public override void LogWarning(string message, Dictionary<string, object?>? data = null)
		{
			LastWarningMessage = message;
			LastWarningData = data;
		}

		public override void LogError(string message, Exception? exception = null, Dictionary<string, object?>? data = null) { }
	}

	private sealed record JsInvocation(string Identifier, object?[] Args);

	private sealed class TestJsRuntime : IJSRuntime
	{
		public List<JsInvocation> Invocations { get; } = new();

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
		{
			Invocations.Add(new JsInvocation(identifier, args ?? Array.Empty<object?>()));
			return ValueTask.FromResult(default(TValue)!);
		}

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
		{
			Invocations.Add(new JsInvocation(identifier, args ?? Array.Empty<object?>()));
			return ValueTask.FromResult(default(TValue)!);
		}
	}

	[Fact]
	public void WhyDidYouRenderLoggerBase_LogRenderEvent_UnnecessaryRerender_ProducesExpectedStructuredKeys()
	{
		var logger = new TestStructuredLogger();
		var ts = DateTime.UtcNow;
		var renderEvent = new RenderEvent
		{
			Timestamp = ts,
			ComponentName = "MyComponent",
			ComponentType = "My.Component.Type",
			Method = "StateHasChanged",
			DurationMs = 1.23,
			IsUnnecessaryRerender = true,
			UnnecessaryRerenderReason = "StateHasChanged called but no state changes detected",
			IsFrequentRerender = false,
		};

		logger.LogRenderEvent(renderEvent);

		Assert.NotNull(logger.LastData);
		var data = logger.LastData!;
		Assert.Equal("render", data["event"]);
		Assert.Equal(renderEvent.ComponentName, data["component"]);
		Assert.Equal(renderEvent.Method, data["method"]);
		Assert.Equal(renderEvent.Timestamp, data["timestamp"]);
		Assert.Equal(renderEvent.DurationMs, data["duration.ms"]);
		Assert.Equal(renderEvent.IsUnnecessaryRerender, data["unnecessary"]);
		Assert.Equal(renderEvent.IsFrequentRerender, data["frequent"]);
		Assert.Equal(renderEvent.UnnecessaryRerenderReason, data["reason"]);
	}

	[Fact]
	public async Task BrowserConsoleLogger_UnnecessaryRerender_EmitsGroupTableAndOptimizationWarning()
	{
		var js = new TestJsRuntime();
		var logger = new BrowserConsoleLogger(js);
		var renderEvent = new RenderEvent
		{
			ComponentName = "MyComponent",
			ComponentType = "My.Component.Type",
			Method = "StateHasChanged",
			IsUnnecessaryRerender = true,
			UnnecessaryRerenderReason = "StateHasChanged called but no state changes detected",
		};

		await logger.LogRenderEventAsync(renderEvent);

		Assert.Contains(js.Invocations, i => i.Identifier == "console.group");
		Assert.Contains(js.Invocations, i => i.Identifier == "console.table");

		var warn = Assert.Single(js.Invocations, i => i.Identifier == "console.warn");
		var warnMessage = Assert.IsType<string>(warn.Args[0]);
		Assert.Contains(renderEvent.UnnecessaryRerenderReason, warnMessage);

		var table = js.Invocations.First(i => i.Identifier == "console.table");
		var logData = table.Args[0]!;
		var componentProp = logData.GetType().GetProperty("component");
		Assert.NotNull(componentProp);
		Assert.Equal(renderEvent.ComponentName, componentProp!.GetValue(logData));
	}

	[Fact]
	public void WhyDidYouRenderLoggerBase_LogFrequentRerender_ProducesExpectedStructuredKeys()
	{
		var logger = new TestStructuredLogger();
		var componentName = "MyComponent";
		var renderCount = 10;
		var window = TimeSpan.FromSeconds(2);
		var extraContext = new Dictionary<string, object?> { ["extra"] = "value" };

		logger.LogFrequentRerender(componentName, renderCount, window, extraContext);

		Assert.NotNull(logger.LastWarningData);
		var data = logger.LastWarningData!;
		Assert.Equal("frequent-rerender", data["event"]);
		Assert.Equal(componentName, data["component"]);
		Assert.Equal(renderCount, data["renderCount"]);
		Assert.Equal(window.TotalMilliseconds, data["timeSpan.ms"]);
		Assert.Equal(renderCount / window.TotalSeconds, data["rendersPerSecond"]);
		Assert.Equal("value", data["extra"]);
	}

	[Fact]
	public async Task BrowserConsoleLogger_FrequentRerender_EmitsCollapsedGroupTableAndPerformanceWarning()
	{
		var js = new TestJsRuntime();
		var logger = new BrowserConsoleLogger(js);
		var renderEvent = new RenderEvent
		{
			ComponentName = "MyComponent",
			ComponentType = "My.Component.Type",
			Method = "StateHasChanged",
			IsFrequentRerender = true,
		};

		await logger.LogRenderEventAsync(renderEvent);

		var group = Assert.Single(js.Invocations, i => i.Identifier == "console.groupCollapsed");
		var header = Assert.IsType<string>(group.Args[0]);
		Assert.Contains("FREQUENT RE-RENDER", header);
		Assert.Contains(renderEvent.ComponentName, header);
		Assert.Contains(renderEvent.Method, header);

		Assert.Contains(js.Invocations, i => i.Identifier == "console.table");

		var warn = Assert.Single(js.Invocations, i => i.Identifier == "console.warn");
		var warnMessage = Assert.IsType<string>(warn.Args[0]);
		Assert.Contains("Performance Warning: This component is re-rendering frequently", warnMessage);
	}

	[Fact]
	public async Task BrowserConsoleLogger_LogsParameterAndStateChanges_WithDetailedTablesAndEntries()
	{
		var js = new TestJsRuntime();
		var logger = new BrowserConsoleLogger(js);
		var parameterChange = new { Previous = new { Value = 1, Text = "old" }, Current = new { Value = 2, Text = "new" } };
		var stateChanges = new List<StateChange>
		{
			new()
			{
				FieldName = "Counter",
				PreviousValue = 0,
				CurrentValue = 1,
				ChangeType = StateChangeType.Modified,
			},
		};

		var renderEvent = new RenderEvent
		{
			ComponentName = "MyComponent",
			ComponentType = "My.Component.Type",
			Method = "OnParametersSet",
			ParameterChanges = new Dictionary<string, object?> { ["Count"] = parameterChange },
			StateChanges = stateChanges,
		};

		await logger.LogRenderEventAsync(renderEvent);

		// Parameter changes header
		var paramHeader = Assert.Single(
			js.Invocations,
			i => i.Identifier == "console.log" && i.Args.Length > 0 && i.Args[0] is string s && s.Contains("Parameter Changes:")
		);

		// Parameter group label should include the name and icon
		var paramGroup = Assert.Single(js.Invocations, i => i.Identifier == "console.group");
		var groupLabel = Assert.IsType<string>(paramGroup.Args[0]);
		Assert.Contains("📝", groupLabel);
		Assert.Contains("Count", groupLabel);

		// Previous / Current logs should be present
		Assert.Contains(
			js.Invocations,
			i => i.Identifier == "console.log" && i.Args.Length > 0 && i.Args[0] is string s && s.Contains("Previous:")
		);

		Assert.Contains(
			js.Invocations,
			i => i.Identifier == "console.log" && i.Args.Length > 0 && i.Args[0] is string s && s.Contains("Current:")
		);

		// State changes header
		var stateHeader = Assert.Single(
			js.Invocations,
			i => i.Identifier == "console.log" && i.Args.Length > 0 && i.Args[0] is string s && s.Contains("State Changes:")
		);

		// There should be a comparison table for complex parameter values
		Assert.Contains(
			js.Invocations,
			i =>
				i.Identifier == "console.table"
				&& i.Args.Length > 0
				&& i.Args[0] is not null
				&& i.Args[0]!.GetType().GetProperty("Previous") != null
				&& i.Args[0]!.GetType().GetProperty("Current") != null
		);

		// There should be a table for state change details
		Assert.True(js.Invocations.Count(i => i.Identifier == "console.table") >= 2);
		var stateTableInvocation = js.Invocations.Last(i => i.Identifier == "console.table");
		var stateTableArg = stateTableInvocation.Args[0]!;
		var dict = Assert.IsAssignableFrom<IDictionary>(stateTableArg);
		Assert.True(dict.Contains("Counter"));
		var entry = dict["Counter"]!;
		var entryType = entry.GetType();
		Assert.NotNull(entryType.GetProperty("Previous"));
		Assert.NotNull(entryType.GetProperty("Current"));
		Assert.NotNull(entryType.GetProperty("Type"));
	}

	[Fact]
	public void RenderFrequencyTracker_StatsAndFrequentRerenderLogging_AreConsistent()
	{
		var config = new WhyDidYouRenderConfig { FrequentRerenderThreshold = 2.0 };
		var tracker = new RenderFrequencyTracker(config);
		var logger = new TestStructuredLogger();
		var component = new DummyComponent();

		bool isFrequent = false;
		for (var i = 0; i < 5; i++)
		{
			isFrequent = tracker.TrackRenderFrequency(component);
		}

		Assert.True(isFrequent);

		var stats = tracker.GetRenderStatistics(component);
		Assert.Equal(component.GetType().Name, stats.ComponentName);
		Assert.True(stats.IsFrequentRenderer);
		Assert.True(stats.RendersLastSecond >= 5);
		Assert.True(stats.RendersLastSecond > config.FrequentRerenderThreshold);

		logger.LogFrequentRerender(
			stats.ComponentName,
			stats.RendersLastSecond,
			TimeSpan.FromSeconds(1),
			new Dictionary<string, object?> { ["fromStats"] = true }
		);

		Assert.NotNull(logger.LastWarningData);
		var data = logger.LastWarningData!;
		Assert.Equal("frequent-rerender", data["event"]);
		Assert.Equal(stats.ComponentName, data["component"]);
		Assert.Equal(stats.RendersLastSecond, data["renderCount"]);
		Assert.Equal(1000.0, data["timeSpan.ms"]);
		Assert.Equal(stats.RendersLastSecond / 1.0, data["rendersPerSecond"]);
		Assert.Equal(true, data["fromStats"]);
	}

	#region OpenTelemetry, Server, and Browser Parity Tests

	/// <summary>
	/// Verifies that the same RenderEvent produces consistent data across all three logging backends:
	/// OpenTelemetry (ActivitySource), Server structured logs, and Browser console (JSInterop).
	/// </summary>
	[Fact]
	public async Task AllLoggers_SameRenderEvent_ProduceConsistentCoreData()
	{
		// Arrange: Create a RenderEvent with all key properties populated
		var ts = DateTime.UtcNow;
		var renderEvent = new RenderEvent
		{
			Timestamp = ts,
			ComponentName = "MyComponent",
			ComponentType = "My.Namespace.MyComponent",
			Method = "OnParametersSet",
			DurationMs = 5.67,
			FirstRender = false,
			IsUnnecessaryRerender = true,
			UnnecessaryRerenderReason = "StateHasChanged called but no state changes detected",
			IsFrequentRerender = true,
			SessionId = Guid.NewGuid().ToString(),
		};

		// --- Server structured logger ---
		var serverLogger = new TestStructuredLogger();
		serverLogger.LogRenderEvent(renderEvent);

		// --- Browser console logger ---
		var js = new TestJsRuntime();
		var browserLogger = new BrowserConsoleLogger(js);
		await browserLogger.LogRenderEventAsync(renderEvent);

		// --- OpenTelemetry logger ---
		var capturedActivities = new List<Activity>();
		using var activityListener = new ActivityListener
		{
			ShouldListenTo = source => source.Name == "Blazor.WhyDidYouRender",
			Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
			ActivityStarted = activity => capturedActivities.Add(activity),
		};
		ActivitySource.AddActivityListener(activityListener);

		var otelConfig = new WhyDidYouRenderConfig
		{
			EnableOpenTelemetry = true,
			EnableOtelTraces = true,
			EnableOtelMetrics = false,
		};
		var otelLogger = new AspireWhyDidYouRenderLogger(otelConfig);
		otelLogger.LogRenderEvent(renderEvent);

		// --- Assertions ---

		// 1. Server logger produces structured data with correct keys
		Assert.NotNull(serverLogger.LastData);
		var serverData = serverLogger.LastData!;
		Assert.Equal("render", serverData["event"]);
		Assert.Equal(renderEvent.ComponentName, serverData["component"]);
		Assert.Equal(renderEvent.Method, serverData["method"]);
		Assert.Equal(renderEvent.DurationMs, serverData["duration.ms"]);
		Assert.Equal(renderEvent.IsUnnecessaryRerender, serverData["unnecessary"]);
		Assert.Equal(renderEvent.IsFrequentRerender, serverData["frequent"]);
		Assert.Equal(renderEvent.UnnecessaryRerenderReason, serverData["reason"]);

		// 2. Browser logger emits console calls with consistent data
		var tableInvocation = js.Invocations.FirstOrDefault(i => i.Identifier == "console.table");
		Assert.NotNull(tableInvocation);
		var tableArg = tableInvocation.Args[0]!;
		var tableType = tableArg.GetType();
		Assert.Equal(renderEvent.ComponentName, tableType.GetProperty("component")!.GetValue(tableArg));
		Assert.Equal(renderEvent.ComponentType, tableType.GetProperty("componentType")!.GetValue(tableArg));
		Assert.Equal(renderEvent.Method, tableType.GetProperty("method")!.GetValue(tableArg));
		Assert.Equal(renderEvent.DurationMs, tableType.GetProperty("duration")!.GetValue(tableArg));
		Assert.Equal(renderEvent.FirstRender, tableType.GetProperty("firstRender")!.GetValue(tableArg));

		// Browser should emit warnings for both unnecessary and frequent rerenders
		var warnInvocations = js.Invocations.Where(i => i.Identifier == "console.warn").ToList();
		Assert.True(warnInvocations.Count >= 2, "Expected at least 2 warnings (one for unnecessary, one for frequent)");
		Assert.Contains(warnInvocations, w => w.Args[0]?.ToString()?.Contains(renderEvent.UnnecessaryRerenderReason!) == true);
		Assert.Contains(warnInvocations, w => w.Args[0]?.ToString()?.Contains("Performance Warning") == true);

		// 3. OpenTelemetry logger sets Activity tags with consistent data
		var activity = capturedActivities.FirstOrDefault();
		Assert.NotNull(activity);
		Assert.Equal("render", activity.GetTagItem("wdyrl.event"));
		Assert.Equal(renderEvent.ComponentName, activity.GetTagItem("wdyrl.component"));
		Assert.Equal(renderEvent.ComponentType, activity.GetTagItem("wdyrl.component.type"));
		Assert.Equal(renderEvent.Method, activity.GetTagItem("wdyrl.method"));
		Assert.Equal(renderEvent.DurationMs, activity.GetTagItem("wdyrl.duration.ms"));
		Assert.Equal(renderEvent.IsUnnecessaryRerender, activity.GetTagItem("wdyrl.unnecessary"));
		Assert.Equal(renderEvent.IsFrequentRerender, activity.GetTagItem("wdyrl.frequent"));
		Assert.Equal(renderEvent.UnnecessaryRerenderReason, activity.GetTagItem("wdyrl.reason"));
		Assert.Equal(renderEvent.FirstRender, activity.GetTagItem("wdyrl.first_render"));
	}

	/// <summary>
	/// Verifies that state changes are consistently represented across all loggers.
	/// </summary>
	[Fact]
	public async Task AllLoggers_StateChanges_ProduceConsistentData()
	{
		// Arrange
		var stateChanges = new List<StateChange>
		{
			new()
			{
				FieldName = "_counter",
				PreviousValue = 10,
				CurrentValue = 20,
				ChangeType = StateChangeType.Modified,
			},
			new()
			{
				FieldName = "_items",
				PreviousValue = null,
				CurrentValue = new List<string> { "a", "b" },
				ChangeType = StateChangeType.Added,
			},
		};

		var renderEvent = new RenderEvent
		{
			ComponentName = "StatefulComponent",
			ComponentType = "My.StatefulComponent",
			Method = "StateHasChanged",
			StateChanges = stateChanges,
			IsUnnecessaryRerender = false,
			IsFrequentRerender = false,
		};

		// --- Server logger ---
		var serverLogger = new TestStructuredLogger();
		serverLogger.LogRenderEvent(renderEvent);

		// --- Browser logger ---
		var js = new TestJsRuntime();
		var browserLogger = new BrowserConsoleLogger(js);
		await browserLogger.LogRenderEventAsync(renderEvent);

		// --- OpenTelemetry logger ---
		var capturedActivities = new List<Activity>();
		using var activityListener = new ActivityListener
		{
			ShouldListenTo = source => source.Name == "Blazor.WhyDidYouRender",
			Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
			ActivityStarted = activity => capturedActivities.Add(activity),
		};
		ActivitySource.AddActivityListener(activityListener);

		var otelConfig = new WhyDidYouRenderConfig
		{
			EnableOpenTelemetry = true,
			EnableOtelTraces = true,
			EnableOtelMetrics = false,
		};
		var otelLogger = new AspireWhyDidYouRenderLogger(otelConfig);
		otelLogger.LogRenderEvent(renderEvent);

		// --- Assertions ---

		// Server logger has expected component
		Assert.NotNull(serverLogger.LastData);
		Assert.Equal(renderEvent.ComponentName, serverLogger.LastData!["component"]);

		// Browser logger emits state changes header and table
		var stateHeader = js.Invocations.FirstOrDefault(i =>
			i.Identifier == "console.log" && i.Args.Length > 0 && i.Args[0] is string s && s.Contains("State Changes:")
		);
		Assert.NotNull(stateHeader);

		// There should be a table with state change data
		var stateTable = js.Invocations.Last(i => i.Identifier == "console.table");
		Assert.NotNull(stateTable);
		var tableArg = stateTable.Args[0]!;
		var dict = Assert.IsAssignableFrom<IDictionary>(tableArg);
		Assert.True(dict.Contains("_counter"), "State table should contain _counter field");

		// OTel logger sets state change count tag
		var activity = capturedActivities.FirstOrDefault();
		Assert.NotNull(activity);
		Assert.Equal(stateChanges.Count, activity.GetTagItem("wdyrl.state.change.count"));
	}

	/// <summary>
	/// Verifies that correlation IDs are consistently set across all loggers.
	/// </summary>
	[Fact]
	public void AllLoggers_CorrelationId_PropagatesToAllBackends()
	{
		// Arrange
		var correlationId = "test-correlation-123";
		var renderEvent = new RenderEvent
		{
			ComponentName = "CorrelatedComponent",
			ComponentType = "My.CorrelatedComponent",
			Method = "OnInitialized",
			IsUnnecessaryRerender = false,
			IsFrequentRerender = false,
		};

		// --- Server logger with correlation ---
		var serverLogger = new TestStructuredLogger();
		serverLogger.SetCorrelationId(correlationId);

		// --- OpenTelemetry logger with correlation ---
		var capturedActivities = new List<Activity>();
		using var activityListener = new ActivityListener
		{
			ShouldListenTo = source => source.Name == "Blazor.WhyDidYouRender",
			Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
			ActivityStarted = activity => capturedActivities.Add(activity),
		};
		ActivitySource.AddActivityListener(activityListener);

		var otelConfig = new WhyDidYouRenderConfig
		{
			EnableOpenTelemetry = true,
			EnableOtelTraces = true,
			EnableOtelMetrics = false,
		};
		var otelLogger = new AspireWhyDidYouRenderLogger(otelConfig);
		otelLogger.SetCorrelationId(correlationId);
		otelLogger.LogRenderEvent(renderEvent);

		// --- Assertions ---

		// Server logger stores correlation ID
		Assert.Equal(correlationId, serverLogger.GetCorrelationId());

		// OTel logger sets correlation ID tag on activity
		var activity = capturedActivities.FirstOrDefault();
		Assert.NotNull(activity);
		Assert.Equal(correlationId, activity.GetTagItem("wdyrl.correlationId"));
	}

	/// <summary>
	/// Verifies that parameter changes are logged consistently across loggers.
	/// </summary>
	[Fact]
	public async Task AllLoggers_ParameterChanges_ProduceConsistentData()
	{
		// Arrange
		var parameterChanges = new Dictionary<string, object?>
		{
			["Count"] = new { Previous = 1, Current = 2 },
			["Name"] = new { Previous = "old", Current = "new" },
		};

		var renderEvent = new RenderEvent
		{
			ComponentName = "ParameterComponent",
			ComponentType = "My.ParameterComponent",
			Method = "OnParametersSet",
			ParameterChanges = parameterChanges,
			IsUnnecessaryRerender = false,
			IsFrequentRerender = false,
		};

		// --- Server logger ---
		var serverLogger = new TestStructuredLogger();
		serverLogger.LogRenderEvent(renderEvent);

		// --- Browser logger ---
		var js = new TestJsRuntime();
		var browserLogger = new BrowserConsoleLogger(js);
		await browserLogger.LogRenderEventAsync(renderEvent);

		// --- Assertions ---

		// Server logger has component
		Assert.NotNull(serverLogger.LastData);
		Assert.Equal(renderEvent.ComponentName, serverLogger.LastData!["component"]);

		// Browser logger emits parameter changes header
		var paramHeader = js.Invocations.FirstOrDefault(i =>
			i.Identifier == "console.log" && i.Args.Length > 0 && i.Args[0] is string s && s.Contains("Parameter Changes:")
		);
		Assert.NotNull(paramHeader);

		// Browser logger emits group for each parameter
		var paramGroups = js
			.Invocations.Where(i => i.Identifier == "console.group" && i.Args.Length > 0 && i.Args[0] is string s && s.Contains("📝"))
			.ToList();
		Assert.Equal(2, paramGroups.Count); // Count and Name

		// Verify "Previous" and "Current" logs are emitted
		var previousLogs = js
			.Invocations.Where(i => i.Identifier == "console.log" && i.Args.Length > 0 && i.Args[0] is string s && s.Contains("Previous:"))
			.ToList();
		var currentLogs = js
			.Invocations.Where(i => i.Identifier == "console.log" && i.Args.Length > 0 && i.Args[0] is string s && s.Contains("Current:"))
			.ToList();
		Assert.Equal(2, previousLogs.Count);
		Assert.Equal(2, currentLogs.Count);
	}

	/// <summary>
	/// Verifies that metrics are recorded by OpenTelemetry with the same data that server and browser loggers produce.
	/// </summary>
	[Fact]
	public void OTelMetrics_MatchesRenderEventData()
	{
		// Arrange
		var capturedCounters = new ConcurrentDictionary<string, List<(long Value, KeyValuePair<string, object?>[] Tags)>>();

		using var meterListener = new MeterListener
		{
			InstrumentPublished = (instrument, listener) =>
			{
				if (instrument.Meter.Name == "Blazor.WhyDidYouRender")
				{
					listener.EnableMeasurementEvents(instrument);
				}
			},
		};
		meterListener.SetMeasurementEventCallback<long>(
			(Instrument instrument, long measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state) =>
			{
				var list = capturedCounters.GetOrAdd(instrument.Name, _ => new List<(long, KeyValuePair<string, object?>[])>());
				lock (list)
				{
					list.Add((measurement, tags.ToArray()));
				}
			}
		);
		meterListener.Start();

		var renderEvent = new RenderEvent
		{
			ComponentName = "MetricsComponent",
			ComponentType = "My.MetricsComponent",
			Method = "StateHasChanged",
			DurationMs = 3.14,
			IsUnnecessaryRerender = true,
			UnnecessaryRerenderReason = "No parameter or state changes",
			IsFrequentRerender = false,
		};

		var otelConfig = new WhyDidYouRenderConfig
		{
			EnableOpenTelemetry = true,
			EnableOtelTraces = false,
			EnableOtelMetrics = true,
		};
		var otelLogger = new AspireWhyDidYouRenderLogger(otelConfig);
		otelLogger.LogRenderEvent(renderEvent);
		meterListener.RecordObservableInstruments();

		// --- Assertions ---

		// Render counter was recorded - filter by expected component name to avoid test pollution
		Assert.True(capturedCounters.ContainsKey("wdyrl.renders"));
		var renderMetrics = capturedCounters["wdyrl.renders"]
			.Where(m => m.Tags.Any(t => t.Key == "component" && (string?)t.Value == renderEvent.ComponentName))
			.Last();
		Assert.Equal(1, renderMetrics.Value);
		var renderTags = renderMetrics.Tags.ToDictionary(t => t.Key, t => t.Value);
		Assert.Equal(renderEvent.ComponentName, renderTags["component"]);
		Assert.Equal(renderEvent.Method, renderTags["method"]);
		Assert.Equal(renderEvent.IsUnnecessaryRerender, renderTags["unnecessary"]);
		Assert.Equal(renderEvent.IsFrequentRerender, renderTags["frequent"]);

		// Unnecessary rerender counter was recorded - filter by expected component name
		Assert.True(capturedCounters.ContainsKey("wdyrl.rerenders.unnecessary"));
		var unnecessaryMetrics = capturedCounters["wdyrl.rerenders.unnecessary"]
			.Where(m => m.Tags.Any(t => t.Key == "component" && (string?)t.Value == renderEvent.ComponentName))
			.Last();
		Assert.Equal(1, unnecessaryMetrics.Value);
		var unnecessaryTags = unnecessaryMetrics.Tags.ToDictionary(t => t.Key, t => t.Value);
		Assert.Equal(renderEvent.ComponentName, unnecessaryTags["component"]);
		Assert.Equal(renderEvent.UnnecessaryRerenderReason, unnecessaryTags["reason"]);
	}

	#endregion
}
