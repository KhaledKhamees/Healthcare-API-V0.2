using Healthcare_API.Models;
using Healthcare_API_V0._2_Application.Interfaces;
using Healthcare_API_V0._2_Application.Interfaces.Repository;
using Healthcare_API_V0._2_Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace Healthcare_API_V0._2_Application.Services;

public class PatientService : IPatientService
{
    private readonly IPatientRepository _patientRepository;
    private readonly ILogger<PatientService> _logger;
    private readonly ICacheService _cacheService;

    public PatientService(IPatientRepository patientRepository, ILogger<PatientService> logger, ICacheService cacheService)
    {
        _patientRepository = patientRepository;
        _logger = logger;
        _cacheService = cacheService;
    }

    private static string GetCacheKey(Guid patientId) => $"patient:{patientId}";

    public async Task<Patient?> GetByIdAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        if (patientId == Guid.Empty)
        {
            _logger.LogWarning("Get patient failed: {PatientId} is empty", nameof(patientId));
            throw new ArgumentException("Patient ID cannot be empty.", nameof(patientId));
        }

        _logger.LogInformation("Getting patient with ID {PatientId}", patientId);
        var patient = await _cacheService.GetAsync<Patient>(GetCacheKey(patientId), cancellationToken);

        if (patient is not null)
        {
            return patient;
        }

        patient = await _patientRepository.GetByIdAsync(patientId, cancellationToken);

        if (patient is null)
        {
            _logger.LogWarning("Patient with ID {PatientId} was not found", patientId);
        }
        else
        {
            await _cacheService.SetAsync(GetCacheKey(patientId), patient, cancellationToken: cancellationToken);
        }

        return patient;
    }

    public async Task<Patient?> GetByApplicationUserIdAsync(Guid applicationUserId, CancellationToken cancellationToken = default)
    {
        if (applicationUserId == Guid.Empty)
        {
            _logger.LogWarning("Get patient failed: {ApplicationUserId} is empty", nameof(applicationUserId));
            throw new ArgumentException("Application user ID cannot be empty.", nameof(applicationUserId));
        }

        _logger.LogInformation("Getting patient with application user ID {ApplicationUserId}", applicationUserId);
        var patient = await _patientRepository.GetByApplicationUserIdAsync(applicationUserId, cancellationToken);

        if (patient is null)
        {
            _logger.LogWarning("Patient with application user ID {ApplicationUserId} was not found", applicationUserId);
        }

        return patient;
    }

    public async Task<Patient> CreateAsync(Guid applicationUserId, CancellationToken cancellationToken = default)
    {
        if (applicationUserId == Guid.Empty)
        {
            _logger.LogWarning("Create patient failed: {ApplicationUserId} is empty", nameof(applicationUserId));
            throw new ArgumentException("Application user ID cannot be empty.", nameof(applicationUserId));
        }

        _logger.LogInformation("Creating patient with application user ID {ApplicationUserId}", applicationUserId);

        var existing = await _patientRepository.GetByApplicationUserIdAsync(applicationUserId, cancellationToken);
        if (existing is not null)
        {
            _logger.LogWarning("Create patient failed: patient already exists for application user ID {ApplicationUserId}", applicationUserId);
            throw new InvalidOperationException("A patient already exists for this application user.");
        }

        var patient = new Patient(applicationUserId);
        await _patientRepository.AddAsync(patient, cancellationToken);
        await _cacheService.SetAsync(GetCacheKey(patient.PatientId), patient, cancellationToken: cancellationToken);

        _logger.LogInformation("Patient {PatientId} was successfully created", patient.PatientId);
        return patient;
    }

    public async Task DeleteAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        if (patientId == Guid.Empty)
        {
            _logger.LogWarning("Delete patient failed: {PatientId} is empty", nameof(patientId));
            throw new ArgumentException("Patient ID cannot be empty.", nameof(patientId));
        }

        _logger.LogInformation("Deleting patient with ID {PatientId}", patientId);

        var patient = await _patientRepository.GetByIdAsync(patientId, cancellationToken);
        if (patient is null)
        {
            _logger.LogWarning("Delete patient failed: patient with ID {PatientId} was not found", patientId);
            throw new KeyNotFoundException($"Patient with ID {patientId} was not found.");
        }

        await _patientRepository.DeleteAsync(patient, cancellationToken);
        await _cacheService.RemoveAsync(GetCacheKey(patientId), cancellationToken);

        _logger.LogInformation("Patient {PatientId} was successfully deleted", patientId);
    }
}
