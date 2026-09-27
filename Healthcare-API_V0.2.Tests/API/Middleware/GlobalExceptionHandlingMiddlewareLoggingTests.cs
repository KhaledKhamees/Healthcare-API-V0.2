using FluentAssertions;
using Healthcare_API_V0._2_API.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace Healthcare_API_V0._2.Tests.API.Middleware;

public sealed class GlobalExceptionHandlingMiddlewareLoggingTests
{
    private static async Task<HttpContext> InvokeWithLoggerAsync(
        Mock<ILogger<GlobalExceptionHandlingMiddleware>> loggerMock,
        Exception thrown)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var sut = new GlobalExceptionHandlingMiddleware(loggerMock.Object);
        await sut.InvokeAsync(context, _ => Task.FromException(thrown));

        return context;
    }

    private static bool HasStateKeys(object state, params string[] keys)
    {
        if (state is not IReadOnlyList<KeyValuePair<string, object?>> values)
        {
            return false;
        }

        return keys.All(key => values.Any(pair => pair.Key == key));
    }

    [Fact]
    public async Task InvokeAsync_WithUnknownException_LogsErrorOnce_WithExceptionAndRequestProperties()
    {
        var loggerMock = new Mock<ILogger<GlobalExceptionHandlingMiddleware>>();
        var unknown = new OutOfMemoryException("boom");

        await InvokeWithLoggerAsync(loggerMock, unknown);

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) =>
                    HasStateKeys(state, "RequestMethod", "RequestPath", "TraceId")),
                It.Is<Exception>(ex => ReferenceEquals(ex, unknown)),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithKnownException_LogsSingleWarning_AndNoError()
    {
        var loggerMock = new Mock<ILogger<GlobalExceptionHandlingMiddleware>>();
        var exception = new KeyNotFoundException("Clinic with ID 1 was not found.");

        await InvokeWithLoggerAsync(loggerMock, exception);

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) =>
                    HasStateKeys(state, "RequestMethod", "RequestPath", "TraceId")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WithUnknownException_DoesNotLogSensitiveExceptionMessage()
    {
        string? loggedMessage = null;
        var loggerMock = new Mock<ILogger<GlobalExceptionHandlingMiddleware>>();
        loggerMock
            .Setup(x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback(new InvocationAction(invocation =>
            {
                loggedMessage = invocation.Arguments[2]?.ToString();
            }));

        // The rendered log message template must not embed secret-bearing exception text.
        await InvokeWithLoggerAsync(
            loggerMock,
            new OutOfMemoryException("Server=prod;Password=supersecret"));

        loggedMessage.Should().NotBeNull();
        loggedMessage.Should().NotContain("supersecret");
    }
}
