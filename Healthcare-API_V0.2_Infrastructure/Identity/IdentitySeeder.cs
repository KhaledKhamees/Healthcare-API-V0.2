using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Healthcare_API_V0._2_Infrastructure.Identity
{
    public static class IdentitySeeder
    {
        public static async Task SeedRolesAsync(RoleManager<IdentityRole<Guid>> rolemanager)
        {
            string[] roles = { 
                "Doctor",
                "Patient"
            };
            foreach (string role in roles)
            {
                if (!await rolemanager.RoleExistsAsync(role)) {
                    await rolemanager.CreateAsync(new IdentityRole<Guid>(role));
                }
            }
        }
    }
}
