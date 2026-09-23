using Healthcare_API.Models.Enums;

namespace Healthcare_API_V0._2_Application.DTOs.Appointments;

public sealed class AppointmentResponse
{
    public Guid AppointmentId { get; set; }
    public Guid PatientId { get; set; }
    public Guid DoctorClinicId { get; set; }
    public DateTime StartTime { get; set; }
    public string? ReasonForVisit { get; set; }
    public AppointmentStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
