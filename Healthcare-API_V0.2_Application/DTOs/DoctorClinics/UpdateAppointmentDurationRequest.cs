using System.ComponentModel.DataAnnotations;

namespace Healthcare_API_V0._2_Application.DTOs.DoctorClinics;

public sealed class UpdateAppointmentDurationRequest
{
    [Range(1, int.MaxValue)]
    public int AppointmentDurationMinutes { get; set; }
}
