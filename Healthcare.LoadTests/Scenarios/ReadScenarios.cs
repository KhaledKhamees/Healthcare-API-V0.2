using Healthcare.LoadTests.Helpers;
using NBomber.Contracts;
using NBomber.CSharp;

namespace Healthcare.LoadTests.Scenarios;

/// <summary>
/// Simple + list READ scenarios. Anonymous (no auth needed), so they isolate
/// API + SQL Server + serialization cost without JWT overhead.
/// </summary>
public static class ReadScenarios
{
    public const string SimpleReadName = "simple_read_doctor_by_id";
    public const string ListQueryName = "list_query_doctors_by_specialization";

    // WHAT it tests: GET api/doctors/{id} — single-row lookup by primary key.
    // WHY selected: the most common read shape in the system; smallest response.
    // ARCHITECTURE: Controller -> DoctorService (cache-aside "doctor:{id}") ->
    //   EITHER Redis (hit) OR EF Core AsNoTracking + SQL Server (miss).
    //   Rotating ids spreads load across rows/keys (realistic browsing).
    // PROBLEM SIGN: P95 grows with users while error rate stays 0 -> downstream
    //   (SQL/Redis) saturating. Investigate: APM spans (redis vs mssql), then
    //   SQL query stats / Redis latency in Kibana.
    public static ScenarioProps SimpleReadDoctorById(
        HttpClient http, TestDataProvider data, string? token)
        => Scenario.Create(SimpleReadName, async context =>
        {
            var id = data.RandomDoctorId();
            return await ApiClient.GetAsync(
                http, $"api/doctors/{id}", SimpleReadName, token, context.ScenarioCancellationToken);
        });

    // WHAT it tests: GET api/doctors?specializationId={id} — filtered list.
    // WHY selected: exposes query + serialization cost of MULTI-row responses;
    //   typically slower and larger than by-id reads.
    // ARCHITECTURE: DoctorService.GetBySpecializationAsync -> EF Core WHERE
    //   SpecializationId == ... (NOT cached) -> JSON array serialization.
    // PROBLEM SIGN: P99 much worse than simple-read, or latency grows with row
    //   count -> missing index / large payloads. Investigate: APM span duration
    //   for the EF query, response sizeBytes in NBomber report, SQL execution plan.
    public static ScenarioProps ListDoctorsBySpecialization(
        HttpClient http, TestDataProvider data, string? token)
        => Scenario.Create(ListQueryName, async context =>
        {
            var url = $"api/doctors?specializationId={data.SpecializationId}";
            return await ApiClient.GetAsync(http, url, ListQueryName, token, context.ScenarioCancellationToken);
        });
}
