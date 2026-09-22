using System.ComponentModel.DataAnnotations;

namespace Healthcare_API_V0._2_Application.DTOs.Auth;

public sealed class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}
