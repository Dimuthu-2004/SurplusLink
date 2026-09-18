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
