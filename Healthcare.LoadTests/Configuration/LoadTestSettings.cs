namespace Healthcare.LoadTests.Configuration;

/// <summary>
/// Root settings object, bound from the "LoadTest" section of appsettings.json.
/// Environment variables override file values:
///   LOADTEST_BASEURL, LOADTEST_EMAIL, LOADTEST_PASSWORD.
/// </summary>
public sealed class LoadTestSettings
{
    public string BaseUrl { get; set; } = "http://localhost:5208";
    public AuthSettings Auth { get; set; } = new();
    public TestDataSettings TestData { get; set; } = new();
    public LoadSettings Load { get; set; } = new();
}

public sealed class AuthSettings
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class TestDataSettings
{
    public string[] DoctorIds { get; set; } = [];
    public string CacheHitClinicId { get; set; } = string.Empty;
    public string[] ClinicIds { get; set; } = [];
    public string SpecializationId { get; set; } = string.Empty;
    public string City { get; set; } = "Springfield";
    public string PatientId { get; set; } = string.Empty;
    public string DoctorClinicId { get; set; } = string.Empty;
    public string[] DoctorClinicIds { get; set; } = [];
    public string[] AppointmentIds { get; set; } = [];
    public string[] UpdateAppointmentIds { get; set; } = [];
}

public sealed class LoadSettings
{
    public int BaselineUsers { get; set; } = 5;
    public int BaselineDurationSeconds { get; set; } = 30;
    public int NormalUsers { get; set; } = 50;
    public int NormalDurationSeconds { get; set; } = 60;
    public int SustainedMinutes { get; set; } = 5;
    public int RampStepSeconds { get; set; } = 30;
    public int[] RampSteps { get; set; } = [10, 25, 50, 100];
    public int StressUsers { get; set; } = 200;
    public int StressDurationSeconds { get; set; } = 60;
    public int RequestTimeoutSeconds { get; set; } = 30;
}
