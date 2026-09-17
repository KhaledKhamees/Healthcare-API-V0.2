using Healthcare_API.Models;

namespace Healthcare_API_V0._2_Application.Interfaces.Repository;

public interface ISpecializationRepository
{
    Task<Specialization?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Specialization?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task AddAsync(Specialization specialization, CancellationToken cancellationToken = default);
    Task UpdateAsync(Specialization specialization, CancellationToken cancellationToken = default);
    Task DeleteAsync(Specialization specialization, CancellationToken cancellationToken = default);
}
