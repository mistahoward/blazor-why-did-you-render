using System;
using System.Collections.Generic;
using Blazor.WhyDidYouRender.Abstractions;
using Blazor.WhyDidYouRender.Configuration;
using Blazor.WhyDidYouRender.Core;
using Blazor.WhyDidYouRender.Helpers;
using Blazor.WhyDidYouRender.Logging;
using Blazor.WhyDidYouRender.Records;
using Microsoft.AspNetCore.Components;
using Xunit;

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
		// Auto-tracked simple field used to exercise state tracking end-to-end.
		private int _counter;

		public int Counter => _counter;

		public void Increment() => _counter++;
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

		// ParameterComponent has a [Parameter] property that remains null, so
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

		// First render primes the state snapshot for this component.
		tracker.TrackRender(component, "OnParametersSet", false);

		// Second render via StateHasChanged without any state mutation should be
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

		// Baseline snapshot with initial counter value.
		tracker.TrackRender(component, "OnParametersSet", false);

		// Mutate state before calling StateHasChanged so that the snapshot manager
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
}
