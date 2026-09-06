using System;
using System.Collections.Generic;

namespace Healthcare_API.Models;

public partial class Patient
{
    public long PatientID { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public DateOnly DOB { get; private set; }
    public string Gender { get; private set; }
    public string ContactNumber { get; private set; }
    public string Address { get; private set; }
    public string Email { get; private set; }
    public string EmergencyContact { get; private set; }
    public string MedicalHistory { get; private set; }
    public DateTime? CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public virtual ICollection<Appointment> Appointments { get; private set; } = new List<Appointment>();
    public virtual ICollection<Billing> Billings { get; private set; } = new List<Billing>();
    public virtual ICollection<MedicalRecord> MedicalRecords { get; private set; } = new List<MedicalRecord>();

    public Patient(string firstName, string lastName, DateOnly dob, string gender, string contactNumber, string address, string email, string emergencyContact, string medicalHistory)
    {
        FirstName = firstName;
        LastName = lastName;
        DOB = dob;
        Gender = gender;
        ContactNumber = contactNumber;
        Address = address;
        Email = email;
        EmergencyContact = emergencyContact;
        MedicalHistory = medicalHistory;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdatePatient(string firstName, string lastName, string gender, string contactNumber, string address, string email, string emergencyContact, string medicalHistory)
    {
        FirstName = firstName;
        LastName = lastName;
        Gender = gender;
        ContactNumber = contactNumber;
        Address = address;
        Email = email;
        EmergencyContact = emergencyContact;
        MedicalHistory = medicalHistory;
        UpdatedAt = DateTime.UtcNow;
    }
}