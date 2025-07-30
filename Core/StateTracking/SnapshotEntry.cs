using Microsoft.AspNetCore.Components;

namespace Blazor.WhyDidYouRender.Core.StateTracking;

/// <summary>
/// Entry in the optimized snapshot storage containing metadata and weak references.
/// </summary>
internal class SnapshotEntry {
	/// <summary>
	/// Gets or sets the weak reference to the component to prevent memory leaks.
	/// </summary>
	public required WeakReference<ComponentBase> ComponentReference { get; set; }

	/// <summary>
	/// Gets or sets the optimized snapshot data.
	/// </summary>
	public required OptimizedSnapshot Snapshot { get; set; }

	/// <summary>
	/// Gets or sets when this snapshot was stored.
	/// </summary>
	public required DateTime StoredAt { get; set; }

	/// <summary>
	/// Gets or sets when this snapshot was last accessed.
	/// </summary>
	public DateTime LastAccessTime { get; set; }

	/// <summary>
	/// Gets or sets the number of times this snapshot has been accessed.
	/// </summary>
	public int AccessCount { get; set; }
}

/// <summary>
/// Optimized representation of a state snapshot for efficient storage.
/// </summary>
internal class OptimizedSnapshot {
	/// <summary>
	/// Gets or sets the component type.
	/// </summary>
	public required Type ComponentType { get; set; }

	/// <summary>
	/// Gets or sets the field values dictionary (may be pooled).
	/// </summary>
	public Dictionary<string, object?>? FieldValues { get; set; }

	/// <summary>
	/// Gets or sets when this snapshot was captured.
	/// </summary>
	public required DateTime CapturedAt { get; set; }
}
