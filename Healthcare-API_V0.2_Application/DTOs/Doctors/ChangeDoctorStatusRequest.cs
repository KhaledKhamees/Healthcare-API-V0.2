using Healthcare_API.Models.Enums;

namespace Healthcare_API_V0._2_Application.DTOs.Doctors;

public sealed class ChangeDoctorStatusRequest
{
    public DoctorStatus Status { get; set; }
}
