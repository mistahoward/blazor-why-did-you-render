using System.Text;
using Blazor.WhyDidYouRender.Configuration;
using Blazor.WhyDidYouRender.Logging;
using Blazor.WhyDidYouRender.Services;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Blazor.WhyDidYouRender.Tests;

public class ServerSessionContextServiceTests
{
	private readonly WhyDidYouRenderConfig _config;

	public ServerSessionContextServiceTests()
	{
		_config = new WhyDidYouRenderConfig { Enabled = true };
	}

	[Fact]
	public void GetSessionId_WhenHttpContextIsNull_ReturnsGuidBasedSessionId()
	{
		// Arrange
		var httpContextAccessor = new Mock<IHttpContextAccessor>();
		httpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext)null!);

		var service = new ServerSessionContextService(httpContextAccessor.Object, _config);

		// Act
		var sessionId = service.GetSessionId();

		// Assert
		Assert.NotNull(sessionId);
		Assert.StartsWith("server-", sessionId);
		Assert.Matches(@"^server-[a-f0-9]{32}$", sessionId); // GUID format
	}

	[Fact]
	public void GetSessionId_WhenSessionIsNull_ReturnsTraceIdentifierBasedSessionId()
	{
		// Arrange
		var httpContext = new DefaultHttpContext();
		httpContext.TraceIdentifier = "test-trace-id-123";
		// Don't set up session - it will be null

		var httpContextAccessor = new Mock<IHttpContextAccessor>();
		httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

		var service = new ServerSessionContextService(httpContextAccessor.Object, _config);

		// Act
		var sessionId = service.GetSessionId();

		// Assert
		Assert.Equal("server-test-trace-id-123", sessionId);
	}

	[Fact]
	public void GetSessionId_WhenResponseHasStarted_ReturnsTraceIdentifierFallback()
	{
		// Arrange
		var httpContext = new DefaultHttpContext();
		httpContext.TraceIdentifier = "test-trace-id-456";

		var sessionMock = new Mock<ISession>();
		httpContext.Features.Set(sessionMock.Object);

		// Make Response.HasStarted return true
		httpContext.Response.Body = new MemoryStream();
		httpContext.Response.Body.Write(Encoding.UTF8.GetBytes("x")); // Start the response
		httpContext.Response.Body.Flush();

		var httpContextAccessor = new Mock<IHttpContextAccessor>();
		httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

		var service = new ServerSessionContextService(httpContextAccessor.Object, _config);

		// Act
		var sessionId = service.GetSessionId();

		// Assert
		Assert.Equal("server-test-trace-id-456", sessionId);
		sessionMock.Verify(s => s.GetString(It.IsAny<string>()), Times.Never);
	}

	[Fact]
	public void GetSessionId_WhenResponseHasStartedWithLogger_LogsDebugMessage()
	{
		// Arrange
		var httpContext = new DefaultHttpContext();
		httpContext.TraceIdentifier = "test-trace-id-789";

		var sessionMock = new Mock<ISession>();
		httpContext.Features.Set(sessionMock.Object);

		// Make Response.HasStarted return true
		httpContext.Response.Body = new MemoryStream();
		httpContext.Response.Body.Write(Encoding.UTF8.GetBytes("x"));
		httpContext.Response.Body.Flush();

		var httpContextAccessor = new Mock<IHttpContextAccessor>();
		httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

		var loggerMock = new Mock<IWhyDidYouRenderLogger>();

		var service = new ServerSessionContextService(httpContextAccessor.Object, _config, loggerMock.Object);

		// Act
		var sessionId = service.GetSessionId();

		// Assert
		Assert.Equal("server-test-trace-id-789", sessionId);
		loggerMock.Verify(
			l => l.LogDebug(It.Is<string>(msg => msg.Contains("Response has started")), It.IsAny<Dictionary<string, object>>()),
			Times.Once
		);
	}

	[Fact]
	public void GetSessionId_WhenSessionAccessThrowsException_ReturnsFallbackSessionId()
	{
		// Arrange
		var httpContext = new DefaultHttpContext();
		httpContext.TraceIdentifier = "test-trace-id-error";

		var sessionMock = new Mock<ISession>();
		sessionMock.Setup(s => s.GetString(It.IsAny<string>())).Throws(new InvalidOperationException("Session cannot be established"));

		httpContext.Features.Set(sessionMock.Object);

		var httpContextAccessor = new Mock<IHttpContextAccessor>();
		httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

		var service = new ServerSessionContextService(httpContextAccessor.Object, _config);

		// Act
		var sessionId = service.GetSessionId();

		// Assert
		Assert.Equal("server-test-trace-id-error", sessionId);
	}

	[Fact]
	public void GetSessionId_WhenSessionAccessThrowsExceptionWithLogger_LogsError()
	{
		// Arrange
		var httpContext = new DefaultHttpContext();
		httpContext.TraceIdentifier = "test-trace-id-error-log";

		var sessionMock = new Mock<ISession>();
		var expectedException = new InvalidOperationException("Session cannot be established");
		sessionMock.Setup(s => s.GetString(It.IsAny<string>())).Throws(expectedException);

		httpContext.Features.Set(sessionMock.Object);

		var httpContextAccessor = new Mock<IHttpContextAccessor>();
		httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

		var loggerMock = new Mock<IWhyDidYouRenderLogger>();

		var service = new ServerSessionContextService(httpContextAccessor.Object, _config, loggerMock.Object);

		// Act
		var sessionId = service.GetSessionId();

		// Assert
		Assert.Equal("server-test-trace-id-error-log", sessionId);
		loggerMock.Verify(
			l =>
				l.LogError(
					It.Is<string>(msg => msg.Contains("Failed to access session")),
					It.IsAny<Exception>(),
					It.IsAny<Dictionary<string, object>>()
				),
			Times.Once
		);
	}

	[Fact]
	public void GetSessionId_WhenSessionExistsAndResponseNotStarted_ReturnsExistingSessionId()
	{
		// Arrange
		var httpContext = new DefaultHttpContext();
		var existingSessionId = "server-existing-session-12345";

		var sessionMock = new Mock<ISession>();
		sessionMock.Setup(s => s.GetString("WhyDidYouRender_SessionId")).Returns(existingSessionId);

		httpContext.Features.Set(sessionMock.Object);

		var httpContextAccessor = new Mock<IHttpContextAccessor>();
		httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

		var service = new ServerSessionContextService(httpContextAccessor.Object, _config);

		// Act
		var sessionId = service.GetSessionId();

		// Assert
		Assert.Equal(existingSessionId, sessionId);
		sessionMock.Verify(s => s.GetString("WhyDidYouRender_SessionId"), Times.Once);
	}

	[Fact]
	public void GetSessionId_WhenSessionDoesNotExistAndResponseNotStarted_CreatesNewSessionId()
	{
		// Arrange
		var httpContext = new DefaultHttpContext();

		var sessionMock = new Mock<ISession>();
		sessionMock.Setup(s => s.GetString("WhyDidYouRender_SessionId")).Returns((string?)null);

		httpContext.Features.Set(sessionMock.Object);

		var httpContextAccessor = new Mock<IHttpContextAccessor>();
		httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

		var service = new ServerSessionContextService(httpContextAccessor.Object, _config);

		// Act
		var sessionId = service.GetSessionId();

		// Assert
		Assert.NotNull(sessionId);
		Assert.StartsWith("server-", sessionId);
		Assert.Matches(@"^server-[a-f0-9]{32}$", sessionId); // GUID format
		sessionMock.Verify(s => s.GetString("WhyDidYouRender_SessionId"), Times.Once);
		sessionMock.Verify(s => s.SetString("WhyDidYouRender_SessionId", It.IsAny<string>()), Times.Once);
	}

	[Fact]
	public void GetSessionId_WhenCalledMultipleTimes_ReturnsSameSessionId()
	{
		// Arrange
		var httpContext = new DefaultHttpContext();
		string? storedSessionId = null;

		var sessionMock = new Mock<ISession>();
		sessionMock.Setup(s => s.GetString("WhyDidYouRender_SessionId")).Returns(() => storedSessionId);
		sessionMock
			.Setup(s => s.SetString("WhyDidYouRender_SessionId", It.IsAny<string>()))
			.Callback<string, string>((key, value) => storedSessionId = value);

		httpContext.Features.Set(sessionMock.Object);

		var httpContextAccessor = new Mock<IHttpContextAccessor>();
		httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

		var service = new ServerSessionContextService(httpContextAccessor.Object, _config);

		// Act
		var sessionId1 = service.GetSessionId();
		var sessionId2 = service.GetSessionId();
		var sessionId3 = service.GetSessionId();

		// Assert
		Assert.Equal(sessionId1, sessionId2);
		Assert.Equal(sessionId2, sessionId3);
		sessionMock.Verify(s => s.SetString("WhyDidYouRender_SessionId", It.IsAny<string>()), Times.Once);
	}

	[Fact]
	public void StorageDescription_ReturnsCorrectDescription()
	{
		// Arrange
		var httpContextAccessor = new Mock<IHttpContextAccessor>();
		var service = new ServerSessionContextService(httpContextAccessor.Object, _config);

		// Act
		var description = service.StorageDescription;

		// Assert
		Assert.Equal("Server-side session storage (HttpContext.Session)", description);
	}

	[Fact]
	public void SupportsPersistentStorage_ReturnsTrue()
	{
		// Arrange
		var httpContextAccessor = new Mock<IHttpContextAccessor>();
		var service = new ServerSessionContextService(httpContextAccessor.Object, _config);

		// Act
		var supports = service.SupportsPersistentStorage;

		// Assert
		Assert.True(supports);
	}

	[Fact]
	public void SupportsCrossRequestPersistence_ReturnsTrue()
	{
		// Arrange
		var httpContextAccessor = new Mock<IHttpContextAccessor>();
		var service = new ServerSessionContextService(httpContextAccessor.Object, _config);

		// Act
		var supports = service.SupportsCrossRequestPersistence;

		// Assert
		Assert.True(supports);
	}
}
