using Healthcare_API.Models;

namespace Healthcare_API_V0._2_Application;

public interface IDoctorClinicRepository
{
    Task<DoctorClinic?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DoctorClinic?> GetByDoctorAndClinicAsync(Guid doctorId, Guid clinicId, CancellationToken cancellationToken = default);
    Task AddAsync(DoctorClinic doctorClinic, CancellationToken cancellationToken = default);
    Task UpdateAsync(DoctorClinic doctorClinic, CancellationToken cancellationToken = default);
    Task DeleteAsync(DoctorClinic doctorClinic, CancellationToken cancellationToken = default);
}
