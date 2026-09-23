using Microsoft.EntityFrameworkCore;
using Healthcare_API.Models;
using Healthcare_API.Models.Enums;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace Healthcare_API_V0._2_Infrastructure.Persistence
{
    /// <summary>
    /// DbContext for Healthcare application.
    /// Manages both ASP.NET Core Identity (ApplicationUser) and Domain entities.
    /// Relationships between Identity and Domain are configured via Fluent API without navigation properties.
    /// </summary>
    public class HealthcareDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
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
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ===== Patient Configuration =====
            modelBuilder.Entity<Patient>(entity =>
            {
                entity.HasKey(x => x.PatientId);

                // One-to-One relationship with ApplicationUser (via primitive FK only)
                entity.HasOne<ApplicationUser>()
                    .WithOne()
                    .HasForeignKey<Patient>(x => x.ApplicationUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(x => x.Appointments)
                    .WithOne(x => x.Patient)
                    .HasForeignKey(x => x.PatientId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(x => x.ApplicationUserId)
                    .IsUnique();
            });

            // ===== Doctor Configuration =====
            modelBuilder.Entity<Doctor>(entity =>
            {
                entity.HasKey(x => x.DoctorId);

                // One-to-One relationship with ApplicationUser (via primitive FK only)
                entity.HasOne<ApplicationUser>()
                    .WithOne()
                    .HasForeignKey<Doctor>(x => x.ApplicationUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Specialization)
                    .WithMany(x => x.Doctors)
                    .HasForeignKey(x => x.SpecializationId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(x => x.DoctorClinics)
                    .WithOne(x => x.Doctor)
                    .HasForeignKey(x => x.DoctorId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => x.ApplicationUserId)
                    .IsUnique();

                entity.HasIndex(x => x.Status);
                entity.HasIndex(x => x.SpecializationId);

                entity.Property(x => x.Status)
                    .HasConversion<int>();
            });

            // ===== Specialization Configuration =====
            modelBuilder.Entity<Specialization>(entity =>
            {
                entity.HasKey(x => x.SpecializationId);

                entity.HasMany(x => x.Doctors)
                    .WithOne(x => x.Specialization)
                    .HasForeignKey(x => x.SpecializationId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(x => x.Name)
                    .IsUnique();
            });

            // ===== Clinic Configuration =====
            modelBuilder.Entity<Clinic>(entity =>
            {
                entity.HasKey(x => x.ClinicId);

                entity.HasMany(x => x.DoctorClinics)
                    .WithOne(x => x.Clinic)
                    .HasForeignKey(x => x.ClinicId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => x.City);
                entity.HasIndex(x => x.Name);
            });

            // ===== DoctorClinic Configuration (Junction Table) =====
            modelBuilder.Entity<DoctorClinic>(entity =>
            {
                entity.HasKey(x => x.DoctorClinicId);

                entity.HasOne(x => x.Doctor)
                    .WithMany(x => x.DoctorClinics)
                    .HasForeignKey(x => x.DoctorId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.Clinic)
                    .WithMany(x => x.DoctorClinics)
                    .HasForeignKey(x => x.ClinicId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(x => x.Availabilities)
                    .WithOne(x => x.DoctorClinic)
                    .HasForeignKey(x => x.DoctorClinicId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(x => x.Appointments)
                    .WithOne(x => x.DoctorClinic)
                    .HasForeignKey(x => x.DoctorClinicId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(x => new { x.DoctorId, x.ClinicId })
                    .IsUnique();

                entity.Property(x => x.ConsultationFee)
                    .HasPrecision(10, 2);
            });

            // ===== DoctorAvailability Configuration =====
            modelBuilder.Entity<DoctorAvailability>(entity =>
            {
                entity.HasKey(x => x.AvailabilityId);

                entity.HasOne(x => x.DoctorClinic)
                    .WithMany(x => x.Availabilities)
                    .HasForeignKey(x => x.DoctorClinicId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => new { x.DoctorClinicId, x.DayOfWeek });
                entity.HasIndex(x => x.IsActive);

                entity.Property(x => x.DayOfWeek)
                    .HasConversion<int>();
            });

            // ===== Appointment Configuration =====
            modelBuilder.Entity<Appointment>(entity =>
            {
                entity.HasKey(x => x.AppointmentId);

                entity.HasOne(x => x.Patient)
                    .WithMany(x => x.Appointments)
                    .HasForeignKey(x => x.PatientId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.DoctorClinic)
                    .WithMany(x => x.Appointments)
                    .HasForeignKey(x => x.DoctorClinicId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(x => x.PatientId);
                entity.HasIndex(x => x.DoctorClinicId);
                entity.HasIndex(x => x.StartTime);
                entity.HasIndex(x => x.Status);

                entity.Property(x => x.Status)
                    .HasConversion<int>();
            });

            // ===== RefreshToken Configuration =====
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(x => x.RefreshTokenId);

                // Many-to-One relationship with ApplicationUser (via primitive FK only).
                // Cascade so a deleted user does not leave orphaned session records.
                entity.HasOne<ApplicationUser>()
                    .WithMany()
                    .HasForeignKey(x => x.ApplicationUserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(x => x.TokenHash)
                    .IsRequired();

                entity.HasIndex(x => x.TokenHash)
                    .IsUnique();

                entity.HasIndex(x => x.ApplicationUserId);
            });
        }
    }
}

