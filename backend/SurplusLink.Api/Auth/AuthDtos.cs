using System.ComponentModel.DataAnnotations;

namespace SurplusLink.Api.Auth;

public class ProfileRequest
{
    [Required, MaxLength(120)]
    public string FullName { get; init; } = string.Empty;
    [Required, SriLankanPhone]
    public string PhoneNumber { get; init; } = string.Empty;
    public string? Nic { get; init; }
    [MaxLength(160)]
    public string? BusinessName { get; init; }
    [Required, MaxLength(400)]
    public string Address { get; init; } = string.Empty;
}

public sealed class RegisterRequest : ProfileRequest
{
    [Required, StrictEmail, MaxLength(320)]
    public string Email { get; init; } = string.Empty;

    [Required, StrongPassword]
    public string Password { get; init; } = string.Empty;

    [MarketplaceRoles]
    public string[] Roles { get; init; } = [];
}

public class EmailCodeRequest
{
    [Required, StrictEmail, MaxLength(320)] public string Email { get; init; } = string.Empty;
}

public class VerifyEmailRequest : EmailCodeRequest
{
    [Required, RegularExpression(@"^\d{6}$")] public string Code { get; init; } = string.Empty;
}

public sealed class ResetPasswordRequest : VerifyEmailRequest
{
    [Required, StrongPassword] public string NewPassword { get; init; } = string.Empty;
}

public sealed class StrongPasswordAttribute : ValidationAttribute
{
    public const string DefaultErrorMessage = "Password must be at least 8 characters long and contain at least one uppercase letter, one lowercase letter, one number, and one special character.";

    public StrongPasswordAttribute() : base(DefaultErrorMessage) { }

    public override bool IsValid(object? value)
    {
        if (value is not string password) return false;
        return IsStrong(password);
    }

    public static bool IsStrong(string? password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < 8 || password.Length > 128) return false;
        var hasUpper = password.Any(char.IsUpper);
        var hasLower = password.Any(char.IsLower);
        var hasDigit = password.Any(char.IsDigit);
        var hasSpecial = password.Any(ch => !char.IsLetterOrDigit(ch));
        return hasUpper && hasLower && hasDigit && hasSpecial;
    }
}

public sealed class MarketplaceRolesAttribute : ValidationAttribute
{
    public MarketplaceRolesAttribute() : base("Select SELLER, BUYER, or both, without duplicates.") { }

    public override bool IsValid(object? value) => value is string[] roles
        && roles.Length is >= 1 and <= 2
        && roles.All(role => role is "SELLER" or "BUYER")
        && roles.Distinct(StringComparer.Ordinal).Count() == roles.Length;
}

public sealed class SriLankanPhoneAttribute : ValidationAttribute
{
    public SriLankanPhoneAttribute() : base("Enter a valid Sri Lankan phone number.") { }

    public override bool IsValid(object? value) => value is not string phone ||
        string.IsNullOrWhiteSpace(phone) || SriLankanContact.TryNormalizePhone(phone, out _);
}

public sealed class StrictEmailAttribute : ValidationAttribute
{
    public const string DefaultErrorMessage = "Please provide a valid email address.";

    public StrictEmailAttribute() : base(DefaultErrorMessage) { }

    public override bool IsValid(object? value)
    {
        if (value is not string email || string.IsNullOrWhiteSpace(email)) return false;
        if (email.Contains(' ') || !email.Contains('@') || !email.Contains('.')) return false;
        var parts = email.Split('@');
        if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1])) return false;
        if (!parts[1].Contains('.') || parts[1].StartsWith('.') || parts[1].EndsWith('.')) return false;
        return true;
    }
}

public sealed class LoginRequest
{
    [Required, EmailAddress, MaxLength(320)]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}

public sealed record UserResponse(Guid Id, string Email, string[] Roles,
    string? FullName = null, string? PhoneNumber = null, string? BusinessName = null, string? Address = null, string? ProfilePhotoUrl = null)
{
    public static UserResponse From(Models.User user) => new(user.Id, user.Email, user.RoleAssignments.OrderBy(x => x.Role).Select(x => x.Role.ToString()).ToArray(),
        user.FullName, user.PhoneNumber, user.BusinessName, user.Address, user.ProfilePhotoUrl);
}

public sealed record AuthResponse(string Token, UserResponse User);
public sealed record RegistrationResponse(string Email, bool EmailVerificationRequired = true);
