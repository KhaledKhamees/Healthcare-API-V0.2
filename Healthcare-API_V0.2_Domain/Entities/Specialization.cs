
using System;
using System.Collections.Generic;

namespace Healthcare_API.Models;

public partial class Specialization
{
    public int SpecializationID { get; private set; }
    public string Name { get; private set; }
    public virtual ICollection<Doctor> Doctors { get; private set; } = new List<Doctor>();

    public Specialization(string name)
    {
        Name = name;
    }

    public void UpdateSpecialization(string name)
    {
        Name = name;
    }
}