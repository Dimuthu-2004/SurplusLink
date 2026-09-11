using System.ComponentModel.DataAnnotations;

namespace SurplusLink.Api.Auth;

public sealed class RegisterRequest
{
    [Required, EmailAddress, MaxLength(320)]
    public string Email { get; init; } = string.Empty;

    [Required, MinLength(8), MaxLength(128)]
    public string Password { get; init; } = string.Empty;

    [Required]
    [RegularExpression("^(SELLER|BUYER)$", ErrorMessage = "Role must be SELLER or BUYER.")]
    public string Role { get; init; } = string.Empty;
}

public sealed class LoginRequest
{
    [Required, EmailAddress, MaxLength(320)]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}

public sealed record UserResponse(Guid Id, string Email, string Role);

public sealed record AuthResponse(string Token, UserResponse User);