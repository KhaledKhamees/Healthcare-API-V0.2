namespace Healthcare_API_V0._2_Application.DTOs.Auth;

public sealed class RegisterResponse
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public Guid ProfileId { get; set; }
}
