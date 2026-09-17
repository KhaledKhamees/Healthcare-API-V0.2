using Healthcare_API.Models;

namespace Healthcare_API_V0._2_Application.Interfaces.Services;

public interface IDoctorAvailabilityService
{
    Task<List<DoctorAvailability>> GetByDoctorClinicAsync(Guid doctorClinicId, CancellationToken cancellationToken = default);
    Task<DoctorAvailability> CreateAsync(Guid doctorClinicId, DayOfWeek dayOfWeek, TimeOnly startTime, TimeOnly endTime, CancellationToken cancellationToken = default);
    Task UpdateTimeSlotAsync(Guid doctorClinicId, Guid availabilityId, TimeOnly startTime, TimeOnly endTime, CancellationToken cancellationToken = default);
    Task ActivateAsync(Guid doctorClinicId, Guid availabilityId, CancellationToken cancellationToken = default);
    Task DeactivateAsync(Guid doctorClinicId, Guid availabilityId, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid doctorClinicId, Guid availabilityId, CancellationToken cancellationToken = default);
}
