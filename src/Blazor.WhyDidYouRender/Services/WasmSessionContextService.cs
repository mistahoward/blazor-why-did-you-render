using System.Text.Json;
using Microsoft.JSInterop;

using Blazor.WhyDidYouRender.Abstractions;
using Blazor.WhyDidYouRender.Configuration;

namespace Blazor.WhyDidYouRender.Services;

/// <summary>
/// WebAssembly implementation of session context service using browser storage.
/// </summary>
public class WasmSessionContextService : ISessionContextService {
	private readonly IJSRuntime _jsRuntime;
	private readonly WhyDidYouRenderConfig _config;
	private readonly JsonSerializerOptions _jsonOptions;
	private string? _sessionId;
	private readonly object _lock = new object();

	/// <summary>
	/// Initializes a new instance of the <see cref="WasmSessionContextService"/> class.
	/// </summary>
	/// <param name="jsRuntime">The JavaScript runtime for browser interop.</param>
	/// <param name="config">The WhyDidYouRender configuration.</param>
	public WasmSessionContextService(IJSRuntime jsRuntime, WhyDidYouRenderConfig config) {
		_jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
		_config = config ?? throw new ArgumentNullException(nameof(config));

		_jsonOptions = new JsonSerializerOptions {
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			WriteIndented = false
		};
	}

	/// <inheritdoc />
	public string GetSessionId() {
		if (_sessionId != null) return _sessionId;

		lock (_lock) {
			if (_sessionId != null) return _sessionId;

			_sessionId = $"wasm-{Guid.NewGuid():N}";

			// store it in sessionStorage for persistence across page reloads
			_ = Task.Run(async () => {
				try {
					await SetSessionStorageAsync("sessionId", _sessionId);
				}
				catch {
					// ignore storage errors during session ID creation
				}
			});

			return _sessionId;
		}
	}

	/// <inheritdoc />
	public async Task SetSessionInfoAsync(string key, object value) {
		if (string.IsNullOrEmpty(key))
			throw new ArgumentException("Key cannot be null or empty", nameof(key));

		try {
			var storageKey = GetStorageKey(key);
			var jsonValue = JsonSerializer.Serialize(value, _jsonOptions);

			if (jsonValue.Length > _config.WasmStorage.MaxStorageEntrySize) {
				throw new InvalidOperationException($"Value too large for storage. Size: {jsonValue.Length}, Max: {_config.WasmStorage.MaxStorageEntrySize}");
			}

			if (_config.WasmStorage.UseSessionStorage) {
				await SetSessionStorageAsync(storageKey, jsonValue);
			}
			else if (_config.WasmStorage.UseLocalStorage) {
				await SetLocalStorageAsync(storageKey, jsonValue);
			}
		}
		catch (Exception ex) {
			Console.WriteLine($"[WhyDidYouRender] Failed to set session info '{key}': {ex.Message}");
			throw;
		}
	}

	/// <inheritdoc />
	public async Task<T?> GetSessionInfoAsync<T>(string key) {
		if (string.IsNullOrEmpty(key))
			return default;

		try {
			var storageKey = GetStorageKey(key);
			string? jsonValue = null;

			if (_config.WasmStorage.UseSessionStorage)
				jsonValue = await GetSessionStorageAsync(storageKey);

			if (jsonValue == null && _config.WasmStorage.UseLocalStorage)
				jsonValue = await GetLocalStorageAsync(storageKey);

			if (string.IsNullOrEmpty(jsonValue))
				return default;

			return JsonSerializer.Deserialize<T>(jsonValue, _jsonOptions);
		}
		catch (Exception ex) {
			Console.WriteLine($"[WhyDidYouRender] Failed to get session info '{key}': {ex.Message}");
			return default;
		}
	}

	/// <inheritdoc />
	public async Task RemoveSessionInfoAsync(string key) {
		if (string.IsNullOrEmpty(key))
			return;

		try {
			var storageKey = GetStorageKey(key);

			if (_config.WasmStorage.UseSessionStorage)
				await RemoveSessionStorageAsync(storageKey);

			if (_config.WasmStorage.UseLocalStorage)
				await RemoveLocalStorageAsync(storageKey);
		}
		catch (Exception ex) {
			Console.WriteLine($"[WhyDidYouRender] Failed to remove session info '{key}': {ex.Message}");
		}
	}

	/// <inheritdoc />
	public async Task<IEnumerable<string>> GetSessionKeysAsync() {
		var keys = new List<string>();

		try {
			var prefix = GetStorageKey("");

			if (_config.WasmStorage.UseSessionStorage) {
				var sessionKeys = await GetStorageKeysAsync("sessionStorage", prefix);
				keys.AddRange(sessionKeys);
			}

			if (_config.WasmStorage.UseLocalStorage) {
				var localKeys = await GetStorageKeysAsync("localStorage", prefix);
				keys.AddRange(localKeys);
			}

			// Remove prefix and deduplicate
			var result = keys
				.Select(k => k.Substring(prefix.Length))
				.Where(k => !string.IsNullOrEmpty(k))
				.Distinct()
				.ToList();

			return result;
		}
		catch (Exception ex) {
			Console.WriteLine($"[WhyDidYouRender] Failed to get session keys: {ex.Message}");
			return [];
		}
	}

	/// <inheritdoc />
	public async Task ClearSessionAsync() {
		try {
			var keys = await GetSessionKeysAsync();

			foreach (var key in keys)
				await RemoveSessionInfoAsync(key);
		}
		catch (Exception ex) {
			Console.WriteLine($"[WhyDidYouRender] Failed to clear session: {ex.Message}");
		}
	}

	/// <inheritdoc />
	public bool SupportsPersistentStorage => _config.WasmStorage.UseLocalStorage;

	/// <inheritdoc />
	public bool SupportsCrossRequestPersistence => _config.WasmStorage.UseSessionStorage || _config.WasmStorage.UseLocalStorage;

	/// <inheritdoc />
	public string StorageDescription =>
		$"Browser storage (Session: {_config.WasmStorage.UseSessionStorage}, Local: {_config.WasmStorage.UseLocalStorage})";

	/// <summary>
	/// Performs automatic cleanup of old storage entries if configured.
	/// </summary>
	/// <returns>A task representing the cleanup operation.</returns>
	public async Task PerformStorageCleanupAsync() {
		if (!_config.WasmStorage.AutoCleanupStorage)
			return;

		try {
			// TODO: Implement actual cleanup logic
			// Console.WriteLine("[WhyDidYouRender] Performing WASM storage cleanup...");
			await Task.CompletedTask;
		}
		catch (Exception ex) {
			Console.WriteLine($"[WhyDidYouRender] Storage cleanup failed: {ex.Message}");
		}
	}

	/// <summary>
	/// Gets the storage key with the configured prefix and session ID.
	/// </summary>
	/// <param name="key">The base key.</param>
	/// <returns>The prefixed storage key.</returns>
	private string GetStorageKey(string key) {
		var sessionId = GetSessionId();
		return $"{_config.WasmStorage.StorageKeyPrefix}{sessionId}:{key}";
	}

	/// <summary>
	/// Sets a value in sessionStorage.
	/// </summary>
	/// <param name="key">The storage key.</param>
	/// <param name="value">The value to store.</param>
	/// <returns>A task representing the operation.</returns>
	private async Task SetSessionStorageAsync(string key, string value) =>
		await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", key, value);

	/// <summary>
	/// Gets a value from sessionStorage.
	/// </summary>
	/// <param name="key">The storage key.</param>
	/// <returns>The stored value or null if not found.</returns>
	private async Task<string?> GetSessionStorageAsync(string key) =>
		await _jsRuntime.InvokeAsync<string?>("sessionStorage.getItem", key);

	/// <summary>
	/// Removes a value from sessionStorage.
	/// </summary>
	/// <param name="key">The storage key.</param>
	/// <returns>A task representing the operation.</returns>
	private async Task RemoveSessionStorageAsync(string key) =>
		await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", key);

	/// <summary>
	/// Sets a value in localStorage.
	/// </summary>
	/// <param name="key">The storage key.</param>
	/// <param name="value">The value to store.</param>
	/// <returns>A task representing the operation.</returns>
	private async Task SetLocalStorageAsync(string key, string value) =>
		await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, value);

	/// <summary>
	/// Gets a value from localStorage.
	/// </summary>
	/// <param name="key">The storage key.</param>
	/// <returns>The stored value or null if not found.</returns>
	private async Task<string?> GetLocalStorageAsync(string key) =>
		await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", key);

	/// <summary>
	/// Removes a value from localStorage.
	/// </summary>
	/// <param name="key">The storage key.</param>
	/// <returns>A task representing the operation.</returns>
	private async Task RemoveLocalStorageAsync(string key) =>
		await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);

	/// <summary>
	/// Gets all keys from the specified storage that match the prefix.
	/// </summary>
	/// <param name="storageType">The storage type ("sessionStorage" or "localStorage").</param>
	/// <param name="prefix">The key prefix to match.</param>
	/// <returns>A list of matching keys.</returns>
	private async Task<List<string>> GetStorageKeysAsync(string storageType, string prefix) {
		try {
			var script = $@"
                (function() {{
                    var keys = [];
                    var storage = {storageType};
                    for (var i = 0; i < storage.length; i++) {{
                        var key = storage.key(i);
                        if (key && key.startsWith('{prefix}')) {{
                            keys.push(key);
                        }}
                    }}
                    return keys;
                }})()";

			var result = await _jsRuntime.InvokeAsync<string[]>("eval", script);
			return result?.ToList() ?? [];
		}
		catch {
			return [];
		}
	}
}
