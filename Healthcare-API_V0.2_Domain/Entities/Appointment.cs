using System;
using System.Collections.Generic;

namespace Healthcare_API.Models;

public partial class Appointment
{
    public long AppointmentID { get; private set; }
    public long PatientID { get; private set; }
    public long DoctorID { get; private set; }
    public DateOnly AppointmentDate { get; private set; }
    public TimeOnly AppointmentTime { get; private set; }
    public string ReasonForVisit { get; private set; }
    public string Status { get; private set; }
    public DateTime? CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public virtual ICollection<Billing> Billings { get; private set; } = new List<Billing>();
    public virtual Doctor Doctor { get; private set; }
    public virtual Patient Patient { get; private set; }

    public Appointment(long patientID, long doctorID, DateOnly appointmentDate, TimeOnly appointmentTime, string reasonForVisit, string status = "Scheduled")
    {
        PatientID = patientID;
        DoctorID = doctorID;
        AppointmentDate = appointmentDate;
        AppointmentTime = appointmentTime;
        ReasonForVisit = reasonForVisit;
        Status = status;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateAppointment(DateOnly appointmentDate, TimeOnly appointmentTime, string reasonForVisit, string status)
    {
        AppointmentDate = appointmentDate;
        AppointmentTime = appointmentTime;
        ReasonForVisit = reasonForVisit;
        Status = status;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangeStatus(string newStatus)
    {
        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }
}