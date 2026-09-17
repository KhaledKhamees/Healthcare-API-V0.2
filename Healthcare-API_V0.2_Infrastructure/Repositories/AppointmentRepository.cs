using Healthcare_API.Models;
using Healthcare_API_V0._2_Application.Interfaces.Repository;
using Healthcare_API_V0._2_Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Healthcare_API_V0._2_Infrastructure.Repositories;

public class AppointmentRepository : IAppointmentRepository
{
    private readonly HealthcareDbContext _dbContext;
    private readonly ILogger<AppointmentRepository> _logger;

    public AppointmentRepository(HealthcareDbContext dbContext, ILogger<AppointmentRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Appointment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching appointment with ID: {AppointmentId}", id);
        return await _dbContext.Appointments
            .Include(a => a.Patient)
            .Include(a => a.DoctorClinic)
            .ThenInclude(dc => dc!.Doctor)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.AppointmentId == id, cancellationToken);
        _logger.LogInformation("Fetched appointment with ID: {AppointmentId}", id);
    }

    public async Task<List<Appointment>> GetByPatientAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching appointments for patient with ID: {PatientId}", patientId);
        return await _dbContext.Appointments.AsNoTracking()
            .Where(a => a.PatientId == patientId)
            .ToListAsync(cancellationToken);
        _logger.LogInformation("Fetched appointments for patient with ID: {PatientId}", patientId);
    }

    public async Task<List<Appointment>> GetByDoctorClinicAsync(Guid doctorClinicId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching appointments for doctor clinic with ID: {DoctorClinicId}", doctorClinicId);
        return await _dbContext.Appointments.AsNoTracking()
            .Where(a => a.DoctorClinicId == doctorClinicId)
            .ToListAsync(cancellationToken);
        _logger.LogInformation("Fetched appointments for doctor clinic with ID: {DoctorClinicId}", doctorClinicId);
    }

    public async Task<List<Appointment>> GetAppointmentsForTimeRangeAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching appointments between {StartTime} and {EndTime}", start, end);
        return await _dbContext.Appointments.AsNoTracking()
            .Where(a => a.StartTime >= start && a.StartTime <= end)
            .ToListAsync(cancellationToken);
        _logger.LogInformation("Fetched appointments between {StartTime} and {EndTime}", start, end);
    }

    public async Task AddAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Adding new appointment with ID: {AppointmentId}", appointment.AppointmentId);
        await _dbContext.Appointments.AddAsync(appointment, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Added new appointment with ID: {AppointmentId}", appointment.AppointmentId);
    }

    public async Task UpdateAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating appointment with ID: {AppointmentId}", appointment.AppointmentId);
        _dbContext.Appointments.Update(appointment);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Updated appointment with ID: {AppointmentId}", appointment.AppointmentId);
    }

    public async Task DeleteAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting appointment with ID: {AppointmentId}", appointment.AppointmentId);
        _dbContext.Appointments.Remove(appointment);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Deleted appointment with ID: {AppointmentId}", appointment.AppointmentId);
    }
}
