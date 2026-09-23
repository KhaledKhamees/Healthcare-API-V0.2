using Healthcare_API.Models.Enums;

namespace Healthcare_API_V0._2_Application.DTOs.Doctors;

public sealed class DoctorResponse
{
    public Guid DoctorId { get; set; }
    public Guid ApplicationUserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? LicenseNumber { get; set; }
    public string? Bio { get; set; }
    public DoctorStatus Status { get; set; }
    public Guid SpecializationId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
