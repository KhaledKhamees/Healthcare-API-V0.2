using System.Diagnostics;
using System.Text.Json;
using Healthcare_API_V0._2_API.Models;

namespace Healthcare_API_V0._2_API.Middleware;

public sealed class GlobalExceptionHandlingMiddleware : IMiddleware
{
    private const string GenericErrorMessage = "An unexpected error occurred.";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            await WriteErrorResponseAsync(context, ex);
        }
    }

    private async Task WriteErrorResponseAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message) = MapException(exception);
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception. TraceId: {TraceId}", traceId);
        }
        else
        {
            _logger.LogWarning(exception, "Handled {StatusCode} error. TraceId: {TraceId}", statusCode, traceId);
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = new ApiErrorResponse(statusCode, message, traceId);
        await JsonSerializer.SerializeAsync(
            context.Response.Body,
            response,
            SerializerOptions,
            context.RequestAborted);
    }

    private static (int StatusCode, string Message) MapException(Exception exception) =>
        exception switch
        {

            ArgumentException ex => (StatusCodes.Status400BadRequest, ex.Message),
            KeyNotFoundException ex => (StatusCodes.Status404NotFound, ex.Message),
            UnauthorizedAccessException ex => (StatusCodes.Status401Unauthorized, ex.Message),
            InvalidOperationException ex => (StatusCodes.Status409Conflict, ex.Message),
            _ => (StatusCodes.Status500InternalServerError, GenericErrorMessage)
        };
}
