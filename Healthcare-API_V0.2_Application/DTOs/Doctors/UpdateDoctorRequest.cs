using System.ComponentModel.DataAnnotations;

namespace Healthcare_API_V0._2_Application.DTOs.Doctors;

public sealed class UpdateDoctorRequest
{
    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    public string? LicenseNumber { get; set; }

    public string? Bio { get; set; }
}
