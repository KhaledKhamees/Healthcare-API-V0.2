using Healthcare_API.Models;
using Healthcare_API_V0._2_Application.Interfaces.Repository;
using Healthcare_API_V0._2_Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace Healthcare_API_V0._2_Application.Services;

public class DoctorAvailabilityService : IDoctorAvailabilityService
{
    private readonly IDoctorAvailabilityRepository _availabilityRepository;
    private readonly IDoctorClinicRepository _doctorClinicRepository;
    private readonly ILogger<DoctorAvailabilityService> _logger;

    public DoctorAvailabilityService(
        IDoctorAvailabilityRepository availabilityRepository,
        IDoctorClinicRepository doctorClinicRepository,
        ILogger<DoctorAvailabilityService> logger)
    {
        _availabilityRepository = availabilityRepository;
        _doctorClinicRepository = doctorClinicRepository;
        _logger = logger;
    }

    public async Task<List<DoctorAvailability>> GetByDoctorClinicAsync(Guid doctorClinicId, CancellationToken cancellationToken = default)
    {
        if (doctorClinicId == Guid.Empty)
        {
            _logger.LogWarning("Get availabilities failed: {DoctorClinicId} is empty", nameof(doctorClinicId));
            throw new ArgumentException("Doctor-clinic ID cannot be empty.", nameof(doctorClinicId));
        }

        _logger.LogInformation("Getting availabilities for doctor-clinic {DoctorClinicId}", doctorClinicId);
        return await _availabilityRepository.GetByDoctorClinicAsync(doctorClinicId, cancellationToken);
    }

    public async Task<DoctorAvailability> CreateAsync(Guid doctorClinicId, DayOfWeek dayOfWeek, TimeOnly startTime, TimeOnly endTime, CancellationToken cancellationToken = default)
    {
        if (doctorClinicId == Guid.Empty)
        {
            _logger.LogWarning("Create availability failed: {DoctorClinicId} is empty", nameof(doctorClinicId));
            throw new ArgumentException("Doctor-clinic ID cannot be empty.", nameof(doctorClinicId));
        }

        if (!Enum.IsDefined(dayOfWeek))
        {
            _logger.LogWarning("Create availability failed: invalid day of week {DayOfWeek}", (int)dayOfWeek);
            throw new ArgumentException("Invalid day of week.", nameof(dayOfWeek));
        }

        if (startTime >= endTime)
        {
            _logger.LogWarning("Create availability failed: start time {StartTime} is not before end time {EndTime}", startTime, endTime);
            throw new ArgumentException("Start time must be before end time.", nameof(startTime));
        }

        _logger.LogInformation("Creating availability for doctor-clinic {DoctorClinicId}", doctorClinicId);

        var doctorClinic = await _doctorClinicRepository.GetByIdAsync(doctorClinicId, cancellationToken);
        if (doctorClinic is null)
        {
            _logger.LogWarning("Create availability failed: doctor-clinic with ID {DoctorClinicId} was not found", doctorClinicId);
            throw new KeyNotFoundException($"Doctor-clinic with ID {doctorClinicId} was not found.");
        }

        var availability = new DoctorAvailability(doctorClinicId, dayOfWeek, startTime, endTime);
        await _availabilityRepository.AddAsync(availability, cancellationToken);

        _logger.LogInformation("Availability {AvailabilityId} was successfully created", availability.AvailabilityId);
        return availability;
    }

    public async Task UpdateTimeSlotAsync(Guid doctorClinicId, Guid availabilityId, TimeOnly startTime, TimeOnly endTime, CancellationToken cancellationToken = default)
    {
        if (startTime >= endTime)
        {
            _logger.LogWarning("Update availability {AvailabilityId} failed: start time {StartTime} is not before end time {EndTime}", availabilityId, startTime, endTime);
            throw new ArgumentException("Start time must be before end time.", nameof(startTime));
        }

        _logger.LogInformation("Updating time slot for availability {AvailabilityId}", availabilityId);

        var availability = await FindAsync(doctorClinicId, availabilityId, cancellationToken);

        availability.UpdateTimeSlot(startTime, endTime);
        await _availabilityRepository.UpdateAsync(availability, cancellationToken);

        _logger.LogInformation("Availability {AvailabilityId} was successfully updated", availabilityId);
    }

    public async Task ActivateAsync(Guid doctorClinicId, Guid availabilityId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Activating availability {AvailabilityId}", availabilityId);

        var availability = await FindAsync(doctorClinicId, availabilityId, cancellationToken);

        availability.Activate();
        await _availabilityRepository.UpdateAsync(availability, cancellationToken);

        _logger.LogInformation("Availability {AvailabilityId} was successfully activated", availabilityId);
    }

    public async Task DeactivateAsync(Guid doctorClinicId, Guid availabilityId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deactivating availability {AvailabilityId}", availabilityId);

        var availability = await FindAsync(doctorClinicId, availabilityId, cancellationToken);

        availability.Deactivate();
        await _availabilityRepository.UpdateAsync(availability, cancellationToken);

        _logger.LogInformation("Availability {AvailabilityId} was successfully deactivated", availabilityId);
    }

    public async Task DeleteAsync(Guid doctorClinicId, Guid availabilityId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting availability {AvailabilityId}", availabilityId);

        var availability = await FindAsync(doctorClinicId, availabilityId, cancellationToken);

        await _availabilityRepository.DeleteAsync(availability, cancellationToken);

        _logger.LogInformation("Availability {AvailabilityId} was successfully deleted", availabilityId);
    }

    private async Task<DoctorAvailability> FindAsync(Guid doctorClinicId, Guid availabilityId, CancellationToken cancellationToken)
    {
        if (doctorClinicId == Guid.Empty)
        {
            _logger.LogWarning("Availability operation failed: {DoctorClinicId} is empty", nameof(doctorClinicId));
            throw new ArgumentException("Doctor-clinic ID cannot be empty.", nameof(doctorClinicId));
        }

        if (availabilityId == Guid.Empty)
        {
            _logger.LogWarning("Availability operation failed: {AvailabilityId} is empty", nameof(availabilityId));
            throw new ArgumentException("Availability ID cannot be empty.", nameof(availabilityId));
        }

        var doctorClinic = await _doctorClinicRepository.GetByIdAsync(doctorClinicId, cancellationToken);
        if (doctorClinic is null)
        {
            _logger.LogWarning("Availability operation failed: doctor-clinic with ID {DoctorClinicId} was not found", doctorClinicId);
            throw new KeyNotFoundException($"Doctor-clinic with ID {doctorClinicId} was not found.");
        }

        var availabilities = await _availabilityRepository.GetByDoctorClinicAsync(doctorClinicId, cancellationToken);
        var availability = availabilities.FirstOrDefault(a => a.AvailabilityId == availabilityId);
        if (availability is null)
        {
            _logger.LogWarning("Availability with ID {AvailabilityId} was not found for doctor-clinic {DoctorClinicId}", availabilityId, doctorClinicId);
            throw new KeyNotFoundException($"Availability with ID {availabilityId} was not found for doctor-clinic {doctorClinicId}.");
        }

        return availability;
    }
}
