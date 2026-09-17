using Healthcare_API.Models;

namespace Healthcare_API_V0._2_Application;

public interface IClinicRepository
{
    Task<Clinic?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Clinic>> GetByCityAsync(string city, CancellationToken cancellationToken = default);
    Task AddAsync(Clinic clinic, CancellationToken cancellationToken = default);
    Task UpdateAsync(Clinic clinic, CancellationToken cancellationToken = default);
    Task DeleteAsync(Clinic clinic, CancellationToken cancellationToken = default);
}
