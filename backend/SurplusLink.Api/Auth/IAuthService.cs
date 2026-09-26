namespace SurplusLink.Api.Auth;

public interface IAuthService
{
    Task<(RegistrationResponse? Response, string? ErrorCode)> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);

    Task<(AuthResponse? Response, string? ErrorCode)> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<string?> SendEmailVerificationAsync(string email, CancellationToken cancellationToken);
    Task<string?> VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken);
    Task<string?> ForgotPasswordAsync(string email, CancellationToken cancellationToken);
    Task<string?> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken);
}
