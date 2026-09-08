using System;
using System;

namespace Healthcare_API.Models;

public class DoctorAvailability
{
    public long AvailabilityID { get; private set; }
    public long DoctorClinicID { get; private set; }
    public System.DayOfWeek DayOfWeek { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public bool IsActive { get; private set; } = true;

    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Navigation: availability belongs to a doctor at a specific clinic
    public virtual DoctorClinic DoctorClinic { get; private set; }

    public DoctorAvailability(long doctorClinicID, System.DayOfWeek dayOfWeek, TimeOnly startTime, TimeOnly endTime)
    {
        DoctorClinicID = doctorClinicID;
        DayOfWeek = dayOfWeek;
        StartTime = startTime;
        EndTime = endTime;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateTimeSlot(TimeOnly startTime, TimeOnly endTime)
    {
        StartTime = startTime;
        EndTime = endTime;
        UpdatedAt = DateTime.UtcNow;
    }
}
