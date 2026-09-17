using Healthcare_API.Models;
using Healthcare_API_V0._2_Application.Interfaces.Repository;
using Healthcare_API_V0._2_Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace Healthcare_API_V0._2_Application.Services;

public class ClinicService : IClinicService
{
    private readonly IClinicRepository _clinicRepository;
    private readonly ILogger<ClinicService> _logger;

    public ClinicService(IClinicRepository clinicRepository, ILogger<ClinicService> logger)
    {
        _clinicRepository = clinicRepository;
        _logger = logger;
    }

    public async Task<Clinic?> GetByIdAsync(Guid clinicId, CancellationToken cancellationToken = default)
    {
        if (clinicId == Guid.Empty)
        {
            _logger.LogWarning("Get clinic failed: {ClinicId} is empty", nameof(clinicId));
            throw new ArgumentException("Clinic ID cannot be empty.", nameof(clinicId));
        }

        _logger.LogInformation("Getting clinic with ID {ClinicId}", clinicId);
        var clinic = await _clinicRepository.GetByIdAsync(clinicId, cancellationToken);

        if (clinic is null)
        {
            _logger.LogWarning("Clinic with ID {ClinicId} was not found", clinicId);
        }

        return clinic;
    }

    public async Task<List<Clinic>> GetByCityAsync(string city, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(city))
        {
            _logger.LogWarning("Get clinics failed: city is missing");
            throw new ArgumentException("City is required.", nameof(city));
        }

        _logger.LogInformation("Getting clinics in city {City}", city);
        return await _clinicRepository.GetByCityAsync(city, cancellationToken);
    }

    public async Task<Clinic> CreateAsync(string name, string address, string city, string? phoneNumber = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            _logger.LogWarning("Create clinic failed: name is missing");
            throw new ArgumentException("Clinic name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(address))
        {
            _logger.LogWarning("Create clinic failed: address is missing");
            throw new ArgumentException("Clinic address is required.", nameof(address));
        }

        if (string.IsNullOrWhiteSpace(city))
        {
            _logger.LogWarning("Create clinic failed: city is missing");
            throw new ArgumentException("City is required.", nameof(city));
        }

        _logger.LogInformation("Creating clinic with name {ClinicName}", name);

        var clinic = new Clinic(name, address, city, phoneNumber);
        await _clinicRepository.AddAsync(clinic, cancellationToken);

        _logger.LogInformation("Clinic {ClinicId} was successfully created", clinic.ClinicId);
        return clinic;
    }

    public async Task UpdateClinicAsync(Guid clinicId, string name, string address, string city, string? phoneNumber, CancellationToken cancellationToken = default)
    {
        if (clinicId == Guid.Empty)
        {
            _logger.LogWarning("Update clinic failed: {ClinicId} is empty", nameof(clinicId));
            throw new ArgumentException("Clinic ID cannot be empty.", nameof(clinicId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            _logger.LogWarning("Update clinic {ClinicId} failed: name is missing", clinicId);
            throw new ArgumentException("Clinic name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(address))
        {
            _logger.LogWarning("Update clinic {ClinicId} failed: address is missing", clinicId);
            throw new ArgumentException("Clinic address is required.", nameof(address));
        }

        if (string.IsNullOrWhiteSpace(city))
        {
            _logger.LogWarning("Update clinic {ClinicId} failed: city is missing", clinicId);
            throw new ArgumentException("City is required.", nameof(city));
        }

        _logger.LogInformation("Updating clinic with ID {ClinicId}", clinicId);

        var clinic = await _clinicRepository.GetByIdAsync(clinicId, cancellationToken);
        if (clinic is null)
        {
            _logger.LogWarning("Update clinic failed: clinic with ID {ClinicId} was not found", clinicId);
            throw new KeyNotFoundException($"Clinic with ID {clinicId} was not found.");
        }

        clinic.UpdateClinic(name, address, city, phoneNumber);
        await _clinicRepository.UpdateAsync(clinic, cancellationToken);

        _logger.LogInformation("Clinic {ClinicId} was successfully updated", clinicId);
    }

    public async Task DeleteAsync(Guid clinicId, CancellationToken cancellationToken = default)
    {
        if (clinicId == Guid.Empty)
        {
            _logger.LogWarning("Delete clinic failed: {ClinicId} is empty", nameof(clinicId));
            throw new ArgumentException("Clinic ID cannot be empty.", nameof(clinicId));
        }

        _logger.LogInformation("Deleting clinic with ID {ClinicId}", clinicId);

        var clinic = await _clinicRepository.GetByIdAsync(clinicId, cancellationToken);
        if (clinic is null)
        {
            _logger.LogWarning("Delete clinic failed: clinic with ID {ClinicId} was not found", clinicId);
            throw new KeyNotFoundException($"Clinic with ID {clinicId} was not found.");
        }

        await _clinicRepository.DeleteAsync(clinic, cancellationToken);

        _logger.LogInformation("Clinic {ClinicId} was successfully deleted", clinicId);
    }
}
