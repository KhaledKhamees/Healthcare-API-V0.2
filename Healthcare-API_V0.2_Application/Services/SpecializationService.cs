using Healthcare_API.Models;
using Healthcare_API_V0._2_Application.Interfaces.Repository;
using Healthcare_API_V0._2_Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace Healthcare_API_V0._2_Application.Services;

public class SpecializationService : ISpecializationService
{
    private readonly ISpecializationRepository _specializationRepository;
    private readonly ILogger<SpecializationService> _logger;

    public SpecializationService(ISpecializationRepository specializationRepository, ILogger<SpecializationService> logger)
    {
        _specializationRepository = specializationRepository;
        _logger = logger;
    }

    public async Task<Specialization?> GetByIdAsync(Guid specializationId, CancellationToken cancellationToken = default)
    {
        if (specializationId == Guid.Empty)
        {
            _logger.LogWarning("Get specialization failed: {SpecializationId} is empty", nameof(specializationId));
            throw new ArgumentException("Specialization ID cannot be empty.", nameof(specializationId));
        }

        _logger.LogInformation("Getting specialization with ID {SpecializationId}", specializationId);
        var specialization = await _specializationRepository.GetByIdAsync(specializationId, cancellationToken);

        if (specialization is null)
        {
            _logger.LogWarning("Specialization with ID {SpecializationId} was not found", specializationId);
        }

        return specialization;
    }

    public async Task<Specialization?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            _logger.LogWarning("Get specialization failed: name is missing");
            throw new ArgumentException("Specialization name is required.", nameof(name));
        }

        _logger.LogInformation("Getting specialization with name {SpecializationName}", name);
        var specialization = await _specializationRepository.GetByNameAsync(name, cancellationToken);

        if (specialization is null)
        {
            _logger.LogWarning("Specialization with name {SpecializationName} was not found", name);
        }

        return specialization;
    }

    public async Task<Specialization> CreateAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            _logger.LogWarning("Create specialization failed: name is missing");
            throw new ArgumentException("Specialization name is required.", nameof(name));
        }

        _logger.LogInformation("Creating specialization with name {SpecializationName}", name);

        var existing = await _specializationRepository.GetByNameAsync(name, cancellationToken);
        if (existing is not null)
        {
            _logger.LogWarning("Create specialization failed: specialization with name {SpecializationName} already exists", name);
            throw new InvalidOperationException($"A specialization with name '{name}' already exists.");
        }

        var specialization = new Specialization(name);
        await _specializationRepository.AddAsync(specialization, cancellationToken);

        _logger.LogInformation("Specialization {SpecializationId} was successfully created", specialization.SpecializationId);
        return specialization;
    }

    public async Task UpdateAsync(Guid specializationId, string name, CancellationToken cancellationToken = default)
    {
        if (specializationId == Guid.Empty)
        {
            _logger.LogWarning("Update specialization failed: {SpecializationId} is empty", nameof(specializationId));
            throw new ArgumentException("Specialization ID cannot be empty.", nameof(specializationId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            _logger.LogWarning("Update specialization {SpecializationId} failed: name is missing", specializationId);
            throw new ArgumentException("Specialization name is required.", nameof(name));
        }

        _logger.LogInformation("Updating specialization with ID {SpecializationId}", specializationId);

        var specialization = await _specializationRepository.GetByIdAsync(specializationId, cancellationToken);
        if (specialization is null)
        {
            _logger.LogWarning("Update specialization failed: specialization with ID {SpecializationId} was not found", specializationId);
            throw new KeyNotFoundException($"Specialization with ID {specializationId} was not found.");
        }

        var existing = await _specializationRepository.GetByNameAsync(name, cancellationToken);
        if (existing is not null && existing.SpecializationId != specializationId)
        {
            _logger.LogWarning("Update specialization failed: specialization with name {SpecializationName} already exists", name);
            throw new InvalidOperationException($"A specialization with name '{name}' already exists.");
        }

        specialization.UpdateSpecialization(name);
        await _specializationRepository.UpdateAsync(specialization, cancellationToken);

        _logger.LogInformation("Specialization {SpecializationId} was successfully updated", specializationId);
    }

    public async Task DeleteAsync(Guid specializationId, CancellationToken cancellationToken = default)
    {
        if (specializationId == Guid.Empty)
        {
            _logger.LogWarning("Delete specialization failed: {SpecializationId} is empty", nameof(specializationId));
            throw new ArgumentException("Specialization ID cannot be empty.", nameof(specializationId));
        }

        _logger.LogInformation("Deleting specialization with ID {SpecializationId}", specializationId);

        var specialization = await _specializationRepository.GetByIdAsync(specializationId, cancellationToken);
        if (specialization is null)
        {
            _logger.LogWarning("Delete specialization failed: specialization with ID {SpecializationId} was not found", specializationId);
            throw new KeyNotFoundException($"Specialization with ID {specializationId} was not found.");
        }

        await _specializationRepository.DeleteAsync(specialization, cancellationToken);

        _logger.LogInformation("Specialization {SpecializationId} was successfully deleted", specializationId);
    }
}
