using System;
using System.Collections.Generic;

namespace Healthcare_API.Models;

public class DoctorClinic
{
    public Guid DoctorClinicId { get; private set; } = Guid.NewGuid();
    public Guid DoctorId { get; private set; }
    public Guid ClinicId { get; private set; }
    public decimal ConsultationFee { get; private set; }
    public int AppointmentDurationMinutes { get; private set; } = 30;

    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Navigation
    public virtual Doctor Doctor { get; private set; }
    public virtual Clinic Clinic { get; private set; }
    public virtual ICollection<DoctorAvailability> Availabilities { get; private set; } = new List<DoctorAvailability>();
    public virtual ICollection<Appointment> Appointments { get; private set; } = new List<Appointment>();

    public DoctorClinic(Guid doctorId, Guid clinicId, decimal consultationFee, int appointmentDurationMinutes = 30)
    {
        DoctorClinicId = Guid.NewGuid();
        DoctorId = doctorId;
        ClinicId = clinicId;
        ConsultationFee = consultationFee;
        AppointmentDurationMinutes = appointmentDurationMinutes;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateConsultationFee(decimal newFee)
    {
        ConsultationFee = newFee;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateAppointmentDuration(int durationMinutes)
    {
        AppointmentDurationMinutes = durationMinutes;
        UpdatedAt = DateTime.UtcNow;
    }
}
