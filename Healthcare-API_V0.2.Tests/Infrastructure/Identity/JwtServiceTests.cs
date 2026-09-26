using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using Healthcare_API_V0._2_Application.Options;
using Healthcare_API_V0._2_Infrastructure.Identity;
using Microsoft.Extensions.Options;

namespace Healthcare_API_V0._2.Tests.Infrastructure.Identity;

public sealed class JwtServiceTests
{
    private static JwtService CreateSut(string secret = "0123456789ABCDEF0123456789ABCDEF",
        string issuer = "test-issuer", string audience = "test-audience", int minutes = 60)
    {
        var opts = Options.Create(new JwtOptions
        {
            Issuer = issuer, Audience = audience, SecretKey = secret,
            ExpirationMinutes = minutes, RefreshTokenExpirationDays = 7
        });
        return new JwtService(opts);
    }

    private static JwtSecurityToken Read(string token) =>
        new JwtSecurityTokenHandler().ReadJwtToken(token);

    [Fact]
    public void GenerateAccessToken_ContainsExpectedSubjectAndEmail()
    {
        var sut = CreateSut();
        var userId = Guid.NewGuid();

        var token = sut.GenerateAccessToken(userId, "a@test.com", ["Patient"]);

        var jwt = Read(token);
        jwt.Subject.Should().Be(userId.ToString());
        jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value.Should().Be("a@test.com");
    }

    [Fact]
    public void GenerateAccessToken_ContainsRoleClaims()
    {
        var sut = CreateSut();
        var token = sut.GenerateAccessToken(Guid.NewGuid(), "a@test.com", ["Patient", "Doctor"]);

        var jwt = Read(token);
        jwt.Claims.Where(c => c.Type == "role").Select(c => c.Value)
            .Should().BeEquivalentTo("Patient", "Doctor");
    }

    [Fact]
    public void GenerateAccessToken_SetsIssuerAudienceAndExpiry()
    {
        var sut = CreateSut(minutes: 42);
        var before = DateTime.UtcNow;

        var token = sut.GenerateAccessToken(Guid.NewGuid(), "a@test.com", []);

        var jwt = Read(token);
        jwt.Issuer.Should().Be("test-issuer");
        jwt.Audiences.Should().Contain("test-audience");
        jwt.ValidTo.Should().BeAfter(before.AddMinutes(41)).And.BeBefore(DateTime.UtcNow.AddMinutes(43));
    }

    [Fact]
    public void GenerateAccessToken_DoesNotContainSecrets()
    {
        var sut = CreateSut();
        var token = sut.GenerateAccessToken(Guid.NewGuid(), "a@test.com", ["Patient"]);

        token.Should().NotContain("Passw0rd");
        var jwt = Read(token);
        jwt.Claims.Should().NotContain(c => c.Type.Contains("password", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GenerateAccessToken_WithNoRoles_ProducesNoRoleClaims()
    {
        var sut = CreateSut();
        var token = sut.GenerateAccessToken(Guid.NewGuid(), "a@test.com", []);
        Read(token).Claims.Where(c => c.Type == "role").Should().BeEmpty();
    }
}
