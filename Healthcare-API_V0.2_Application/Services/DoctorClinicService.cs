using Healthcare_API.Models;
using Healthcare_API_V0._2_Application.Interfaces.Repository;
using Healthcare_API_V0._2_Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace Healthcare_API_V0._2_Application.Services;

public class DoctorClinicService : IDoctorClinicService
{
    private readonly IDoctorClinicRepository _doctorClinicRepository;
    private readonly IDoctorRepository _doctorRepository;
    private readonly IClinicRepository _clinicRepository;
    private readonly ILogger<DoctorClinicService> _logger;

    public DoctorClinicService(
        IDoctorClinicRepository doctorClinicRepository,
        IDoctorRepository doctorRepository,
        IClinicRepository clinicRepository,
        ILogger<DoctorClinicService> logger)
    {
        _doctorClinicRepository = doctorClinicRepository;
        _doctorRepository = doctorRepository;
        _clinicRepository = clinicRepository;
        _logger = logger;
    }

    public async Task<DoctorClinic?> GetByIdAsync(Guid doctorClinicId, CancellationToken cancellationToken = default)
    {
        if (doctorClinicId == Guid.Empty)
        {
            _logger.LogWarning("Get doctor-clinic failed: {DoctorClinicId} is empty", nameof(doctorClinicId));
            throw new ArgumentException("Doctor-clinic ID cannot be empty.", nameof(doctorClinicId));
        }

        _logger.LogInformation("Getting doctor-clinic with ID {DoctorClinicId}", doctorClinicId);
        var doctorClinic = await _doctorClinicRepository.GetByIdAsync(doctorClinicId, cancellationToken);

        if (doctorClinic is null)
        {
            _logger.LogWarning("Doctor-clinic with ID {DoctorClinicId} was not found", doctorClinicId);
        }

        return doctorClinic;
    }

    public async Task<DoctorClinic?> GetByDoctorAndClinicAsync(Guid doctorId, Guid clinicId, CancellationToken cancellationToken = default)
    {
        if (doctorId == Guid.Empty)
        {
            _logger.LogWarning("Get doctor-clinic failed: {DoctorId} is empty", nameof(doctorId));
            throw new ArgumentException("Doctor ID cannot be empty.", nameof(doctorId));
        }

        if (clinicId == Guid.Empty)
        {
            _logger.LogWarning("Get doctor-clinic failed: {ClinicId} is empty", nameof(clinicId));
            throw new ArgumentException("Clinic ID cannot be empty.", nameof(clinicId));
        }

        _logger.LogInformation("Getting doctor-clinic with doctor ID {DoctorId} and clinic ID {ClinicId}", doctorId, clinicId);
        var doctorClinic = await _doctorClinicRepository.GetByDoctorAndClinicAsync(doctorId, clinicId, cancellationToken);

        if (doctorClinic is null)
        {
            _logger.LogWarning("Doctor-clinic with doctor ID {DoctorId} and clinic ID {ClinicId} was not found", doctorId, clinicId);
        }

        return doctorClinic;
    }

    public async Task<DoctorClinic> CreateAsync(Guid doctorId, Guid clinicId, decimal consultationFee, int appointmentDurationMinutes = 30, CancellationToken cancellationToken = default)
    {
        if (doctorId == Guid.Empty)
        {
            _logger.LogWarning("Create doctor-clinic failed: {DoctorId} is empty", nameof(doctorId));
            throw new ArgumentException("Doctor ID cannot be empty.", nameof(doctorId));
        }

        if (clinicId == Guid.Empty)
        {
            _logger.LogWarning("Create doctor-clinic failed: {ClinicId} is empty", nameof(clinicId));
            throw new ArgumentException("Clinic ID cannot be empty.", nameof(clinicId));
        }

        if (consultationFee < 0)
        {
            _logger.LogWarning("Create doctor-clinic failed: consultation fee {ConsultationFee} is negative", consultationFee);
            throw new ArgumentOutOfRangeException(nameof(consultationFee), "Consultation fee cannot be negative.");
        }

        if (appointmentDurationMinutes <= 0)
        {
            _logger.LogWarning("Create doctor-clinic failed: appointment duration {AppointmentDurationMinutes} is not positive", appointmentDurationMinutes);
            throw new ArgumentOutOfRangeException(nameof(appointmentDurationMinutes), "Appointment duration must be greater than zero.");
        }

        _logger.LogInformation("Creating doctor-clinic with doctor ID {DoctorId} and clinic ID {ClinicId}", doctorId, clinicId);

        var doctor = await _doctorRepository.GetByIdAsync(doctorId, cancellationToken);
        if (doctor is null)
        {
            _logger.LogWarning("Create doctor-clinic failed: doctor with ID {DoctorId} was not found", doctorId);
            throw new KeyNotFoundException($"Doctor with ID {doctorId} was not found.");
        }

        var clinic = await _clinicRepository.GetByIdAsync(clinicId, cancellationToken);
        if (clinic is null)
        {
            _logger.LogWarning("Create doctor-clinic failed: clinic with ID {ClinicId} was not found", clinicId);
            throw new KeyNotFoundException($"Clinic with ID {clinicId} was not found.");
        }

        var existing = await _doctorClinicRepository.GetByDoctorAndClinicAsync(doctorId, clinicId, cancellationToken);
        if (existing is not null)
        {
            _logger.LogWarning("Create doctor-clinic failed: relationship already exists for doctor ID {DoctorId} and clinic ID {ClinicId}", doctorId, clinicId);
            throw new InvalidOperationException("This doctor is already associated with this clinic.");
        }

        var doctorClinic = new DoctorClinic(doctorId, clinicId, consultationFee, appointmentDurationMinutes);
        await _doctorClinicRepository.AddAsync(doctorClinic, cancellationToken);

        _logger.LogInformation("Doctor-clinic {DoctorClinicId} was successfully created", doctorClinic.DoctorClinicId);
        return doctorClinic;
    }

    public async Task UpdateConsultationFeeAsync(Guid doctorClinicId, decimal newFee, CancellationToken cancellationToken = default)
    {
        if (doctorClinicId == Guid.Empty)
        {
            _logger.LogWarning("Update consultation fee failed: {DoctorClinicId} is empty", nameof(doctorClinicId));
            throw new ArgumentException("Doctor-clinic ID cannot be empty.", nameof(doctorClinicId));
        }

        if (newFee < 0)
        {
            _logger.LogWarning("Update consultation fee for {DoctorClinicId} failed: fee {ConsultationFee} is negative", doctorClinicId, newFee);
            throw new ArgumentOutOfRangeException(nameof(newFee), "Consultation fee cannot be negative.");
        }

        _logger.LogInformation("Updating consultation fee for doctor-clinic {DoctorClinicId}", doctorClinicId);

        var doctorClinic = await _doctorClinicRepository.GetByIdAsync(doctorClinicId, cancellationToken);
        if (doctorClinic is null)
        {
            _logger.LogWarning("Update consultation fee failed: doctor-clinic with ID {DoctorClinicId} was not found", doctorClinicId);
            throw new KeyNotFoundException($"Doctor-clinic with ID {doctorClinicId} was not found.");
        }

        doctorClinic.UpdateConsultationFee(newFee);
        await _doctorClinicRepository.UpdateAsync(doctorClinic, cancellationToken);

        _logger.LogInformation("Consultation fee for doctor-clinic {DoctorClinicId} was successfully updated", doctorClinicId);
    }

    public async Task UpdateAppointmentDurationAsync(Guid doctorClinicId, int durationMinutes, CancellationToken cancellationToken = default)
    {
        if (doctorClinicId == Guid.Empty)
        {
            _logger.LogWarning("Update appointment duration failed: {DoctorClinicId} is empty", nameof(doctorClinicId));
            throw new ArgumentException("Doctor-clinic ID cannot be empty.", nameof(doctorClinicId));
        }

        if (durationMinutes <= 0)
        {
            _logger.LogWarning("Update appointment duration for {DoctorClinicId} failed: duration {AppointmentDurationMinutes} is not positive", doctorClinicId, durationMinutes);
            throw new ArgumentOutOfRangeException(nameof(durationMinutes), "Appointment duration must be greater than zero.");
        }

        _logger.LogInformation("Updating appointment duration for doctor-clinic {DoctorClinicId}", doctorClinicId);

        var doctorClinic = await _doctorClinicRepository.GetByIdAsync(doctorClinicId, cancellationToken);
        if (doctorClinic is null)
        {
            _logger.LogWarning("Update appointment duration failed: doctor-clinic with ID {DoctorClinicId} was not found", doctorClinicId);
            throw new KeyNotFoundException($"Doctor-clinic with ID {doctorClinicId} was not found.");
        }

        doctorClinic.UpdateAppointmentDuration(durationMinutes);
        await _doctorClinicRepository.UpdateAsync(doctorClinic, cancellationToken);

        _logger.LogInformation("Appointment duration for doctor-clinic {DoctorClinicId} was successfully updated", doctorClinicId);
    }

    public async Task DeleteAsync(Guid doctorClinicId, CancellationToken cancellationToken = default)
    {
        if (doctorClinicId == Guid.Empty)
        {
            _logger.LogWarning("Delete doctor-clinic failed: {DoctorClinicId} is empty", nameof(doctorClinicId));
            throw new ArgumentException("Doctor-clinic ID cannot be empty.", nameof(doctorClinicId));
        }

        _logger.LogInformation("Deleting doctor-clinic with ID {DoctorClinicId}", doctorClinicId);

        var doctorClinic = await _doctorClinicRepository.GetByIdAsync(doctorClinicId, cancellationToken);
        if (doctorClinic is null)
        {
            _logger.LogWarning("Delete doctor-clinic failed: doctor-clinic with ID {DoctorClinicId} was not found", doctorClinicId);
            throw new KeyNotFoundException($"Doctor-clinic with ID {doctorClinicId} was not found.");
        }

        await _doctorClinicRepository.DeleteAsync(doctorClinic, cancellationToken);

        _logger.LogInformation("Doctor-clinic {DoctorClinicId} was successfully deleted", doctorClinicId);
    }
}
