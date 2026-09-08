using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Healthcare_API.Models;
using Healthcare_API.Models.Enums;
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
        public DbSet<Clinic> Clinics { get; set; }
        public DbSet<DoctorClinic> DoctorClinics { get; set; }
        public DbSet<DoctorAvailability> DoctorAvailabilities { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<Specialization> Specializations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ===== Patient Configuration =====
            modelBuilder.Entity<Patient>(entity =>
            {
                entity.HasKey(x => x.PatientID);

                entity.HasOne(x => x.ApplicationUser)
                    .WithOne(x => x.Patient)
                    .HasForeignKey<Patient>(x => x.ApplicationUserID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(x => x.Appointments)
                    .WithOne(x => x.Patient)
                    .HasForeignKey(x => x.PatientID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(x => x.ApplicationUserID)
                    .IsUnique();
            });

            // ===== Doctor Configuration =====
            modelBuilder.Entity<Doctor>(entity =>
            {
                entity.HasKey(x => x.DoctorID);

                entity.HasOne(x => x.ApplicationUser)
                    .WithOne(x => x.Doctor)
                    .HasForeignKey<Doctor>(x => x.ApplicationUserID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Specialization)
                    .WithMany(x => x.Doctors)
                    .HasForeignKey(x => x.SpecializationID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(x => x.DoctorClinics)
                    .WithOne(x => x.Doctor)
                    .HasForeignKey(x => x.DoctorID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => x.ApplicationUserID)
                    .IsUnique();

                entity.HasIndex(x => x.Status);
                entity.HasIndex(x => x.SpecializationID);

                entity.Property(x => x.Status)
                    .HasConversion<int>();
            });

            // ===== Specialization Configuration =====
            modelBuilder.Entity<Specialization>(entity =>
            {
                entity.HasKey(x => x.SpecializationID);

                entity.HasMany(x => x.Doctors)
                    .WithOne(x => x.Specialization)
                    .HasForeignKey(x => x.SpecializationID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(x => x.Name)
                    .IsUnique();
            });

            // ===== Clinic Configuration =====
            modelBuilder.Entity<Clinic>(entity =>
            {
                entity.HasKey(x => x.ClinicID);

                entity.HasMany(x => x.DoctorClinics)
                    .WithOne(x => x.Clinic)
                    .HasForeignKey(x => x.ClinicID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => x.City);
                entity.HasIndex(x => x.Name);
            });

            // ===== DoctorClinic Configuration (Junction Table) =====
            modelBuilder.Entity<DoctorClinic>(entity =>
            {
                entity.HasKey(x => x.DoctorClinicID);

                entity.HasOne(x => x.Doctor)
                    .WithMany(x => x.DoctorClinics)
                    .HasForeignKey(x => x.DoctorID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.Clinic)
                    .WithMany(x => x.DoctorClinics)
                    .HasForeignKey(x => x.ClinicID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(x => x.Availabilities)
                    .WithOne(x => x.DoctorClinic)
                    .HasForeignKey(x => x.DoctorClinicID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(x => x.Appointments)
                    .WithOne(x => x.DoctorClinic)
                    .HasForeignKey(x => x.DoctorClinicID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(x => new { x.DoctorID, x.ClinicID })
                    .IsUnique();

                entity.Property(x => x.ConsultationFee)
                    .HasPrecision(10, 2);
            });

            // ===== DoctorAvailability Configuration =====
            modelBuilder.Entity<DoctorAvailability>(entity =>
            {
                entity.HasKey(x => x.AvailabilityID);

                entity.HasOne(x => x.DoctorClinic)
                    .WithMany(x => x.Availabilities)
                    .HasForeignKey(x => x.DoctorClinicID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => new { x.DoctorClinicID, x.DayOfWeek });
                entity.HasIndex(x => x.IsActive);

                entity.Property(x => x.DayOfWeek)
                    .HasConversion<int>();
            });

            // ===== Appointment Configuration =====
            modelBuilder.Entity<Appointment>(entity =>
            {
                entity.HasKey(x => x.AppointmentID);

                entity.HasOne(x => x.Patient)
                    .WithMany(x => x.Appointments)
                    .HasForeignKey(x => x.PatientID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.DoctorClinic)
                    .WithMany(x => x.Appointments)
                    .HasForeignKey(x => x.DoctorClinicID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(x => x.PatientID);
                entity.HasIndex(x => x.DoctorClinicID);
                entity.HasIndex(x => x.StartTime);
                entity.HasIndex(x => x.Status);

                entity.Property(x => x.Status)
                    .HasConversion<int>();
            });
        }
    }
}
