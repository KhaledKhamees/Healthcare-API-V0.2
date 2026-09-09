using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity;

namespace Healthcare_API.Models;

/// <summary>
/// ASP.NET Core Identity user. Kept in Infrastructure layer.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
}
