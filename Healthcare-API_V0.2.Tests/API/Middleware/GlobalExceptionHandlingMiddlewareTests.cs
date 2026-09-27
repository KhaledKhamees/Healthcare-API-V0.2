using System.Net;
using System.Text.Json;
using FluentAssertions;
using Healthcare_API_V0._2_API.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Healthcare_API_V0._2.Tests.API.Middleware;

public sealed class GlobalExceptionHandlingMiddlewareTests
{
    private static GlobalExceptionHandlingMiddleware CreateSut() =>
        new(NullLogger<GlobalExceptionHandlingMiddleware>.Instance);

    private static async Task<(HttpContext Context, string Body)> InvokeAsync(
        Exception? thrown,
        CancellationToken cancellationToken = default)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.RequestAborted = cancellationToken;

        RequestDelegate next = _ => thrown is null
            ? Task.CompletedTask
            : Task.FromException(thrown);

        await CreateSut().InvokeAsync(context, next);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        return (context, await reader.ReadToEndAsync());
    }

    private static JsonDocument ParseBody(string body) =>
        JsonDocument.Parse(body);

    [Fact]
    public async Task InvokeAsync_WhenNoExceptionThrown_PassesThroughWithoutErrorBody()
    {
        var (context, body) = await InvokeAsync(null);

        context.Response.StatusCode.Should().Be((int)HttpStatusCode.OK);
        body.Should().BeEmpty();
    }

    [Fact]
    public async Task InvokeAsync_WithUnknownException_Returns500WithGenericSafeMessage()
    {
        var (context, body) = await InvokeAsync(
            new InvalidCastException("Server=prod-db;Password=supersecret; Connection refused."));

        context.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
        context.Response.ContentType.Should().Be("application/json");

        var json = ParseBody(body);
        json.RootElement.GetProperty("statusCode").GetInt32().Should().Be(500);
        json.RootElement.GetProperty("message").GetString().Should().Be("An unexpected error occurred.");
        json.RootElement.GetProperty("traceId").GetString().Should().Be(context.TraceIdentifier);

        body.Should().NotContain("supersecret")
            .And.NotContain("stack", "stack traces must never leak to clients")
            .And.NotContain("at ", "stack frame markers must never leak to clients");
    }

    [Fact]
    public async Task InvokeAsync_WithKeyNotFoundException_Returns404()
    {
        var (context, body) = await InvokeAsync(
            new KeyNotFoundException("Clinic with ID 123 was not found."));

        context.Response.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
        context.Response.ContentType.Should().Be("application/json");

        var json = ParseBody(body);
        json.RootElement.GetProperty("statusCode").GetInt32().Should().Be(404);
        json.RootElement.GetProperty("message").GetString().Should().Be("Clinic with ID 123 was not found.");
        json.RootElement.GetProperty("traceId").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task InvokeAsync_WithArgumentException_Returns400()
    {
        var (context, body) = await InvokeAsync(
            new ArgumentException("Email is required.", "request"));

        context.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

        var json = ParseBody(body);
        json.RootElement.GetProperty("statusCode").GetInt32().Should().Be(400);
        json.RootElement.GetProperty("message").GetString().Should().Contain("Email is required.");
    }

    [Fact]
    public async Task InvokeAsync_WithArgumentNullException_Returns400()
    {
        var (context, _) = await InvokeAsync(new ArgumentNullException("request"));

        context.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task InvokeAsync_WithInvalidOperationException_Returns409()
    {
        var (context, body) = await InvokeAsync(
            new InvalidOperationException("A conflicting appointment already exists for this doctor-clinic at the requested time."));

        context.Response.StatusCode.Should().Be((int)HttpStatusCode.Conflict);

        var json = ParseBody(body);
        json.RootElement.GetProperty("statusCode").GetInt32().Should().Be(409);
        json.RootElement.GetProperty("message").GetString().Should().Contain("conflicting appointment");
    }

    [Fact]
    public async Task InvokeAsync_WithUnauthorizedAccessException_Returns401()
    {
        var (context, body) = await InvokeAsync(
            new UnauthorizedAccessException("Invalid email or password."));

        context.Response.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);

        var json = ParseBody(body);
        json.RootElement.GetProperty("statusCode").GetInt32().Should().Be(401);
    }

    [Fact]
    public async Task InvokeAsync_WhenRequestCancelled_RethrowsInsteadOfWritingResponse()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = () => InvokeAsync(new OperationCanceledException(), cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
