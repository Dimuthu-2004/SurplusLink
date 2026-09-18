using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SurplusLink.Api.Data;
using SurplusLink.Api.Models;

namespace SurplusLink.Api.Auth;

public sealed class AuthService(
    SurplusLinkDbContext dbContext,
    IPasswordHasher<User> passwordHasher,
    IOptions<JwtOptions> jwtOptions) : IAuthService
{
    public async Task<(AuthResponse? Response, bool EmailExists)> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        System.ComponentModel.DataAnnotations.Validator.ValidateObject(
            request, new System.ComponentModel.DataAnnotations.ValidationContext(request), true);
        var email = request.Email.Trim().ToLowerInvariant();
        if (await dbContext.Users.AnyAsync(user => user.Email == email, cancellationToken))
        {
            return (null, true);
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            RoleAssignments = request.Roles.Select(role => new UserRoleAssignment { Role = Enum.Parse<UserRole>(role) }).ToList(),
            FullName = request.FullName.Trim(),
            PhoneNumber = request.PhoneNumber.Trim(),
            BusinessName = request.BusinessName?.Trim(),
            Address = request.Address.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (CreateAuthResponse(user), false);
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await dbContext.Users.Include(user => user.RoleAssignments).SingleOrDefaultAsync(item => item.Email == email, cancellationToken);
        if (user is null || passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
        {
            return null;
        }

        return CreateAuthResponse(user);
    }

    private AuthResponse CreateAuthResponse(User user)
    {
        var options = jwtOptions.Value;
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email)
        }.Concat(user.RoleAssignments.Select(assignment => new Claim(ClaimTypes.Role, assignment.Role.ToString())));
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Secret)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(options.ExpirationMinutes),
            signingCredentials: credentials);

        return new AuthResponse(new JwtSecurityTokenHandler().WriteToken(token), ToResponse(user));
    }

    private static UserResponse ToResponse(User user) => UserResponse.From(user);
}
