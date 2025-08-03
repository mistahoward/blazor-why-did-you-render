using Microsoft.AspNetCore.Http;

using Blazor.WhyDidYouRender.Abstractions;
using Blazor.WhyDidYouRender.Configuration;

namespace Blazor.WhyDidYouRender.Services;

/// <summary>
/// Server-side implementation of session context service using HttpContext.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ServerSessionContextService"/> class.
/// </remarks>
/// <param name="httpContextAccessor">The HTTP context accessor.</param>
/// <param name="config">The WhyDidYouRender configuration.</param>
public class ServerSessionContextService(IHttpContextAccessor httpContextAccessor, WhyDidYouRenderConfig config) : ISessionContextService {
	private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
	private const string _sessionKeyPrefix = "WhyDidYouRender_";

	/// <inheritdoc />
	public string GetSessionId() {
		var httpContext = _httpContextAccessor.HttpContext;
		if (httpContext?.Session == null)
			// fallback to a request-scoped identifier if session is not available
			return $"server-{httpContext?.TraceIdentifier ?? Guid.NewGuid().ToString("N")}";

		var sessionId = httpContext.Session.GetString($"{_sessionKeyPrefix}SessionId");
		if (string.IsNullOrEmpty(sessionId)) {
			sessionId = $"server-{Guid.NewGuid():N}";
			httpContext.Session.SetString($"{_sessionKeyPrefix}SessionId", sessionId);
		}

		return sessionId;
	}

	/// <inheritdoc />
	public async Task SetSessionInfoAsync(string key, object value) {
		if (string.IsNullOrEmpty(key))
			throw new ArgumentException("Key cannot be null or empty", nameof(key));

		var httpContext = _httpContextAccessor.HttpContext;
		if (httpContext?.Session == null)
			return;

		try {
			var sessionKey = GetSessionKey(key);
			var jsonValue = System.Text.Json.JsonSerializer.Serialize(value);
			httpContext.Session.SetString(sessionKey, jsonValue);
		}
		catch (Exception ex) {
			Console.WriteLine($"[WhyDidYouRender] Failed to set session info '{key}': {ex.Message}");
		}

		await Task.CompletedTask;
	}

	/// <inheritdoc />
	public async Task<T?> GetSessionInfoAsync<T>(string key) {
		if (string.IsNullOrEmpty(key))
			return default;

		var httpContext = _httpContextAccessor.HttpContext;
		if (httpContext?.Session == null)
			return default;

		try {
			var sessionKey = GetSessionKey(key);
			var jsonValue = httpContext.Session.GetString(sessionKey);

			if (string.IsNullOrEmpty(jsonValue))
				return default;

			return System.Text.Json.JsonSerializer.Deserialize<T>(jsonValue);
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

		var httpContext = _httpContextAccessor.HttpContext;
		if (httpContext?.Session == null)
			return;

		try {
			var sessionKey = GetSessionKey(key);
			httpContext.Session.Remove(sessionKey);
		}
		catch (Exception ex) {
			Console.WriteLine($"[WhyDidYouRender] Failed to remove session info '{key}': {ex.Message}");
		}

		await Task.CompletedTask;
	}

	/// <inheritdoc />
	public async Task<IEnumerable<string>> GetSessionKeysAsync() {
		var httpContext = _httpContextAccessor.HttpContext;
		if (httpContext?.Session == null)
			return [];

		try {
			var keys = new List<string>();
			var prefix = GetSessionKey("");

			// note: HttpContext.Session doesn't provide a way to enumerate keys
			// this is a limitation of the server-side session implementation
			// we would need to maintain our own key registry for full functionality

			return keys;
		}
		catch (Exception ex) {
			Console.WriteLine($"[WhyDidYouRender] Failed to get session keys: {ex.Message}");
			return [];
		}
	}

	/// <inheritdoc />
	public async Task ClearSessionAsync() {
		var httpContext = _httpContextAccessor.HttpContext;
		if (httpContext?.Session == null)
			return;

		try {
			httpContext.Session.Clear();
		}
		catch (Exception ex) {
			Console.WriteLine($"[WhyDidYouRender] Failed to clear session: {ex.Message}");
		}

		await Task.CompletedTask;
	}

	/// <inheritdoc />
	public bool SupportsPersistentStorage => true;

	/// <inheritdoc />
	public bool SupportsCrossRequestPersistence => true;

	/// <inheritdoc />
	public string StorageDescription => "Server-side session storage (HttpContext.Session)";

	/// <summary>
	/// Gets the full session key with prefix.
	/// </summary>
	/// <param name="key">The base key.</param>
	/// <returns>The prefixed session key.</returns>
	private string GetSessionKey(string key) => $"{_sessionKeyPrefix}{key}";
}
