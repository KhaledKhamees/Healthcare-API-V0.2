using FluentAssertions;
using Healthcare_API.Models;
using Healthcare_API_V0._2_Application.Interfaces.Repository;
using Healthcare_API_V0._2_Application.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Healthcare_API_V0._2.Tests.Application.Services;

public sealed class DoctorClinicServiceTests
{
    private readonly Mock<IDoctorClinicRepository> _repo = new();
    private readonly Mock<IDoctorRepository> _doctors = new();
    private readonly Mock<IClinicRepository> _clinics = new();
    private DoctorClinicService CreateSut() => new(
        _repo.Object, _doctors.Object, _clinics.Object, NullLogger<DoctorClinicService>.Instance);

    [Fact]
    public async Task CreateAsync_Valid_Creates()
    {
        var doctorId = Guid.NewGuid();
        var clinicId = Guid.NewGuid();
        _doctors.Setup(r => r.GetByIdAsync(doctorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Doctor(Guid.NewGuid(), "A", "B", Guid.NewGuid()));
        _clinics.Setup(r => r.GetByIdAsync(clinicId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Clinic("N", "A", "C"));
        _repo.Setup(r => r.GetByDoctorAndClinicAsync(doctorId, clinicId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DoctorClinic?)null);

        var result = await CreateSut().CreateAsync(doctorId, clinicId, 150m, 30);

        result.ConsultationFee.Should().Be(150m);
        _repo.Verify(r => r.AddAsync(result, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DuplicatePair_ThrowsConflict()
    {
        var doctorId = Guid.NewGuid();
        var clinicId = Guid.NewGuid();
        _doctors.Setup(r => r.GetByIdAsync(doctorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Doctor(Guid.NewGuid(), "A", "B", Guid.NewGuid()));
        _clinics.Setup(r => r.GetByIdAsync(clinicId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Clinic("N", "A", "C"));
        _repo.Setup(r => r.GetByDoctorAndClinicAsync(doctorId, clinicId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DoctorClinic(doctorId, clinicId, 10m, 30));

        var act = () => CreateSut().CreateAsync(doctorId, clinicId, 10m, 30);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CreateAsync_NegativeFee_ThrowsOutOfRange()
    {
        var act = () => CreateSut().CreateAsync(Guid.NewGuid(), Guid.NewGuid(), -1m, 30);
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task CreateAsync_NonPositiveDuration_ThrowsOutOfRange()
    {
        var act = () => CreateSut().CreateAsync(Guid.NewGuid(), Guid.NewGuid(), 10m, 0);
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task CreateAsync_MissingDoctor_ThrowsKeyNotFound()
    {
        var doctorId = Guid.NewGuid();
        var clinicId = Guid.NewGuid();
        _doctors.Setup(r => r.GetByIdAsync(doctorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Doctor?)null);
        var act = () => CreateSut().CreateAsync(doctorId, clinicId, 10m, 30);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateConsultationFeeAsync_NegativeFee_ThrowsOutOfRange()
    {
        var act = () => CreateSut().UpdateConsultationFeeAsync(Guid.NewGuid(), -5m);
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task UpdateAppointmentDurationAsync_Valid_Updates()
    {
        var dc = new DoctorClinic(Guid.NewGuid(), Guid.NewGuid(), 10m, 30);
        _repo.Setup(r => r.GetByIdAsync(dc.DoctorClinicId, It.IsAny<CancellationToken>())).ReturnsAsync(dc);
        await CreateSut().UpdateAppointmentDurationAsync(dc.DoctorClinicId, 45);
        dc.AppointmentDurationMinutes.Should().Be(45);
        _repo.Verify(r => r.UpdateAsync(dc, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_Missing_ThrowsKeyNotFound()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((DoctorClinic?)null);
        var act = () => CreateSut().DeleteAsync(id);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}

public sealed class DoctorAvailabilityServiceTests
{
    private readonly Mock<IDoctorAvailabilityRepository> _repo = new();
    private readonly Mock<IDoctorClinicRepository> _dcs = new();
    private DoctorAvailabilityService CreateSut() => new(
        _repo.Object, _dcs.Object, NullLogger<DoctorAvailabilityService>.Instance);

    [Fact]
    public async Task CreateAsync_InvalidTimeRange_ThrowsArgumentException()
    {
        var act = () => CreateSut().CreateAsync(Guid.NewGuid(), DayOfWeek.Monday,
            new TimeOnly(10, 0), new TimeOnly(9, 0));
        await act.Should().ThrowAsync<ArgumentException>()
            .Where(e => e.Message.Contains("Start time must be before end time"));
    }

    [Fact]
    public async Task CreateAsync_InvalidDay_ThrowsArgumentException()
    {
        var act = () => CreateSut().CreateAsync(Guid.NewGuid(), (DayOfWeek)99,
            new TimeOnly(9, 0), new TimeOnly(10, 0));
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateAsync_MissingDoctorClinic_ThrowsKeyNotFound()
    {
        var dcId = Guid.NewGuid();
        _dcs.Setup(r => r.GetByIdAsync(dcId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DoctorClinic?)null);
        var act = () => CreateSut().CreateAsync(dcId, DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(10, 0));
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task CreateAsync_Valid_Creates()
    {
        var dcId = Guid.NewGuid();
        _dcs.Setup(r => r.GetByIdAsync(dcId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DoctorClinic(Guid.NewGuid(), Guid.NewGuid(), 10m, 30));
        var result = await CreateSut().CreateAsync(dcId, DayOfWeek.Tuesday, new TimeOnly(9, 0), new TimeOnly(12, 0));
        result.DayOfWeek.Should().Be(DayOfWeek.Tuesday);
        _repo.Verify(r => r.AddAsync(result, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ActivateAsync_MissingAvailability_ThrowsKeyNotFound()
    {
        var dcId = Guid.NewGuid();
        _dcs.Setup(r => r.GetByIdAsync(dcId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DoctorClinic(Guid.NewGuid(), Guid.NewGuid(), 10m, 30));
        _repo.Setup(r => r.GetByDoctorClinicAsync(dcId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DoctorAvailability>());
        var act = () => CreateSut().ActivateAsync(dcId, Guid.NewGuid());
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeactivateAsync_Valid_Deactivates()
    {
        var dcId = Guid.NewGuid();
        var av = new DoctorAvailability(dcId, DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(10, 0));
        _dcs.Setup(r => r.GetByIdAsync(dcId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DoctorClinic(Guid.NewGuid(), Guid.NewGuid(), 10m, 30));
        _repo.Setup(r => r.GetByDoctorClinicAsync(dcId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DoctorAvailability> { av });

        await CreateSut().DeactivateAsync(dcId, av.AvailabilityId);

        av.IsActive.Should().BeFalse();
        _repo.Verify(r => r.UpdateAsync(av, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_Valid_Deletes()
    {
        var dcId = Guid.NewGuid();
        var av = new DoctorAvailability(dcId, DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(10, 0));
        _dcs.Setup(r => r.GetByIdAsync(dcId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DoctorClinic(Guid.NewGuid(), Guid.NewGuid(), 10m, 30));
        _repo.Setup(r => r.GetByDoctorClinicAsync(dcId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DoctorAvailability> { av });

        await CreateSut().DeleteAsync(dcId, av.AvailabilityId);
        _repo.Verify(r => r.DeleteAsync(av, It.IsAny<CancellationToken>()), Times.Once);
    }
}
