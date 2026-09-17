using Healthcare_API.Models;

namespace Healthcare_API_V0._2_Application.Interfaces.Repository;

public interface IDoctorAvailabilityRepository
{
    Task<List<DoctorAvailability>> GetByDoctorClinicAsync(Guid doctorClinicId, CancellationToken cancellationToken = default);
    Task AddAsync(DoctorAvailability availability, CancellationToken cancellationToken = default);
    Task UpdateAsync(DoctorAvailability availability, CancellationToken cancellationToken = default);
    Task DeleteAsync(DoctorAvailability availability, CancellationToken cancellationToken = default);
}
