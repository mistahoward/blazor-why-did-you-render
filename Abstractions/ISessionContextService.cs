using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Blazor.WhyDidYouRender.Abstractions;

/// <summary>
/// Service for managing session context information across different Blazor hosting environments.
/// </summary>
public interface ISessionContextService
{
    /// <summary>
    /// Gets the current session identifier.
    /// </summary>
    /// <returns>A unique session identifier.</returns>
    string GetSessionId();
    
    /// <summary>
    /// Sets session information with the specified key and value.
    /// </summary>
    /// <param name="key">The key to store the value under.</param>
    /// <param name="value">The value to store.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SetSessionInfoAsync(string key, object value);
    
    /// <summary>
    /// Gets session information for the specified key.
    /// </summary>
    /// <typeparam name="T">The type of the value to retrieve.</typeparam>
    /// <param name="key">The key to retrieve the value for.</param>
    /// <returns>The value if found, otherwise the default value for the type.</returns>
    Task<T?> GetSessionInfoAsync<T>(string key);
    
    /// <summary>
    /// Removes session information for the specified key.
    /// </summary>
    /// <param name="key">The key to remove.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RemoveSessionInfoAsync(string key);
    
    /// <summary>
    /// Gets all session information keys.
    /// </summary>
    /// <returns>A collection of all session keys.</returns>
    Task<IEnumerable<string>> GetSessionKeysAsync();
    
    /// <summary>
    /// Clears all session information.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ClearSessionAsync();
    
    /// <summary>
    /// Gets whether the session context service supports persistent storage.
    /// </summary>
    bool SupportsPersistentStorage { get; }
    
    /// <summary>
    /// Gets whether the session context service supports cross-request persistence.
    /// </summary>
    bool SupportsCrossRequestPersistence { get; }
    
    /// <summary>
    /// Gets a description of the session storage mechanism being used.
    /// </summary>
    string StorageDescription { get; }
}
