using System.ComponentModel.DataAnnotations;

namespace Healthcare_API_V0._2_Application.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    [Required]
    [MinLength(32, ErrorMessage = "JWT SecretKey must be at least 32 characters (256 bits) for HMAC-SHA256.")]
    public string SecretKey { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "JWT ExpirationMinutes must be a positive number of minutes.")]
    public int ExpirationMinutes { get; set; }
}
