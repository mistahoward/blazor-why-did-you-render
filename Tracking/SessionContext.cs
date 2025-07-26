using System;
using System.Security.Claims;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;

namespace Blazor.WhyDidYouRender.Tracking;

/// <summary>
/// Represents the context information for a user session in SSR scenarios.
/// </summary>
public class SessionContext {
	/// <summary>
	/// Gets or sets the unique session identifier.
	/// </summary>
	public string SessionId { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the SignalR connection ID (for Blazor Server).
	/// </summary>
	public string? ConnectionId { get; set; }

	/// <summary>
	/// Gets or sets the user identifier (if authenticated).
	/// </summary>
	public string? UserId { get; set; }

	/// <summary>
	/// Gets or sets the user's display name (if available).
	/// </summary>
	public string? UserName { get; set; }

	/// <summary>
	/// Gets or sets whether the user is authenticated.
	/// </summary>
	public bool IsAuthenticated { get; set; }

	/// <summary>
	/// Gets or sets the client IP address.
	/// </summary>
	public string? ClientIpAddress { get; set; }

	/// <summary>
	/// Gets or sets the user agent string.
	/// </summary>
	public string? UserAgent { get; set; }

	/// <summary>
	/// Gets or sets the current request path.
	/// </summary>
	public string? RequestPath { get; set; }

	/// <summary>
	/// Gets or sets whether this is a prerendering request.
	/// </summary>
	public bool IsPrerendering { get; set; }

	/// <summary>
	/// Gets or sets the session start time.
	/// </summary>
	public DateTime SessionStartTime { get; set; } = DateTime.UtcNow;

	/// <summary>
	/// Gets or sets additional custom properties for the session.
	/// </summary>
	public Dictionary<string, object?> CustomProperties { get; set; } = new();
}

/// <summary>
/// Service for managing session context in SSR scenarios.
/// </summary>
public interface ISessionContextService {
	/// <summary>
	/// Gets the current session context.
	/// </summary>
	/// <returns>The current session context or null if not available.</returns>
	SessionContext? GetCurrentContext();

	/// <summary>
	/// Creates a session context from the current HTTP context.
	/// </summary>
	/// <param name="httpContext">The HTTP context.</param>
	/// <returns>A session context object.</returns>
	SessionContext CreateFromHttpContext(HttpContext httpContext);

	/// <summary>
	/// Creates a session context from a SignalR hub context.
	/// </summary>
	/// <param name="hubContext">The SignalR hub context.</param>
	/// <returns>A session context object.</returns>
	SessionContext CreateFromHubContext(HubCallerContext hubContext);

	/// <summary>
	/// Sanitizes session context for logging based on privacy settings.
	/// </summary>
	/// <param name="context">The session context to sanitize.</param>
	/// <returns>A sanitized version of the session context.</returns>
	SessionContext SanitizeForLogging(SessionContext context);
}

/// <summary>
/// Default implementation of session context service.
/// </summary>
public class SessionContextService : ISessionContextService {
	private readonly IHttpContextAccessor _httpContextAccessor;
	private readonly WhyDidYouRenderConfig _config;

	/// <summary>
	/// Initializes a new instance of the <see cref="SessionContextService"/> class.
	/// </summary>
	/// <param name="httpContextAccessor">The HTTP context accessor.</param>
	/// <param name="config">The WhyDidYouRender configuration.</param>
	public SessionContextService(IHttpContextAccessor httpContextAccessor, WhyDidYouRenderConfig config) {
		_httpContextAccessor = httpContextAccessor;
		_config = config;
	}

	/// <inheritdoc />
	public SessionContext? GetCurrentContext() {
		var httpContext = _httpContextAccessor.HttpContext;
		if (httpContext == null) return null;

		return CreateFromHttpContext(httpContext);
	}

	/// <inheritdoc />
	public SessionContext CreateFromHttpContext(HttpContext httpContext) {
		var context = new SessionContext {
			SessionId = GetOrCreateSessionId(httpContext),
			UserId = GetUserId(httpContext),
			UserName = GetUserName(httpContext),
			IsAuthenticated = httpContext.User?.Identity?.IsAuthenticated ?? false,
			ClientIpAddress = GetClientIpAddress(httpContext),
			UserAgent = httpContext.Request.Headers.UserAgent.FirstOrDefault(),
			RequestPath = httpContext.Request.Path.Value,
			IsPrerendering = IsPrerendering(httpContext)
		};

		return SanitizeForLogging(context);
	}

	/// <inheritdoc />
	public SessionContext CreateFromHubContext(HubCallerContext hubContext) {
		var context = new SessionContext {
			SessionId = hubContext.ConnectionId,
			ConnectionId = hubContext.ConnectionId,
			UserId = GetUserId(hubContext),
			UserName = GetUserName(hubContext),
			IsAuthenticated = hubContext.User?.Identity?.IsAuthenticated ?? false,
			IsPrerendering = false // SignalR connections are never prerendering
		};

		return SanitizeForLogging(context);
	}

	/// <inheritdoc />
	public SessionContext SanitizeForLogging(SessionContext context) {
		// Create a copy for sanitization
		var sanitized = new SessionContext {
			SessionId = SanitizeSessionId(context.SessionId),
			ConnectionId = SanitizeConnectionId(context.ConnectionId),
			UserId = SanitizeUserId(context.UserId),
			UserName = SanitizeUserName(context.UserName),
			IsAuthenticated = context.IsAuthenticated,
			ClientIpAddress = SanitizeIpAddress(context.ClientIpAddress),
			UserAgent = SanitizeUserAgent(context.UserAgent),
			RequestPath = context.RequestPath,
			IsPrerendering = context.IsPrerendering,
			SessionStartTime = context.SessionStartTime,
			CustomProperties = new Dictionary<string, object?>(context.CustomProperties)
		};

		return sanitized;
	}

	private string GetOrCreateSessionId(HttpContext httpContext) {
		try {
			// Try to get existing session ID
			if (httpContext.Session.IsAvailable) {
				var sessionId = httpContext.Session.Id;
				if (!string.IsNullOrEmpty(sessionId)) {
					return sessionId;
				}
			}
		}
		catch (InvalidOperationException) {
			// Session not configured - fall back to alternative ID generation
		}
		catch (Exception) {
			// Any other session-related error - fall back gracefully
		}

		// Fallback to connection ID or generate one
		var connectionId = httpContext.Connection.Id;
		if (!string.IsNullOrEmpty(connectionId)) {
			return $"conn-{connectionId}";
		}

		// Final fallback - generate a unique ID
		return $"session-{Guid.NewGuid():N}";
	}

	private string? GetUserId(HttpContext httpContext) {
		return httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
	}

	private string? GetUserId(HubCallerContext hubContext) {
		return hubContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
	}

	private string? GetUserName(HttpContext httpContext) {
		return httpContext.User?.Identity?.Name;
	}

	private string? GetUserName(HubCallerContext hubContext) {
		return hubContext.User?.Identity?.Name;
	}

	private string? GetClientIpAddress(HttpContext httpContext) {
		// Check for forwarded headers first (for load balancers/proxies)
		var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
		if (!string.IsNullOrEmpty(forwardedFor)) {
			return forwardedFor.Split(',')[0].Trim();
		}

		var realIp = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
		if (!string.IsNullOrEmpty(realIp)) {
			return realIp;
		}

		return httpContext.Connection.RemoteIpAddress?.ToString();
	}

	private bool IsPrerendering(HttpContext httpContext) {
		// Check if this is a prerendering request
		// This can be detected by checking for specific headers or request characteristics
		return httpContext.Request.Headers.ContainsKey("X-Prerendering") ||
			   httpContext.Request.Query.ContainsKey("prerender");
	}

	// Privacy/Security sanitization methods
	private string SanitizeSessionId(string sessionId) {
		if (string.IsNullOrEmpty(sessionId)) return sessionId;

		// For privacy, only show first 8 characters + hash
		return sessionId.Length > 8 ? $"{sessionId[..8]}***" : sessionId;
	}

	private string? SanitizeConnectionId(string? connectionId) {
		if (string.IsNullOrEmpty(connectionId)) return connectionId;

		// Similar to session ID
		return connectionId.Length > 8 ? $"{connectionId[..8]}***" : connectionId;
	}

	private string? SanitizeUserId(string? userId) {
		if (string.IsNullOrEmpty(userId) || !_config.IncludeUserInfo) return null;

		// Hash or truncate user ID for privacy
		return userId.Length > 6 ? $"{userId[..3]}***{userId[^3..]}" : "***";
	}

	private string? SanitizeUserName(string? userName) {
		if (string.IsNullOrEmpty(userName) || !_config.IncludeUserInfo) return null;

		// Only show first letter + length for privacy
		return $"{userName[0]}*** ({userName.Length} chars)";
	}

	private string? SanitizeIpAddress(string? ipAddress) {
		if (string.IsNullOrEmpty(ipAddress) || !_config.IncludeClientInfo) return null;

		// Mask last octet for IPv4, or significant portion for IPv6
		if (ipAddress.Contains('.')) {
			var parts = ipAddress.Split('.');
			if (parts.Length == 4) {
				return $"{parts[0]}.{parts[1]}.{parts[2]}.***";
			}
		}

		return "***";
	}

	private string? SanitizeUserAgent(string? userAgent) {
		if (string.IsNullOrEmpty(userAgent) || !_config.IncludeClientInfo) return null;

		// Extract just browser name and version, remove detailed system info
		if (userAgent.Contains("Chrome")) return "Chrome/***";
		if (userAgent.Contains("Firefox")) return "Firefox/***";
		if (userAgent.Contains("Safari")) return "Safari/***";
		if (userAgent.Contains("Edge")) return "Edge/***";

		return "Unknown/***";
	}
}
