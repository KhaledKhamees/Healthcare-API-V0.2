using Healthcare.LoadTests.Helpers;
using NBomber.Contracts;
using NBomber.CSharp;

namespace Healthcare.LoadTests.Scenarios;

/// <summary>
/// Cache-hit vs database-path comparison.
/// The API caches ONLY GetById for Doctor/Patient/Clinic/Specialization
/// (key "{entity}:{id}", 30 min TTL, StackExchange.Redis). Appointments and
/// DoctorClinic/Availability are NEVER cached — every call hits SQL Server.
/// </summary>
public static class CacheScenarios
{
    public const string CacheHitName = "cache_hit_clinic_by_id";
    public const string DbReadName = "db_read_appointment_by_id";

    // WHAT it tests: GET api/clinics/{FIXED_ID} — the same row every time.
    // WHY selected: after the first call populates Redis ("clinic:{id}"), ~all
    //   subsequent calls are served from cache. Measures cached-path latency.
    // ARCHITECTURE: ClinicService cache-aside -> Redis StringGet (hit) -> no SQL.
    // PROBLEM SIGN: latency here is HIGH or CLOSE TO the db_read scenario ->
    //   cache is not effective (evictions, serialization cost, or Redis itself
    //   slow). Investigate: APM "redis" spans vs "mssql" spans, Redis latency
    //   in Kibana, ClinicService logs ("cache hit" vs DB fallback).
    public static ScenarioProps CacheHitClinicById(
        HttpClient http, TestDataProvider data, string? token)
        => Scenario.Create(CacheHitName, async context =>
            await ApiClient.GetAsync(
                http, $"api/clinics/{data.CacheHitClinicId}", CacheHitName, token, context.ScenarioCancellationToken));

    // WHAT it tests: GET api/appointments/{ROTATING_ID} (authenticated).
    // WHY selected: AppointmentService has NO caching, so this is the pure
    //   database path. Compare directly against cache_hit to quantify what the
    //   cache buys you (expect: higher P50/P95 here).
    // ARCHITECTURE: JWT validation -> AppointmentService -> EF Core -> SQL Server.
    //   Rotating ids avoid single-row contention.
    // PROBLEM SIGN: P95 far above cache_hit AND throughput plateaus as users
    //   grow -> SQL Server is the bottleneck. Investigate: APM transaction
    //   breakdown (jwt? ef? sql?), SQL wait stats, connection-pool exhaustion
    //   (default pool 100 — watch for timeouts near ~100+ concurrent users).
    public static ScenarioProps DbReadAppointmentById(
        HttpClient http, TestDataProvider data, string? token)
        => Scenario.Create(DbReadName, async context =>
        {
            var id = data.RandomAppointmentId();
            return await ApiClient.GetAsync(
                http, $"api/appointments/{id}", DbReadName, token, context.ScenarioCancellationToken);
        });
}
