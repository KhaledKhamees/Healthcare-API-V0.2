using System;
using System;
using System.Collections.Generic;

namespace Healthcare_API.Models;

public class Clinic
{
    public long ClinicID { get; private set; }
    public string Name { get; private set; }
    public string Address { get; private set; }
    public string City { get; private set; }
    public string? PhoneNumber { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Navigation: appointments are accessed through DoctorClinic
    public virtual ICollection<DoctorClinic> DoctorClinics { get; private set; } = new List<DoctorClinic>();

    public Clinic(string name, string address, string city, string? phoneNumber = null)
    {
        Name = name;
        Address = address;
        City = city;
        PhoneNumber = phoneNumber;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateClinic(string name, string address, string city, string? phoneNumber = null)
    {
        Name = name;
        Address = address;
        City = city;
        PhoneNumber = phoneNumber;
        UpdatedAt = DateTime.UtcNow;
    }
}
