using Healthcare_API.Models;

namespace Healthcare_API_V0._2_Application.Interfaces.Repository;

public interface IAppointmentRepository
{
    Task<Appointment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Appointment>> GetByPatientAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<List<Appointment>> GetByDoctorClinicAsync(Guid doctorClinicId, CancellationToken cancellationToken = default);
    Task<List<Appointment>> GetAppointmentsForTimeRangeAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default);
    Task AddAsync(Appointment appointment, CancellationToken cancellationToken = default);
    Task UpdateAsync(Appointment appointment, CancellationToken cancellationToken = default);
    Task DeleteAsync(Appointment appointment, CancellationToken cancellationToken = default);
}
