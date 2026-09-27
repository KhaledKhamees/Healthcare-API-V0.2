namespace Healthcare_API_V0._2_API.Models;
public sealed record ApiErrorResponse(int StatusCode, string Message, string? TraceId);
