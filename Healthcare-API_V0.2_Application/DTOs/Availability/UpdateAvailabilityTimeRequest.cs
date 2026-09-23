namespace Healthcare_API_V0._2_Application.DTOs.Availability;

public sealed class UpdateAvailabilityTimeRequest
{
    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }
}
