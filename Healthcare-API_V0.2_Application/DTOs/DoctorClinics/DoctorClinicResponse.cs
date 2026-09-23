namespace Healthcare_API_V0._2_Application.DTOs.DoctorClinics;

public sealed class DoctorClinicResponse
{
    public Guid DoctorClinicId { get; set; }
    public Guid DoctorId { get; set; }
    public Guid ClinicId { get; set; }
    public decimal ConsultationFee { get; set; }
    public int AppointmentDurationMinutes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
