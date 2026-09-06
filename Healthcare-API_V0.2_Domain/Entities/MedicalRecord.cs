using System;
using System.Collections.Generic;

namespace Healthcare_API.Models;

public partial class MedicalRecord
{
    public long RecordID { get; private set; }
    public long PatientID { get; private set; }
    public long DoctorID { get; private set; }
    public DateOnly VisitDate { get; private set; }
    public string Diagnosis { get; private set; }
    public string Treatment { get; private set; }
    public string Prescription { get; private set; }
    public DateTime? CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public virtual Doctor Doctor { get; private set; }
    public virtual Patient Patient { get; private set; }

    public MedicalRecord(long patientID, long doctorID, DateOnly visitDate, string diagnosis, string treatment, string prescription)
    {
        PatientID = patientID;
        DoctorID = doctorID;
        VisitDate = visitDate;
        Diagnosis = diagnosis;
        Treatment = treatment;
        Prescription = prescription;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateMedicalRecord(string diagnosis, string treatment, string prescription)
    {
        Diagnosis = diagnosis;
        Treatment = treatment;
        Prescription = prescription;
        UpdatedAt = DateTime.UtcNow;
    }
}