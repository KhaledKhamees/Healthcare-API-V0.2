using Healthcare_API.Models.Enums;

namespace Healthcare_API_V0._2_Application.DTOs.Appointments;

public sealed class ChangeAppointmentStatusRequest
{
    public AppointmentStatus Status { get; set; }
}
