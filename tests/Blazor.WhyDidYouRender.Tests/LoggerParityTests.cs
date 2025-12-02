using System;
using System.Collections;
using System.Collections.Generic;
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
}
