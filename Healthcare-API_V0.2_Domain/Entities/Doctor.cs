using System;
using System;
using System.Collections.Generic;
using Healthcare_API.Models.Enums;

namespace Healthcare_API.Models;

public class Doctor
{
    public Guid DoctorId { get; private set; } = Guid.NewGuid();

    // Link to ApplicationUser (Identity ID as primitive) - no navigation to ApplicationUser
    public Guid ApplicationUserId { get; private set; }

    // Professional information (source of truth for doctor's name)
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string? LicenseNumber { get; private set; }
    public string? Bio { get; private set; }

    // Status for the platform
    public DoctorStatus Status { get; private set; } = DoctorStatus.Pending;

    // Specialization
    public Guid SpecializationId { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Navigation: appointments are accessed through DoctorClinic
    public virtual Specialization Specialization { get; private set; }
    public virtual ICollection<DoctorClinic> DoctorClinics { get; private set; } = new List<DoctorClinic>();

    public Doctor(Guid applicationUserId, string firstName, string lastName, Guid specializationId)
    {
        DoctorId = Guid.NewGuid();
        ApplicationUserId = applicationUserId;
        FirstName = firstName;
        LastName = lastName;
        SpecializationId = specializationId;
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