using System.Collections.Generic;
using Blazor.WhyDidYouRender.Configuration;
using Blazor.WhyDidYouRender.Core;
using Blazor.WhyDidYouRender.Core.StateTracking;
using Blazor.WhyDidYouRender.Records;
using Microsoft.AspNetCore.Components;

namespace Blazor.WhyDidYouRender.Tests;

public class UnnecessaryRerenderDetectorTests
{
	private sealed class DummyComponent : ComponentBase { }

	private static UnnecessaryRerenderDetector CreateDetector(bool enableStateTracking = true)
	{
		var config = new WhyDidYouRenderConfig
		{
			Enabled = true,
			EnableStateTracking = enableStateTracking,
			DetectUnnecessaryRerenders = true,
			LogDetailedStateChanges = true,
		};
		return new UnnecessaryRerenderDetector(config);
	}

	[Fact]
	public void DetectUnnecessaryRerender_FirstRender_IsNeverUnnecessary()
	{
		var detector = CreateDetector();
		var component = new DummyComponent();

		var (isUnnecessary, reason) = detector.DetectUnnecessaryRerender(
			component,
			method: "OnParametersSet",
			parameterChanges: null,
			stateChanges: null,
			firstRender: true
		);

		Assert.False(isUnnecessary);
		Assert.Null(reason);
	}

	[Fact]
	public void OnParametersSet_NoParameterChanges_IsNotUnnecessary()
	{
		var detector = CreateDetector();
		var component = new DummyComponent();

		var (isUnnecessary, reason) = detector.DetectUnnecessaryRerender(
			component,
			method: "OnParametersSet",
			parameterChanges: null,
			stateChanges: null,
			firstRender: false
		);

		Assert.False(isUnnecessary);
		Assert.Null(reason);
	}

	[Fact]
	public void OnParametersSet_OnlyNonMeaningfulChanges_IsUnnecessary()
	{
		var detector = CreateDetector();
		var component = new DummyComponent();

		// Simulate a parameter change object produced by ParameterChangeDetector
		// with IsMeaningful = false.
		var change = new
		{
			Previous = (object?)new[] { 1, 2, 3 },
			Current = (object?)new[] { 1, 2, 3 },
			Changed = true,
			IsMeaningful = false,
		};

		var parameterChanges = new Dictionary<string, object?> { ["Items"] = change };

		var (isUnnecessary, reason) = detector.DetectUnnecessaryRerender(
			component,
			method: "OnParametersSet",
			parameterChanges: parameterChanges,
			stateChanges: null,
			firstRender: false
		);

		Assert.True(isUnnecessary);
		Assert.Equal("OnParametersSet called but parameter changes are not meaningful", reason);
	}

	[Fact]
	public void OnParametersSet_WithMeaningfulChanges_IsNotUnnecessary()
	{
		var detector = CreateDetector();
		var component = new DummyComponent();

		var change = new
		{
			Previous = (object?)1,
			Current = (object?)2,
			Changed = true,
			IsMeaningful = true,
		};

		var parameterChanges = new Dictionary<string, object?> { ["Count"] = change };

		var (isUnnecessary, reason) = detector.DetectUnnecessaryRerender(
			component,
			method: "OnParametersSet",
			parameterChanges: parameterChanges,
			stateChanges: null,
			firstRender: false
		);

		Assert.False(isUnnecessary);
		Assert.Null(reason);
	}

	[Fact]
	public void StateHasChanged_WithNoStateChanges_IsUnnecessary_WhenStateTrackingEnabled()
	{
		var detector = CreateDetector(enableStateTracking: true);
		var component = new DummyComponent();

		var stateChanges = new List<StateChange>();

		var (isUnnecessary, reason) = detector.DetectUnnecessaryRerender(
			component,
			method: "StateHasChanged",
			parameterChanges: null,
			stateChanges: stateChanges,
			firstRender: false
		);

		Assert.True(isUnnecessary);
		Assert.Equal("StateHasChanged called but no state changes detected", reason);
	}

	[Fact]
	public void StateHasChanged_WithStateChanges_IsNotUnnecessary_WhenStateTrackingEnabled()
	{
		var detector = CreateDetector(enableStateTracking: true);
		var component = new DummyComponent();

		var stateChanges = new List<StateChange>
		{
			new StateChange
			{
				FieldName = "Counter",
				PreviousValue = 1,
				CurrentValue = 2,
				ChangeType = StateChangeType.Modified,
			},
		};

		var (isUnnecessary, reason) = detector.DetectUnnecessaryRerender(
			component,
			method: "StateHasChanged",
			parameterChanges: null,
			stateChanges: stateChanges,
			firstRender: false
		);

		Assert.False(isUnnecessary);
		Assert.NotNull(reason);
		Assert.Contains("State changes detected", reason);
	}

	[Fact]
	public void StateHasChanged_LegacyPath_UsesStateSnapshotEquivalence()
	{
		// Disable state tracking to force the legacy path
		var detector = CreateDetector(enableStateTracking: false);
		var component = new DummyComponent();

		// In the legacy path, we rely purely on state snapshot equivalence.
		// With no state changes, even the first call is classified as unnecessary.
		var first = detector.DetectUnnecessaryRerender(
			component,
			method: "StateHasChanged",
			parameterChanges: null,
			stateChanges: null,
			firstRender: false
		);
		Assert.True(first.IsUnnecessary);
		Assert.Equal("StateHasChanged called but component state hasn't changed", first.Reason);

		// Second call without any state mutation should also be considered unnecessary
		var second = detector.DetectUnnecessaryRerender(
			component,
			method: "StateHasChanged",
			parameterChanges: null,
			stateChanges: null,
			firstRender: false
		);

		Assert.True(second.IsUnnecessary);
		Assert.Equal("StateHasChanged called but component state hasn't changed", second.Reason);
	}
}
