
using System;
using System.Collections.Generic;

namespace Healthcare_API.Models;

public partial class Billing
{
    public long BillID { get; private set; }
    public long PatientID { get; private set; }
    public long AppointmentID { get; private set; }
    public decimal Amount { get; private set; }
    public DateOnly BillDate { get; private set; }
    public string PaymentStatus { get; private set; }
    public DateTime? CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public virtual Appointment Appointment { get; private set; }
    public virtual Patient Patient { get; private set; }

    public Billing(long patientID, long appointmentID, decimal amount, DateOnly billDate, string paymentStatus = "Pending")
    {
        PatientID = patientID;
        AppointmentID = appointmentID;
        Amount = amount;
        BillDate = billDate;
        PaymentStatus = paymentStatus;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateBillingAmount(decimal amount)
    {
        Amount = amount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangePaymentStatus(string newPaymentStatus)
    {
        PaymentStatus = newPaymentStatus;
        UpdatedAt = DateTime.UtcNow;
    }
}