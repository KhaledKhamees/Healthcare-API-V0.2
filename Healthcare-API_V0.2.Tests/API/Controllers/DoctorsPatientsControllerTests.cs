using System.Security.Claims;
using FluentAssertions;
using Healthcare_API.Models;
using Healthcare_API_V0._2_Application.DTOs.Doctors;
using Healthcare_API_V0._2_Application.Interfaces.Services;
using Healthcare_API_V0._2_API.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Healthcare_API_V0._2.Tests.API.Controllers;

public sealed class DoctorsPatientsControllerTests
{
    private static DoctorsController CreateDoctorsSut(Mock<IDoctorService> d, Mock<IDoctorClinicService> dc) =>
        new(d.Object, dc.Object);

    private static PatientsController CreatePatientsSut(Mock<IPatientService> p, Mock<IAppointmentService> a) =>
        new(p.Object, a.Object);

    private static void SetSub(ControllerBase controller, Guid? sub)
    {
        var claims = sub.HasValue
            ? new[] { new Claim("sub", sub.Value.ToString()) }
            : Array.Empty<Claim>();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims)) }
        };
    }

    [Fact]
    public async Task Doctors_GetById_Found_Returns200()
    {
        var doctor = new Doctor(Guid.NewGuid(), "A", "B", Guid.NewGuid());
        var doctors = new Mock<IDoctorService>();
        doctors.Setup(m => m.GetByIdAsync(doctor.DoctorId, It.IsAny<CancellationToken>())).ReturnsAsync(doctor);

        var result = await CreateDoctorsSut(doctors, new Mock<IDoctorClinicService>())
            .GetById(doctor.DoctorId, default);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(StatusCodes.Status200OK);
        ok.Value.Should().BeOfType<DoctorResponse>();
    }

    [Fact]
    public async Task Doctors_GetById_Missing_Returns404()
    {
        var doctors = new Mock<IDoctorService>();
        doctors.Setup(m => m.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Doctor?)null);

        var result = await CreateDoctorsSut(doctors, new Mock<IDoctorClinicService>())
            .GetById(Guid.NewGuid(), default);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Doctors_GetById_EmptyId_Returns400()
    {
        var doctors = new Mock<IDoctorService>();
        doctors.Setup(m => m.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Doctor ID cannot be empty."));

        var result = await CreateDoctorsSut(doctors, new Mock<IDoctorClinicService>())
            .GetById(Guid.Empty, default);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Doctors_GetMe_WithoutSubClaim_Returns400()
    {
        var sut = CreateDoctorsSut(new Mock<IDoctorService>(), new Mock<IDoctorClinicService>());
        SetSub(sut, null);

        var result = await sut.GetMe(default);
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Doctors_GetMe_WithSub_PassesUserIdToService()
    {
        var appUserId = Guid.NewGuid();
        var doctor = new Doctor(appUserId, "A", "B", Guid.NewGuid());
        var doctors = new Mock<IDoctorService>();
        doctors.Setup(m => m.GetByApplicationUserIdAsync(appUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doctor);
        var sut = CreateDoctorsSut(doctors, new Mock<IDoctorClinicService>());
        SetSub(sut, appUserId);

        var result = await sut.GetMe(default);

        result.Should().BeOfType<OkObjectResult>();
        doctors.Verify(m => m.GetByApplicationUserIdAsync(appUserId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Doctors_Update_Missing_Returns404()
    {
        var doctors = new Mock<IDoctorService>();
        doctors.Setup(m => m.UpdateDoctorAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("not found"));

        var result = await CreateDoctorsSut(doctors, new Mock<IDoctorClinicService>())
            .Update(Guid.NewGuid(), new UpdateDoctorRequest { FirstName = "A", LastName = "B" }, default);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Doctors_Delete_Success_Returns204()
    {
        var doctors = new Mock<IDoctorService>();
        var result = await CreateDoctorsSut(doctors, new Mock<IDoctorClinicService>())
            .Delete(Guid.NewGuid(), default);
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Patients_GetById_Found_Returns200()
    {
        var patient = new Patient(Guid.NewGuid());
        var patients = new Mock<IPatientService>();
        patients.Setup(m => m.GetByIdAsync(patient.PatientId, It.IsAny<CancellationToken>())).ReturnsAsync(patient);

        var result = await CreatePatientsSut(patients, new Mock<IAppointmentService>())
            .GetById(patient.PatientId, default);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Patients_GetMe_WithoutSubClaim_Returns400()
    {
        var sut = CreatePatientsSut(new Mock<IPatientService>(), new Mock<IAppointmentService>());
        SetSub(sut, null);
        (await sut.GetMe(default)).Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Patients_GetAppointments_PassesIdToService()
    {
        var id = Guid.NewGuid();
        var appointments = new Mock<IAppointmentService>();
        appointments.Setup(m => m.GetByPatientAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Appointment>());

        var result = await CreatePatientsSut(new Mock<IPatientService>(), appointments)
            .GetAppointments(id, default);

        result.Should().BeOfType<OkObjectResult>();
        appointments.Verify(m => m.GetByPatientAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Patients_Delete_Missing_Returns404()
    {
        var patients = new Mock<IPatientService>();
        patients.Setup(m => m.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("not found"));

        var result = await CreatePatientsSut(patients, new Mock<IAppointmentService>())
            .Delete(Guid.NewGuid(), default);

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
