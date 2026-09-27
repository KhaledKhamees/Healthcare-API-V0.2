using Healthcare_API.Models;
using Healthcare_API.Models.Enums;
using Healthcare_API_V0._2_Application.Interfaces.Repository;
using Healthcare_API_V0._2_Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace Healthcare_API_V0._2_Application.Services;

public class AppointmentService : IAppointmentService
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly IDoctorClinicRepository _doctorClinicRepository;
    private readonly ILogger<AppointmentService> _logger;

    public AppointmentService(
        IAppointmentRepository appointmentRepository,
        IPatientRepository patientRepository,
        IDoctorClinicRepository doctorClinicRepository,
        ILogger<AppointmentService> logger)
    {
        _appointmentRepository = appointmentRepository;
        _patientRepository = patientRepository;
        _doctorClinicRepository = doctorClinicRepository;
        _logger = logger;
    }

    public async Task<Appointment?> GetByIdAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        if (appointmentId == Guid.Empty)
        {
            _logger.LogWarning("Get appointment failed: {AppointmentId} is empty", nameof(appointmentId));
            throw new ArgumentException("Appointment ID cannot be empty.", nameof(appointmentId));
        }

        _logger.LogInformation("Getting appointment with ID {AppointmentId}", appointmentId);
        var appointment = await _appointmentRepository.GetByIdAsync(appointmentId, cancellationToken);

        if (appointment is null)
        {
            _logger.LogWarning("Appointment with ID {AppointmentId} was not found", appointmentId);
        }

        return appointment;
    }

    public async Task<List<Appointment>> GetByPatientAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        if (patientId == Guid.Empty)
        {
            _logger.LogWarning("Get appointments failed: {PatientId} is empty", nameof(patientId));
            throw new ArgumentException("Patient ID cannot be empty.", nameof(patientId));
        }

        _logger.LogInformation("Getting appointments for patient {PatientId}", patientId);
        return await _appointmentRepository.GetByPatientAsync(patientId, cancellationToken);
    }

    public async Task<List<Appointment>> GetByDoctorClinicAsync(Guid doctorClinicId, CancellationToken cancellationToken = default)
    {
        if (doctorClinicId == Guid.Empty)
        {
            _logger.LogWarning("Get appointments failed: {DoctorClinicId} is empty", nameof(doctorClinicId));
            throw new ArgumentException("Doctor-clinic ID cannot be empty.", nameof(doctorClinicId));
        }

        _logger.LogInformation("Getting appointments for doctor-clinic {DoctorClinicId}", doctorClinicId);
        return await _appointmentRepository.GetByDoctorClinicAsync(doctorClinicId, cancellationToken);
    }

    public async Task<List<Appointment>> GetForTimeRangeAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default)
    {
        if (start > end)
        {
            _logger.LogWarning("Get appointments failed: start {StartTime} is after end {EndTime}", start, end);
            throw new ArgumentException("Start must be before or equal to end.", nameof(start));
        }

        _logger.LogInformation("Getting appointments between {StartTime} and {EndTime}", start, end);
        return await _appointmentRepository.GetAppointmentsForTimeRangeAsync(start, end, cancellationToken);
    }

    public async Task<Appointment> CreateAsync(Guid patientId, Guid doctorClinicId, DateTime startTime, string? reasonForVisit, CancellationToken cancellationToken = default)
    {
        if (patientId == Guid.Empty)
        {
            _logger.LogWarning("Create appointment failed: {PatientId} is empty", nameof(patientId));
            throw new ArgumentException("Patient ID cannot be empty.", nameof(patientId));
        }

        if (doctorClinicId == Guid.Empty)
        {
            _logger.LogWarning("Create appointment failed: {DoctorClinicId} is empty", nameof(doctorClinicId));
            throw new ArgumentException("Doctor-clinic ID cannot be empty.", nameof(doctorClinicId));
        }

        if (startTime == default)
        {
            _logger.LogWarning("Create appointment failed: start time is missing");
            throw new ArgumentException("Start time is required.", nameof(startTime));
        }

        _logger.LogInformation("Creating appointment for patient {PatientId} with doctor-clinic {DoctorClinicId}", patientId, doctorClinicId);

        var patient = await _patientRepository.GetByIdAsync(patientId, cancellationToken);
        if (patient is null)
        {
            _logger.LogWarning("Create appointment failed: patient with ID {PatientId} was not found", patientId);
            throw new KeyNotFoundException($"Patient with ID {patientId} was not found.");
        }

        var doctorClinic = await _doctorClinicRepository.GetByIdAsync(doctorClinicId, cancellationToken);
        if (doctorClinic is null)
        {
            _logger.LogWarning("Create appointment failed: doctor-clinic with ID {DoctorClinicId} was not found", doctorClinicId);
            throw new KeyNotFoundException($"Doctor-clinic with ID {doctorClinicId} was not found.");
        }

        await EnsureNoConflictingAppointmentAsync(doctorClinicId, startTime, null, cancellationToken);

        var appointment = new Appointment(patientId, doctorClinicId, startTime, reasonForVisit);
        await _appointmentRepository.AddAsync(appointment, cancellationToken);

        _logger.LogInformation("Appointment {AppointmentId} was successfully created", appointment.AppointmentId);
        return appointment;
    }

    public async Task UpdateAsync(Guid appointmentId, DateTime startTime, string? reasonForVisit, CancellationToken cancellationToken = default)
    {
        if (appointmentId == Guid.Empty)
        {
            _logger.LogWarning("Update appointment failed: {AppointmentId} is empty", nameof(appointmentId));
            throw new ArgumentException("Appointment ID cannot be empty.", nameof(appointmentId));
        }

        if (startTime == default)
        {
            _logger.LogWarning("Update appointment {AppointmentId} failed: start time is missing", appointmentId);
            throw new ArgumentException("Start time is required.", nameof(startTime));
        }

        _logger.LogInformation("Updating appointment with ID {AppointmentId}", appointmentId);

        var appointment = await _appointmentRepository.GetByIdAsync(appointmentId, cancellationToken);
        if (appointment is null)
        {
            _logger.LogWarning("Update appointment failed: appointment with ID {AppointmentId} was not found", appointmentId);
            throw new KeyNotFoundException($"Appointment with ID {appointmentId} was not found.");
        }

        if (appointment.StartTime != startTime)
        {
            await EnsureNoConflictingAppointmentAsync(appointment.DoctorClinicId, startTime, appointmentId, cancellationToken);
        }

        appointment.UpdateAppointment(startTime, reasonForVisit);
        await _appointmentRepository.UpdateAsync(appointment, cancellationToken);

        _logger.LogInformation("Appointment {AppointmentId} was successfully updated", appointmentId);
    }

    public async Task ChangeStatusAsync(Guid appointmentId, AppointmentStatus newStatus, CancellationToken cancellationToken = default)
    {
        if (appointmentId == Guid.Empty)
        {
            _logger.LogWarning("Change appointment status failed: {AppointmentId} is empty", nameof(appointmentId));
            throw new ArgumentException("Appointment ID cannot be empty.", nameof(appointmentId));
        }

        if (!Enum.IsDefined(newStatus))
        {
            _logger.LogWarning("Change appointment {AppointmentId} status failed: invalid status {AppointmentStatus}", appointmentId, (int)newStatus);
            throw new ArgumentException("Invalid appointment status.", nameof(newStatus));
        }

        _logger.LogInformation("Changing status of appointment {AppointmentId} to {AppointmentStatus}", appointmentId, newStatus);

        var appointment = await _appointmentRepository.GetByIdAsync(appointmentId, cancellationToken);
        if (appointment is null)
        {
            _logger.LogWarning("Change appointment status failed: appointment with ID {AppointmentId} was not found", appointmentId);
            throw new KeyNotFoundException($"Appointment with ID {appointmentId} was not found.");
        }

        var oldStatus = appointment.Status;
        appointment.ChangeStatus(newStatus);
        await _appointmentRepository.UpdateAsync(appointment, cancellationToken);

        _logger.LogInformation("Appointment {AppointmentId} status changed from {OldStatus} to {NewStatus}", appointmentId, oldStatus, newStatus);
    }

    public async Task CancelAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        if (appointmentId == Guid.Empty)
        {
            _logger.LogWarning("Cancel appointment failed: {AppointmentId} is empty", nameof(appointmentId));
            throw new ArgumentException("Appointment ID cannot be empty.", nameof(appointmentId));
        }

        _logger.LogInformation("Cancelling appointment with ID {AppointmentId}", appointmentId);

        var appointment = await _appointmentRepository.GetByIdAsync(appointmentId, cancellationToken);
        if (appointment is null)
        {
            _logger.LogWarning("Cancel appointment failed: appointment with ID {AppointmentId} was not found", appointmentId);
            throw new KeyNotFoundException($"Appointment with ID {appointmentId} was not found.");
        }

        appointment.Cancel();
        await _appointmentRepository.UpdateAsync(appointment, cancellationToken);

        _logger.LogInformation("Appointment {AppointmentId} was successfully cancelled", appointmentId);
    }

    public async Task DeleteAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        if (appointmentId == Guid.Empty)
        {
            _logger.LogWarning("Delete appointment failed: {AppointmentId} is empty", nameof(appointmentId));
            throw new ArgumentException("Appointment ID cannot be empty.", nameof(appointmentId));
        }

        _logger.LogInformation("Deleting appointment with ID {AppointmentId}", appointmentId);

        var appointment = await _appointmentRepository.GetByIdAsync(appointmentId, cancellationToken);
        if (appointment is null)
        {
            _logger.LogWarning("Delete appointment failed: appointment with ID {AppointmentId} was not found", appointmentId);
            throw new KeyNotFoundException($"Appointment with ID {appointmentId} was not found.");
        }

        await _appointmentRepository.DeleteAsync(appointment, cancellationToken);

        _logger.LogInformation("Appointment {AppointmentId} was successfully deleted", appointmentId);
    }

    private async Task EnsureNoConflictingAppointmentAsync(Guid doctorClinicId, DateTime startTime, Guid? excludeAppointmentId, CancellationToken cancellationToken)
    {
        var existing = await _appointmentRepository.GetByDoctorClinicAsync(doctorClinicId, cancellationToken);
        var conflict = existing.Any(a =>
            a.AppointmentId != excludeAppointmentId &&
            a.StartTime == startTime &&
            a.Status != AppointmentStatus.Cancelled);

        if (conflict)
        {
            _logger.LogWarning("Appointment operation failed: conflicting appointment exists for doctor-clinic {DoctorClinicId} at {StartTime}", doctorClinicId, startTime);
            throw new InvalidOperationException("A conflicting appointment already exists for this doctor-clinic at the requested time.");
        }
    }
}
