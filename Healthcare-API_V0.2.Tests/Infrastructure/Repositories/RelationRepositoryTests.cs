using FluentAssertions;
using Healthcare_API.Models;
using Healthcare_API_V0._2_Infrastructure.Persistence;
using Healthcare_API_V0._2_Infrastructure.Repositories;
using Healthcare_API_V0._2.Tests.Helpers;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;

namespace Healthcare_API_V0._2.Tests.Infrastructure.Repositories;

public sealed class AppointmentRepositoryTests : IDisposable
{
    private readonly HealthcareDbContext _db;
    private readonly SqliteConnection _conn;
    private readonly AppointmentRepository _sut;

    public AppointmentRepositoryTests()
    {
        _db = TestDbContextFactory.CreateDisposable(out _conn);
        _sut = new AppointmentRepository(_db, NullLogger<AppointmentRepository>.Instance);
    }

    public void Dispose()
    {
        _db.Dispose();
        _conn.Dispose();
    }

    private async Task<(Guid patientId, Guid dcId)> SeedGraphAsync()
    {
        var spec = new Specialization("S-" + Guid.NewGuid().ToString("N")[..6]);
        _db.Specializations.Add(spec);
        var clinic = new Clinic("C", "A", "City");
        _db.Clinics.Add(clinic);
        var doctorUserId = Guid.NewGuid();
        var patientUserId = Guid.NewGuid();
        _db.Users.Add(new ApplicationUser { Id = doctorUserId, UserName = $"d{doctorUserId:N}@t.com", Email = $"d{doctorUserId:N}@t.com" });
        _db.Users.Add(new ApplicationUser { Id = patientUserId, UserName = $"p{patientUserId:N}@t.com", Email = $"p{patientUserId:N}@t.com" });
        await _db.SaveChangesAsync();
        var doctor = new Doctor(doctorUserId, "A", "B", spec.SpecializationId);
        _db.Doctors.Add(doctor);
        await _db.SaveChangesAsync();
        var dc = new DoctorClinic(doctor.DoctorId, clinic.ClinicId, 50m, 30);
        _db.DoctorClinics.Add(dc);
        var patient = new Patient(patientUserId);
        _db.Patients.Add(patient);
        await _db.SaveChangesAsync();
        return (patient.PatientId, dc.DoctorClinicId);
    }

    [Fact]
    public async Task AddAndGet_IncludesRelations()
    {
        var (patientId, dcId) = await SeedGraphAsync();
        var appt = new Appointment(patientId, dcId, DateTime.UtcNow.AddDays(1), "flu");
        await _sut.AddAsync(appt);
        _db.ChangeTracker.Clear();

        var loaded = await _sut.GetByIdAsync(appt.AppointmentId);
        loaded.Should().NotBeNull();
        loaded!.Patient.Should().NotBeNull();
        loaded.DoctorClinic.Should().NotBeNull();
        (await _sut.GetByPatientAsync(patientId)).Should().ContainSingle();
        (await _sut.GetByDoctorClinicAsync(dcId)).Should().ContainSingle();
    }

    [Fact]
    public async Task GetForTimeRange_FiltersByRange()
    {
        var (patientId, dcId) = await SeedGraphAsync();
        var inRange = new Appointment(patientId, dcId, new DateTime(2026, 5, 10, 10, 0, 0, DateTimeKind.Utc), null);
        var outOfRange = new Appointment(patientId, dcId, new DateTime(2026, 8, 10, 10, 0, 0, DateTimeKind.Utc), null);
        await _sut.AddAsync(inRange);
        await _sut.AddAsync(outOfRange);

        var result = await _sut.GetAppointmentsForTimeRangeAsync(
            new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 5, 31, 0, 0, 0, DateTimeKind.Utc));

        result.Should().ContainSingle(a => a.AppointmentId == inRange.AppointmentId);
    }

    [Fact]
    public async Task UpdateAndDelete_Persist()
    {
        var (patientId, dcId) = await SeedGraphAsync();
        var appt = new Appointment(patientId, dcId, DateTime.UtcNow.AddDays(1), "x");
        await _sut.AddAsync(appt);
        _db.ChangeTracker.Clear();

        var loaded = (await _sut.GetByIdAsync(appt.AppointmentId))!;
        loaded.UpdateAppointment(DateTime.UtcNow.AddDays(2), "updated");
        await _sut.UpdateAsync(loaded);
        _db.ChangeTracker.Clear();
        (await _sut.GetByIdAsync(appt.AppointmentId))!.ReasonForVisit.Should().Be("updated");

        await _sut.DeleteAsync((await _sut.GetByIdAsync(appt.AppointmentId))!);
        (await _sut.GetByIdAsync(appt.AppointmentId)).Should().BeNull();
    }
}

public sealed class DoctorClinicAvailabilityRefreshTokenRepositoryTests : IDisposable
{
    private readonly HealthcareDbContext _db;
    private readonly SqliteConnection _conn;

    public DoctorClinicAvailabilityRefreshTokenRepositoryTests()
    {
        _db = TestDbContextFactory.CreateDisposable(out _conn);
    }

    public void Dispose()
    {
        _db.Dispose();
        _conn.Dispose();
    }

    private async Task<(Guid doctorId, Guid clinicId)> SeedDoctorClinicAsync()
    {
        var spec = new Specialization("S-" + Guid.NewGuid().ToString("N")[..6]);
        _db.Specializations.Add(spec);
        var clinic = new Clinic("C", "A", "City");
        _db.Clinics.Add(clinic);
        var doctorUserId = Guid.NewGuid();
        _db.Users.Add(new ApplicationUser { Id = doctorUserId, UserName = $"d{doctorUserId:N}@t.com", Email = $"d{doctorUserId:N}@t.com" });
        await _db.SaveChangesAsync();
        var doctor = new Doctor(doctorUserId, "A", "B", spec.SpecializationId);
        _db.Doctors.Add(doctor);
        await _db.SaveChangesAsync();
        return (doctor.DoctorId, clinic.ClinicId);
    }

    [Fact]
    public async Task DoctorClinic_AddGetByPairUpdateDelete()
    {
        var sut = new DoctorClinicRepository(_db);
        var (doctorId, clinicId) = await SeedDoctorClinicAsync();
        var dc = new DoctorClinic(doctorId, clinicId, 75m, 30);
        await sut.AddAsync(dc);

        (await sut.GetByIdAsync(dc.DoctorClinicId))!.ConsultationFee.Should().Be(75m);
        (await sut.GetByDoctorAndClinicAsync(doctorId, clinicId))!.DoctorClinicId.Should().Be(dc.DoctorClinicId);

        dc.UpdateConsultationFee(99m);
        await sut.UpdateAsync(dc);
        _db.ChangeTracker.Clear();
        (await sut.GetByIdAsync(dc.DoctorClinicId))!.ConsultationFee.Should().Be(99m);

        await sut.DeleteAsync((await sut.GetByIdAsync(dc.DoctorClinicId))!);
        (await sut.GetByIdAsync(dc.DoctorClinicId)).Should().BeNull();
    }

    [Fact]
    public async Task Availability_AddGetUpdateDelete()
    {
        var (doctorId, clinicId) = await SeedDoctorClinicAsync();
        var dcRepo = new DoctorClinicRepository(_db);
        var dc = new DoctorClinic(doctorId, clinicId, 10m, 30);
        await dcRepo.AddAsync(dc);

        var sut = new DoctorAvailabilityRepository(_db);
        var av = new DoctorAvailability(dc.DoctorClinicId, DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(11, 0));
        await sut.AddAsync(av);

        (await sut.GetByDoctorClinicAsync(dc.DoctorClinicId)).Should().ContainSingle();
        av.UpdateTimeSlot(new TimeOnly(10, 0), new TimeOnly(12, 0));
        await sut.UpdateAsync(av);
        _db.ChangeTracker.Clear();
        (await sut.GetByDoctorClinicAsync(dc.DoctorClinicId)).Single().StartTime.Should().Be(new TimeOnly(10, 0));

        await sut.DeleteAsync((await sut.GetByDoctorClinicAsync(dc.DoctorClinicId)).Single());
        (await sut.GetByDoctorClinicAsync(dc.DoctorClinicId)).Should().BeEmpty();
    }

    [Fact]
    public async Task RefreshToken_AddGetUpdate_ByHash()
    {
        var sut = new RefreshTokenRepository(_db);
        var userId = Guid.NewGuid();
        _db.Users.Add(new ApplicationUser { Id = userId, UserName = $"r{userId:N}@t.com", Email = $"r{userId:N}@t.com" });
        await _db.SaveChangesAsync();
        var token = new RefreshToken(userId, "HASH-ABC", DateTime.UtcNow.AddDays(7));
        await sut.AddAsync(token);
        _db.ChangeTracker.Clear();

        (await sut.GetByTokenHashAsync("HASH-ABC"))!.ApplicationUserId.Should().Be(userId);
        (await sut.GetByTokenHashAsync("NOPE")).Should().BeNull();

        token.MarkReplaced("HASH-DEF");
        await sut.UpdateAsync(token);
        _db.ChangeTracker.Clear();
        (await sut.GetByTokenHashAsync("HASH-ABC"))!.IsRevoked.Should().BeTrue();
    }
}
