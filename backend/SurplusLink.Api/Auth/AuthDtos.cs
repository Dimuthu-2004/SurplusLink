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

    [MarketplaceRoles]
    public string[] Roles { get; init; } = [];
}

public sealed class MarketplaceRolesAttribute : ValidationAttribute
{
    public MarketplaceRolesAttribute() : base("Select SELLER, BUYER, or both, without duplicates.") { }

    public override bool IsValid(object? value) => value is string[] roles
        && roles.Length is >= 1 and <= 2
        && roles.All(role => role is "SELLER" or "BUYER")
        && roles.Distinct(StringComparer.Ordinal).Count() == roles.Length;
}

public sealed class LoginRequest
{
    [Required, EmailAddress, MaxLength(320)]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}

public sealed record UserResponse(Guid Id, string Email, string[] Roles,
    string? FullName = null, string? PhoneNumber = null, string? BusinessName = null, string? Address = null)
{
    public static UserResponse From(Models.User user) => new(user.Id, user.Email, user.RoleAssignments.OrderBy(x => x.Role).Select(x => x.Role.ToString()).ToArray(),
        user.FullName, user.PhoneNumber, user.BusinessName, user.Address);
}

public sealed record AuthResponse(string Token, UserResponse User);
