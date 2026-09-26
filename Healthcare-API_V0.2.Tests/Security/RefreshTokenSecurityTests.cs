using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using Healthcare_API.Models;
using Healthcare_API_V0._2_Application.Options;
using Healthcare_API_V0._2_Infrastructure.Identity;
using Healthcare_API_V0._2.Tests.Helpers;
using Microsoft.Extensions.Options;

namespace Healthcare_API_V0._2.Tests.Security;

public sealed class RefreshTokenSecurityTests
{
    [Fact]
    public void HashLookup_IsDeterministicSha256Hex()
    {
        const string raw = "some-raw-token";
        var expected = IdentityMocks.Sha256Hex(raw);
        expected.Should().MatchRegex("^[0-9A-F]{64}$");

        // Same raw token always maps to the same stored hash (lookup behavior).
        IdentityMocks.Sha256Hex(raw).Should().Be(expected);
        IdentityMocks.Sha256Hex(raw + "x").Should().NotBe(expected);
    }

    [Fact]
    public void StoredToken_NeverEqualsRaw()
    {
        const string raw = "raw-secret-token";
        var stored = new RefreshToken(Guid.NewGuid(), IdentityMocks.Sha256Hex(raw), DateTime.UtcNow.AddDays(7));
        stored.TokenHash.Should().NotBe(raw);
        stored.TokenHash.Should().NotContain(raw);
    }

    [Fact]
    public void RotatedToken_LineageRecorded()
    {
        var oldStored = new RefreshToken(Guid.NewGuid(), "OLDHASH", DateTime.UtcNow.AddDays(7));
        oldStored.MarkReplaced("NEWHASH");
        oldStored.ReplacedByTokenHash.Should().Be("NEWHASH");
        oldStored.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public void Jwt_DoesNotLeakPasswordOrHash()
    {
        var sut = new JwtService(Options.Create(new JwtOptions
        {
            Issuer = "i", Audience = "a", SecretKey = new string('s', 40),
            ExpirationMinutes = 60, RefreshTokenExpirationDays = 7
        }));
        var token = sut.GenerateAccessToken(Guid.NewGuid(), "user@test.com", ["Patient"]);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var all = string.Join(";", jwt.Claims.Select(c => c.Type + "=" + c.Value));
        all.Should().NotContain("password", Exactly.Once().ToString(), "case-insensitive check below");
        jwt.Claims.Should().NotContain(c =>
            c.Type.Contains("password", StringComparison.OrdinalIgnoreCase) ||
            c.Type.Contains("secret", StringComparison.OrdinalIgnoreCase) ||
            c.Type.Contains("hash", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Jwt_OnlyIntendedClaimsPresent()
    {
        var sut = new JwtService(Options.Create(new JwtOptions
        {
            Issuer = "i", Audience = "a", SecretKey = new string('s', 40),
            ExpirationMinutes = 60, RefreshTokenExpirationDays = 7
        }));
        var token = sut.GenerateAccessToken(Guid.NewGuid(), "user@test.com", ["Patient"]);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.Claims.Select(c => c.Type).Should().OnlyContain(t =>
            t == "sub" || t == "email" || t == "role" ||
            t == "nbf" || t == "exp" || t == "iat" || t == "iss" || t == "aud" || t == "jti");
    }
}
