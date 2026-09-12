using System.ComponentModel.DataAnnotations;
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
            Role = "MANAGER"
        };
        var validationContext = new ValidationContext(request);
        var errors = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(
            request,
            validationContext,
            errors,
            validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(errors, error => error.ErrorMessage == "Role must be SELLER or BUYER.");
    }
}
