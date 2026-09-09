using System;
using System;
using System.Collections.Generic;

namespace Healthcare_API.Models;

public class Patient
{
    public Guid PatientId { get; private set; } = Guid.NewGuid();

    // Link to ApplicationUser (Identity ID as primitive) - no navigation to ApplicationUser
    public Guid ApplicationUserId { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Navigation
    public virtual ICollection<Appointment> Appointments { get; private set; } = new List<Appointment>();

    public Patient(Guid applicationUserId)
    {
        PatientId = Guid.NewGuid();
        ApplicationUserId = applicationUserId;
        CreatedAt = DateTime.UtcNow;
    }
}