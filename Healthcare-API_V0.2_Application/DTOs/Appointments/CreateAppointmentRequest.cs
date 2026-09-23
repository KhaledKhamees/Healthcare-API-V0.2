using System.ComponentModel.DataAnnotations;

namespace Healthcare_API_V0._2_Application.DTOs.Appointments;

public sealed class CreateAppointmentRequest
{
    [Required]
    public Guid PatientId { get; set; }

    [Required]
    public Guid DoctorClinicId { get; set; }

    [Required]
    public DateTime StartTime { get; set; }

    public string? ReasonForVisit { get; set; }
}
