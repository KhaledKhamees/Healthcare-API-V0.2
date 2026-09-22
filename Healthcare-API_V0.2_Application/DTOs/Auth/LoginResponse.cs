namespace Healthcare_API_V0._2_Application.DTOs.Auth;

public sealed class LoginResponse
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}
