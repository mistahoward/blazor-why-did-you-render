using Blazor.WhyDidYouRender.Abstractions;
using Blazor.WhyDidYouRender.Configuration;
using Blazor.WhyDidYouRender.Logging;
using Microsoft.AspNetCore.Http;

namespace Blazor.WhyDidYouRender.Services;

/// <summary>
/// Server-side implementation of session context service using HttpContext.
/// </summary>
/// <param name="httpContextAccessor">The HTTP context accessor.</param>
/// <param name="config">The WhyDidYouRender configuration.</param>
/// <param name="logger">Optional unified logger for diagnostics.</param>
public class ServerSessionContextService(
	IHttpContextAccessor httpContextAccessor,
	WhyDidYouRenderConfig config,
	IWhyDidYouRenderLogger? logger = null
) : ISessionContextService
{
	private readonly IHttpContextAccessor _httpContextAccessor =
		httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
	private readonly WhyDidYouRenderConfig _config = config;
	private readonly IWhyDidYouRenderLogger? _logger = logger;
	private const string _sessionKeyPrefix = "WhyDidYouRender_";

	/// <inheritdoc />
	public string GetSessionId()
	{
		var httpContext = _httpContextAccessor.HttpContext;
		if (httpContext?.Session == null)
			return $"server-{httpContext?.TraceIdentifier ?? Guid.NewGuid().ToString("N")}";

		// ! check if the response has already started. In Interactive Server mode during prerendering,
		// ! accessing session before the response starts will throw "The session cannot be established
		// ! after the response has started." We fall back to TraceIdentifier in this case.
		if (httpContext.Response.HasStarted)
		{
			_logger?.LogDebug(
				"Response has started, using TraceIdentifier for session ID",
				new() { ["traceId"] = httpContext.TraceIdentifier }
			);
			return $"server-{httpContext.TraceIdentifier ?? Guid.NewGuid().ToString("N")}";
		}

		try
		{
			var sessionId = httpContext.Session.GetString($"{_sessionKeyPrefix}SessionId");
			if (string.IsNullOrEmpty(sessionId))
			{
				sessionId = $"server-{Guid.NewGuid():N}";
				httpContext.Session.SetString($"{_sessionKeyPrefix}SessionId", sessionId);
			}

			return sessionId;
		}
		catch (Exception ex)
		{
			// ! session access failed (e.g., timing issues, session unavailable), fall back to TraceIdentifier
			if (_logger != null)
				_logger.LogError(
					"Failed to access session, using TraceIdentifier fallback",
					ex,
					new() { ["traceId"] = httpContext.TraceIdentifier }
				);
			else
				Console.WriteLine($"[WhyDidYouRender] Failed to access session: {ex.Message}. Using TraceIdentifier fallback.");

			return $"server-{httpContext.TraceIdentifier ?? Guid.NewGuid().ToString("N")}";
		}
	}

	/// <inheritdoc />
	public Task SetSessionInfoAsync(string key, object value)
	{
		if (string.IsNullOrEmpty(key))
			throw new ArgumentException("Key cannot be null or empty", nameof(key));

		var httpContext = _httpContextAccessor.HttpContext;
		if (httpContext?.Session == null)
			return Task.CompletedTask;

		try
		{
			var sessionKey = GetSessionKey(key);
			var jsonValue = System.Text.Json.JsonSerializer.Serialize(value);
			httpContext.Session.SetString(sessionKey, jsonValue);
		}
		catch (Exception ex)
		{
			if (_logger != null)
				_logger.LogError($"Failed to set session info '{key}'", ex, new() { ["key"] = key });
			else
				Console.WriteLine($"[WhyDidYouRender] Failed to set session info '{key}': {ex.Message}");
		}

		return Task.CompletedTask;
	}

	/// <inheritdoc />
	public Task<T?> GetSessionInfoAsync<T>(string key)
	{
		if (string.IsNullOrEmpty(key))
			return Task.FromResult(default(T?));

		var httpContext = _httpContextAccessor.HttpContext;
		if (httpContext?.Session == null)
			return Task.FromResult(default(T?));

		try
		{
			var sessionKey = GetSessionKey(key);
			var jsonValue = httpContext.Session.GetString(sessionKey);

			if (string.IsNullOrEmpty(jsonValue))
				return Task.FromResult(default(T?));

			var result = System.Text.Json.JsonSerializer.Deserialize<T>(jsonValue);
			return Task.FromResult(result);
		}
		catch (Exception ex)
		{
			if (_logger != null)
				_logger.LogError($"Failed to get session info '{key}'", ex, new() { ["key"] = key });
			else
				Console.WriteLine($"[WhyDidYouRender] Failed to get session info '{key}': {ex.Message}");
			return Task.FromResult(default(T?));
		}
	}

	/// <inheritdoc />
	public Task RemoveSessionInfoAsync(string key)
	{
		if (string.IsNullOrEmpty(key))
			return Task.CompletedTask;

		var httpContext = _httpContextAccessor.HttpContext;
		if (httpContext?.Session == null)
			return Task.CompletedTask;

		try
		{
			var sessionKey = GetSessionKey(key);
			httpContext.Session.Remove(sessionKey);
		}
		catch (Exception ex)
		{
			if (_logger != null)
				_logger.LogError($"Failed to remove session info '{key}'", ex, new() { ["key"] = key });
			else
				Console.WriteLine($"[WhyDidYouRender] Failed to remove session info '{key}': {ex.Message}");
		}

		return Task.CompletedTask;
	}

	/// <inheritdoc />
	public Task<IEnumerable<string>> GetSessionKeysAsync()
	{
		var httpContext = _httpContextAccessor.HttpContext;
		if (httpContext?.Session == null)
			return Task.FromResult<IEnumerable<string>>(Array.Empty<string>());

		try
		{
			var keys = new List<string>();
			var prefix = GetSessionKey("");

			// HttpContext.Session does not provide a key enumeration API. A registry would be required to fully implement this.

			return Task.FromResult<IEnumerable<string>>(keys);
		}
		catch (Exception ex)
		{
			if (_logger != null)
				_logger.LogError("Failed to get session keys", ex);
			else
				Console.WriteLine($"[WhyDidYouRender] Failed to get session keys: {ex.Message}");
			return Task.FromResult<IEnumerable<string>>(Array.Empty<string>());
		}
	}

	/// <inheritdoc />
	public Task ClearSessionAsync()
	{
		var httpContext = _httpContextAccessor.HttpContext;
		if (httpContext?.Session == null)
			return Task.CompletedTask;

		try
		{
			httpContext.Session.Clear();
		}
		catch (Exception ex)
		{
			if (_logger != null)
				_logger.LogError("Failed to clear session", ex);
			else
				Console.WriteLine($"[WhyDidYouRender] Failed to clear session: {ex.Message}");
		}

		return Task.CompletedTask;
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
