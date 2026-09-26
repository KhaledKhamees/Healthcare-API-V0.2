using FluentAssertions;
using Healthcare_API.Models;
using Healthcare_API_V0._2_Application.DTOs.Appointments;
using Healthcare_API_V0._2_Application.DTOs.Availability;
using Healthcare_API_V0._2_Application.DTOs.Clinics;
using Healthcare_API_V0._2_Application.DTOs.DoctorClinics;
using Healthcare_API_V0._2_Application.DTOs.Specializations;
using Healthcare_API_V0._2_Application.Interfaces.Services;
using Healthcare_API_V0._2_API.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Healthcare_API_V0._2.Tests.API.Controllers;

public sealed class ResourceControllerTests
{
    [Fact]
    public async Task Appointments_Create_Success_Returns201()
    {
        var svc = new Mock<IAppointmentService>();
        var appt = new Appointment(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(1), null);
        svc.Setup(m => m.CreateAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(appt);

        var result = await new AppointmentsController(svc.Object).Create(
            new CreateAppointmentRequest { PatientId = Guid.NewGuid(), DoctorClinicId = Guid.NewGuid(), StartTime = DateTime.UtcNow }, default);

        var created = result.Should().BeOfType<ObjectResult>().Subject;
        created.StatusCode.Should().Be(StatusCodes.Status201Created);
    }

    [Fact]
    public async Task Appointments_Create_Conflict_Returns409()
    {
        var svc = new Mock<IAppointmentService>();
        svc.Setup(m => m.CreateAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("conflict"));

        var result = await new AppointmentsController(svc.Object).Create(
            new CreateAppointmentRequest(), default);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task Appointments_GetById_Missing_Returns404()
    {
        var svc = new Mock<IAppointmentService>();
        svc.Setup(m => m.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Appointment?)null);

        var result = await new AppointmentsController(svc.Object).GetById(Guid.NewGuid(), default);
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Appointments_Cancel_Missing_Returns404()
    {
        var svc = new Mock<IAppointmentService>();
        svc.Setup(m => m.CancelAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("x"));
        var result = await new AppointmentsController(svc.Object).Cancel(Guid.NewGuid(), default);
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Clinics_Create_Success_Returns201()
    {
        var svc = new Mock<IClinicService>();
        svc.Setup(m => m.CreateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Clinic("N", "A", "C"));

        var result = await new ClinicsController(svc.Object).Create(new ClinicRequest { Name = "N", Address = "A", City = "C" }, default);

        result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status201Created);
    }

    [Fact]
    public async Task Clinics_GetById_Missing_Returns404()
    {
        var svc = new Mock<IClinicService>();
        svc.Setup(m => m.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Clinic?)null);
        var result = await new ClinicsController(svc.Object).GetById(Guid.NewGuid(), default);
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Specializations_Create_Duplicate_Returns409()
    {
        var svc = new Mock<ISpecializationService>();
        svc.Setup(m => m.CreateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("exists"));
        var result = await new SpecializationsController(svc.Object)
            .Create(new SpecializationRequest { Name = "X" }, default);
        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task DoctorClinics_UpdateFee_Missing_Returns404()
    {
        var svc = new Mock<IDoctorClinicService>();
        var appts = new Mock<IAppointmentService>();
        svc.Setup(m => m.UpdateConsultationFeeAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("x"));
        var result = await new DoctorClinicsController(svc.Object, appts.Object)
            .UpdateFee(Guid.NewGuid(), new UpdateConsultationFeeRequest { ConsultationFee = 10m }, default);
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Availability_Create_Success_Returns201()
    {
        var svc = new Mock<IDoctorAvailabilityService>();
        var dcId = Guid.NewGuid();
        svc.Setup(m => m.CreateAsync(dcId, It.IsAny<DayOfWeek>(), It.IsAny<TimeOnly>(), It.IsAny<TimeOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DoctorAvailability(dcId, DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(10, 0)));

        var result = await new DoctorAvailabilityController(svc.Object).Create(
            dcId, new CreateAvailabilityRequest { DayOfWeek = DayOfWeek.Monday, StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(10, 0) }, default);

        result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status201Created);
    }
}
