using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SurplusLink.Api.Data;
using SurplusLink.Api.Models;

namespace SurplusLink.Api.Auth;

public sealed class AuthService(SurplusLinkDbContext dbContext, IPasswordHasher<User> passwordHasher, IAuthEmailSender emailSender, IOptions<JwtOptions> jwtOptions) : IAuthService
{
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(60);
    private const int MaxAttempts = 5;

    public async Task<(RegistrationResponse? Response, string? ErrorCode)> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        System.ComponentModel.DataAnnotations.Validator.ValidateObject(request, new System.ComponentModel.DataAnnotations.ValidationContext(request), true);
        if (!SriLankanContact.TryNormalizeNic(request.Nic, out var nic)) return (null, "INVALID_NIC");
        if (!SriLankanContact.TryNormalizePhone(request.PhoneNumber, out var phone)) return (null, "INVALID_PHONE");
        var email = request.Email.Trim().ToLowerInvariant();
        if (await dbContext.Users.AnyAsync(x => x.Email == email, ct)) return (null, "EMAIL_ALREADY_EXISTS");
        if (await dbContext.Users.AnyAsync(x => x.Nic == nic, ct)) return (null, "NIC_ALREADY_EXISTS");
        var user = new User { Id = Guid.NewGuid(), Email = email, Nic = nic, PhoneNumber = phone, FullName = request.FullName.Trim(), BusinessName = request.BusinessName?.Trim(), Address = request.Address.Trim(), EmailVerified = false, RoleAssignments = request.Roles.Select(x => new UserRoleAssignment { Role = Enum.Parse<UserRole>(x) }).ToList(), CreatedAtUtc = DateTime.UtcNow };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        SetVerificationCode(user, out var code);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(ct);
        try { await emailSender.SendVerificationAsync(email, code, ct); }
        catch (EmailDeliveryException)
        {
            ClearVerificationCode(user, clearLastSent: true);
            await dbContext.SaveChangesAsync(ct);
            return (null, "EMAIL_DELIVERY_FAILED");
        }
        return (new RegistrationResponse(email), null);
    }

    public async Task<(AuthResponse? Response, string? ErrorCode)> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var user = await FindUser(request.Email, ct);
        if (user is null || passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed) return (null, "INVALID_CREDENTIALS");
        if (!user.EmailVerified) return (null, "EMAIL_NOT_VERIFIED");
        return (CreateAuthResponse(user), null);
    }

    public async Task<string?> SendEmailVerificationAsync(string email, CancellationToken ct)
    {
        var user = await FindUser(email, ct);
        if (user is null || user.EmailVerified) return null;
        if (user.EmailVerificationLastSentAtUtc is { } last && DateTime.UtcNow - last < ResendCooldown) return "EMAIL_VERIFICATION_RESEND_TOO_SOON";
        SetVerificationCode(user, out var code);
        await dbContext.SaveChangesAsync(ct);
        try { await emailSender.SendVerificationAsync(user.Email, code, ct); }
        catch (EmailDeliveryException)
        {
            ClearVerificationCode(user, clearLastSent: true);
            await dbContext.SaveChangesAsync(ct);
            return "EMAIL_DELIVERY_FAILED";
        }
        return null;
    }

    public async Task<string?> VerifyEmailAsync(VerifyEmailRequest request, CancellationToken ct)
    {
        var user = await FindUser(request.Email, ct);
        if (user is null || user.EmailVerified) return "EMAIL_VERIFICATION_CODE_INVALID";
        if (user.EmailVerificationExpiresAtUtc is null || user.EmailVerificationExpiresAtUtc < DateTime.UtcNow) return "EMAIL_VERIFICATION_CODE_EXPIRED";
        if (user.EmailVerificationAttempts >= MaxAttempts) return "EMAIL_VERIFICATION_TOO_MANY_ATTEMPTS";
        if (!VerifyCode(user.EmailVerificationCodeHash, request.Code)) { user.EmailVerificationAttempts++; await dbContext.SaveChangesAsync(ct); return user.EmailVerificationAttempts >= MaxAttempts ? "EMAIL_VERIFICATION_TOO_MANY_ATTEMPTS" : "EMAIL_VERIFICATION_CODE_INVALID"; }
        user.EmailVerified = true; ClearVerificationCode(user, clearLastSent: false);
        await dbContext.SaveChangesAsync(ct);
        return null;
    }

    public async Task<string?> ForgotPasswordAsync(string email, CancellationToken ct)
    {
        var user = await FindUser(email, ct);
        if (user is null) return null;
        if (user.PasswordResetLastSentAtUtc is { } last && DateTime.UtcNow - last < ResendCooldown) return "PASSWORD_RESET_RESEND_TOO_SOON";
        var code = NewCode(); user.PasswordResetCodeHash = HashCode(code); user.PasswordResetExpiresAtUtc = DateTime.UtcNow.Add(CodeLifetime); user.PasswordResetLastSentAtUtc = DateTime.UtcNow; user.PasswordResetAttempts = 0;
        await dbContext.SaveChangesAsync(ct);
        try { await emailSender.SendPasswordResetAsync(user.Email, code, ct); }
        catch (EmailDeliveryException)
        {
            ClearResetCode(user, clearLastSent: true);
            await dbContext.SaveChangesAsync(ct);
            return "EMAIL_DELIVERY_FAILED";
        }
        return null;
    }

    public async Task<string?> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct)
    {
        var user = await FindUser(request.Email, ct);
        if (user is null || user.PasswordResetCodeHash is null) return "PASSWORD_RESET_CODE_INVALID";
        if (user.PasswordResetExpiresAtUtc is null || user.PasswordResetExpiresAtUtc < DateTime.UtcNow) return "PASSWORD_RESET_CODE_EXPIRED";
        if (user.PasswordResetAttempts >= MaxAttempts) return "PASSWORD_RESET_TOO_MANY_ATTEMPTS";
        if (!VerifyCode(user.PasswordResetCodeHash, request.Code)) { user.PasswordResetAttempts++; await dbContext.SaveChangesAsync(ct); return user.PasswordResetAttempts >= MaxAttempts ? "PASSWORD_RESET_TOO_MANY_ATTEMPTS" : "PASSWORD_RESET_CODE_INVALID"; }
        user.PasswordHash = passwordHasher.HashPassword(user, request.NewPassword);
        ClearResetCode(user, clearLastSent: false);
        await dbContext.SaveChangesAsync(ct);
        return null;
    }

    private async Task<User?> FindUser(string email, CancellationToken ct) => await dbContext.Users.Include(x => x.RoleAssignments).SingleOrDefaultAsync(x => x.Email == email.Trim().ToLowerInvariant(), ct);
    private static string NewCode() => RandomNumberGenerator.GetInt32(1_000_000).ToString("D6");
    private static string HashCode(string value) => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static bool VerifyCode(string? hash, string code) => hash is not null && CryptographicOperations.FixedTimeEquals(Convert.FromBase64String(hash), SHA256.HashData(Encoding.UTF8.GetBytes(code)));
    private void SetVerificationCode(User user, out string code) { code = NewCode(); user.EmailVerificationCodeHash = HashCode(code); user.EmailVerificationExpiresAtUtc = DateTime.UtcNow.Add(CodeLifetime); user.EmailVerificationLastSentAtUtc = DateTime.UtcNow; user.EmailVerificationAttempts = 0; }
    private static void ClearVerificationCode(User user, bool clearLastSent) { user.EmailVerificationCodeHash = null; user.EmailVerificationExpiresAtUtc = null; user.EmailVerificationAttempts = 0; if (clearLastSent) user.EmailVerificationLastSentAtUtc = null; }
    private static void ClearResetCode(User user, bool clearLastSent) { user.PasswordResetCodeHash = null; user.PasswordResetExpiresAtUtc = null; user.PasswordResetAttempts = 0; if (clearLastSent) user.PasswordResetLastSentAtUtc = null; }
    private AuthResponse CreateAuthResponse(User user)
    {
        var options = jwtOptions.Value;
        var claims = new[] { new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()), new Claim(JwtRegisteredClaimNames.Email, user.Email) }.Concat(user.RoleAssignments.Select(x => new Claim(ClaimTypes.Role, x.Role.ToString())));
        var token = new JwtSecurityToken(options.Issuer, options.Audience, claims, expires: DateTime.UtcNow.AddMinutes(options.ExpirationMinutes), signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Secret)), SecurityAlgorithms.HmacSha256));
        return new AuthResponse(new JwtSecurityTokenHandler().WriteToken(token), UserResponse.From(user));
    }
}
