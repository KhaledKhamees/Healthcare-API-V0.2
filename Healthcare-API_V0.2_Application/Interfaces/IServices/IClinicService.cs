using Healthcare_API.Models;

namespace Healthcare_API_V0._2_Application.Interfaces.Services;

public interface IClinicService
{
    Task<Clinic?> GetByIdAsync(Guid clinicId, CancellationToken cancellationToken = default);
    Task<List<Clinic>> GetByCityAsync(string city, CancellationToken cancellationToken = default);
    Task<Clinic> CreateAsync(string name, string address, string city, string? phoneNumber = null, CancellationToken cancellationToken = default);
    Task UpdateClinicAsync(Guid clinicId, string name, string address, string city, string? phoneNumber, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid clinicId, CancellationToken cancellationToken = default);
}
