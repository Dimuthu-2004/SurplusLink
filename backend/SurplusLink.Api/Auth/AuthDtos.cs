using System.ComponentModel.DataAnnotations;

namespace SurplusLink.Api.Auth;

public class ProfileRequest
{
    [Required, MaxLength(120)]
    public string FullName { get; init; } = string.Empty;
    [Required, RegularExpression(@"^\+?[0-9][0-9 ()-]{6,24}$", ErrorMessage = "Enter a valid phone number.")]
    public string PhoneNumber { get; init; } = string.Empty;
    [MaxLength(160)]
    public string? BusinessName { get; init; }
    [Required, MaxLength(400)]
    public string Address { get; init; } = string.Empty;
}

public sealed class RegisterRequest : ProfileRequest
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

public sealed record UserResponse(Guid Id, string Email, string Role,
    string? FullName = null, string? PhoneNumber = null, string? BusinessName = null, string? Address = null)
{
    public static UserResponse From(Models.User user) => new(user.Id, user.Email, user.Role.ToString(),
        user.FullName, user.PhoneNumber, user.BusinessName, user.Address);
}

public sealed record AuthResponse(string Token, UserResponse User);
