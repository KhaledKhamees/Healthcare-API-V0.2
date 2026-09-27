using Healthcare_API.Models;
using Healthcare_API.Models.Enums;
using Healthcare_API_V0._2_Application.Interfaces;
using Healthcare_API_V0._2_Application.Interfaces.Repository;
using Healthcare_API_V0._2_Application.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Healthcare_API_V0._2.Tests.Application.Services;

public sealed class StatusTransitionLoggingTests
{
    private static bool HasStateValues(object state, params KeyValuePair<string, object?>[] expected)
    {
        if (state is not IReadOnlyList<KeyValuePair<string, object?>> values)
        {
            return false;
        }

        return expected.All(e =>
            values.Any(pair => pair.Key == e.Key && Equals(pair.Value, e.Value)));
    }

    private static KeyValuePair<string, object?> Prop(string key, object? value) =>
        new(key, value);

    [Fact]
    public async Task DoctorChangeStatusAsync_LogsOldAndNewStatusAsStructuredProperties()
    {
        var doctorId = Guid.NewGuid();
        var doctor = new Doctor(Guid.NewGuid(), "John", "Doe", Guid.NewGuid());
        doctor.ChangeStatus(DoctorStatus.Active);

        var doctorRepo = new Mock<IDoctorRepository>();
        doctorRepo.Setup(r => r.GetByIdAsync(doctorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doctor);
        var loggerMock = new Mock<ILogger<DoctorService>>();

        var sut = new DoctorService(
            doctorRepo.Object,
            Mock.Of<ISpecializationRepository>(),
            loggerMock.Object,
            Mock.Of<ICacheService>());

        // Pending is the domain default; Active was set above to simulate prior state.
        await sut.ChangeStatusAsync(doctorId, DoctorStatus.Suspended);

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => HasStateValues(
                    state,
                    Prop("DoctorId", doctorId),
                    Prop("OldStatus", DoctorStatus.Active),
                    Prop("NewStatus", DoctorStatus.Suspended))),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task AppointmentChangeStatusAsync_LogsOldAndNewStatusAsStructuredProperties()
    {
        var appointmentId = Guid.NewGuid();
        var appointment = new Appointment(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(1), null);
        appointment.ChangeStatus(AppointmentStatus.Completed);

        var appointmentRepo = new Mock<IAppointmentRepository>();
        appointmentRepo.Setup(r => r.GetByIdAsync(appointmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(appointment);
        var loggerMock = new Mock<ILogger<AppointmentService>>();

        var sut = new AppointmentService(
            appointmentRepo.Object,
            Mock.Of<IPatientRepository>(),
            Mock.Of<IDoctorClinicRepository>(),
            loggerMock.Object);

        await sut.ChangeStatusAsync(appointmentId, AppointmentStatus.Cancelled);

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => HasStateValues(
                    state,
                    Prop("AppointmentId", appointmentId),
                    Prop("OldStatus", AppointmentStatus.Completed),
                    Prop("NewStatus", AppointmentStatus.Cancelled))),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
