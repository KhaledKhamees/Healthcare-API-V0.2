using Healthcare_API.Models;
using Healthcare_API.Models.Enums;
using Healthcare_API_V0._2_Application.Interfaces.Repository;
using Healthcare_API_V0._2_Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace Healthcare_API_V0._2_Application.Services;

public class DoctorService : IDoctorService
{
    private readonly IDoctorRepository _doctorRepository;
    private readonly ISpecializationRepository _specializationRepository;
    private readonly ILogger<DoctorService> _logger;

    public DoctorService(
        IDoctorRepository doctorRepository,
        ISpecializationRepository specializationRepository,
        ILogger<DoctorService> logger)
    {
        _doctorRepository = doctorRepository;
        _specializationRepository = specializationRepository;
        _logger = logger;
    }

    public async Task<Doctor?> GetByIdAsync(Guid doctorId, CancellationToken cancellationToken = default)
    {
        if (doctorId == Guid.Empty)
        {
            _logger.LogWarning("Get doctor failed: {DoctorId} is empty", nameof(doctorId));
            throw new ArgumentException("Doctor ID cannot be empty.", nameof(doctorId));
        }

        _logger.LogInformation("Getting doctor with ID {DoctorId}", doctorId);
        var doctor = await _doctorRepository.GetByIdAsync(doctorId, cancellationToken);

        if (doctor is null)
        {
            _logger.LogWarning("Doctor with ID {DoctorId} was not found", doctorId);
        }

        return doctor;
    }

    public async Task<Doctor?> GetByApplicationUserIdAsync(Guid applicationUserId, CancellationToken cancellationToken = default)
    {
        if (applicationUserId == Guid.Empty)
        {
            _logger.LogWarning("Get doctor failed: {ApplicationUserId} is empty", nameof(applicationUserId));
            throw new ArgumentException("Application user ID cannot be empty.", nameof(applicationUserId));
        }

        _logger.LogInformation("Getting doctor with application user ID {ApplicationUserId}", applicationUserId);
        var doctor = await _doctorRepository.GetByApplicationUserIdAsync(applicationUserId, cancellationToken);

        if (doctor is null)
        {
            _logger.LogWarning("Doctor with application user ID {ApplicationUserId} was not found", applicationUserId);
        }

        return doctor;
    }

    public async Task<List<Doctor>> GetBySpecializationAsync(Guid specializationId, CancellationToken cancellationToken = default)
    {
        if (specializationId == Guid.Empty)
        {
            _logger.LogWarning("Get doctors failed: {SpecializationId} is empty", nameof(specializationId));
            throw new ArgumentException("Specialization ID cannot be empty.", nameof(specializationId));
        }

        _logger.LogInformation("Getting doctors with specialization ID {SpecializationId}", specializationId);

        var specialization = await _specializationRepository.GetByIdAsync(specializationId, cancellationToken);
        if (specialization is null)
        {
            _logger.LogWarning("Get doctors failed: specialization with ID {SpecializationId} was not found", specializationId);
            throw new KeyNotFoundException($"Specialization with ID {specializationId} was not found.");
        }

        return await _doctorRepository.GetBySpecializationAsync(specializationId, cancellationToken);
    }

    public async Task<Doctor> CreateAsync(Guid applicationUserId, string firstName, string lastName, Guid specializationId, CancellationToken cancellationToken = default)
    {
        if (applicationUserId == Guid.Empty)
        {
            _logger.LogWarning("Create doctor failed: {ApplicationUserId} is empty", nameof(applicationUserId));
            throw new ArgumentException("Application user ID cannot be empty.", nameof(applicationUserId));
        }

        if (string.IsNullOrWhiteSpace(firstName))
        {
            _logger.LogWarning("Create doctor failed: first name is missing");
            throw new ArgumentException("First name is required.", nameof(firstName));
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            _logger.LogWarning("Create doctor failed: last name is missing");
            throw new ArgumentException("Last name is required.", nameof(lastName));
        }

        if (specializationId == Guid.Empty)
        {
            _logger.LogWarning("Create doctor failed: {SpecializationId} is empty", nameof(specializationId));
            throw new ArgumentException("Specialization ID cannot be empty.", nameof(specializationId));
        }

        _logger.LogInformation("Creating doctor with application user ID {ApplicationUserId}", applicationUserId);

        var specialization = await _specializationRepository.GetByIdAsync(specializationId, cancellationToken);
        if (specialization is null)
        {
            _logger.LogWarning("Create doctor failed: specialization with ID {SpecializationId} was not found", specializationId);
            throw new KeyNotFoundException($"Specialization with ID {specializationId} was not found.");
        }

        var existing = await _doctorRepository.GetByApplicationUserIdAsync(applicationUserId, cancellationToken);
        if (existing is not null)
        {
            _logger.LogWarning("Create doctor failed: doctor already exists for application user ID {ApplicationUserId}", applicationUserId);
            throw new InvalidOperationException("A doctor already exists for this application user.");
        }

        var doctor = new Doctor(applicationUserId, firstName, lastName, specializationId);
        await _doctorRepository.AddAsync(doctor, cancellationToken);

        _logger.LogInformation("Doctor {DoctorId} was successfully created", doctor.DoctorId);
        return doctor;
    }

    public async Task UpdateDoctorAsync(Guid doctorId, string firstName, string lastName, string? licenseNumber, string? bio, CancellationToken cancellationToken = default)
    {
        if (doctorId == Guid.Empty)
        {
            _logger.LogWarning("Update doctor failed: {DoctorId} is empty", nameof(doctorId));
            throw new ArgumentException("Doctor ID cannot be empty.", nameof(doctorId));
        }

        if (string.IsNullOrWhiteSpace(firstName))
        {
            _logger.LogWarning("Update doctor {DoctorId} failed: first name is missing", doctorId);
            throw new ArgumentException("First name is required.", nameof(firstName));
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            _logger.LogWarning("Update doctor {DoctorId} failed: last name is missing", doctorId);
            throw new ArgumentException("Last name is required.", nameof(lastName));
        }

        _logger.LogInformation("Updating doctor with ID {DoctorId}", doctorId);

        var doctor = await _doctorRepository.GetByIdAsync(doctorId, cancellationToken);
        if (doctor is null)
        {
            _logger.LogWarning("Update doctor failed: doctor with ID {DoctorId} was not found", doctorId);
            throw new KeyNotFoundException($"Doctor with ID {doctorId} was not found.");
        }

        doctor.UpdateDoctor(firstName, lastName, licenseNumber, bio);
        await _doctorRepository.UpdateAsync(doctor, cancellationToken);

        _logger.LogInformation("Doctor {DoctorId} was successfully updated", doctorId);
    }

    public async Task ChangeStatusAsync(Guid doctorId, DoctorStatus newStatus, CancellationToken cancellationToken = default)
    {
        if (doctorId == Guid.Empty)
        {
            _logger.LogWarning("Change doctor status failed: {DoctorId} is empty", nameof(doctorId));
            throw new ArgumentException("Doctor ID cannot be empty.", nameof(doctorId));
        }

        if (!Enum.IsDefined(newStatus))
        {
            _logger.LogWarning("Change doctor {DoctorId} status failed: invalid status {DoctorStatus}", doctorId, (int)newStatus);
            throw new ArgumentException("Invalid doctor status.", nameof(newStatus));
        }

        _logger.LogInformation("Changing status of doctor {DoctorId} to {DoctorStatus}", doctorId, newStatus);

        var doctor = await _doctorRepository.GetByIdAsync(doctorId, cancellationToken);
        if (doctor is null)
        {
            _logger.LogWarning("Change doctor status failed: doctor with ID {DoctorId} was not found", doctorId);
            throw new KeyNotFoundException($"Doctor with ID {doctorId} was not found.");
        }

        doctor.ChangeStatus(newStatus);
        await _doctorRepository.UpdateAsync(doctor, cancellationToken);

        _logger.LogInformation("Doctor {DoctorId} status was successfully changed to {DoctorStatus}", doctorId, newStatus);
    }

    public async Task DeleteAsync(Guid doctorId, CancellationToken cancellationToken = default)
    {
        if (doctorId == Guid.Empty)
        {
            _logger.LogWarning("Delete doctor failed: {DoctorId} is empty", nameof(doctorId));
            throw new ArgumentException("Doctor ID cannot be empty.", nameof(doctorId));
        }

        _logger.LogInformation("Deleting doctor with ID {DoctorId}", doctorId);

        var doctor = await _doctorRepository.GetByIdAsync(doctorId, cancellationToken);
        if (doctor is null)
        {
            _logger.LogWarning("Delete doctor failed: doctor with ID {DoctorId} was not found", doctorId);
            throw new KeyNotFoundException($"Doctor with ID {doctorId} was not found.");
        }

        await _doctorRepository.DeleteAsync(doctor, cancellationToken);

        _logger.LogInformation("Doctor {DoctorId} was successfully deleted", doctorId);
    }
}
