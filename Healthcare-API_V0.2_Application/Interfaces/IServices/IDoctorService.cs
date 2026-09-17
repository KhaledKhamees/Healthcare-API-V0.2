using Healthcare_API.Models;
using Healthcare_API.Models.Enums;

namespace Healthcare_API_V0._2_Application.Interfaces.Services;

public interface IDoctorService
{
    Task<Doctor?> GetByIdAsync(Guid doctorId, CancellationToken cancellationToken = default);
    Task<Doctor?> GetByApplicationUserIdAsync(Guid applicationUserId, CancellationToken cancellationToken = default);
    Task<List<Doctor>> GetBySpecializationAsync(Guid specializationId, CancellationToken cancellationToken = default);
    Task<Doctor> CreateAsync(Guid applicationUserId, string firstName, string lastName, Guid specializationId, CancellationToken cancellationToken = default);
    Task UpdateDoctorAsync(Guid doctorId, string firstName, string lastName, string? licenseNumber, string? bio, CancellationToken cancellationToken = default);
    Task ChangeStatusAsync(Guid doctorId, DoctorStatus newStatus, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid doctorId, CancellationToken cancellationToken = default);
}
