using Healthcare.LoadTests.Configuration;

namespace Healthcare.LoadTests.Helpers;

/// <summary>
/// Supplies test-data ids and realistic random payloads to scenarios.
///
/// DESIGN NOTES (learning):
/// - READ scenarios rotate through POOLS of ids (random pick per request) so
///   concurrent users spread across rows instead of serializing on one row
///   lock / one cache key. The bigger the pool, the more realistic.
/// - CACHE-HIT scenario deliberately uses ONE fixed id so Redis can serve it.
/// - WRITE scenarios generate a UNIQUE StartTime per request. The API rejects
///   duplicate (DoctorClinicId + StartTime) with 409, so reusing a fixed time
///   would turn the whole run into "409 Conflict" noise instead of real writes.
/// </summary>
public sealed class TestDataProvider
{
    private static readonly string[] Reasons =
    [
        "Routine checkup",
        "Follow-up visit",
        "Persistent headache",
        "Annual physical examination",
        "Back pain consultation",
        "Blood pressure review"
    ];

    private readonly TestDataSettings _data;

    public TestDataProvider(LoadTestSettings settings)
    {
        _data = settings.TestData;
    }

    public string CacheHitClinicId => _data.CacheHitClinicId;
    public string SpecializationId => _data.SpecializationId;
    public string City => _data.City;
    public string PatientId => _data.PatientId;
    public string DoctorClinicId => _data.DoctorClinicId;

    public string RandomDoctorId() => Pick(_data.DoctorIds);
    public string RandomClinicId() => Pick(_data.ClinicIds);
    public string RandomDoctorClinicId() => Pick(_data.DoctorClinicIds);
    public string RandomAppointmentId() => Pick(_data.AppointmentIds);
    public string RandomUpdateAppointmentId() => Pick(_data.UpdateAppointmentIds);

    /// <summary>
    /// A future slot unlikely to collide with existing bookings:
    /// random day 2..45 days out, 08:00..17:59, random second.
    /// </summary>
    public DateTime NewAppointmentStartTime() => RandomFutureSlot(2, 45);

    /// <summary>Same idea for updates: a different future window.</summary>
    public DateTime NewUpdateStartTime() => RandomFutureSlot(3, 60);

    public string RandomReason()
        => Reasons[Random.Shared.Next(Reasons.Length)];

    private static DateTime RandomFutureSlot(int minDays, int maxDays)
    {
        var rnd = Random.Shared;
        return DateTime.UtcNow.Date
            .AddDays(rnd.Next(minDays, maxDays))
            .AddHours(rnd.Next(8, 18))
            .AddMinutes(rnd.Next(0, 60))
            .AddSeconds(rnd.Next(0, 60));
    }

    private static string Pick(string[] pool)
    {
        if (pool.Length == 0)
            throw new InvalidOperationException("Test-data pool is empty; check appsettings.json.");
        return pool[Random.Shared.Next(pool.Length)];
    }

    /// <summary>
    /// Validates that every required id was replaced with a real value.
    /// Returns human-readable errors (empty = OK).
    /// </summary>
    public static List<string> Validate(LoadTestSettings settings)
    {
        var errors = new List<string>();
        var d = settings.TestData;

        void Need(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(value) || IsPlaceholder(value))
                errors.Add($"{name} is not configured (still placeholder).");
        }

        void NeedPool(string name, string[] pool)
        {
            if (pool.Length == 0 || pool.All(IsPlaceholder))
                errors.Add($"{name} has no real ids (still placeholder).");
        }

        NeedPool(nameof(d.DoctorIds), d.DoctorIds);
        Need(nameof(d.CacheHitClinicId), d.CacheHitClinicId);
        NeedPool(nameof(d.ClinicIds), d.ClinicIds);
        Need(nameof(d.SpecializationId), d.SpecializationId);
        Need(nameof(d.PatientId), d.PatientId);
        Need(nameof(d.DoctorClinicId), d.DoctorClinicId);
        NeedPool(nameof(d.DoctorClinicIds), d.DoctorClinicIds);
        NeedPool(nameof(d.AppointmentIds), d.AppointmentIds);
        NeedPool(nameof(d.UpdateAppointmentIds), d.UpdateAppointmentIds);

        if (string.IsNullOrWhiteSpace(d.City))
            errors.Add($"{nameof(d.City)} is empty.");

        return errors;
    }

    private static bool IsPlaceholder(string value)
        => value.Contains("00000000-0000-0000-0000-000000000000");
}
