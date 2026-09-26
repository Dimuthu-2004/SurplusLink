namespace SurplusLink.Api.Models;

public enum UserRole
{
    SELLER,
    BUYER,
    MANAGER
}

public sealed class User : AuditableEntity
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    // Historical users are marked verified by the migration; new accounts are not.
    public bool EmailVerified { get; set; }
    public string? Nic { get; set; }
    public string? EmailVerificationCodeHash { get; set; }
    public DateTime? EmailVerificationExpiresAtUtc { get; set; }
    public DateTime? EmailVerificationLastSentAtUtc { get; set; }
    public int EmailVerificationAttempts { get; set; }
    public string? PasswordResetCodeHash { get; set; }
    public DateTime? PasswordResetExpiresAtUtc { get; set; }
    public DateTime? PasswordResetLastSentAtUtc { get; set; }
    public int PasswordResetAttempts { get; set; }

    public ICollection<UserRoleAssignment> RoleAssignments { get; set; } = new List<UserRoleAssignment>();

    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? BusinessName { get; set; }
    public string? Address { get; set; }

}

public sealed class UserRoleAssignment
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public UserRole Role { get; set; }
}
