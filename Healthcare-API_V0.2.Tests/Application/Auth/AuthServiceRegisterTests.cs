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

public sealed class AuthServiceRegisterTests : IDisposable
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

    public AuthServiceRegisterTests()
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

    private void SetupCreateUserSuccess(string email)
    {
        _users.Setup(m => m.FindByEmailAsync(email)).ReturnsAsync((ApplicationUser?)null);
        _roles.Setup(m => m.RoleExistsAsync(It.IsAny<string>())).ReturnsAsync(true);
        _users.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser u, string p) => { u.Id = Guid.NewGuid(); return IdentityResult.Success; });
        _users.Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
    }

    [Fact]
    public async Task RegisterPatientAsync_WithValidRequest_CreatesPatientAndReturnsResponse()
    {
        var email = "patient@test.com";
        SetupCreateUserSuccess(email);
        var patient = new Patient(Guid.NewGuid());
        _patients.Setup(m => m.CreateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(patient);

        var result = await _sut.RegisterPatientAsync(new PatientRegisterRequest
        {
            Email = "  " + email + "  ", Password = "Passw0rd!", ConfirmPassword = "Passw0rd!"
        });

        result.Email.Should().Be(email);
        result.Role.Should().Be("Patient");
        result.ProfileId.Should().Be(patient.PatientId);
        result.UserId.Should().NotBeEmpty();
        _patients.Verify(m => m.CreateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterPatientAsync_WithDuplicateEmail_ThrowsConflict()
    {
        var email = "dup@test.com";
        _users.Setup(m => m.FindByEmailAsync(email)).ReturnsAsync(IdentityMocks.NewUser(email));

        var act = () => _sut.RegisterPatientAsync(new PatientRegisterRequest
        {
            Email = email, Password = "Passw0rd!", ConfirmPassword = "Passw0rd!"
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Email {email} is already registered.");
    }

    [Fact]
    public async Task RegisterPatientAsync_WithMismatchedPasswords_ThrowsArgumentException()
    {
        var act = () => _sut.RegisterPatientAsync(new PatientRegisterRequest
        {
            Email = "a@test.com", Password = "Passw0rd!", ConfirmPassword = "Other123!"
        });

        await act.Should().ThrowAsync<ArgumentException>()
            .Where(e => e.Message.Contains("Password and confirmation do not match"));
    }

    [Fact]
    public async Task RegisterPatientAsync_WithMissingEmail_ThrowsArgumentException()
    {
        var act = () => _sut.RegisterPatientAsync(new PatientRegisterRequest
        {
            Email = "   ", Password = "Passw0rd!", ConfirmPassword = "Passw0rd!"
        });

        await act.Should().ThrowAsync<ArgumentException>()
            .Where(e => e.Message.Contains("Email is required"));
    }

    [Fact]
    public async Task RegisterPatientAsync_WithInvalidIdentityPassword_ThrowsArgumentException()
    {
        var email = "weak@test.com";
        _users.Setup(m => m.FindByEmailAsync(email)).ReturnsAsync((ApplicationUser?)null);
        _roles.Setup(m => m.RoleExistsAsync(It.IsAny<string>())).ReturnsAsync(true);
        _users.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Password too weak." }));

        var act = () => _sut.RegisterPatientAsync(new PatientRegisterRequest
        {
            Email = email, Password = "weak", ConfirmPassword = "weak"
        });

        await act.Should().ThrowAsync<ArgumentException>()
            .Where(e => e.Message.Contains("Password too weak"));
    }

    [Fact]
    public async Task RegisterPatientAsync_WhenProfileCreationFails_RollsBackTransaction()
    {
        var email = "rollback@test.com";
        SetupCreateUserSuccess(email);
        _patients.Setup(m => m.CreateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("profile boom"));

        var act = () => _sut.RegisterPatientAsync(new PatientRegisterRequest
        {
            Email = email, Password = "Passw0rd!", ConfirmPassword = "Passw0rd!"
        });

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("profile boom");
        _patients.Verify(m => m.CreateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterPatientAsync_WhenRoleMissing_ThrowsKeyNotFound()
    {
        var email = "norole@test.com";
        _users.Setup(m => m.FindByEmailAsync(email)).ReturnsAsync((ApplicationUser?)null);
        _roles.Setup(m => m.RoleExistsAsync("Patient")).ReturnsAsync(false);

        var act = () => _sut.RegisterPatientAsync(new PatientRegisterRequest
        {
            Email = email, Password = "Passw0rd!", ConfirmPassword = "Passw0rd!"
        });

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task RegisterDoctorAsync_WithValidRequest_CreatesDoctorWithPendingStatus()
    {
        var email = "doc@test.com";
        var specId = Guid.NewGuid();
        SetupCreateUserSuccess(email);
        Doctor captured = null!;
        _doctors.Setup(m => m.CreateAsync(It.IsAny<Guid>(), "John", "Doe", specId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid uid, string f, string l, Guid s, CancellationToken ct) =>
            {
                captured = new Doctor(uid, f, l, s);
                return captured;
            });

        var result = await _sut.RegisterDoctorAsync(new DoctorRegisterRequest
        {
            Email = email, Password = "Passw0rd!", ConfirmPassword = "Passw0rd!",
            FirstName = " John ", LastName = " Doe ", SpecializationId = specId
        });

        result.Role.Should().Be("Doctor");
        result.Email.Should().Be(email);
        result.ProfileId.Should().Be(captured.DoctorId);
        captured.Status.Should().Be(Healthcare_API.Models.Enums.DoctorStatus.Pending);
        _doctors.Verify(m => m.CreateAsync(It.IsAny<Guid>(), "John", "Doe", specId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterDoctorAsync_WithDuplicateEmail_ThrowsConflict()
    {
        var email = "docdup@test.com";
        _users.Setup(m => m.FindByEmailAsync(email)).ReturnsAsync(IdentityMocks.NewUser(email));

        var act = () => _sut.RegisterDoctorAsync(new DoctorRegisterRequest
        {
            Email = email, Password = "Passw0rd!", ConfirmPassword = "Passw0rd!",
            FirstName = "A", LastName = "B", SpecializationId = Guid.NewGuid()
        });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task RegisterDoctorAsync_WithEmptySpecialization_ThrowsArgumentException()
    {
        var act = () => _sut.RegisterDoctorAsync(new DoctorRegisterRequest
        {
            Email = "d@test.com", Password = "Passw0rd!", ConfirmPassword = "Passw0rd!",
            FirstName = "A", LastName = "B", SpecializationId = Guid.Empty
        });

        await act.Should().ThrowAsync<ArgumentException>()
            .Where(e => e.Message.Contains("Specialization ID is required"));
        _doctors.Verify(m => m.CreateAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterDoctorAsync_WithMissingFirstName_ThrowsArgumentException()
    {
        var act = () => _sut.RegisterDoctorAsync(new DoctorRegisterRequest
        {
            Email = "d@test.com", Password = "Passw0rd!", ConfirmPassword = "Passw0rd!",
            FirstName = " ", LastName = "B", SpecializationId = Guid.NewGuid()
        });

        await act.Should().ThrowAsync<ArgumentException>()
            .Where(e => e.Message.Contains("First name is required"));
    }
}
