using FluentAssertions;
using Healthcare_API.Models;
using Healthcare_API_V0._2_Application.DTOs.Auth;
using Healthcare_API_V0._2_Application.Interfaces.Repository;
using Healthcare_API_V0._2_Application.Interfaces.Services;
using Healthcare_API_V0._2_Application.Options;
using Healthcare_API_V0._2_Infrastructure.Identity;
using Healthcare_API_V0._2_Infrastructure.Persistence;
using Healthcare_API_V0._2.Tests.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Healthcare_API_V0._2.Tests.Application.Auth;

public sealed class AuthServiceRefreshTests : IDisposable
{
    private readonly HealthcareDbContext _db;
    private readonly SqliteConnection _conn;
    private readonly Mock<UserManager<ApplicationUser>> _users;
    private readonly Mock<RoleManager<IdentityRole<Guid>>> _roles;
    private readonly Mock<IJwtService> _jwt;
    private readonly Mock<IRefreshTokenGenerator> _gen;
    private readonly Mock<IRefreshTokenRepository> _tokens;
    private readonly AuthService _sut;
    private readonly JwtOptions _jwtOptions = new()
    {
        Issuer = "t", Audience = "t", SecretKey = new string('k', 40),
        ExpirationMinutes = 60, RefreshTokenExpirationDays = 7
    };

    public AuthServiceRefreshTests()
    {
        _db = TestDbContextFactory.CreateDisposable(out _conn);
        _users = IdentityMocks.MockUserManager();
        _roles = IdentityMocks.MockRoleManager();
        _jwt = new Mock<IJwtService>();
        _gen = new Mock<IRefreshTokenGenerator>();
        _tokens = new Mock<IRefreshTokenRepository>();
        _sut = new AuthService(_users.Object, _roles.Object, _db,
            Mock.Of<IPatientService>(), Mock.Of<IDoctorService>(),
            _jwt.Object, _gen.Object, _tokens.Object,
            Options.Create(_jwtOptions), NullLogger<AuthService>.Instance);
    }

    public void Dispose()
    {
        _db.Dispose();
        _conn.Dispose();
    }

    private static RefreshToken StoredToken(Guid userId, string raw) =>
        new(userId, IdentityMocks.Sha256Hex(raw), DateTime.UtcNow.AddDays(7));

    [Fact]
    public async Task RefreshAsync_WithValidToken_RotatesRefreshToken()
    {
        var user = IdentityMocks.NewUser("r@test.com");
        const string oldRaw = "old-raw-token";
        var stored = StoredToken(user.Id, oldRaw);
        _tokens.Setup(m => m.GetByTokenHashAsync(IdentityMocks.Sha256Hex(oldRaw), It.IsAny<CancellationToken>()))
            .ReturnsAsync(stored);
        _users.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _users.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false);
        _users.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Patient" });
        _jwt.Setup(m => m.GenerateAccessToken(user.Id, It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
            .Returns("new-access");
        _gen.Setup(m => m.GenerateRefreshToken()).Returns("new-raw-token");
        RefreshToken? added = null;
        _tokens.Setup(m => m.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Callback<RefreshToken, CancellationToken>((t, _) => added = t).Returns(Task.CompletedTask);
        _tokens.Setup(m => m.UpdateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.RefreshAsync(new RefreshTokenRequest { RefreshToken = oldRaw });

        result.AccessToken.Should().Be("new-access");
        result.RefreshToken.Should().Be("new-raw-token");
        stored.IsRevoked.Should().BeTrue("old token must be revoked");
        stored.ReplacedByTokenHash.Should().Be(added!.TokenHash);
        added.TokenHash.Should().Be(IdentityMocks.Sha256Hex("new-raw-token"));
        _tokens.Verify(m => m.UpdateAsync(stored, It.IsAny<CancellationToken>()), Times.Once);
        _tokens.Verify(m => m.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshAsync_WithRevokedToken_ThrowsInvalidOperation()
    {
        var user = IdentityMocks.NewUser("r@test.com");
        const string raw = "revoked-raw";
        var stored = StoredToken(user.Id, raw);
        stored.Revoke();
        _tokens.Setup(m => m.GetByTokenHashAsync(IdentityMocks.Sha256Hex(raw), It.IsAny<CancellationToken>()))
            .ReturnsAsync(stored);

        var act = () => _sut.RefreshAsync(new RefreshTokenRequest { RefreshToken = raw });
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Invalid refresh token.");
    }

    [Fact]
    public async Task RefreshAsync_WithExpiredToken_ThrowsInvalidOperation()
    {
        var user = IdentityMocks.NewUser("r@test.com");
        const string raw = "expired-raw";
        var stored = new RefreshToken(user.Id, IdentityMocks.Sha256Hex(raw), DateTime.UtcNow.AddMinutes(-1));
        _tokens.Setup(m => m.GetByTokenHashAsync(IdentityMocks.Sha256Hex(raw), It.IsAny<CancellationToken>()))
            .ReturnsAsync(stored);

        var act = () => _sut.RefreshAsync(new RefreshTokenRequest { RefreshToken = raw });
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Invalid refresh token.");
    }

    [Fact]
    public async Task RefreshAsync_WithUnknownToken_ThrowsInvalidOperation()
    {
        _tokens.Setup(m => m.GetByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        var act = () => _sut.RefreshAsync(new RefreshTokenRequest { RefreshToken = "random" });
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Invalid refresh token.");
    }

    [Fact]
    public async Task RefreshAsync_WithMissingToken_ThrowsArgumentException()
    {
        var act = () => _sut.RefreshAsync(new RefreshTokenRequest { RefreshToken = " " });
        await act.Should().ThrowAsync<ArgumentException>()
            .Where(e => e.Message.Contains("Refresh token is required"));
    }

    [Fact]
    public async Task RefreshAsync_WithNonexistentUser_ThrowsInvalidOperation()
    {
        var userId = Guid.NewGuid();
        const string raw = "orphan-raw";
        var stored = StoredToken(userId, raw);
        _tokens.Setup(m => m.GetByTokenHashAsync(IdentityMocks.Sha256Hex(raw), It.IsAny<CancellationToken>()))
            .ReturnsAsync(stored);
        _users.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync((ApplicationUser?)null);

        var act = () => _sut.RefreshAsync(new RefreshTokenRequest { RefreshToken = raw });
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Invalid refresh token.");
    }

    [Fact]
    public async Task RefreshAsync_WithLockedOutUser_ThrowsInvalidOperation()
    {
        var user = IdentityMocks.NewUser("locked@test.com");
        const string raw = "locked-raw";
        var stored = StoredToken(user.Id, raw);
        _tokens.Setup(m => m.GetByTokenHashAsync(IdentityMocks.Sha256Hex(raw), It.IsAny<CancellationToken>()))
            .ReturnsAsync(stored);
        _users.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _users.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(true);

        var act = () => _sut.RefreshAsync(new RefreshTokenRequest { RefreshToken = raw });
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Invalid refresh token.");
    }

    [Fact]
    public async Task RefreshAsync_ReusesSha256HashLookup()
    {
        const string raw = "lookup-raw";
        _tokens.Setup(m => m.GetByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        var act = () => _sut.RefreshAsync(new RefreshTokenRequest { RefreshToken = raw });
        await act.Should().ThrowAsync<InvalidOperationException>();

        _tokens.Verify(m => m.GetByTokenHashAsync(IdentityMocks.Sha256Hex(raw), It.IsAny<CancellationToken>()), Times.Once);
    }
}
