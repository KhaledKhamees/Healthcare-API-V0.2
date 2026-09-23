using System.Security.Cryptography;
using Healthcare_API_V0._2_Application.Interfaces.Services;

namespace Healthcare_API_V0._2_Infrastructure.Identity;

public class RefreshTokenGenerator : IRefreshTokenGenerator
{
    private const int TokenSizeBytes = 32;

    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(TokenSizeBytes);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}
