using System.ComponentModel.DataAnnotations;

namespace Healthcare_API_V0._2_Application.DTOs.Specializations;

public sealed class SpecializationRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
}
