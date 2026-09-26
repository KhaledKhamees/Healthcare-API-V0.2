using FluentAssertions;
using Healthcare_API.Models;
using Healthcare_API_V0._2_Application.Interfaces;
using Healthcare_API_V0._2_Application.Interfaces.Repository;
using Healthcare_API_V0._2_Application.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Healthcare_API_V0._2.Tests.Application.Services;

public sealed class PatientServiceTests
{
    private readonly Mock<IPatientRepository> _repo = new();
    private readonly Mock<ICacheService> _cache = new();
    private PatientService CreateSut() =>
        new(_repo.Object, NullLogger<PatientService>.Instance, _cache.Object);

    [Fact]
    public async Task GetByIdAsync_CacheHit_ReturnsCached()
    {
        var patient = new Patient(Guid.NewGuid());
        _cache.Setup(c => c.GetAsync<Patient>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(patient);

        (await CreateSut().GetByIdAsync(patient.PatientId)).Should().Be(patient);
        _repo.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_CacheMiss_LoadsAndPopulates()
    {
        var patient = new Patient(Guid.NewGuid());
        _cache.Setup(c => c.GetAsync<Patient>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Patient?)null);
        _repo.Setup(r => r.GetByIdAsync(patient.PatientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(patient);

        (await CreateSut().GetByIdAsync(patient.PatientId)).Should().Be(patient);
        _cache.Verify(c => c.SetAsync(It.IsAny<string>(), patient, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DuplicateForUser_ThrowsConflict()
    {
        var appUserId = Guid.NewGuid();
        _repo.Setup(r => r.GetByApplicationUserIdAsync(appUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Patient(appUserId));

        var act = () => CreateSut().CreateAsync(appUserId);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CreateAsync_Valid_CreatesAndCaches()
    {
        var appUserId = Guid.NewGuid();
        _repo.Setup(r => r.GetByApplicationUserIdAsync(appUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Patient?)null);

        var result = await CreateSut().CreateAsync(appUserId);

        result.ApplicationUserId.Should().Be(appUserId);
        _repo.Verify(r => r.AddAsync(result, It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.SetAsync(It.IsAny<string>(), result, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_Missing_ThrowsKeyNotFound()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Patient?)null);
        var act = () => CreateSut().DeleteAsync(id);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_Valid_DeletesAndInvalidatesCache()
    {
        var patient = new Patient(Guid.NewGuid());
        _repo.Setup(r => r.GetByIdAsync(patient.PatientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(patient);

        await CreateSut().DeleteAsync(patient.PatientId);

        _repo.Verify(r => r.DeleteAsync(patient, It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_EmptyId_ThrowsArgumentException()
    {
        var act = () => CreateSut().GetByIdAsync(Guid.Empty);
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
