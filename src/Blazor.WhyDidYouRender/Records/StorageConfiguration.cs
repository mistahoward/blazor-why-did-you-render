namespace Blazor.WhyDidYouRender.Records;

/// <summary>
/// Configuration for optimized snapshot storage.
/// </summary>
public record StorageConfiguration
{
	/// <summary>
	/// Gets the maximum number of snapshots to store.
	/// </summary>
	public int MaxSnapshots { get; init; } = 1000;

	/// <summary>
	/// Gets the maximum age of snapshots in minutes.
	/// </summary>
	public int MaxSnapshotAgeMinutes { get; init; } = 30;

	/// <summary>
	/// Gets whether to use object pooling for dictionaries.
	/// </summary>
	public bool UseObjectPooling { get; init; } = true;
}
