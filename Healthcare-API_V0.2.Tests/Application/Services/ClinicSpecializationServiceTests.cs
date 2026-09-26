using FluentAssertions;
using Healthcare_API.Models;
using Healthcare_API_V0._2_Application.Interfaces;
using Healthcare_API_V0._2_Application.Interfaces.Repository;
using Healthcare_API_V0._2_Application.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Healthcare_API_V0._2.Tests.Application.Services;

public sealed class ClinicServiceTests
{
    private readonly Mock<IClinicRepository> _repo = new();
    private readonly Mock<ICacheService> _cache = new();
    private ClinicService CreateSut() =>
        new(_repo.Object, NullLogger<ClinicService>.Instance, _cache.Object);

    [Fact]
    public async Task GetByIdAsync_CacheHit_ReturnsCached()
    {
        var clinic = new Clinic("N", "A", "C");
        _cache.Setup(c => c.GetAsync<Clinic>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(clinic);
        (await CreateSut().GetByIdAsync(clinic.ClinicId)).Should().Be(clinic);
        _repo.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_MissingName_ThrowsArgumentException()
    {
        var act = () => CreateSut().CreateAsync(" ", "addr", "city");
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateAsync_Valid_CreatesAndCaches()
    {
        var result = await CreateSut().CreateAsync("Name", "Addr", "City", "123");
        _repo.Verify(r => r.AddAsync(result, It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.SetAsync(It.IsAny<string>(), result, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateClinicAsync_Missing_ThrowsKeyNotFound()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Clinic?)null);
        var act = () => CreateSut().UpdateClinicAsync(id, "n", "a", "c", null);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_Valid_RemovesCache()
    {
        var clinic = new Clinic("N", "A", "C");
        _repo.Setup(r => r.GetByIdAsync(clinic.ClinicId, It.IsAny<CancellationToken>())).ReturnsAsync(clinic);
        await CreateSut().DeleteAsync(clinic.ClinicId);
        _repo.Verify(r => r.DeleteAsync(clinic, It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByCityAsync_EmptyCity_ThrowsArgumentException()
    {
        var act = () => CreateSut().GetByCityAsync(" ");
        await act.Should().ThrowAsync<ArgumentException>();
    }
}

public sealed class SpecializationServiceTests
{
    private readonly Mock<ISpecializationRepository> _repo = new();
    private readonly Mock<ICacheService> _cache = new();
    private SpecializationService CreateSut() =>
        new(_repo.Object, NullLogger<SpecializationService>.Instance, _cache.Object);

    [Fact]
    public async Task CreateAsync_DuplicateName_ThrowsConflict()
    {
        _repo.Setup(r => r.GetByNameAsync("Cardio", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Specialization("Cardio"));
        var act = () => CreateSut().CreateAsync("Cardio");
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CreateAsync_Valid_CreatesAndCaches()
    {
        _repo.Setup(r => r.GetByNameAsync("Neuro", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Specialization?)null);
        var result = await CreateSut().CreateAsync("Neuro");
        result.Name.Should().Be("Neuro");
        _cache.Verify(c => c.SetAsync(It.IsAny<string>(), result, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_DuplicateDifferentId_ThrowsConflict()
    {
        var id = Guid.NewGuid();
        var existing = new Specialization("A");
        _repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _repo.Setup(r => r.GetByNameAsync("B", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Specialization("B"));
        var act = () => CreateSut().UpdateAsync(id, "B");
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UpdateAsync_Missing_ThrowsKeyNotFound()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Specialization?)null);
        var act = () => CreateSut().UpdateAsync(id, "X");
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GetByNameAsync_Empty_ThrowsArgumentException()
    {
        var act = () => CreateSut().GetByNameAsync(" ");
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task DeleteAsync_Valid_RemovesCache()
    {
        var spec = new Specialization("X");
        _repo.Setup(r => r.GetByIdAsync(spec.SpecializationId, It.IsAny<CancellationToken>())).ReturnsAsync(spec);
        await CreateSut().DeleteAsync(spec.SpecializationId);
        _cache.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
