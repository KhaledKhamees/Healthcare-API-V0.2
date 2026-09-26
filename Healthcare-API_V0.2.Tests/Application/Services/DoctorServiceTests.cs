using FluentAssertions;
using Healthcare_API.Models;
using Healthcare_API.Models.Enums;
using Healthcare_API_V0._2_Application.Interfaces;
using Healthcare_API_V0._2_Application.Interfaces.Repository;
using Healthcare_API_V0._2_Application.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Healthcare_API_V0._2.Tests.Application.Services;

public sealed class DoctorServiceTests
{
    private readonly Mock<IDoctorRepository> _doctors = new();
    private readonly Mock<ISpecializationRepository> _specs = new();
    private readonly Mock<ICacheService> _cache = new();

    private DoctorService CreateSut() => new(
        _doctors.Object, _specs.Object, NullLogger<DoctorService>.Instance, _cache.Object);

    private static Doctor NewDoctor(Guid? specId = null) =>
        new(Guid.NewGuid(), "John", "Doe", specId ?? Guid.NewGuid());

    [Fact]
    public async Task GetByIdAsync_CacheHit_ReturnsWithoutHittingRepository()
    {
        var doctor = NewDoctor();
        _cache.Setup(c => c.GetAsync<Doctor>($"doctor:{doctor.DoctorId}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(doctor);

        var result = await CreateSut().GetByIdAsync(doctor.DoctorId);

        result.Should().Be(doctor);
        _doctors.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_CacheMiss_PopulatesCache()
    {
        var doctor = NewDoctor();
        _cache.Setup(c => c.GetAsync<Doctor>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Doctor?)null);
        _doctors.Setup(r => r.GetByIdAsync(doctor.DoctorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doctor);

        var result = await CreateSut().GetByIdAsync(doctor.DoctorId);

        result.Should().Be(doctor);
        _cache.Verify(c => c.SetAsync($"doctor:{doctor.DoctorId}", doctor, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenMissing_ReturnsNullWithoutCaching()
    {
        var id = Guid.NewGuid();
        _cache.Setup(c => c.GetAsync<Doctor>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Doctor?)null);
        _doctors.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Doctor?)null);

        (await CreateSut().GetByIdAsync(id)).Should().BeNull();
        _cache.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<Doctor>(), null, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_WithEmptyId_ThrowsArgumentException()
    {
        var act = () => CreateSut().GetByIdAsync(Guid.Empty);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateAsync_WithValidData_CreatesAndCaches()
    {
        var specId = Guid.NewGuid();
        var appUserId = Guid.NewGuid();
        _specs.Setup(r => r.GetByIdAsync(specId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Specialization("Cardiology"));
        _doctors.Setup(r => r.GetByApplicationUserIdAsync(appUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Doctor?)null);

        var result = await CreateSut().CreateAsync(appUserId, "John", "Doe", specId);

        result.Status.Should().Be(DoctorStatus.Pending);
        _doctors.Verify(r => r.AddAsync(result, It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.SetAsync($"doctor:{result.DoctorId}", result, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithMissingSpecialization_ThrowsKeyNotFound()
    {
        var specId = Guid.NewGuid();
        _specs.Setup(r => r.GetByIdAsync(specId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Specialization?)null);

        var act = () => CreateSut().CreateAsync(Guid.NewGuid(), "J", "D", specId);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task CreateAsync_WhenDoctorExistsForUser_ThrowsConflict()
    {
        var specId = Guid.NewGuid();
        var appUserId = Guid.NewGuid();
        _specs.Setup(r => r.GetByIdAsync(specId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Specialization("X"));
        _doctors.Setup(r => r.GetByApplicationUserIdAsync(appUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(NewDoctor());

        var act = () => CreateSut().CreateAsync(appUserId, "J", "D", specId);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UpdateDoctorAsync_WhenMissing_ThrowsKeyNotFound()
    {
        var id = Guid.NewGuid();
        _doctors.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Doctor?)null);

        var act = () => CreateSut().UpdateDoctorAsync(id, "A", "B", null, null);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task ChangeStatusAsync_WithInvalidStatus_ThrowsArgumentException()
    {
        var act = () => CreateSut().ChangeStatusAsync(Guid.NewGuid(), (DoctorStatus)999);
        await act.Should().ThrowAsync<ArgumentException>()
            .Where(e => e.Message.Contains("Invalid doctor status"));
    }

    [Fact]
    public async Task ChangeStatusAsync_ValidChange_InvalidatesCache()
    {
        var doctor = NewDoctor();
        _doctors.Setup(r => r.GetByIdAsync(doctor.DoctorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doctor);

        await CreateSut().ChangeStatusAsync(doctor.DoctorId, DoctorStatus.Active);

        doctor.Status.Should().Be(DoctorStatus.Active);
        _doctors.Verify(r => r.UpdateAsync(doctor, It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.RemoveAsync($"doctor:{doctor.DoctorId}", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenMissing_ThrowsKeyNotFound()
    {
        var id = Guid.NewGuid();
        _doctors.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Doctor?)null);
        var act = () => CreateSut().DeleteAsync(id);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_ValidDelete_InvalidatesCache()
    {
        var doctor = NewDoctor();
        _doctors.Setup(r => r.GetByIdAsync(doctor.DoctorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doctor);

        await CreateSut().DeleteAsync(doctor.DoctorId);

        _doctors.Verify(r => r.DeleteAsync(doctor, It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.RemoveAsync($"doctor:{doctor.DoctorId}", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetBySpecializationAsync_WithMissingSpec_ThrowsKeyNotFound()
    {
        var specId = Guid.NewGuid();
        _specs.Setup(r => r.GetByIdAsync(specId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Specialization?)null);

        var act = () => CreateSut().GetBySpecializationAsync(specId);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
