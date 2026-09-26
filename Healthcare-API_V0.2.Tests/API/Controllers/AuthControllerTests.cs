using FluentAssertions;
using Healthcare_API.Models;
using Healthcare_API_V0._2_Application.DTOs.Auth;
using Healthcare_API_V0._2_Application.Interfaces.Services;
using Healthcare_API_V0._2_API.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Healthcare_API_V0._2.Tests.API.Controllers;

public sealed class AuthControllerTests
{
    private readonly Mock<IAuthService> _auth = new();
    private AuthController CreateSut() => new(_auth.Object);

    [Fact]
    public async Task Login_Success_Returns200WithBody()
    {
        var response = new LoginResponse { UserId = Guid.NewGuid(), Email = "a@t.com" };
        _auth.Setup(a => a.LoginAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await CreateSut().Login(new LoginRequest { Email = "a@t.com", Password = "P" }, default);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(StatusCodes.Status200OK);
        ok.Value.Should().Be(response);
    }

    [Fact]
    public async Task Login_InvalidCredentials_Returns401()
    {
        _auth.Setup(a => a.LoginAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Invalid email or password."));

        var result = await CreateSut().Login(new LoginRequest(), default);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Login_BadInput_Returns400()
    {
        _auth.Setup(a => a.LoginAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Email is required."));
        var result = await CreateSut().Login(new LoginRequest(), default);
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Refresh_InvalidToken_Returns401()
    {
        _auth.Setup(a => a.RefreshAsync(It.IsAny<RefreshTokenRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Invalid refresh token."));
        var result = await CreateSut().Refresh(new RefreshTokenRequest { RefreshToken = "x" }, default);
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task RegisterPatient_Success_Returns201()
    {
        var response = new RegisterResponse { UserId = Guid.NewGuid(), Role = "Patient" };
        _auth.Setup(a => a.RegisterPatientAsync(It.IsAny<PatientRegisterRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await CreateSut().RegisterPatient(new PatientRegisterRequest(), default);

        var created = result.Should().BeOfType<ObjectResult>().Subject;
        created.StatusCode.Should().Be(StatusCodes.Status201Created);
        created.Value.Should().Be(response);
    }

    [Fact]
    public async Task RegisterPatient_Duplicate_Returns409()
    {
        _auth.Setup(a => a.RegisterPatientAsync(It.IsAny<PatientRegisterRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Email x is already registered."));
        var result = await CreateSut().RegisterPatient(new PatientRegisterRequest(), default);
        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task RegisterDoctor_MissingSpecialization_Returns404()
    {
        _auth.Setup(a => a.RegisterDoctorAsync(It.IsAny<DoctorRegisterRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("Specialization with ID x was not found."));
        var result = await CreateSut().RegisterDoctor(new DoctorRegisterRequest(), default);
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task RegisterDoctor_Success_Returns201()
    {
        var response = new RegisterResponse { UserId = Guid.NewGuid(), Role = "Doctor" };
        _auth.Setup(a => a.RegisterDoctorAsync(It.IsAny<DoctorRegisterRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
        var result = await CreateSut().RegisterDoctor(new DoctorRegisterRequest(), default);
        var created = result.Should().BeOfType<ObjectResult>().Subject;
        created.StatusCode.Should().Be(StatusCodes.Status201Created);
    }
}
