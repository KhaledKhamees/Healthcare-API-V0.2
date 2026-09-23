using System.ComponentModel.DataAnnotations;

namespace Healthcare_API_V0._2_Application.DTOs.Appointments;

public sealed class UpdateAppointmentRequest
{
    [Required]
    public DateTime StartTime { get; set; }

    public string? ReasonForVisit { get; set; }
}
