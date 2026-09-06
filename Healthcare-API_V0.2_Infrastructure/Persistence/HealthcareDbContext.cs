using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Healthcare_API.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace Healthcare_API_V0._2_Infrastructure.Persistence
{
    internal class HealthcareDbContext : IdentityDbContext<ApplicationUser>
    {
        public HealthcareDbContext(DbContextOptions<HealthcareDbContext> options) : base(options)
        {
        }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<MedicalRecord> MedicalRecords { get; set; }
        public DbSet<Billing> Billings { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Specialization> Specializations { get; set; }
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<Message> Messages { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Patient>(entity =>
            {
                entity.HasKey(x => x.PatientID)
                    .HasName("PK_PatientID");
                entity.HasIndex(x => x.Email)
                    .IsUnique()
                    .HasDatabaseName("UQ_Patient_Email");
                entity.HasIndex(x => x.ContactNumber)
                    .HasDatabaseName("IX_Patient_ContactNumber");
            });

            modelBuilder.Entity<Doctor>(entity =>
            {
                entity.HasKey(x => x.DoctorID)
                    .HasName("PK_DoctorID");
                entity.HasIndex(x => x.Email)
                    .IsUnique()
                    .HasDatabaseName("UQ_Doctor_Email");
                entity.HasIndex(x => x.SpecializationID)
                    .HasDatabaseName("IX_Doctor_SpecializationID");
                entity.HasIndex(x => x.DepartmentID)
                    .HasDatabaseName("IX_Doctor_DepartmentID");
                entity.HasIndex(x => x.ContactNumber)
                    .HasDatabaseName("IX_Doctor_ContactNumber");
            });

            modelBuilder.Entity<Appointment>(entity =>
            {
                entity.HasKey(x => x.AppointmentID)
                    .HasName("PK_AppointmentID");
                entity.HasIndex(x => x.PatientID)
                    .HasDatabaseName("IX_Appointment_PatientID");
                entity.HasIndex(x => x.DoctorID)
                    .HasDatabaseName("IX_Appointment_DoctorID");
                entity.HasIndex(x => new { x.AppointmentDate, x.AppointmentTime })
                    .HasDatabaseName("IX_Appointment_DateTime");
                entity.HasIndex(x => x.Status)
                    .HasDatabaseName("IX_Appointment_Status");
            });

            modelBuilder.Entity<MedicalRecord>(entity =>
            {
                entity.HasKey(x => x.RecordID)
                    .HasName("PK_MedicalRecordID");
                entity.HasIndex(x => x.PatientID)
                    .HasDatabaseName("IX_MedicalRecord_PatientID");
                entity.HasIndex(x => x.DoctorID)
                    .HasDatabaseName("IX_MedicalRecord_DoctorID");
                entity.HasIndex(x => x.VisitDate)
                    .HasDatabaseName("IX_MedicalRecord_VisitDate");
            });

            modelBuilder.Entity<Billing>(entity =>
            {
                entity.HasKey(x => x.BillID)
                    .HasName("PK_BillID");
                entity.HasIndex(x => x.PatientID)
                    .HasDatabaseName("IX_Billing_PatientID");
                entity.HasIndex(x => x.AppointmentID)
                    .HasDatabaseName("IX_Billing_AppointmentID");
                entity.HasIndex(x => x.PaymentStatus)
                    .HasDatabaseName("IX_Billing_PaymentStatus");
                entity.HasIndex(x => x.BillDate)
                    .HasDatabaseName("IX_Billing_BillDate");
            });

            modelBuilder.Entity<Department>(entity =>
            {
                entity.HasKey(x => x.DepartmentID)
                    .HasName("PK_DepartmentID");
                entity.HasIndex(x => x.DepartmentName)
                    .IsUnique()
                    .HasDatabaseName("UQ_Department_Name");
            });

            modelBuilder.Entity<Specialization>(entity =>
            {
                entity.HasKey(x => x.SpecializationID)
                    .HasName("PK_SpecializationID");
                entity.HasIndex(x => x.Name)
                    .IsUnique()
                    .HasDatabaseName("UQ_Specialization_Name");
            });

            modelBuilder.Entity<Conversation>(entity =>
            {
                entity.HasKey(x => x.Id)
                    .HasName("PK_ConversationID");
                entity.HasIndex(x => new { x.UserAId, x.UserBId })
                    .IsUnique()
                    .HasDatabaseName("UQ_Conversation_Users");
                entity.HasIndex(x => x.CreatedAt)
                    .HasDatabaseName("IX_Conversation_CreatedAt");
            });

            modelBuilder.Entity<Message>(entity =>
            {
                entity.HasKey(x => x.Id)
                    .HasName("PK_MessageID");
                entity.HasIndex(x => x.ConversationId)
                    .HasDatabaseName("IX_Message_ConversationId");
                entity.HasIndex(x => x.SenderId)
                    .HasDatabaseName("IX_Message_SenderId");
                entity.HasIndex(x => x.ReceiverId)
                    .HasDatabaseName("IX_Message_ReceiverId");
                entity.HasIndex(x => x.SentAt)
                    .HasDatabaseName("IX_Message_SentAt");
                entity.HasIndex(x => x.ReadAt)
                    .HasDatabaseName("IX_Message_ReadAt");
            });
        }
    }
}
