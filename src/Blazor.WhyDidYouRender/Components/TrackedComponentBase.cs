using Blazor.WhyDidYouRender.Core;
using Microsoft.AspNetCore.Components;

namespace Blazor.WhyDidYouRender.Components;

/// <summary>
/// Base class for Blazor components that automatically tracks and logs render events for diagnostics.
/// Inherit from this class to enable WhyDidYouRender-style tracking of lifecycle and manual renders.
/// </summary>
public abstract class TrackedComponentBase : ComponentBase
{
	/// <summary>
	/// The service responsible for tracking and logging render events.
	/// </summary>
	private readonly RenderTrackerService _tracker = RenderTrackerService.Instance;

	/// <summary>
	/// Tracks the number of StateHasChanged calls between actual renders.
	/// Used to detect when Blazor batches multiple StateHasChanged calls into a single render.
	/// </summary>
	private int _stateHasChangedCallCount;

	/// <summary>
	/// Called when the component is initialized.
	/// Tracks the initialization event for diagnostics.
	/// </summary>
	protected override void OnInitialized()
	{
		_tracker.StartRenderTiming(this);
		_tracker.Track(this, "OnInitialized");
		base.OnInitialized();
	}

	/// <summary>
	/// Called when the component receives parameters from its parent.
	/// Tracks the parameter set event for diagnostics.
	/// </summary>
	protected override void OnParametersSet()
	{
		_tracker.Track(this, "OnParametersSet");
		base.OnParametersSet();
	}

	/// <summary>
	/// Called after the component has rendered.
	/// Tracks the after render event for diagnostics, including whether this is the first render.
	/// Reports batching information if multiple StateHasChanged calls were coalesced into this render.
	/// </summary>
	/// <param name="firstRender">True if this is the first time the component has rendered; otherwise, false.</param>
	protected override void OnAfterRender(bool firstRender)
	{
		var stateHasChangedCalls = _stateHasChangedCallCount;
		_stateHasChangedCallCount = 0;

		_tracker.Track(this, "OnAfterRender", firstRender, stateHasChangedCalls);
		base.OnAfterRender(firstRender);
	}

	/// <summary>
	/// Called to manually trigger a re-render of the component.
	/// Tracks manual render triggers for diagnostics and counts calls for batching detection.
	/// </summary>
	protected new void StateHasChanged()
	{
		_stateHasChangedCallCount++;
		_tracker.StartRenderTiming(this);
		_tracker.Track(this, "StateHasChanged");
		base.StateHasChanged();
	}
}
