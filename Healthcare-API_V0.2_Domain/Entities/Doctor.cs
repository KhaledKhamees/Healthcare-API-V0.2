using System;
using System;
using System.Collections.Generic;
using Healthcare_API.Models.Enums;

namespace Healthcare_API.Models;

public class Doctor
{
    public long DoctorID { get; private set; }

    // Link to ApplicationUser (one-to-one)
    public string ApplicationUserID { get; private set; }

    // Professional information (source of truth for doctor's name)
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string? LicenseNumber { get; private set; }
    public string? Bio { get; private set; }

    // Status for the platform
    public DoctorStatus Status { get; private set; } = DoctorStatus.Pending;

    // Specialization
    public int SpecializationID { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Navigation: appointments are accessed through DoctorClinic
    public virtual ApplicationUser ApplicationUser { get; private set; }
    public virtual Specialization Specialization { get; private set; }
    public virtual ICollection<DoctorClinic> DoctorClinics { get; private set; } = new List<DoctorClinic>();

    public Doctor(string applicationUserID, string firstName, string lastName, int specializationID)
    {
        ApplicationUserID = applicationUserID;
        FirstName = firstName;
        LastName = lastName;
        SpecializationID = specializationID;
        Status = DoctorStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateDoctor(string firstName, string lastName, string? licenseNumber, string? bio)
    {
        FirstName = firstName;
        LastName = lastName;
        LicenseNumber = licenseNumber;
        Bio = bio;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangeStatus(DoctorStatus newStatus)
    {
        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }
}