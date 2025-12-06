using Blazor.WhyDidYouRender.Abstractions;

namespace Blazor.WhyDidYouRender.Services;

/// <summary>
/// WebAssembly implementation of session context service (in-memory; no browser storage).
/// </summary>
public class WasmSessionContextService : ISessionContextService
{
	private string? _sessionId;
	private readonly Lock _lock = new();

	/// <inheritdoc />
	public string GetSessionId()
	{
		if (_sessionId != null)
			return _sessionId;

		lock (_lock)
		{
			if (_sessionId != null)
				return _sessionId;

			_sessionId = $"wasm-{Guid.NewGuid():N}";

			return _sessionId;
		}
	}
}
