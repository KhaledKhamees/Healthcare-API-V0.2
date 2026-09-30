using System.Text.Json;
using System.Text.Json.Serialization;

namespace Healthcare.LoadTests.Models;

public sealed record LoginRequestPayload(string Email, string Password);

public sealed record LoginResponseDto
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public List<string> Roles { get; init; } = [];
    public string AccessToken { get; init; } = string.Empty;
    public string TokenType { get; init; } = "Bearer";
    public string RefreshToken { get; init; } = string.Empty;
}

public sealed record CreateAppointmentPayload(
    Guid PatientId,
    Guid DoctorClinicId,
    DateTime StartTime,
    string? ReasonForVisit);

public sealed record UpdateAppointmentPayload(
    DateTime StartTime,
    string? ReasonForVisit);

public static class JsonContext
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}
