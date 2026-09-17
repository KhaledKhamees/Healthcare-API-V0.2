using Healthcare_API.Models;
using Healthcare_API_V0._2_Application.Interfaces.Repository;
using Healthcare_API_V0._2_Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Healthcare_API_V0._2_Infrastructure.Repositories;

public class AppointmentRepository : IAppointmentRepository
{
    private readonly HealthcareDbContext _dbContext;

    public AppointmentRepository(HealthcareDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Appointment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Appointments
            .Include(a => a.Patient)
            .Include(a => a.DoctorClinic)
            .ThenInclude(dc => dc!.Doctor)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.AppointmentId == id, cancellationToken);
    }

    public async Task<List<Appointment>> GetByPatientAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Appointments.AsNoTracking()
            .Where(a => a.PatientId == patientId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Appointment>> GetByDoctorClinicAsync(Guid doctorClinicId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Appointments.AsNoTracking()
            .Where(a => a.DoctorClinicId == doctorClinicId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Appointment>> GetAppointmentsForTimeRangeAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Appointments.AsNoTracking()
            .Where(a => a.StartTime >= start && a.StartTime <= end)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        await _dbContext.Appointments.AddAsync(appointment, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        _dbContext.Appointments.Update(appointment);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        _dbContext.Appointments.Remove(appointment);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

