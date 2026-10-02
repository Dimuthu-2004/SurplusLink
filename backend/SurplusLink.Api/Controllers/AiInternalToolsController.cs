using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SurplusLink.Api.Data;
using SurplusLink.Api.Models;

namespace SurplusLink.Api.Controllers;

[ApiController]
[Route("api/ai-internal/tools")]
public sealed class AiInternalToolsController(
    SurplusLinkDbContext db,
    IConfiguration config) : ControllerBase
{
    private bool ValidateInternalToken()
    {
        var expected = config["AI_SERVICE_SHARED_TOKEN"] ?? config["Workflow:SharedToken"] ?? "";
        if (string.IsNullOrWhiteSpace(expected))
        {
            return true;
        }

        var header = Request.Headers["x-internal-token"].ToString();
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(header));
    }

    private (Guid UserId, bool IsAuthenticated) GetRequestUser()
    {
        var userIdHeader = Request.Headers["x-user-id"].ToString();
        if (Guid.TryParse(userIdHeader, out var uid))
        {
            return (uid, true);
        }

        return (Guid.Empty, false);
    }

    [HttpGet("user-summary")]
    public async Task<IActionResult> GetCurrentUserSummary(CancellationToken ct)
    {
        if (!ValidateInternalToken()) return Unauthorized(new { error = "Invalid internal token" });
        var (userId, isAuth) = GetRequestUser();
        if (!isAuth) return Unauthorized(new { error = "User identity context required" });

        var user = await db.Users
            .AsNoTracking()
            .Include(u => u.RoleAssignments)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null) return NotFound(new { error = "User not found" });

        var activeListings = await db.Listings.CountAsync(l => l.SellerId == userId && l.Status == ListingStatus.ACTIVE, ct);
        var activeRequests = await db.BuyerRequests.CountAsync(r => r.BuyerId == userId, ct);
        var openOffers = await db.Offers.CountAsync(o => (o.BuyerId == userId || o.SellerId == userId), ct);

        return Ok(new
        {
            userId = user.Id,
            fullName = user.FullName,
            email = user.Email,
            roles = user.RoleAssignments.Select(r => r.Role.ToString()).ToList(),
            activeListingsCount = activeListings,
            activeRequestsCount = activeRequests,
            openOffersCount = openOffers,
        });
    }

    [HttpGet("my-active-listings")]
    public async Task<IActionResult> GetMyActiveListings(CancellationToken ct)
    {
        if (!ValidateInternalToken()) return Unauthorized(new { error = "Invalid internal token" });
        var (userId, isAuth) = GetRequestUser();
        if (!isAuth) return Unauthorized(new { error = "User identity context required" });

        var listings = await db.Listings
            .AsNoTracking()
            .Include(l => l.Category)
            .Include(l => l.Photos)
            .Where(l => l.SellerId == userId && l.Status == ListingStatus.ACTIVE)
            .OrderByDescending(l => l.CreatedAtUtc)
            .Take(20)
            .Select(l => new
            {
                id = l.Id,
                title = l.Title,
                categoryName = l.Category != null ? l.Category.Name : "General",
                quantity = l.Quantity,
                unit = l.Unit,
                unitPrice = l.UnitPrice,
                condition = l.Condition.ToString(),
                quantityMode = l.QuantityMode.ToString(),
                packageType = l.PackageType,
                packageSize = l.PackageSize,
                packageCount = l.PackageCount,
                availableUntil = l.AvailableUntil,
                status = l.Status.ToString(),
            })
            .ToListAsync(ct);

        return Ok(listings);
    }

    [HttpGet("my-listings/{id:guid}")]
    public async Task<IActionResult> GetMyListing(Guid id, CancellationToken ct)
    {
        if (!ValidateInternalToken()) return Unauthorized(new { error = "Invalid internal token" });
        var (userId, isAuth) = GetRequestUser();
        if (!isAuth) return Unauthorized(new { error = "User identity context required" });

        var listing = await db.Listings
            .AsNoTracking()
            .Include(l => l.Category)
            .Include(l => l.ConstructionItemTemplate)
            .FirstOrDefaultAsync(l => l.Id == id && (l.SellerId == userId || l.Status == ListingStatus.ACTIVE), ct);

        if (listing is null) return NotFound(new { error = "Listing not found or access denied" });

        return Ok(new
        {
            id = listing.Id,
            title = listing.Title,
            description = listing.Description,
            categoryName = listing.Category?.Name,
            templateName = listing.ConstructionItemTemplate?.Name,
            quantity = listing.Quantity,
            unit = listing.Unit,
            unitPrice = listing.UnitPrice,
            condition = listing.Condition.ToString(),
            quantityMode = listing.QuantityMode.ToString(),
            packageType = listing.PackageType,
            packageSize = listing.PackageSize,
            packageCount = listing.PackageCount,
            status = listing.Status.ToString(),
        });
    }

    [HttpGet("my-requirements")]
    public async Task<IActionResult> GetMyRequirements(CancellationToken ct)
    {
        if (!ValidateInternalToken()) return Unauthorized(new { error = "Invalid internal token" });
        var (userId, isAuth) = GetRequestUser();
        if (!isAuth) return Unauthorized(new { error = "User identity context required" });

        var requirements = await db.BuyerRequests
            .AsNoTracking()
            .Include(r => r.Category)
            .Where(r => r.BuyerId == userId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Take(20)
            .Select(r => new
            {
                id = r.Id,
                title = r.Title,
                categoryName = r.Category != null ? r.Category.Name : "General",
                quantity = r.RequiredQuantity,
                unit = r.Unit,
                maxBudget = r.MaximumBudget,
                status = r.Status.ToString(),
                createdAtUtc = r.CreatedAtUtc,
            })
            .ToListAsync(ct);

        return Ok(requirements);
    }

    [HttpGet("my-requirements/{id:guid}")]
    public async Task<IActionResult> GetMyRequirement(Guid id, CancellationToken ct)
    {
        if (!ValidateInternalToken()) return Unauthorized(new { error = "Invalid internal token" });
        var (userId, isAuth) = GetRequestUser();
        if (!isAuth) return Unauthorized(new { error = "User identity context required" });

        var req = await db.BuyerRequests
            .AsNoTracking()
            .Include(r => r.Category)
            .Include(r => r.ConstructionItemTemplate)
            .FirstOrDefaultAsync(r => r.Id == id && r.BuyerId == userId, ct);

        if (req is null) return NotFound(new { error = "Requirement not found" });

        return Ok(new
        {
            id = req.Id,
            title = req.Title,
            categoryName = req.Category?.Name,
            templateName = req.ConstructionItemTemplate?.Name,
            quantity = req.RequiredQuantity,
            unit = req.Unit,
            maxBudget = req.MaximumBudget,
            status = req.Status.ToString(),
            createdAtUtc = req.CreatedAtUtc,
        });
    }

    [HttpGet("my-matches")]
    public async Task<IActionResult> GetMyMatches(CancellationToken ct)
    {
        if (!ValidateInternalToken()) return Unauthorized(new { error = "Invalid internal token" });
        var (userId, isAuth) = GetRequestUser();
        if (!isAuth) return Unauthorized(new { error = "User identity context required" });

        var matches = await db.Matches
            .AsNoTracking()
            .Include(m => m.MaterialRequest)
            .Include(m => m.Listing)
            .Where(m => m.MaterialRequest != null && m.MaterialRequest.BuyerId == userId)
            .OrderByDescending(m => m.Score)
            .Take(10)
            .Select(m => new
            {
                id = m.Id,
                requestId = m.MaterialRequestId,
                requestTitle = m.MaterialRequest != null ? m.MaterialRequest.Title : null,
                listingId = m.ListingId,
                listingTitle = m.Listing != null ? m.Listing.Title : null,
                score = m.Score,
                unitPrice = m.Listing != null ? (decimal?)m.Listing.UnitPrice : null,
                distanceKm = m.Distance,
                transportCost = m.EstimatedTransportCost,
                status = m.Status.ToString(),
                recommendationReason = m.MaterialRequest != null ? m.MaterialRequest.RecommendationReason : null,
            })
            .ToListAsync(ct);

        return Ok(matches);
    }

    [HttpGet("my-offers")]
    public async Task<IActionResult> GetMyOffers(CancellationToken ct)
    {
        if (!ValidateInternalToken()) return Unauthorized(new { error = "Invalid internal token" });
        var (userId, isAuth) = GetRequestUser();
        if (!isAuth) return Unauthorized(new { error = "User identity context required" });

        var offers = await db.Offers
            .AsNoTracking()
            .Include(o => o.Buyer)
            .Include(o => o.Seller)
            .Include(o => o.MaterialMatch)
                .ThenInclude(m => m.Listing)
            .Where(o => o.BuyerId == userId || o.SellerId == userId)
            .OrderByDescending(o => o.CreatedAtUtc)
            .Take(20)
            .Select(o => new
            {
                id = o.Id,
                materialName = o.MaterialMatch != null && o.MaterialMatch.Listing != null ? o.MaterialMatch.Listing.Title : "Material",
                buyerName = o.Buyer != null ? o.Buyer.FullName : "Buyer",
                sellerName = o.Seller != null ? o.Seller.FullName : "Seller",
                sellerBusinessName = o.Seller != null ? o.Seller.BusinessName : null,
                quantity = o.Quantity,
                packageCount = o.PackageCount,
                unit = o.MaterialMatch != null && o.MaterialMatch.Listing != null ? o.MaterialMatch.Listing.Unit : "unit",
                totalValue = o.TotalValue,
                status = o.Status.ToString(),
                createdAtUtc = o.CreatedAtUtc,
            })
            .ToListAsync(ct);

        return Ok(offers);
    }

    [HttpGet("my-transactions")]
    public async Task<IActionResult> GetMyTransactions(CancellationToken ct)
    {
        if (!ValidateInternalToken()) return Unauthorized(new { error = "Invalid internal token" });
        var (userId, isAuth) = GetRequestUser();
        if (!isAuth) return Unauthorized(new { error = "User identity context required" });

        var txs = await db.Transactions
            .AsNoTracking()
            .Include(t => t.Offer)
                .ThenInclude(o => o.MaterialMatch)
                    .ThenInclude(m => m.Listing)
            .Where(t => t.BuyerId == userId || t.SellerId == userId)
            .OrderByDescending(t => t.CreatedAtUtc)
            .Take(20)
            .Select(t => new
            {
                id = t.Id,
                referenceNumber = t.Id.ToString(),
                materialName = t.Offer != null && t.Offer.MaterialMatch != null && t.Offer.MaterialMatch.Listing != null ? t.Offer.MaterialMatch.Listing.Title : "Material",
                status = t.Status.ToString(),
                handedOverAtUtc = t.SellerHandoverConfirmedAtUtc,
                completedAtUtc = t.CompletedAtUtc,
                createdAtUtc = t.CreatedAtUtc,
            })
            .ToListAsync(ct);

        return Ok(txs);
    }

    [HttpGet("my-transactions/{reference}")]
    public async Task<IActionResult> GetMyTransaction(string reference, CancellationToken ct)
    {
        if (!ValidateInternalToken()) return Unauthorized(new { error = "Invalid internal token" });
        var (userId, isAuth) = GetRequestUser();
        if (!isAuth) return Unauthorized(new { error = "User identity context required" });

        var tx = await db.Transactions
            .AsNoTracking()
            .Include(t => t.Offer)
                .ThenInclude(o => o.MaterialMatch)
                    .ThenInclude(m => m.Listing)
            .FirstOrDefaultAsync(t => (t.Id.ToString().StartsWith(reference) || t.Id.ToString() == reference) &&
                                     (t.BuyerId == userId || t.SellerId == userId), ct);

        if (tx is null) return NotFound(new { error = "Transaction reference not found" });

        return Ok(new
        {
            id = tx.Id,
            referenceNumber = tx.Id.ToString(),
            materialName = tx.Offer?.MaterialMatch?.Listing?.Title ?? "Material",
            quantity = tx.Quantity,
            packageCount = tx.PackageCount,
            unit = tx.Offer?.MaterialMatch?.Listing?.Unit ?? "unit",
            totalValue = tx.TotalValue,
            status = tx.Status.ToString(),
            handedOverAtUtc = tx.SellerHandoverConfirmedAtUtc,
            completedAtUtc = tx.CompletedAtUtc,
            createdAtUtc = tx.CreatedAtUtc,
        });
    }

    [HttpGet("catalog-item")]
    public async Task<IActionResult> GetCatalogItem([FromQuery] string? query, [FromQuery] Guid? templateId, CancellationToken ct)
    {
        if (!ValidateInternalToken()) return Unauthorized(new { error = "Invalid internal token" });

        if (templateId.HasValue)
        {
            var t = await db.ConstructionItemTemplates
                .AsNoTracking()
                .Include(x => x.Category)
                .FirstOrDefaultAsync(x => x.Id == templateId.Value, ct);
            if (t is null) return NotFound(new { error = "Template not found" });
            return Ok(new
            {
                id = t.Id,
                name = t.Name,
                categoryId = t.CategoryId,
                categoryName = t.Category?.Name,
                itemClass = t.ItemClass.ToString(),
                quantityMode = t.QuantityMode.ToString(),
                baseUnit = t.BaseUnit,
                packageType = t.PackageType,
                allowedUnits = t.AllowedUnits,
                allowedPackageSizes = t.AllowedPackageSizes,
                buyerInputModes = t.BuyerInputModes,
                attributeSchema = t.AttributeSchema,
            });
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            var all = await db.ConstructionItemTemplates
                .AsNoTracking()
                .Include(x => x.Category)
                .Where(x => x.IsActive)
                .Take(20)
                .Select(t => new
                {
                    id = t.Id,
                    name = t.Name,
                    categoryId = t.CategoryId,
                    categoryName = t.Category != null ? t.Category.Name : null,
                    itemClass = t.ItemClass.ToString(),
                    quantityMode = t.QuantityMode.ToString(),
                    baseUnit = t.BaseUnit,
                    packageType = t.PackageType,
                    allowedUnits = t.AllowedUnits,
                    allowedPackageSizes = t.AllowedPackageSizes,
                    buyerInputModes = t.BuyerInputModes,
                    attributeSchema = t.AttributeSchema,
                    aliases = t.Aliases,
                })
                .ToListAsync(ct);
            return Ok(all);
        }

        var qLower = query.Trim().ToLowerInvariant();
        var match = await db.ConstructionItemTemplates
            .AsNoTracking()
            .Include(x => x.Category)
            .Where(x => x.IsActive && (x.Name.ToLower().Contains(qLower) || (x.Category != null && x.Category.Name.ToLower().Contains(qLower))))
            .Take(5)
            .Select(t => new
            {
                id = t.Id,
                name = t.Name,
                categoryId = t.CategoryId,
                categoryName = t.Category != null ? t.Category.Name : null,
                itemClass = t.ItemClass.ToString(),
                quantityMode = t.QuantityMode.ToString(),
                baseUnit = t.BaseUnit,
                packageType = t.PackageType,
                allowedUnits = t.AllowedUnits,
                allowedPackageSizes = t.AllowedPackageSizes,
                buyerInputModes = t.BuyerInputModes,
                attributeSchema = t.AttributeSchema,
                aliases = t.Aliases,
            })
            .ToListAsync(ct);

        return Ok(match);
    }

    [HttpGet("material-categories")]
    public async Task<IActionResult> GetMaterialCategories(CancellationToken ct)
    {
        if (!ValidateInternalToken()) return Unauthorized(new { error = "Invalid internal token" });

        var categories = await db.Categories
            .AsNoTracking()
            .Select(c => new
            {
                id = c.Id,
                name = c.Name,
                allowedUnits = c.AllowedUnits,
            })
            .ToListAsync(ct);

        return Ok(categories);
    }
}
