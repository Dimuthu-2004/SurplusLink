namespace SurplusLink.Api.Auth;

public interface IAuthService
{
    Task<(AuthResponse? Response, bool EmailExists)> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);

    Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
}