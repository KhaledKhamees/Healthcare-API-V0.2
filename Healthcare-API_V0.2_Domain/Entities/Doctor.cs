using System;
using System.Collections.Generic;

namespace Healthcare_API.Models;

public partial class Doctor
{
    public long DoctorID { get;private set; }

    public string FirstName { get;private set; }

    public string LastName { get;private set; }

    public int? SpecializationID { get;private set; }   
    public string ContactNumber { get;private set; }

    public string Email { get;private set; }

    public long DepartmentID { get;private set; }   
    public DateOnly HireDate { get;private set; }

    public DateTime? CreatedAt { get;private set; }

    public DateTime? UpdatedAt { get;private set; }
    public virtual ICollection<Appointment> Appointments { get;private set; } = new List<Appointment>();

    public virtual Department Department { get;private set; }

    public virtual ICollection<MedicalRecord> MedicalRecords { get;private set; } = new List<MedicalRecord>();

    public virtual Specialization Specialization { get;private set; }
    public Doctor(string firstName, string lastName, int? specializationID, string contactNumber, string email, long departmentID, DateOnly hireDate)
    {
        FirstName = firstName;
        LastName = lastName;
        SpecializationID = specializationID;
        ContactNumber = contactNumber;
        Email = email;
        DepartmentID = departmentID;
        HireDate = hireDate;
        CreatedAt = DateTime.UtcNow;
    }
    public void UpdateDoctor(string firstName, string lastName, int? specializationID, string contactNumber, string email, long departmentID, DateOnly hireDate)
    {
        FirstName = firstName;
        LastName = lastName;
        SpecializationID = specializationID;
        ContactNumber = contactNumber;
        Email = email;
        DepartmentID = departmentID;
        HireDate = hireDate;
        UpdatedAt = DateTime.UtcNow;
    }
}