
using System;
using System.Collections.Generic;

namespace Healthcare_API.Models;

public class Specialization
{
    public Guid SpecializationId { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Navigation
    public virtual ICollection<Doctor> Doctors { get; private set; } = new List<Doctor>();

    public Specialization(string name)
    {
        SpecializationId = Guid.NewGuid();
        Name = name;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateSpecialization(string name)
    {
        Name = name;
        UpdatedAt = DateTime.UtcNow;
    }
}