using Healthcare_API_V0._2_Application.DTOs.Auth;

namespace Healthcare_API_V0._2_Application.Interfaces.Services;

public interface IAuthService
{
    Task<RegisterResponse> RegisterPatientAsync(PatientRegisterRequest request, CancellationToken cancellationToken = default);
    Task<RegisterResponse> RegisterDoctorAsync(DoctorRegisterRequest request, CancellationToken cancellationToken = default);
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}
