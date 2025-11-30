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
	/// </summary>
	/// <param name="firstRender">True if this is the first time the component has rendered; otherwise, false.</param>
	protected override void OnAfterRender(bool firstRender)
	{
		_tracker.Track(this, "OnAfterRender", firstRender);
		base.OnAfterRender(firstRender);
	}

	/// <summary>
	/// Called to manually trigger a re-render of the component.
	/// Tracks manual render triggers for diagnostics.
	/// </summary>
	protected new void StateHasChanged()
	{
		_tracker.StartRenderTiming(this);
		_tracker.Track(this, "StateHasChanged");
		base.StateHasChanged();
	}
}
