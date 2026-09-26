using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SurplusLink.Api.Auth;
using SurplusLink.Api.Data;

namespace SurplusLink.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService, SurplusLinkDbContext dbContext) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<RegistrationResponse>> Register(RegisterRequest request, CancellationToken ct)
    {
        var (response, error) = await authService.RegisterAsync(request, ct);
        return error is null ? StatusCode(StatusCodes.Status201Created, response) : Error(error, error is "EMAIL_ALREADY_EXISTS" or "NIC_ALREADY_EXISTS" ? 409 : 400);
    }
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var (response, error) = await authService.LoginAsync(request, ct);
        return error is null ? Ok(response) : Error(error, error == "EMAIL_NOT_VERIFIED" ? 403 : 401);
    }
    [HttpPost("email-verification/send"), HttpPost("email-verification/resend")]
    public async Task<IActionResult> SendEmailVerification(EmailCodeRequest request, CancellationToken ct)
    {
        var error = await authService.SendEmailVerificationAsync(request.Email, ct);
        return error is null ? Ok(new { message = "A new verification code was sent." }) : Error(error, error == "EMAIL_DELIVERY_FAILED" ? 503 : 429);
    }
    [HttpPost("email-verification/verify")]
    public async Task<IActionResult> VerifyEmail(VerifyEmailRequest request, CancellationToken ct)
    {
        var error = await authService.VerifyEmailAsync(request, ct);
        return error is null ? Ok(new { message = "Your email has been verified." }) : Error(error, 400);
    }
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(EmailCodeRequest request, CancellationToken ct)
    {
        var error = await authService.ForgotPasswordAsync(request.Email, ct);
        return error is null ? Ok(new { message = "If an account exists for this email, a reset code has been sent." }) : Error(error, error == "EMAIL_DELIVERY_FAILED" ? 503 : 429);
    }
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken ct)
    {
        var error = await authService.ResetPasswordAsync(request, ct);
        return error is null ? Ok(new { message = "Your password has been reset." }) : Error(error, 400);
    }
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> Me(CancellationToken ct)
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(subject, out var userId)) return Unauthorized();
        var user = await dbContext.Users.Include(x => x.RoleAssignments).SingleOrDefaultAsync(x => x.Id == userId, ct);
        return user is null ? NotFound() : Ok(UserResponse.From(user));
    }
    [Authorize]
    [HttpPut("me")]
    public async Task<ActionResult<UserResponse>> UpdateProfile(ProfileRequest request, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)) return Unauthorized();
        if (!SriLankanContact.TryNormalizePhone(request.PhoneNumber, out var phone)) return Error("INVALID_PHONE", 400);
        var user = await dbContext.Users.Include(x => x.RoleAssignments).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (user is null) return NotFound();
        if (!string.IsNullOrWhiteSpace(request.Nic))
        {
            if (!SriLankanContact.TryNormalizeNic(request.Nic, out var nic)) return Error("INVALID_NIC", 400);
            if (await dbContext.Users.AnyAsync(x => x.Id != id && x.Nic == nic, ct)) return Error("NIC_ALREADY_EXISTS", 409);
            user.Nic = nic;
        }
        user.FullName = request.FullName.Trim(); user.PhoneNumber = phone; user.BusinessName = request.BusinessName?.Trim(); user.Address = request.Address.Trim();
        await dbContext.SaveChangesAsync(ct);
        return Ok(UserResponse.From(user));
    }
    private ObjectResult Error(string code, int status) => StatusCode(status, new { code, message = FriendlyMessage(code) });
    private static string FriendlyMessage(string code) => code switch { "INVALID_NIC" => "Enter a valid Sri Lankan NIC number.", "INVALID_PHONE" => "Enter a valid Sri Lankan phone number.", "EMAIL_NOT_VERIFIED" => "Your email has not been verified.", "EMAIL_ALREADY_EXISTS" => "An account with that email already exists.", "NIC_ALREADY_EXISTS" => "An account with that NIC already exists.", "EMAIL_DELIVERY_FAILED" => "We could not send the email. Please try again shortly.", "EMAIL_VERIFICATION_RESEND_TOO_SOON" or "PASSWORD_RESET_RESEND_TOO_SOON" => "Please wait before requesting another code.", "EMAIL_VERIFICATION_CODE_EXPIRED" or "PASSWORD_RESET_CODE_EXPIRED" => "This code has expired.", "EMAIL_VERIFICATION_TOO_MANY_ATTEMPTS" or "PASSWORD_RESET_TOO_MANY_ATTEMPTS" => "Too many attempts. Request a new code.", "EMAIL_VERIFICATION_CODE_INVALID" or "PASSWORD_RESET_CODE_INVALID" => "The code is invalid. Please try again.", _ => "Authentication request could not be completed." };
}
