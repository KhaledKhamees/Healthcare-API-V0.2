using Healthcare_API.Models;

namespace Healthcare_API_V0._2_Application.Interfaces.Services;

public interface ISpecializationService
{
    Task<Specialization?> GetByIdAsync(Guid specializationId, CancellationToken cancellationToken = default);
    Task<Specialization?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<Specialization> CreateAsync(string name, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid specializationId, string name, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid specializationId, CancellationToken cancellationToken = default);
}
