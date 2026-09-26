using FluentAssertions;
using Healthcare_API.Models;
using Healthcare_API_V0._2_Infrastructure.Persistence;
using Healthcare_API_V0._2_Infrastructure.Repositories;
using Healthcare_API_V0._2.Tests.Helpers;
using Microsoft.Data.Sqlite;

namespace Healthcare_API_V0._2.Tests.Infrastructure.Repositories;

public sealed class PatientRepositoryTests : IDisposable
{
    private readonly HealthcareDbContext _db;
    private readonly SqliteConnection _conn;
    private readonly PatientRepository _sut;

    public PatientRepositoryTests()
    {
        _db = TestDbContextFactory.CreateDisposable(out _conn);
        _sut = new PatientRepository(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _conn.Dispose();
    }

    [Fact]
    public async Task AddAndGet_RoundTrips()
    {
        var userId = Guid.NewGuid();
        _db.Users.Add(new ApplicationUser { Id = userId, UserName = "p@t.com", Email = "p@t.com" });
        await _db.SaveChangesAsync();
        var patient = new Patient(userId);
        await _sut.AddAsync(patient);

        (await _sut.GetByIdAsync(patient.PatientId))!.PatientId.Should().Be(patient.PatientId);
        (await _sut.GetByApplicationUserIdAsync(patient.ApplicationUserId))!.PatientId.Should().Be(patient.PatientId);
    }

    [Fact]
    public async Task GetById_WhenMissing_ReturnsNull()
    {
        (await _sut.GetByIdAsync(Guid.NewGuid())).Should().BeNull();
    }

    [Fact]
    public async Task Delete_RemovesEntity()
    {
        var userId = Guid.NewGuid();
        _db.Users.Add(new ApplicationUser { Id = userId, UserName = "d@t.com", Email = "d@t.com" });
        await _db.SaveChangesAsync();
        var patient = new Patient(userId);
        await _sut.AddAsync(patient);
        await _sut.DeleteAsync(patient);
        (await _sut.GetByIdAsync(patient.PatientId)).Should().BeNull();
    }
}

public sealed class DoctorRepositoryTests : IDisposable
{
    private readonly HealthcareDbContext _db;
    private readonly SqliteConnection _conn;
    private readonly DoctorRepository _sut;

    public DoctorRepositoryTests()
    {
        _db = TestDbContextFactory.CreateDisposable(out _conn);
        _sut = new DoctorRepository(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _conn.Dispose();
    }

    private async Task<(Guid specId, Guid userId)> SeedSpecAndUserAsync()
    {
        var spec = new Specialization("Spec-" + Guid.NewGuid().ToString("N")[..8]);
        _db.Specializations.Add(spec);
        var userId = Guid.NewGuid();
        _db.Users.Add(new ApplicationUser { Id = userId, UserName = $"u{userId:N}@t.com", Email = $"u{userId:N}@t.com" });
        await _db.SaveChangesAsync();
        return (spec.SpecializationId, userId);
    }

    [Fact]
    public async Task AddAndGet_IncludesSpecialization()
    {
        var (specId, userId) = await SeedSpecAndUserAsync();
        var doctor = new Doctor(userId, "A", "B", specId);
        await _sut.AddAsync(doctor);

        var loaded = await _sut.GetByIdAsync(doctor.DoctorId);
        loaded.Should().NotBeNull();
        loaded!.Specialization.Should().NotBeNull();
        (await _sut.GetByApplicationUserIdAsync(doctor.ApplicationUserId))!.DoctorId.Should().Be(doctor.DoctorId);
        (await _sut.GetBySpecializationAsync(specId)).Should().ContainSingle(d => d.DoctorId == doctor.DoctorId);
    }

    [Fact]
    public async Task Update_PersistsChanges()
    {
        var (specId, userId) = await SeedSpecAndUserAsync();
        var doctor = new Doctor(userId, "A", "B", specId);
        await _sut.AddAsync(doctor);
        _db.ChangeTracker.Clear();

        var loaded = (await _sut.GetByIdAsync(doctor.DoctorId))!;
        loaded.UpdateDoctor("X", "Y", "LIC-1", "bio");
        await _sut.UpdateAsync(loaded);
        _db.ChangeTracker.Clear();

        (await _sut.GetByIdAsync(doctor.DoctorId))!.FirstName.Should().Be("X");
    }

    [Fact]
    public async Task Delete_RemovesEntity()
    {
        var (specId, userId) = await SeedSpecAndUserAsync();
        var doctor = new Doctor(userId, "A", "B", specId);
        await _sut.AddAsync(doctor);
        _db.ChangeTracker.Clear();

        await _sut.DeleteAsync((await _sut.GetByIdAsync(doctor.DoctorId))!);
        (await _sut.GetByIdAsync(doctor.DoctorId)).Should().BeNull();
    }
}

public sealed class ClinicSpecializationRepositoryTests : IDisposable
{
    private readonly HealthcareDbContext _db;
    private readonly SqliteConnection _conn;

    public ClinicSpecializationRepositoryTests()
    {
        _db = TestDbContextFactory.CreateDisposable(out _conn);
    }

    public void Dispose()
    {
        _db.Dispose();
        _conn.Dispose();
    }

    [Fact]
    public async Task Clinic_AddGetByCityUpdateDelete()
    {
        var sut = new ClinicRepository(_db);
        var clinic = new Clinic("Central", "1 Main St", "Springfield");
        await sut.AddAsync(clinic);

        (await sut.GetByIdAsync(clinic.ClinicId))!.Name.Should().Be("Central");
        (await sut.GetByCityAsync("Springfield")).Should().ContainSingle();
        (await sut.GetByCityAsync("Nowhere")).Should().BeEmpty();

        clinic.UpdateClinic("Central+", "2 Main St", "Springfield", null);
        await sut.UpdateAsync(clinic);
        _db.ChangeTracker.Clear();
        (await sut.GetByIdAsync(clinic.ClinicId))!.Name.Should().Be("Central+");

        await sut.DeleteAsync((await sut.GetByIdAsync(clinic.ClinicId))!);
        (await sut.GetByIdAsync(clinic.ClinicId)).Should().BeNull();
    }

    [Fact]
    public async Task Specialization_AddGetByNameUpdateDelete()
    {
        var sut = new SpecializationRepository(_db);
        var spec = new Specialization("Dermatology-" + Guid.NewGuid().ToString("N")[..6]);
        await sut.AddAsync(spec);

        (await sut.GetByIdAsync(spec.SpecializationId))!.Name.Should().Be(spec.Name);
        (await sut.GetByNameAsync(spec.Name))!.SpecializationId.Should().Be(spec.SpecializationId);
        (await sut.GetByNameAsync("missing")).Should().BeNull();

        spec.UpdateSpecialization("Derm-Updated");
        await sut.UpdateAsync(spec);
        _db.ChangeTracker.Clear();
        (await sut.GetByIdAsync(spec.SpecializationId))!.Name.Should().Be("Derm-Updated");

        await sut.DeleteAsync((await sut.GetByIdAsync(spec.SpecializationId))!);
        (await sut.GetByIdAsync(spec.SpecializationId)).Should().BeNull();
    }
}
