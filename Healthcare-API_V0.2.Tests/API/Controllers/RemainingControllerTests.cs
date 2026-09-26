using FluentAssertions;
using Healthcare_API.Models;
using Healthcare_API_V0._2_Application.DTOs.Appointments;
using Healthcare_API_V0._2_Application.DTOs.Availability;
using Healthcare_API_V0._2_Application.DTOs.Clinics;
using Healthcare_API_V0._2_Application.DTOs.DoctorClinics;
using Healthcare_API_V0._2_Application.DTOs.Doctors;
using Healthcare_API_V0._2_Application.DTOs.Specializations;
using Healthcare_API_V0._2_Application.Interfaces.Services;
using Healthcare_API_V0._2_API.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Healthcare_API_V0._2.Tests.API.Controllers;

public sealed class RemainingControllerTests
{
    [Fact]
    public async Task Appointments_GetForTimeRange_Returns200()
    {
        var svc = new Mock<IAppointmentService>();
        svc.Setup(m => m.GetForTimeRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Appointment>());
        var result = await new AppointmentsController(svc.Object)
            .GetForTimeRange(DateTime.UtcNow, DateTime.UtcNow.AddDays(1), default);
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Appointments_Update_Conflict_Returns409()
    {
        var svc = new Mock<IAppointmentService>();
        svc.Setup(m => m.UpdateAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("conflict"));
        var result = await new AppointmentsController(svc.Object).Update(
            Guid.NewGuid(), new UpdateAppointmentRequest { StartTime = DateTime.UtcNow }, default);
        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task Appointments_ChangeStatus_Success_Returns204()
    {
        var svc = new Mock<IAppointmentService>();
        var result = await new AppointmentsController(svc.Object).ChangeStatus(
            Guid.NewGuid(), new ChangeAppointmentStatusRequest { Status = Healthcare_API.Models.Enums.AppointmentStatus.Completed }, default);
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Appointments_Delete_Success_Returns204()
    {
        var svc = new Mock<IAppointmentService>();
        var result = await new AppointmentsController(svc.Object).Delete(Guid.NewGuid(), default);
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Clinics_GetByCity_PassesCityToService()
    {
        var svc = new Mock<IClinicService>();
        svc.Setup(m => m.GetByCityAsync("Springfield", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Clinic> { new("N", "A", "Springfield") });
        var result = await new ClinicsController(svc.Object).GetByCity("Springfield", default);
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<List<ClinicResponse>>();
        svc.Verify(m => m.GetByCityAsync("Springfield", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Clinics_Update_Missing_Returns404()
    {
        var svc = new Mock<IClinicService>();
        svc.Setup(m => m.UpdateClinicAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("x"));
        var result = await new ClinicsController(svc.Object).Update(
            Guid.NewGuid(), new ClinicRequest { Name = "N", Address = "A", City = "C" }, default);
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Clinics_Delete_Success_Returns204()
    {
        var result = await new ClinicsController(new Mock<IClinicService>().Object).Delete(Guid.NewGuid(), default);
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Specializations_GetById_Found_Returns200()
    {
        var svc = new Mock<ISpecializationService>();
        svc.Setup(m => m.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Specialization("Cardio"));
        var result = await new SpecializationsController(svc.Object).GetById(Guid.NewGuid(), default);
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Specializations_GetByName_Missing_Returns404()
    {
        var svc = new Mock<ISpecializationService>();
        svc.Setup(m => m.GetByNameAsync("X", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Specialization?)null);
        var result = await new SpecializationsController(svc.Object).GetByName("X", default);
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Specializations_Update_Duplicate_Returns409()
    {
        var svc = new Mock<ISpecializationService>();
        svc.Setup(m => m.UpdateAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("exists"));
        var result = await new SpecializationsController(svc.Object)
            .Update(Guid.NewGuid(), new SpecializationRequest { Name = "X" }, default);
        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task Specializations_Delete_Success_Returns204()
    {
        var result = await new SpecializationsController(new Mock<ISpecializationService>().Object)
            .Delete(Guid.NewGuid(), default);
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Doctors_ChangeStatus_Success_Returns204()
    {
        var sut = new DoctorsController(new Mock<IDoctorService>().Object, new Mock<IDoctorClinicService>().Object);
        var result = await sut.ChangeStatus(Guid.NewGuid(),
            new ChangeDoctorStatusRequest { Status = Healthcare_API.Models.Enums.DoctorStatus.Active }, default);
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Doctors_AssignClinic_Conflict_Returns409()
    {
        var dc = new Mock<IDoctorClinicService>();
        dc.Setup(m => m.CreateAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("already associated"));
        var sut = new DoctorsController(new Mock<IDoctorService>().Object, dc.Object);
        var result = await sut.AssignClinic(Guid.NewGuid(),
            new AssignDoctorClinicRequest { ClinicId = Guid.NewGuid(), ConsultationFee = 10m }, default);
        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task Doctors_AssignClinic_Success_Returns201()
    {
        var doctorId = Guid.NewGuid();
        var clinicId = Guid.NewGuid();
        var dc = new Mock<IDoctorClinicService>();
        dc.Setup(m => m.CreateAsync(doctorId, clinicId, 10m, 30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DoctorClinic(doctorId, clinicId, 10m, 30));
        var sut = new DoctorsController(new Mock<IDoctorService>().Object, dc.Object);
        var result = await sut.AssignClinic(doctorId,
            new AssignDoctorClinicRequest { ClinicId = clinicId, ConsultationFee = 10m }, default);
        result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status201Created);
    }

    [Fact]
    public async Task Doctors_GetClinicAssociation_Missing_Returns404()
    {
        var dc = new Mock<IDoctorClinicService>();
        dc.Setup(m => m.GetByDoctorAndClinicAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DoctorClinic?)null);
        var sut = new DoctorsController(new Mock<IDoctorService>().Object, dc.Object);
        var result = await sut.GetClinicAssociation(Guid.NewGuid(), Guid.NewGuid(), default);
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Doctors_GetBySpecialization_Success_Returns200()
    {
        var doctors = new Mock<IDoctorService>();
        doctors.Setup(m => m.GetBySpecializationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Doctor>());
        var sut = new DoctorsController(doctors.Object, new Mock<IDoctorClinicService>().Object);
        (await sut.GetBySpecialization(Guid.NewGuid(), default)).Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task DoctorClinics_GetById_Found_Returns200()
    {
        var svc = new Mock<IDoctorClinicService>();
        var dcId = Guid.NewGuid();
        svc.Setup(m => m.GetByIdAsync(dcId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DoctorClinic(Guid.NewGuid(), Guid.NewGuid(), 10m, 30));
        var result = await new DoctorClinicsController(svc.Object, new Mock<IAppointmentService>().Object)
            .GetById(dcId, default);
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task DoctorClinics_GetAppointments_Returns200()
    {
        var appts = new Mock<IAppointmentService>();
        appts.Setup(m => m.GetByDoctorClinicAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Appointment>());
        var result = await new DoctorClinicsController(new Mock<IDoctorClinicService>().Object, appts.Object)
            .GetAppointments(Guid.NewGuid(), default);
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task DoctorClinics_UpdateDuration_InvalidInput_Returns400()
    {
        var svc = new Mock<IDoctorClinicService>();
        svc.Setup(m => m.UpdateAppointmentDurationAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentOutOfRangeException("durationMinutes", "must be positive"));
        var result = await new DoctorClinicsController(svc.Object, new Mock<IAppointmentService>().Object)
            .UpdateDuration(Guid.NewGuid(), new UpdateAppointmentDurationRequest { AppointmentDurationMinutes = 0 }, default);
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task DoctorClinics_Delete_Success_Returns204()
    {
        var result = await new DoctorClinicsController(
            new Mock<IDoctorClinicService>().Object, new Mock<IAppointmentService>().Object)
            .Delete(Guid.NewGuid(), default);
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Availability_Get_Returns200()
    {
        var svc = new Mock<IDoctorAvailabilityService>();
        svc.Setup(m => m.GetByDoctorClinicAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DoctorAvailability>());
        var result = await new DoctorAvailabilityController(svc.Object)
            .GetByDoctorClinic(Guid.NewGuid(), default);
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Availability_Update_Success_Returns204()
    {
        var dcId = Guid.NewGuid();
        var result = await new DoctorAvailabilityController(new Mock<IDoctorAvailabilityService>().Object)
            .UpdateTimeSlot(dcId, Guid.NewGuid(),
                new UpdateAvailabilityTimeRequest { StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(10, 0) }, default);
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Availability_ActivateDeactivateDelete_Return204()
    {
        var svc = new DoctorAvailabilityController(new Mock<IDoctorAvailabilityService>().Object);
        var dcId = Guid.NewGuid();
        var avId = Guid.NewGuid();
        (await svc.Activate(dcId, avId, default)).Should().BeOfType<NoContentResult>();
        (await svc.Deactivate(dcId, avId, default)).Should().BeOfType<NoContentResult>();
        (await svc.Delete(dcId, avId, default)).Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Availability_Create_MissingClinic_Returns404()
    {
        var svc = new Mock<IDoctorAvailabilityService>();
        svc.Setup(m => m.CreateAsync(It.IsAny<Guid>(), It.IsAny<DayOfWeek>(), It.IsAny<TimeOnly>(), It.IsAny<TimeOnly>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("dc not found"));
        var result = await new DoctorAvailabilityController(svc.Object).Create(
            Guid.NewGuid(), new CreateAvailabilityRequest
            {
                DayOfWeek = DayOfWeek.Monday,
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(10, 0)
            }, default);
        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
