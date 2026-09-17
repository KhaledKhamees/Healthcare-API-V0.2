using Healthcare_API.Models;

namespace Healthcare_API_V0._2_Application.Interfaces.Services;

public interface IPatientService
{
    Task<Patient?> GetByIdAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<Patient?> GetByApplicationUserIdAsync(Guid applicationUserId, CancellationToken cancellationToken = default);
    Task<Patient> CreateAsync(Guid applicationUserId, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid patientId, CancellationToken cancellationToken = default);
}
