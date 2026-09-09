using Healthcare_API_V0._2_Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Healthcare_API.Models;
using Healthcare_API_V0._2_Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace Healthcare_API_V0._2_Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Add DbContext with SQL Server
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            services.AddDbContext<HealthcareDbContext>(options =>
                options.UseSqlServer(connectionString)
            );

            // Add ASP.NET Core Identity with Guid
            services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                // Configure identity options as needed
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 8;
            })
            .AddEntityFrameworkStores<HealthcareDbContext>()
            .AddDefaultTokenProviders();

            return services;
        }
    }
}

