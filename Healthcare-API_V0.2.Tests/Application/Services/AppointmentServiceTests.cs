using FluentAssertions;
using Healthcare_API.Models;
using Healthcare_API.Models.Enums;
using Healthcare_API_V0._2_Application.Interfaces.Repository;
using Healthcare_API_V0._2_Application.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Healthcare_API_V0._2.Tests.Application.Services;

public sealed class AppointmentServiceTests
{
    private readonly Mock<IAppointmentRepository> _appts = new();
    private readonly Mock<IPatientRepository> _patients = new();
    private readonly Mock<IDoctorClinicRepository> _dcs = new();

    private AppointmentService CreateSut() => new(
        _appts.Object, _patients.Object, _dcs.Object, NullLogger<AppointmentService>.Instance);

    private static (Guid patientId, Guid dcId, DateTime start) ValidIds() =>
        (Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(1).Date.AddHours(10));

    [Fact]
    public async Task CreateAsync_Valid_CreatesAppointment()
    {
        var (patientId, dcId, start) = ValidIds();
        _patients.Setup(r => r.GetByIdAsync(patientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Patient(patientId));
        _dcs.Setup(r => r.GetByIdAsync(dcId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DoctorClinic(Guid.NewGuid(), Guid.NewGuid(), 100m, 30));
        _appts.Setup(r => r.GetByDoctorClinicAsync(dcId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Appointment>());

        var result = await CreateSut().CreateAsync(patientId, dcId, start, "checkup");

        result.PatientId.Should().Be(patientId);
        result.StartTime.Should().Be(start);
        _appts.Verify(r => r.AddAsync(result, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ConflictingTime_ThrowsConflict()
    {
        var (patientId, dcId, start) = ValidIds();
        _patients.Setup(r => r.GetByIdAsync(patientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Patient(patientId));
        _dcs.Setup(r => r.GetByIdAsync(dcId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DoctorClinic(Guid.NewGuid(), Guid.NewGuid(), 100m, 30));
        _appts.Setup(r => r.GetByDoctorClinicAsync(dcId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Appointment>
            {
                new(Guid.NewGuid(), dcId, start, null)
            });

        var act = () => CreateSut().CreateAsync(patientId, dcId, start, null);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .Where(e => e.Message.Contains("conflicting appointment"));
    }

    [Fact]
    public async Task CreateAsync_CancelledSlot_DoesNotConflict()
    {
        var (patientId, dcId, start) = ValidIds();
        _patients.Setup(r => r.GetByIdAsync(patientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Patient(patientId));
        _dcs.Setup(r => r.GetByIdAsync(dcId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DoctorClinic(Guid.NewGuid(), Guid.NewGuid(), 100m, 30));
        var cancelled = new Appointment(Guid.NewGuid(), dcId, start, null);
        cancelled.Cancel();
        _appts.Setup(r => r.GetByDoctorClinicAsync(dcId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Appointment> { cancelled });

        var result = await CreateSut().CreateAsync(patientId, dcId, start, null);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAsync_MissingPatient_ThrowsKeyNotFound()
    {
        var (patientId, dcId, start) = ValidIds();
        _patients.Setup(r => r.GetByIdAsync(patientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Patient?)null);

        var act = () => CreateSut().CreateAsync(patientId, dcId, start, null);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task CreateAsync_MissingDoctorClinic_ThrowsKeyNotFound()
    {
        var (patientId, dcId, start) = ValidIds();
        _patients.Setup(r => r.GetByIdAsync(patientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Patient(patientId));
        _dcs.Setup(r => r.GetByIdAsync(dcId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DoctorClinic?)null);

        var act = () => CreateSut().CreateAsync(patientId, dcId, start, null);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_SameStartTime_SkipsConflictCheck()
    {
        var id = Guid.NewGuid();
        var start = DateTime.UtcNow.AddDays(2);
        var appt = new Appointment(Guid.NewGuid(), Guid.NewGuid(), start, "old");
        var field = typeof(Appointment).GetProperty("AppointmentId")!;
        // AppointmentId has private setter; use existing instance id via reflection-free approach:
        _appts.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(appt);

        await CreateSut().UpdateAsync(id, start, "new reason");

        _appts.Verify(r => r.GetByDoctorClinicAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _appts.Verify(r => r.UpdateAsync(appt, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ChangeStatusAsync_InvalidStatus_ThrowsArgumentException()
    {
        var act = () => CreateSut().ChangeStatusAsync(Guid.NewGuid(), (AppointmentStatus)999);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ChangeStatusAsync_Missing_ThrowsKeyNotFound()
    {
        var id = Guid.NewGuid();
        _appts.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Appointment?)null);

        var act = () => CreateSut().ChangeStatusAsync(id, AppointmentStatus.Completed);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task CancelAsync_Valid_SetsCancelled()
    {
        var id = Guid.NewGuid();
        var appt = new Appointment(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(1), null);
        _appts.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(appt);

        await CreateSut().CancelAsync(id);

        appt.Status.Should().Be(AppointmentStatus.Cancelled);
        _appts.Verify(r => r.UpdateAsync(appt, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_Missing_ThrowsKeyNotFound()
    {
        var id = Guid.NewGuid();
        _appts.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Appointment?)null);
        var act = () => CreateSut().DeleteAsync(id);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GetForTimeRangeAsync_StartAfterEnd_ThrowsArgumentException()
    {
        var act = () => CreateSut().GetForTimeRangeAsync(DateTime.UtcNow.AddDays(2), DateTime.UtcNow.AddDays(1));
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
