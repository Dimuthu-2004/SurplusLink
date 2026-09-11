namespace SurplusLink.Api.Models;

public enum UserRole
{
    SELLER,
    BUYER,
    MANAGER
}

public sealed class User
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}