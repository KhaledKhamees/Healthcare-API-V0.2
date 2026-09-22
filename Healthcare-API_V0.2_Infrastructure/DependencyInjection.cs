using Healthcare_API.Models;
using Healthcare_API_V0._2_Application.Interfaces;
using Healthcare_API_V0._2_Application.Interfaces.Repository;
using Healthcare_API_V0._2_Application.Interfaces.Services;
using Healthcare_API_V0._2_Application.Options;
using Healthcare_API_V0._2_Application.Services;
using Healthcare_API_V0._2_Infrastructure.Caching;
using Healthcare_API_V0._2_Infrastructure.Identity;
using Healthcare_API_V0._2_Infrastructure.Persistence;
using Healthcare_API_V0._2_Infrastructure.Persistence;
using Healthcare_API_V0._2_Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

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

            // JWT Bearer authentication (Phase 5: authentication only, no authorization).
            // Reuses the Jwt section bound to JwtOptions; JwtOptions validation
            // (ValidateOnStart) guarantees these values before the app serves traffic.
            var jwtSection = configuration.GetSection(JwtOptions.SectionName);
            var jwtIssuer = jwtSection["Issuer"];
            var jwtAudience = jwtSection["Audience"];
            var jwtSecretKey = jwtSection["SecretKey"];
            if (string.IsNullOrWhiteSpace(jwtIssuer)
                || string.IsNullOrWhiteSpace(jwtAudience)
                || string.IsNullOrWhiteSpace(jwtSecretKey))
            {
                throw new InvalidOperationException("JWT configuration (Jwt:Issuer, Jwt:Audience, Jwt:SecretKey) is missing.");
            }

            services.AddAuthentication(options =>
            {
                // Override the cookie defaults registered by AddIdentity so that
                // JWT Bearer becomes the application's authentication scheme.
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                // Preserve the Phase 4 payload (sub/email/role) exactly as issued.
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = jwtAudience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
                    RoleClaimType = "role",
                    NameClaimType = JwtRegisteredClaimNames.Email
                };
            });

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
            services.AddScoped<IJwtService, JwtService>();

            // Bind and validate JWT options at startup. Production must override
            // Jwt:SecretKey via environment variable (Jwt__SecretKey) or User Secrets.
            services.AddOptions<JwtOptions>()
                .Bind(configuration.GetSection(JwtOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();
            services.AddSingleton<IConnectionMultiplexer>(
                ConnectionMultiplexer.Connect(
                    configuration.GetConnectionString("Redis")!));
            services.AddScoped<ICacheService, CacheService>();

            return services;
        }
    }
}

