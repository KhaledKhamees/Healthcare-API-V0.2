using System;
using System;
using Healthcare_API.Models.Enums;

namespace Healthcare_API.Models;

public class Appointment
{
    public Guid AppointmentId { get; private set; } = Guid.NewGuid();
    public Guid PatientId { get; private set; }
    public Guid DoctorClinicId { get; private set; }

    public DateTime StartTime { get; private set; }
    public string? ReasonForVisit { get; private set; }
    public AppointmentStatus Status { get; private set; } = AppointmentStatus.Scheduled;

    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Navigation: appointment is for a patient with a doctor at a clinic
    public virtual Patient Patient { get; private set; }
    public virtual DoctorClinic DoctorClinic { get; private set; }

    public Appointment(Guid patientId, Guid doctorClinicId, DateTime startTime, string? reasonForVisit = null)
    {
        AppointmentId = Guid.NewGuid();
        PatientId = patientId;
        DoctorClinicId = doctorClinicId;
        StartTime = startTime;
        ReasonForVisit = reasonForVisit;
        Status = AppointmentStatus.Scheduled;
        CreatedAt = DateTime.UtcNow;
    }

    public void ChangeStatus(AppointmentStatus newStatus)
    {
        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateAppointment(DateTime startTime, string? reasonForVisit)
    {
        StartTime = startTime;
        ReasonForVisit = reasonForVisit;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        Status = AppointmentStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }
}