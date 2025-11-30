using System.Collections.Concurrent;

namespace Blazor.WhyDidYouRender.Helpers;

/// <summary>
/// Represents session-specific context data for tracking render events and performance metrics.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="SessionContext"/> class.
/// </remarks>
/// <param name="sessionId">The unique session identifier.</param>
public class SessionContext(string sessionId)
{
	/// <summary>
	/// Gets the unique session identifier.
	/// </summary>
	public string SessionId { get; } = sessionId ?? throw new ArgumentNullException(nameof(sessionId));

	/// <summary>
	/// Gets the timestamp when this session was created.
	/// </summary>
	public DateTime CreatedAt { get; } = DateTime.UtcNow;

	/// <summary>
	/// Gets the timestamp when this session was last accessed.
	/// </summary>
	public DateTime LastAccessedAt { get; private set; } = DateTime.UtcNow;

	/// <summary>
	/// Gets the total number of render events tracked in this session.
	/// </summary>
	public int RenderEventCount { get; private set; }

	/// <summary>
	/// Gets the session-specific data storage.
	/// </summary>
	public ConcurrentDictionary<string, object> Data { get; } = [];

	/// <summary>
	/// Updates the last accessed timestamp and increments the render event count.
	/// </summary>
	public void RecordAccess()
	{
		LastAccessedAt = DateTime.UtcNow;
		RenderEventCount++;
	}

	/// <summary>
	/// Gets a value from the session data storage.
	/// </summary>
	/// <typeparam name="T">The type of the value to retrieve.</typeparam>
	/// <param name="key">The key of the value to retrieve.</param>
	/// <returns>The value if found; otherwise, the default value for the type.</returns>
	public T? GetValue<T>(string key)
	{
		if (string.IsNullOrEmpty(key))
			return default;

		if (Data.TryGetValue(key, out var value) && value is T typedValue)
			return typedValue;

		return default;
	}

	/// <summary>
	/// Sets a value in the session data storage.
	/// </summary>
	/// <param name="key">The key of the value to set.</param>
	/// <param name="value">The value to set.</param>
	public void SetValue(string key, object value)
	{
		if (string.IsNullOrEmpty(key))
			return;

		Data.AddOrUpdate(key, value, (k, v) => value);
		LastAccessedAt = DateTime.UtcNow;
	}

	/// <summary>
	/// Removes a value from the session data storage.
	/// </summary>
	/// <param name="key">The key of the value to remove.</param>
	/// <returns>True if the value was removed; otherwise, false.</returns>
	public bool RemoveValue(string key)
	{
		if (string.IsNullOrEmpty(key))
			return false;

		var result = Data.TryRemove(key, out _);
		if (result)
			LastAccessedAt = DateTime.UtcNow;

		return result;
	}

	/// <summary>
	/// Gets the age of this session.
	/// </summary>
	/// <returns>The time elapsed since the session was created.</returns>
	public TimeSpan GetAge() => DateTime.UtcNow - CreatedAt;

	/// <summary>
	/// Gets the time since this session was last accessed.
	/// </summary>
	/// <returns>The time elapsed since the session was last accessed.</returns>
	public TimeSpan GetTimeSinceLastAccess() => DateTime.UtcNow - LastAccessedAt;

	/// <summary>
	/// Returns a string representation of this session context.
	/// </summary>
	/// <returns>A string containing session information.</returns>
	public override string ToString() =>
		$"Session {SessionId}: {RenderEventCount} events, Age: {GetAge():hh\\:mm\\:ss}, Last access: {GetTimeSinceLastAccess():hh\\:mm\\:ss} ago";
}
