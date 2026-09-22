using Healthcare_API.Models;
using Healthcare_API_V0._2_Application.DTOs.Auth;
using Healthcare_API_V0._2_Application.Interfaces.Services;
using Healthcare_API_V0._2_Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Healthcare_API_V0._2_Infrastructure.Identity;

public class AuthService : IAuthService
{
    private const string PatientRole = "Patient";
    private const string DoctorRole = "Doctor";

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly HealthcareDbContext _dbContext;
    private readonly IPatientService _patientService;
    private readonly IDoctorService _doctorService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        HealthcareDbContext dbContext,
        IPatientService patientService,
        IDoctorService doctorService,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _dbContext = dbContext;
        _patientService = patientService;
        _doctorService = doctorService;
        _logger = logger;
    }

    public async Task<RegisterResponse> RegisterPatientAsync(PatientRegisterRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var email = request.Email?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(email))
        {
            _logger.LogWarning("Patient registration failed: email is missing");
            throw new ArgumentException("Email is required.", nameof(request));
        }

        EnsurePasswordsMatch(request.Password, request.ConfirmPassword);

        _logger.LogInformation("Registering patient with email {Email}", email);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var user = await CreateUserAsync(email, request.Password, PatientRole);
            var patient = await _patientService.CreateAsync(user.Id, cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Patient {PatientId} was successfully registered", patient.PatientId);
            return new RegisterResponse
            {
                UserId = user.Id,
                Email = email,
                Role = PatientRole,
                ProfileId = patient.PatientId
            };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<RegisterResponse> RegisterDoctorAsync(DoctorRegisterRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var email = request.Email?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(email))
        {
            _logger.LogWarning("Doctor registration failed: email is missing");
            throw new ArgumentException("Email is required.", nameof(request));
        }

        EnsurePasswordsMatch(request.Password, request.ConfirmPassword);

        if (string.IsNullOrWhiteSpace(request.FirstName))
        {
            _logger.LogWarning("Doctor registration failed: first name is missing");
            throw new ArgumentException("First name is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.LastName))
        {
            _logger.LogWarning("Doctor registration failed: last name is missing");
            throw new ArgumentException("Last name is required.", nameof(request));
        }

        if (request.SpecializationId == Guid.Empty)
        {
            _logger.LogWarning("Doctor registration failed: specialization ID is empty");
            throw new ArgumentException("Specialization ID is required.", nameof(request));
        }

        _logger.LogInformation("Registering doctor with email {Email}", email);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var user = await CreateUserAsync(email, request.Password, DoctorRole);
            var doctor = await _doctorService.CreateAsync(
                user.Id,
                request.FirstName.Trim(),
                request.LastName.Trim(),
                request.SpecializationId,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Doctor {DoctorId} was successfully registered", doctor.DoctorId);
            return new RegisterResponse
            {
                UserId = user.Id,
                Email = email,
                Role = DoctorRole,
                ProfileId = doctor.DoctorId
            };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<ApplicationUser> CreateUserAsync(string email, string password, string role)
    {
        var existing = await _userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            _logger.LogWarning("Registration failed: email {Email} is already registered", email);
            throw new InvalidOperationException($"Email {email} is already registered.");
        }

        if (!await _roleManager.RoleExistsAsync(role))
        {
            _logger.LogWarning("Registration failed: role {Role} is not configured", role);
            throw new KeyNotFoundException($"Role '{role}' was not found. Ensure roles are seeded.");
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email
        };

        var createResult = await _userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
            _logger.LogWarning("Registration failed: identity user creation failed for email {Email}: {Errors}", email, errors);
            throw new ArgumentException(errors);
        }

        var roleResult = await _userManager.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
        {
            var errors = string.Join("; ", roleResult.Errors.Select(e => e.Description));
            _logger.LogWarning("Registration failed: role assignment failed for email {Email}: {Errors}", email, errors);
            throw new ArgumentException(errors);
        }

        return user;
    }

    private void EnsurePasswordsMatch(string password, string confirmPassword)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning("Registration failed: password is missing");
            throw new ArgumentException("Password is required.", nameof(password));
        }

        if (!string.Equals(password, confirmPassword))
        {
            _logger.LogWarning("Registration failed: password and confirmation do not match");
            throw new ArgumentException("Password and confirmation do not match.", nameof(confirmPassword));
        }
    }
}
