using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity;

namespace Healthcare_API.Models;

public class ApplicationUser : IdentityUser
{
    // Navigation properties to link to domain profiles
    public Doctor? Doctor { get; set; }
    public Patient? Patient { get; set; }
}
