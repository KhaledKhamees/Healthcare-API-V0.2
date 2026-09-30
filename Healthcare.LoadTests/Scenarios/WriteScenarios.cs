using Healthcare.LoadTests.Helpers;
using Healthcare.LoadTests.Models;
using NBomber.Contracts;
using NBomber.CSharp;

namespace Healthcare.LoadTests.Scenarios;

/// <summary>
/// WRITE scenarios (both require auth). Writes are the interesting path: they
/// touch transactions, constraint checks, and cache invalidation — none of
/// which reads exercise.
/// </summary>
public static class WriteScenarios
{
    public const string CreateName = "create_appointment";
    public const string UpdateName = "update_appointment";

    // WHAT it tests: POST api/appointments with a unique future slot.
    // WHY selected: the representative write — validates FKs (patient +
    //   doctor-clinic exist), checks for conflicting bookings (extra SELECT),
    //   then INSERTs in a transaction.
    // ARCHITECTURE: JWT -> AppointmentService.CreateAsync (existence checks +
    //   EnsureNoConflictingAppointmentAsync + INSERT) -> SQL Server.
    //   Each request uses a UNIQUE StartTime so 409s stay rare.
    // DATA WARNING: every success inserts a REAL row for TestData.PatientId /
    //   DoctorClinicId. That is intentional (no safe auto-cleanup exists), so
    //   use a dedicated TEST patient/clinic and purge with the SQL in README.
    // PROBLEM SIGN: rising P95 or 409 rate as load grows -> the conflict-check
    //   SELECT + INSERT is contending (locking on the same doctor-clinic).
    //   Investigate: APM spans (which query is slow), SQL blocking/locks.
    public static ScenarioProps CreateAppointment(
        HttpClient http, TestDataProvider data, string? token)
        => Scenario.Create(CreateName, async context =>
        {
            var payload = new CreateAppointmentPayload(
                Guid.Parse(data.PatientId),
                Guid.Parse(data.DoctorClinicId),
                data.NewAppointmentStartTime(),
                data.RandomReason());

            return await ApiClient.PostJsonAsync(
                http, "api/appointments", CreateName, token, payload, context.ScenarioCancellationToken);
        });

    // WHAT it tests: PUT api/appointments/{id} against a pool of TEST rows.
    // WHY selected: representative update — re-validates + conflict-checks, then
    //   UPDATEs. Exercises write path without growing the table (unlike create).
    // RACE-CONDITION NOTE: ids rotate randomly; with 20+ dedicated TEST rows,
    //   two users rarely update the same row at once. Same-row concurrent
    //   updates are still possible (last-writer-wins, no concurrency token in
    //   this API) — acceptable for a perf test, but use TEST rows only, never
    //   real bookings (see README).
    // PROBLEM SIGN: 404s -> your UpdateAppointmentIds contain ids that were
    //   deleted; 409s -> generated slot collides with an existing booking for
    //   that doctor-clinic (harmless, retry covers it). Sustained high latency
    //   -> same investigation as create (SQL locks on hot rows).
    public static ScenarioProps UpdateAppointment(
        HttpClient http, TestDataProvider data, string? token)
        => Scenario.Create(UpdateName, async context =>
        {
            var id = data.RandomUpdateAppointmentId();
            var payload = new UpdateAppointmentPayload(
                data.NewUpdateStartTime(),
                data.RandomReason());

            return await ApiClient.PutJsonAsync(
                http, $"api/appointments/{id}", UpdateName, token, payload, context.ScenarioCancellationToken);
        });

    // NOTE — no DELETE scenario, deliberately:
    // Deleting under load would destroy the very seed/test rows the other
    // scenarios need (AppointmentIds pool shrinks -> 404 noise everywhere).
    // DELETE is also rare in real usage for this domain. If you need it, create
    // dedicated rows and delete each exactly once (IterationsForConstant).
}
