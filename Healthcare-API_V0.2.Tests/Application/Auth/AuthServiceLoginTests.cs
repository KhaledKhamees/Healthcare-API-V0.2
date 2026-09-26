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

public sealed class AuthServiceLoginTests : IDisposable
{
    private readonly HealthcareDbContext _db;
    private readonly SqliteConnection _conn;
    private readonly Mock<UserManager<ApplicationUser>> _users;
    private readonly Mock<RoleManager<IdentityRole<Guid>>> _roles;
    private readonly Mock<IPatientService> _patients;
    private readonly Mock<IDoctorService> _doctors;
    private readonly Mock<IJwtService> _jwt;
    private readonly Mock<IRefreshTokenGenerator> _gen;
    private readonly Mock<IRefreshTokenRepository> _tokens;
    private readonly AuthService _sut;

    public AuthServiceLoginTests()
    {
        _db = TestDbContextFactory.CreateDisposable(out _conn);
        _users = IdentityMocks.MockUserManager();
        _roles = IdentityMocks.MockRoleManager();
        _patients = new Mock<IPatientService>();
        _doctors = new Mock<IDoctorService>();
        _jwt = new Mock<IJwtService>();
        _gen = new Mock<IRefreshTokenGenerator>();
        _tokens = new Mock<IRefreshTokenRepository>();
        var opts = Options.Create(new JwtOptions
        {
            Issuer = "t", Audience = "t", SecretKey = new string('k', 40),
            ExpirationMinutes = 60, RefreshTokenExpirationDays = 7
        });
        _sut = new AuthService(_users.Object, _roles.Object, _db, _patients.Object,
            _doctors.Object, _jwt.Object, _gen.Object, _tokens.Object, opts,
            NullLogger<AuthService>.Instance);
    }

    public void Dispose()
    {
        _db.Dispose();
        _conn.Dispose();
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsTokensAndRoles()
    {
        var user = IdentityMocks.NewUser("user@test.com");
        _users.Setup(m => m.FindByEmailAsync("user@test.com")).ReturnsAsync(user);
        _users.Setup(m => m.CheckPasswordAsync(user, "Passw0rd!")).ReturnsAsync(true);
        _users.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Patient" });
        _jwt.Setup(m => m.GenerateAccessToken(user.Id, user.Email!, It.IsAny<IEnumerable<string>>()))
            .Returns("access-token");
        _gen.Setup(m => m.GenerateRefreshToken()).Returns("raw-refresh");
        _tokens.Setup(m => m.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.LoginAsync(new LoginRequest { Email = "  user@test.com ", Password = "Passw0rd!" });

        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("raw-refresh");
        result.TokenType.Should().Be("Bearer");
        result.Roles.Should().ContainSingle().Which.Should().Be("Patient");
        result.UserId.Should().Be(user.Id);
        _tokens.Verify(m => m.AddAsync(It.Is<RefreshToken>(t => t.ApplicationUserId == user.Id), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ThrowsGenericError()
    {
        var user = IdentityMocks.NewUser("user@test.com");
        _users.Setup(m => m.FindByEmailAsync("user@test.com")).ReturnsAsync(user);
        _users.Setup(m => m.CheckPasswordAsync(user, "Wrong!")).ReturnsAsync(false);

        var act = () => _sut.LoginAsync(new LoginRequest { Email = "user@test.com", Password = "Wrong!" });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Invalid email or password.");
    }

    [Fact]
    public async Task LoginAsync_WithUnknownEmail_ThrowsSameGenericError()
    {
        _users.Setup(m => m.FindByEmailAsync("ghost@test.com")).ReturnsAsync((ApplicationUser?)null);

        var act = () => _sut.LoginAsync(new LoginRequest { Email = "ghost@test.com", Password = "Whatever1!" });

        // Must not reveal whether the email exists.
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Invalid email or password.");
    }

    [Fact]
    public async Task LoginAsync_WithMissingPassword_ThrowsArgumentException()
    {
        var act = () => _sut.LoginAsync(new LoginRequest { Email = "a@test.com", Password = " " });
        await act.Should().ThrowAsync<ArgumentException>()
            .Where(e => e.Message.Contains("Password is required"));
    }

    [Fact]
    public async Task LoginAsync_WithMissingEmail_ThrowsArgumentException()
    {
        var act = () => _sut.LoginAsync(new LoginRequest { Email = " ", Password = "x" });
        await act.Should().ThrowAsync<ArgumentException>()
            .Where(e => e.Message.Contains("Email is required"));
    }

    [Fact]
    public async Task LoginAsync_PersistsHashedRefreshToken_NeverRaw()
    {
        var user = IdentityMocks.NewUser("hash@test.com");
        const string raw = "raw-token-value";
        _users.Setup(m => m.FindByEmailAsync("hash@test.com")).ReturnsAsync(user);
        _users.Setup(m => m.CheckPasswordAsync(user, It.IsAny<string>())).ReturnsAsync(true);
        _users.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string>());
        _jwt.Setup(m => m.GenerateAccessToken(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>())).Returns("at");
        _gen.Setup(m => m.GenerateRefreshToken()).Returns(raw);
        RefreshToken? stored = null;
        _tokens.Setup(m => m.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Callback<RefreshToken, CancellationToken>((t, _) => stored = t)
            .Returns(Task.CompletedTask);

        await _sut.LoginAsync(new LoginRequest { Email = "hash@test.com", Password = "Passw0rd!" });

        stored.Should().NotBeNull();
        stored!.TokenHash.Should().Be(IdentityMocks.Sha256Hex(raw));
        stored.TokenHash.Should().NotBe(raw);
    }
}
