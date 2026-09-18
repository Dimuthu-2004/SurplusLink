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

    public UserRole Role { get; set; }

    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? BusinessName { get; set; }
    public string? Address { get; set; }

}
