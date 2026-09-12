using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SurplusLink.Api.Auth;
using SurplusLink.Api.Data;
using SurplusLink.Api.Models;

namespace SurplusLink.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService, SurplusLinkDbContext dbContext) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var (response, emailExists) = await authService.RegisterAsync(request, cancellationToken);
        if (emailExists)
        {
            return Conflict(new { message = "An account with that email already exists." });
        }

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var response = await authService.LoginAsync(request, cancellationToken);
        return response is null ? Unauthorized(new { message = "Invalid email or password." }) : Ok(response);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> Me(CancellationToken cancellationToken)
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(subject, out var userId))
        {
            return Unauthorized();
        }

        var user = await dbContext.Users.FindAsync([userId], cancellationToken);
        return user is null ? NotFound() : Ok(new UserResponse(user.Id, user.Email, user.Role.ToString()));
    }
}