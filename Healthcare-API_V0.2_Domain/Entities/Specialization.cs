
using System;
using System.Collections.Generic;

namespace Healthcare_API.Models;

public class Specialization
{
    public int SpecializationID { get; private set; }
    public string Name { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Navigation
    public virtual ICollection<Doctor> Doctors { get; private set; } = new List<Doctor>();

    public Specialization(string name)
    {
        Name = name;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateSpecialization(string name)
    {
        Name = name;
        UpdatedAt = DateTime.UtcNow;
    }
}