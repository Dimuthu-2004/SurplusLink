using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SurplusLink.Api.Data;
using SurplusLink.Api.Models;

namespace SurplusLink.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "MANAGER,ADMIN")]
public class UsersController : ControllerBase
{
    private readonly SurplusLinkDbContext _db;

    public UsersController(SurplusLinkDbContext db)
    {
        _db = db;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<UserSummaryResponse>> GetSummary(CancellationToken ct)
    {
        var users = await _db.Users.Include(u => u.RoleAssignments).AsNoTracking().ToListAsync(ct);

        int totalUsers = users.Count;
        int buyers = users.Count(u => u.RoleAssignments.Any(r => r.Role == UserRole.BUYER));
        int sellers = users.Count(u => u.RoleAssignments.Any(r => r.Role == UserRole.SELLER));
        int dualRoleUsers = users.Count(u => u.RoleAssignments.Any(r => r.Role == UserRole.BUYER) && u.RoleAssignments.Any(r => r.Role == UserRole.SELLER));
        int managers = users.Count(u => u.RoleAssignments.Any(r => r.Role == UserRole.MANAGER));

        return new UserSummaryResponse(totalUsers, buyers, sellers, dualRoleUsers, managers);
    }
}

public record UserSummaryResponse(int TotalUsers, int Buyers, int Sellers, int DualRoleUsers, int Managers);
