using System.ComponentModel.DataAnnotations;

namespace Healthcare_API_V0._2_Application.DTOs.Auth;

public sealed class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
