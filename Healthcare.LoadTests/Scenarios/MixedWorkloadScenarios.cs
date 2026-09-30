using Healthcare.LoadTests.Helpers;
using Healthcare.LoadTests.Models;
using NBomber.Contracts;
using NBomber.CSharp;

namespace Healthcare.LoadTests.Scenarios;

/// <summary>
/// ONE scenario that behaves like a crowd of real users doing different things
/// at the same time, instead of every virtual user hammering the same endpoint.
///
/// WEIGHT ASSUMPTION (documented, not copied blindly): this is a healthcare
/// provider directory + booking system. Browsing (doctor/clinic reads) dominates;
/// booking (create/update) is rare; deep relationship queries are occasional:
///   30% doctor by id (rotating)      — browsing doctors
///   15% clinic by id (fixed id)      — cache-hit traffic from repeat views
///   15% doctors by specialization    — filtered directory search
///   10% clinics by city              — location search
///   10% doctor-clinic appointments   — schedule browsing (complex query)
///   10% appointment by id (auth)     — patients checking bookings
///    6% create appointment (auth)    — new bookings
///    4% update appointment (auth)    — rescheduling
/// Tune the weights when you know real traffic (APM/Kibana request counts).
///
/// WHY it matters (learning note): isolated scenarios find per-endpoint limits;
/// only a MIXED run shows cross-endpoint contention (shared SQL pool, shared
/// Redis, shared thread pool) — i.e. "how does the API behave when everything
/// happens at once". If mixed P95 is worse than every isolated P95, the
/// bottleneck is a SHARED resource (connection pools, CPU/GC), not one query.
/// </summary>
public static class MixedWorkloadScenarios
{
    public const string MixedFlowName = "mixed_realistic_user_flow";

    public static ScenarioProps MixedRealisticUserFlow(
        HttpClient http, TestDataProvider data, string? token)
        => Scenario.Create(MixedFlowName, async context =>
        {
            var roll = Random.Shared.Next(100);
            var ct = context.ScenarioCancellationToken;

            // 0-29: simple read (rotating doctor).
            if (roll < 30)
            {
                return await ApiClient.GetAsync(
                    http, $"api/doctors/{data.RandomDoctorId()}", MixedFlowName, token, ct);
            }

            // 30-44: cache-hit read (fixed clinic).
            if (roll < 45)
            {
                return await ApiClient.GetAsync(
                    http, $"api/clinics/{data.CacheHitClinicId}", MixedFlowName, token, ct);
            }

            // 45-59: list query (doctors by specialization).
            if (roll < 60)
            {
                return await ApiClient.GetAsync(
                    http, $"api/doctors?specializationId={data.SpecializationId}", MixedFlowName, token, ct);
            }

            // 60-69: list query (clinics by city).
            if (roll < 70)
            {
                return await ApiClient.GetAsync(
                    http, $"api/clinics/by-city?city={Uri.EscapeDataString(data.City)}", MixedFlowName, token, ct);
            }

            // 70-79: complex query (appointments of a doctor-clinic).
            if (roll < 80)
            {
                return await ApiClient.GetAsync(
                    http, $"api/doctor-clinics/{data.RandomDoctorClinicId()}/appointments", MixedFlowName, token, ct);
            }

            // 80-89: authenticated DB read (patient checking a booking).
            if (roll < 90)
            {
                return await ApiClient.GetAsync(
                    http, $"api/appointments/{data.RandomAppointmentId()}", MixedFlowName, token, ct);
            }

            // 90-95: create (new booking, unique slot).
            if (roll < 96)
            {
                var payload = new CreateAppointmentPayload(
                    Guid.Parse(data.PatientId),
                    Guid.Parse(data.DoctorClinicId),
                    data.NewAppointmentStartTime(),
                    data.RandomReason());

                return await ApiClient.PostJsonAsync(
                    http, "api/appointments", MixedFlowName, token, payload, ct);
            }

            // 96-99: update (reschedule a TEST booking).
            var update = new UpdateAppointmentPayload(
                data.NewUpdateStartTime(),
                data.RandomReason());

            return await ApiClient.PutJsonAsync(
                http, $"api/appointments/{data.RandomUpdateAppointmentId()}", MixedFlowName, token, update, ct);
        });
}
