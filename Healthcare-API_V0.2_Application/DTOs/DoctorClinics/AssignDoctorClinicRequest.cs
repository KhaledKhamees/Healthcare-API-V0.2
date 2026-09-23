using System.ComponentModel.DataAnnotations;

namespace Healthcare_API_V0._2_Application.DTOs.DoctorClinics;

public sealed class AssignDoctorClinicRequest
{
    [Required]
    public Guid ClinicId { get; set; }

    [Range(0, double.MaxValue)]
    public decimal ConsultationFee { get; set; }

    [Range(1, int.MaxValue)]
    public int AppointmentDurationMinutes { get; set; } = 30;
}
