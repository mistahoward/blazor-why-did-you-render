namespace Blazor.WhyDidYouRender.Configuration;

/// <summary>
/// Configuration options specific to WebAssembly hosting environments.
/// </summary>
public class WasmStorageOptions {
	/// <summary>
	/// Gets or sets whether to use browser localStorage for persistent session data.
	/// </summary>
	public bool UseLocalStorage { get; set; } = true;

	/// <summary>
	/// Gets or sets whether to use browser sessionStorage for temporary session data.
	/// </summary>
	public bool UseSessionStorage { get; set; } = true;

	/// <summary>
	/// Gets or sets the prefix for all storage keys to avoid conflicts.
	/// </summary>
	public string StorageKeyPrefix { get; set; } = "WhyDidYouRender_";

	/// <summary>
	/// Gets or sets the maximum size (in characters) for individual storage entries.
	/// Helps prevent quota exceeded errors in browser storage.
	/// </summary>
	public int MaxStorageEntrySize { get; set; } = 1024 * 1024; // 1MB

	/// <summary>
	/// Gets or sets the maximum number of error entries to store in browser storage.
	/// </summary>
	public int MaxStoredErrors { get; set; } = 100;

	/// <summary>
	/// Gets or sets the maximum number of session entries to store in browser storage.
	/// </summary>
	public int MaxStoredSessions { get; set; } = 10;

	/// <summary>
	/// Gets or sets whether to automatically clean up old storage entries.
	/// </summary>
	public bool AutoCleanupStorage { get; set; } = true;

	/// <summary>
	/// Gets or sets the storage cleanup interval in minutes.
	/// </summary>
	public int StorageCleanupIntervalMinutes { get; set; } = 60;

	/// <summary>
	/// Gets or sets whether to compress data before storing in browser storage.
	/// Can help with storage quota limitations but adds CPU overhead.
	/// </summary>
	public bool CompressStorageData { get; set; } = false;

	/// <summary>
	/// Gets or sets whether to encrypt sensitive data in browser storage.
	/// Provides additional security for stored tracking data.
	/// </summary>
	public bool EncryptSensitiveData { get; set; } = false;
}
