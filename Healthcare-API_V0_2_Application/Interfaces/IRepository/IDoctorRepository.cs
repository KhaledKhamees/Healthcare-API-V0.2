using Healthcare_API.Models;
using Healthcare_API.Models.Enums;

namespace Healthcare_API_V0._2_Application;

public interface IDoctorRepository
{
    Task<Doctor?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Doctor?> GetByApplicationUserIdAsync(Guid applicationUserId, CancellationToken cancellationToken = default);
    Task<List<Doctor>> GetBySpecializationAsync(Guid specializationId, CancellationToken cancellationToken = default);
    Task AddAsync(Doctor doctor, CancellationToken cancellationToken = default);
    Task UpdateAsync(Doctor doctor, CancellationToken cancellationToken = default);
    Task DeleteAsync(Doctor doctor, CancellationToken cancellationToken = default);
}
