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

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CommunityUserResponse>>> GetUsers(
        [FromQuery] string? search,
        [FromQuery] string? role,
        CancellationToken ct)
    {
        var query = _db.Users.Include(u => u.RoleAssignments).AsNoTracking();

        if (!string.IsNullOrWhiteSpace(role) && Enum.TryParse<UserRole>(role, true, out var parsedRole))
        {
            query = query.Where(u => u.RoleAssignments.Any(r => r.Role == parsedRole));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(u =>
                u.Email.ToLower().Contains(term) ||
                (u.FullName != null && u.FullName.ToLower().Contains(term)) ||
                (u.BusinessName != null && u.BusinessName.ToLower().Contains(term)) ||
                (u.PhoneNumber != null && u.PhoneNumber.Contains(term)));
        }

        var users = await query.OrderByDescending(u => u.CreatedAtUtc).ToListAsync(ct);

        return users.Select(u => new CommunityUserResponse(
            u.Id,
            u.Email,
            u.FullName,
            u.BusinessName,
            u.PhoneNumber,
            u.Address,
            u.ProfilePhotoUrl,
            u.RoleAssignments.Select(r => r.Role.ToString()).ToList(),
            u.CreatedAtUtc,
            u.EmailVerified)).ToList();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CommunityUserDetailsResponse>> GetUser(Guid id, CancellationToken ct)
    {
        var user = await _db.Users.Include(u => u.RoleAssignments).AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null) return NotFound(new ProblemDetails { Status = 404, Title = "User not found." });

        int listingsCount = await _db.Listings.CountAsync(l => l.SellerId == id, ct);
        int requestsCount = await _db.BuyerRequests.CountAsync(r => r.BuyerId == id, ct);
        int completedTxCount = await _db.Transactions.CountAsync(t => (t.BuyerId == id || t.SellerId == id) && t.Status == TransactionStatus.COMPLETED, ct);

        return new CommunityUserDetailsResponse(
            user.Id,
            user.Email,
            user.FullName,
            user.BusinessName,
            user.PhoneNumber,
            user.Address,
            user.ProfilePhotoUrl,
            user.RoleAssignments.Select(r => r.Role.ToString()).ToList(),
            user.CreatedAtUtc,
            user.EmailVerified,
            listingsCount,
            requestsCount,
            completedTxCount);
    }
}

public record UserSummaryResponse(int TotalUsers, int Buyers, int Sellers, int DualRoleUsers, int Managers);

public sealed record CommunityUserResponse(
    Guid Id,
    string Email,
    string? FullName,
    string? BusinessName,
    string? PhoneNumber,
    string? Address,
    string? ProfilePhotoUrl,
    IReadOnlyList<string> Roles,
    DateTime CreatedAtUtc,
    bool EmailVerified);

public sealed record CommunityUserDetailsResponse(
    Guid Id,
    string Email,
    string? FullName,
    string? BusinessName,
    string? PhoneNumber,
    string? Address,
    string? ProfilePhotoUrl,
    IReadOnlyList<string> Roles,
    DateTime CreatedAtUtc,
    bool EmailVerified,
    int ListingsCount,
    int RequestsCount,
    int CompletedTransactionsCount);
