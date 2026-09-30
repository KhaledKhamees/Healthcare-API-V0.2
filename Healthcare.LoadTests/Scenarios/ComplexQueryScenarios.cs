using Healthcare.LoadTests.Helpers;
using NBomber.Contracts;
using NBomber.CSharp;

namespace Healthcare.LoadTests.Scenarios;

/// <summary>
/// Relationship / complex-query scenario (anonymous — no auth noise).
/// </summary>
public static class ComplexQueryScenarios
{
    public const string DoctorClinicAppointmentsName = "complex_query_doctor_clinic_appointments";

    // WHAT it tests: GET api/doctor-clinics/{id}/appointments.
    // WHY selected: the representative multi-row relationship query — all
    //   appointments for one doctor-clinic (JOIN + filter + list serialization).
    //   This is the query shape most likely to degrade first (row count per
    //   doctor-clinic varies, no cache anywhere on this path).
    // ARCHITECTURE: DoctorClinic path is uncached; AppointmentService.
    //   GetByDoctorClinicAsync -> EF Core WHERE DoctorClinicId == ... -> SQL ->
    //   JSON array. Response size varies with booking density.
    // PROBLEM SIGN: P99 spikes while simple reads stay flat -> per-clinic row
    //   volume or a missing index on Appointments.DoctorClinicId. Investigate:
    //   correlate NBomber sizeBytes with latency, check APM EF spans, verify the
    //   index in SQL Server (README has the query).
    public static ScenarioProps DoctorClinicAppointments(
        HttpClient http, TestDataProvider data, string? token)
        => Scenario.Create(DoctorClinicAppointmentsName, async context =>
        {
            var id = data.RandomDoctorClinicId();
            return await ApiClient.GetAsync(
                http,
                $"api/doctor-clinics/{id}/appointments",
                DoctorClinicAppointmentsName,
                token,
                context.ScenarioCancellationToken);
        });
}
