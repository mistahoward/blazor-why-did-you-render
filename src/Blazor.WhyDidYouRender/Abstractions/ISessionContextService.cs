using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Blazor.WhyDidYouRender.Abstractions;

/// <summary>
/// Service for managing session context information across different Blazor hosting environments.
/// </summary>
public interface ISessionContextService {
    /// <summary>
    /// Gets the current session identifier.
    /// </summary>
    /// <returns>A unique session identifier.</returns>
    string GetSessionId();
}
