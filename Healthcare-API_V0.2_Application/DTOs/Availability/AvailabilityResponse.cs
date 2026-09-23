namespace Healthcare_API_V0._2_Application.DTOs.Availability;

public sealed class AvailabilityResponse
{
    public Guid AvailabilityId { get; set; }
    public Guid DoctorClinicId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
