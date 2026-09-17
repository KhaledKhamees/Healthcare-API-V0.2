using Healthcare_API.Models;
using Healthcare_API.Models.Enums;

namespace Healthcare_API_V0._2_Application.Interfaces.Services;

public interface IAppointmentService
{
    Task<Appointment?> GetByIdAsync(Guid appointmentId, CancellationToken cancellationToken = default);
    Task<List<Appointment>> GetByPatientAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<List<Appointment>> GetByDoctorClinicAsync(Guid doctorClinicId, CancellationToken cancellationToken = default);
    Task<List<Appointment>> GetForTimeRangeAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default);
    Task<Appointment> CreateAsync(Guid patientId, Guid doctorClinicId, DateTime startTime, string? reasonForVisit, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid appointmentId, DateTime startTime, string? reasonForVisit, CancellationToken cancellationToken = default);
    Task ChangeStatusAsync(Guid appointmentId, AppointmentStatus newStatus, CancellationToken cancellationToken = default);
    Task CancelAsync(Guid appointmentId, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid appointmentId, CancellationToken cancellationToken = default);
}
