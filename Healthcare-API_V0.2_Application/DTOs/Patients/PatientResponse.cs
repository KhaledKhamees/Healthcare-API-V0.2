namespace Healthcare_API_V0._2_Application.DTOs.Patients;

public sealed class PatientResponse
{
    public Guid PatientId { get; set; }
    public Guid ApplicationUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
