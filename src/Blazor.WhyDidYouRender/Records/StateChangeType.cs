namespace Blazor.WhyDidYouRender.Records;

/// <summary>
/// Defines the types of state changes that can occur during component state tracking.
/// These values indicate how a field's value has changed between state snapshots.
/// </summary>
public enum StateChangeType
{
	/// <summary>
	/// A field was added (new field tracking).
	/// This occurs when a field is being tracked for the first time.
	/// </summary>
	Added,

	/// <summary>
	/// A field value was modified.
	/// This occurs when a field's value changes between snapshots.
	/// </summary>
	Modified,

	/// <summary>
	/// A field value was changed (alias for Modified).
	/// This occurs when a field's value changes between snapshots.
	/// </summary>
	ValueChanged,

	/// <summary>
	/// A field was removed (no longer being tracked).
	/// This occurs when a field is no longer present in the current snapshot.
	/// </summary>
	Removed,
}
