namespace Healthcare_API_V0._2_Application.Interfaces.Services;

public interface IJwtService
{
    string GenerateAccessToken(Guid userId, string email, IEnumerable<string> roles);
}
