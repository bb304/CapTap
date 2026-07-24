using CapTap.Application.DTOs.Auth;

namespace CapTap.Application.Interfaces;

public interface IAuthService
{
    Task<RegisterResponse> RegisterAsync(RegisterRequest request, string? ipAddress, CancellationToken cancellationToken = default);

    Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken = default);

    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, string? ipAddress, CancellationToken cancellationToken = default);

    Task LogoutAsync(RefreshTokenRequest request, string? ipAddress, CancellationToken cancellationToken = default);

    Task ForgotPasswordAsync(ForgotPasswordRequest request, string? ipAddress, CancellationToken cancellationToken = default);

    Task ResetPasswordAsync(ResetPasswordRequest request, string? ipAddress, CancellationToken cancellationToken = default);

    Task VerifyEmailAsync(VerifyEmailRequest request, string? ipAddress, CancellationToken cancellationToken = default);
}
