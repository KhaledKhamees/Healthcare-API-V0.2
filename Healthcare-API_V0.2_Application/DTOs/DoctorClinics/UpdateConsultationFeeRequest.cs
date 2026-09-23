using System.ComponentModel.DataAnnotations;

namespace Healthcare_API_V0._2_Application.DTOs.DoctorClinics;

public sealed class UpdateConsultationFeeRequest
{
    [Range(0, double.MaxValue)]
    public decimal ConsultationFee { get; set; }
}
