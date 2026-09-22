using System.ComponentModel.DataAnnotations;

namespace Healthcare_API_V0._2_Application.DTOs.Auth;

public sealed class DoctorRegisterRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Password and confirmation do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    [Required]
    public Guid SpecializationId { get; set; }
}
