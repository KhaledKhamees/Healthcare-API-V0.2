using Healthcare_API.Models;

namespace Healthcare_API_V0._2_Application.Interfaces.Services;

public interface IDoctorClinicService
{
    Task<DoctorClinic?> GetByIdAsync(Guid doctorClinicId, CancellationToken cancellationToken = default);
    Task<DoctorClinic?> GetByDoctorAndClinicAsync(Guid doctorId, Guid clinicId, CancellationToken cancellationToken = default);
    Task<DoctorClinic> CreateAsync(Guid doctorId, Guid clinicId, decimal consultationFee, int appointmentDurationMinutes = 30, CancellationToken cancellationToken = default);
    Task UpdateConsultationFeeAsync(Guid doctorClinicId, decimal newFee, CancellationToken cancellationToken = default);
    Task UpdateAppointmentDurationAsync(Guid doctorClinicId, int durationMinutes, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid doctorClinicId, CancellationToken cancellationToken = default);
}
