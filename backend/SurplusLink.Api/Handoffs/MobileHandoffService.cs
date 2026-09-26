using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using SurplusLink.Api.Data;
using SurplusLink.Api.Models;

namespace SurplusLink.Api.Handoffs;

public sealed class MobileHandoffService(SurplusLinkDbContext dbContext) : IMobileHandoffService
{
    public async Task<MobileHandoffResponse> CreateHandoffAsync(
        Guid userId,
        CreateMobileHandoffRequest request,
        CancellationToken ct)
    {
        var category = await dbContext.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId, ct);

        if (category is null)
        {
            throw new MobileHandoffException(MobileHandoffErrorCode.NotFound, "Category not found.", StatusCodes.Status404NotFound);
        }

        var code = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(15);
        var source = string.IsNullOrWhiteSpace(request.Source) ? "REACT_MARKETPLACE" : request.Source.Trim();

        var handoff = new MobileHandoff
        {
            Id = Guid.NewGuid(),
            Code = code,
            UserId = userId,
            CategoryId = category.Id,
            Source = source,
            ExpiresAt = expiresAt,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        dbContext.MobileHandoffs.Add(handoff);
        await dbContext.SaveChangesAsync(ct);

        return new MobileHandoffResponse(
            handoff.Id,
            handoff.Code,
            $"surpluslink://handoff/{handoff.Code}",
            handoff.CategoryId,
            category.Name,
            handoff.Source,
            handoff.CreatedAtUtc,
            handoff.ExpiresAt);
    }

    public async Task<RedeemMobileHandoffResponse> RedeemHandoffAsync(
        Guid actorUserId,
        RedeemMobileHandoffRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            throw new MobileHandoffException(MobileHandoffErrorCode.Validation, "Handoff code is required.", StatusCodes.Status400BadRequest);
        }

        var handoff = await dbContext.MobileHandoffs
            .Include(h => h.Category)
            .FirstOrDefaultAsync(h => h.Code == request.Code.Trim(), ct);

        if (handoff is null)
        {
            throw new MobileHandoffException(MobileHandoffErrorCode.NotFound, "Invalid handoff code.", StatusCodes.Status404NotFound);
        }

        if (DateTime.UtcNow > handoff.ExpiresAt)
        {
            throw new MobileHandoffException(MobileHandoffErrorCode.Expired, "This handoff has expired.", StatusCodes.Status400BadRequest);
        }

        if (handoff.RedeemedAt.HasValue)
        {
            throw new MobileHandoffException(MobileHandoffErrorCode.AlreadyRedeemed, "This handoff has already been redeemed.", StatusCodes.Status400BadRequest);
        }

        if (handoff.UserId != actorUserId)
        {
            throw new MobileHandoffException(MobileHandoffErrorCode.Forbidden, "This handoff belongs to another account.", StatusCodes.Status403Forbidden);
        }

        var now = DateTime.UtcNow;
        handoff.RedeemedAt = now;
        handoff.RedeemedByUserId = actorUserId;
        handoff.UpdatedAtUtc = now;

        await dbContext.SaveChangesAsync(ct);

        return new RedeemMobileHandoffResponse(
            handoff.Id,
            handoff.CategoryId,
            handoff.Category.Name,
            handoff.Source,
            handoff.RedeemedAt.Value);
    }

    public async Task<MobileHandoffStatusResponse> GetStatusAsync(string code, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new MobileHandoffException(MobileHandoffErrorCode.Validation, "Handoff code is required.", StatusCodes.Status400BadRequest);
        }

        var handoff = await dbContext.MobileHandoffs
            .AsNoTracking()
            .Include(h => h.Category)
            .FirstOrDefaultAsync(h => h.Code == code.Trim(), ct);

        if (handoff is null)
        {
            throw new MobileHandoffException(MobileHandoffErrorCode.NotFound, "Invalid handoff code.", StatusCodes.Status404NotFound);
        }

        var isExpired = DateTime.UtcNow > handoff.ExpiresAt;
        var isRedeemed = handoff.RedeemedAt.HasValue;

        return new MobileHandoffStatusResponse(
            handoff.Code,
            handoff.CategoryId,
            handoff.Category.Name,
            handoff.Source,
            isExpired,
            isRedeemed,
            handoff.ExpiresAt);
    }
}
