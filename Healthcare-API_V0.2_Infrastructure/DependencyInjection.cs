using Healthcare_API.Models;
using Healthcare_API_V0._2_Application.Interfaces;
using Healthcare_API_V0._2_Application.Interfaces.Repository;
using Healthcare_API_V0._2_Application.Interfaces.Services;
using Healthcare_API_V0._2_Application.Services;
using Healthcare_API_V0._2_Infrastructure.Caching;
using Healthcare_API_V0._2_Infrastructure.Identity;
using Healthcare_API_V0._2_Infrastructure.Persistence;
using Healthcare_API_V0._2_Infrastructure.Persistence;
using Healthcare_API_V0._2_Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

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

            services.AddScoped<IPatientRepository, PatientRepository>();
            services.AddScoped<IDoctorRepository, DoctorRepository>();
            services.AddScoped<IClinicRepository, ClinicRepository>();
            services.AddScoped<ISpecializationRepository, SpecializationRepository>();
            services.AddScoped<IDoctorClinicRepository, DoctorClinicRepository>();
            services.AddScoped<IDoctorAvailabilityRepository, DoctorAvailabilityRepository>();
            services.AddScoped<IAppointmentRepository, AppointmentRepository>();

            services.AddScoped<IPatientService, PatientService>();
            services.AddScoped<IDoctorService, DoctorService>();
            services.AddScoped<IClinicService, ClinicService>();
            services.AddScoped<ISpecializationService, SpecializationService>();
            services.AddScoped<IDoctorClinicService, DoctorClinicService>();
            services.AddScoped<IDoctorAvailabilityService, DoctorAvailabilityService>();
            services.AddScoped<IAppointmentService, AppointmentService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddSingleton<IConnectionMultiplexer>(
                ConnectionMultiplexer.Connect(
                    configuration.GetConnectionString("Redis")!));
            services.AddScoped<ICacheService, CacheService>();

            return services;
        }
    }
}

