using System;
using System;
using System.Collections.Generic;

namespace Healthcare_API.Models;

public class Patient
{
    public long PatientID { get; private set; }

    // Link to ApplicationUser (one-to-one)
    public string ApplicationUserID { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Navigation
    public virtual ApplicationUser ApplicationUser { get; private set; }
    public virtual ICollection<Appointment> Appointments { get; private set; } = new List<Appointment>();

    public Patient(string applicationUserID)
    {
        ApplicationUserID = applicationUserID;
        CreatedAt = DateTime.UtcNow;
    }
}