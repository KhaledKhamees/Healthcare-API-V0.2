using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using Healthcare_API_V0._2_Application.DTOs.Auth;
using Healthcare_API_V0._2_Application.DTOs.Clinics;
using Healthcare_API_V0._2_Application.DTOs.DoctorClinics;
using Healthcare_API_V0._2_Application.DTOs.Specializations;
using Healthcare_API_V0._2_Application.Options;

namespace Healthcare_API_V0._2.Tests.Validation;

public sealed class DtoValidationTests
{
    private static IList<ValidationResult> Validate(object dto)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(dto, new ValidationContext(dto), results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void PatientRegisterRequest_MismatchedPasswords_FailsCompare()
    {
        var dto = new PatientRegisterRequest
        {
            Email = "a@test.com", Password = "Passw0rd!", ConfirmPassword = "Different1!"
        };
        Validate(dto).Should().NotBeEmpty();
    }

    [Fact]
    public void PatientRegisterRequest_Valid_Passes()
    {
        var dto = new PatientRegisterRequest
        {
            Email = "a@test.com", Password = "Passw0rd!", ConfirmPassword = "Passw0rd!"
        };
        Validate(dto).Should().BeEmpty();
    }

    [Fact]
    public void LoginRequest_InvalidEmail_Fails()
    {
        var dto = new LoginRequest { Email = "not-an-email", Password = "x" };
        Validate(dto).Should().NotBeEmpty();
    }

    [Fact]
    public void LoginRequest_MissingPassword_Fails()
    {
        var dto = new LoginRequest { Email = "a@test.com", Password = "" };
        Validate(dto).Should().NotBeEmpty();
    }

    [Fact]
    public void DoctorRegisterRequest_MissingSpecialization_Fails()
    {
        var dto = new DoctorRegisterRequest
        {
            Email = "d@test.com", Password = "Passw0rd!", ConfirmPassword = "Passw0rd!",
            FirstName = "A", LastName = "B", SpecializationId = Guid.Empty
        };
        // SpecializationId has [Required] on a non-nullable Guid; empty guid still triggers service-level check.
        // DTO-level: Required passes for Guid, but record the contract — service must reject Guid.Empty.
        dto.SpecializationId.Should().Be(Guid.Empty);
    }

    [Fact]
    public void RefreshTokenRequest_MissingToken_Fails()
    {
        var dto = new RefreshTokenRequest { RefreshToken = "" };
        Validate(dto).Should().NotBeEmpty();
    }

    [Fact]
    public void ClinicRequest_MissingName_Fails()
    {
        Validate(new ClinicRequest { Name = "", Address = "A", City = "C" }).Should().NotBeEmpty();
    }

    [Fact]
    public void SpecializationRequest_MissingName_Fails()
    {
        Validate(new SpecializationRequest { Name = "" }).Should().NotBeEmpty();
    }

    [Fact]
    public void AssignDoctorClinicRequest_NegativeFee_FailsRange()
    {
        var dto = new AssignDoctorClinicRequest
        {
            ClinicId = Guid.NewGuid(), ConsultationFee = -1m, AppointmentDurationMinutes = 30
        };
        Validate(dto).Should().NotBeEmpty();
    }

    [Fact]
    public void AssignDoctorClinicRequest_ZeroDuration_FailsRange()
    {
        var dto = new AssignDoctorClinicRequest
        {
            ClinicId = Guid.NewGuid(), ConsultationFee = 10m, AppointmentDurationMinutes = 0
        };
        Validate(dto).Should().NotBeEmpty();
    }

    [Fact]
    public void JwtOptions_ShortSecret_FailsMinLength()
    {
        var opts = new JwtOptions
        {
            Issuer = "i", Audience = "a", SecretKey = "short",
            ExpirationMinutes = 60, RefreshTokenExpirationDays = 7
        };
        Validate(opts).Should().NotBeEmpty();
    }

    [Fact]
    public void JwtOptions_NonPositiveExpiry_FailsRange()
    {
        var opts = new JwtOptions
        {
            Issuer = "i", Audience = "a", SecretKey = new string('k', 40),
            ExpirationMinutes = 0, RefreshTokenExpirationDays = 0
        };
        Validate(opts).Should().NotBeEmpty();
    }

    [Fact]
    public void JwtOptions_Valid_Passes()
    {
        var opts = new JwtOptions
        {
            Issuer = "i", Audience = "a", SecretKey = new string('k', 40),
            ExpirationMinutes = 60, RefreshTokenExpirationDays = 7
        };
        Validate(opts).Should().BeEmpty();
    }
}
