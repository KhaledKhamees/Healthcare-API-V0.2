using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
namespace Healthcare_API.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
        public string? Address { get; set; }
        public string? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? NationalId { get; set; }
        public string? ProfileImageUrl { get; set; }
        public string? Role { get; set; }

        // Doctor-specific
        public string? Specialization { get; set; }
        public string? Qualifications { get; set; }
        public string? ClinicAddress { get; set; }
        public string? LicenseNumber { get; set; }
        public bool? IsApproved { get; set; }

        // Patient-specific
        public string? EmergencyContact { get; set; }
        public string? BloodType { get; set; }
        public string? InsuranceProvider { get; set; }
    }
}
