using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SurplusLink.Api.Auth;

namespace SurplusLink.Tests;

public sealed class AuthValidationTests
{
    [Fact]
    public void Public_registration_rejects_manager_role()
    {
        var request = new RegisterRequest
        {
            Email = "manager@example.com",
            Password = "Manager123!",
            Roles = ["MANAGER"]
        };
        var validationContext = new ValidationContext(request);
        var errors = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(
            request,
            validationContext,
            errors,
            validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(errors, error => error.ErrorMessage == "Select SELLER, BUYER, or both, without duplicates.");
    }

    [Theory]
    [InlineData("short1!")] // < 8 characters
    [InlineData("nocapital123!")] // no uppercase
    [InlineData("NOLOWERCASE123!")] // no lowercase
    [InlineData("NoNumberSymbol!")] // no number
    [InlineData("NoSpecialNumber1")] // no special character
    public void Registration_rejects_weak_password(string weakPassword)
    {
        var request = new RegisterRequest
        {
            FullName = "Test User",
            PhoneNumber = "0771234567",
            Address = "Colombo",
            Email = "valid@example.com",
            Password = weakPassword,
            Roles = ["BUYER"]
        };
        var errors = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(request, new ValidationContext(request), errors, true);

        Assert.False(isValid);
        Assert.Contains(errors, error => error.ErrorMessage?.Contains("Password must be at least 8 characters long and contain at least one uppercase letter, one lowercase letter, one number, and one special character.") == true);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Registration_rejects_missing_password(string? missingPassword)
    {
        var request = new RegisterRequest
        {
            FullName = "Test User",
            PhoneNumber = "0771234567",
            Address = "Colombo",
            Email = "valid@example.com",
            Password = missingPassword!,
            Roles = ["BUYER"]
        };
        var errors = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(request, new ValidationContext(request), errors, true);

        Assert.False(isValid);
        Assert.NotEmpty(errors);
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("@missingusername.com")]
    [InlineData("missingdomain@")]
    [InlineData("has spaces@domain.com")]
    public void Registration_rejects_invalid_email(string invalidEmail)
    {
        var request = new RegisterRequest
        {
            FullName = "Test User",
            PhoneNumber = "0771234567",
            Address = "Colombo",
            Email = invalidEmail,
            Password = "StrongPassword123!",
            Roles = ["BUYER"]
        };
        var errors = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(request, new ValidationContext(request), errors, true);

        Assert.False(isValid);
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(RegisterRequest.Email)));
    }

    [Fact]
    public void Registration_accepts_valid_strong_password()
    {
        var request = new RegisterRequest
        {
            FullName = "Valid User",
            PhoneNumber = "0771234567",
            Address = "123 Main St, Colombo",
            Email = "valid.user@example.com",
            Password = "StrongPassword123!",
            Roles = ["BUYER", "SELLER"]
        };
        var errors = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(request, new ValidationContext(request), errors, true);

        Assert.True(isValid);
        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("short1!")]
    [InlineData("nocapital123!")]
    [InlineData("NOLOWERCASE123!")]
    [InlineData("NoNumberSymbol!")]
    [InlineData("NoSpecialNumber1")]
    public async Task Api_registration_rejects_weak_password_with_bad_request(string weakPassword)
    {
        using var app = new ApiWebApplicationFactory();
        using var client = app.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Weak User",
            phoneNumber = "0771234567",
            address = "Colombo",
            email = "weak@example.com",
            password = weakPassword,
            roles = new[] { "BUYER" }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Api_login_with_invalid_credentials_returns_unauthorized_with_friendly_message()
    {
        using var factory = new ApiWebApplicationFactory();
        using var app = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IAuthService>();
            services.AddScoped<IAuthService, InvalidCredentialsAuthService>();
        }));
        using var client = app.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "nonexistent@example.com",
            password = "WrongPassword123!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid email or password.", content);
    }
}

public sealed class AuthDatabaseTests(RequirementsDatabase fixture) : IClassFixture<RequirementsDatabase>
{
    [PostgresFact]
    public async Task Registration_succeeds_with_strong_password_and_rejects_duplicate_email()
    {
        using var app = fixture.App();
        using var client = app.CreateClient();
        var email = $"auth-test-{Guid.NewGuid():N}@example.com";
        var registrationPayload = new
        {
            fullName = "Auth Test User",
            phoneNumber = "0771234567",
            nic = $"2000000000{Random.Shared.Next(10, 100):D2}",
            address = "Colombo 03",
            email,
            password = "StrongPassword123!",
            roles = new[] { "BUYER" }
        };

        var response1 = await client.PostAsJsonAsync("/api/auth/register", registrationPayload);
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);

        var response2 = await client.PostAsJsonAsync("/api/auth/register", registrationPayload);
        Assert.Equal(HttpStatusCode.Conflict, response2.StatusCode);
    }

}

internal sealed class InvalidCredentialsAuthService : IAuthService
{
    public Task<(RegistrationResponse? Response, string? ErrorCode)> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<(AuthResponse? Response, string? ErrorCode)> LoginAsync(LoginRequest request, CancellationToken cancellationToken) =>
        Task.FromResult<(AuthResponse?, string?)>((null, "INVALID_CREDENTIALS"));

    public Task<string?> SendEmailVerificationAsync(string email, CancellationToken cancellationToken) => throw new NotSupportedException();
    public Task<string?> VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
    public Task<string?> ForgotPasswordAsync(string email, CancellationToken cancellationToken) => throw new NotSupportedException();
    public Task<string?> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
    public Task<string?> DeleteAccountAsync(Guid userId, CancellationToken cancellationToken) => throw new NotSupportedException();
}
