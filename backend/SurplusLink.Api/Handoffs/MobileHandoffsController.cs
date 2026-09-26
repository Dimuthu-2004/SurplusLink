using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SurplusLink.Api.Handoffs;

[ApiController]
[Route("api/mobile-handoffs")]
public sealed class MobileHandoffsController(IMobileHandoffService service) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "BUYER")]
    [ProducesResponseType<MobileHandoffResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        [FromBody] CreateMobileHandoffRequest request,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await service.CreateHandoffAsync(userId, request, ct);
            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (MobileHandoffException ex)
        {
            return Problem(statusCode: ex.StatusCode, detail: ex.Message);
        }
    }

    [HttpPost("redeem")]
    [Authorize]
    [ProducesResponseType<RedeemMobileHandoffResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Redeem(
        [FromBody] RedeemMobileHandoffRequest request,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await service.RedeemHandoffAsync(userId, request, ct);
            return Ok(response);
        }
        catch (MobileHandoffException ex)
        {
            return Problem(statusCode: ex.StatusCode, detail: ex.Message);
        }
    }

    [HttpGet("{code}")]
    [AllowAnonymous]
    [ProducesResponseType<MobileHandoffStatusResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatus(string code, CancellationToken ct)
    {
        try
        {
            var response = await service.GetStatusAsync(code, ct);
            return Ok(response);
        }
        catch (MobileHandoffException ex)
        {
            return Problem(statusCode: ex.StatusCode, detail: ex.Message);
        }
    }

    private bool TryGetUserId(out Guid userId)
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(subject, out userId);
    }
}
