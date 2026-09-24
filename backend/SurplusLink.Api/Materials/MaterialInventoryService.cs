using Npgsql;
using Microsoft.EntityFrameworkCore;
using SurplusLink.Api.Data;
using SurplusLink.Api.Models;

namespace SurplusLink.Api.Materials;

public sealed class MaterialInventoryService(SurplusLinkDbContext dbContext) : IMaterialInventoryService
{
    public async Task<MaterialListingResponse> CreateListingAsync(
        Guid sellerId,
        CreateMaterialListingRequest request,
        CancellationToken cancellationToken)
    {
        ValidateListingRequest(request);
        await EnsureCategoryUnitAsync(request.CategoryId, request.Unit, cancellationToken);

        var listing = new Listing
        {
            Id = Guid.NewGuid(),
            SellerId = sellerId,
            CategoryId = request.CategoryId,
            Status = ListingStatus.DRAFT
        };
        ApplyListingRequest(listing, request);
        ReplacePhotos(listing, request.Photos);
        dbContext.Listings.Add(listing);
        AddAudit(sellerId, listing.Id, "LISTING_CREATED");
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetResponseAsync(listing.Id, cancellationToken);
    }

    public async Task<PagedMaterialListingsResponse> SearchListingsAsync(
        MaterialActor actor,
        MaterialListingQuery query,
        CancellationToken cancellationToken)
    {
        IQueryable<Listing> listings = dbContext.Listings.AsNoTracking();
        listings = RestrictToReadableListings(listings, actor);
        if (query.MineOnly) listings = listings.Where(listing => listing.SellerId == actor.Id);
        listings = MaterialListingQueryBuilder.ApplyFilters(listings, query);

        var totalCount = await listings.CountAsync(cancellationToken);
        var items = await MaterialListingQueryBuilder.ApplySort(listings, query)
            .Include(listing => listing.Seller)
            .Include(listing => listing.Category)
            .Include(listing => listing.Photos)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedMaterialListingsResponse(
            items.Select(ToResponse).ToList(),
            totalCount,
            (int)Math.Ceiling(totalCount / (double)query.PageSize),
            query.Page,
            query.PageSize);
    }

    public async Task<MaterialListingResponse> GetListingAsync(
        Guid listingId,
        MaterialActor actor,
        CancellationToken cancellationToken)
    {
        var listing = await GetListingWithDetailsAsync(listingId, cancellationToken)
            ?? throw new MaterialOperationException(MaterialOperationError.NotFound, "Material listing was not found.");

        var canRead = actor.HasRole(UserRole.MANAGER)
            || (actor.HasRole(UserRole.SELLER) && listing.SellerId == actor.Id)
            || (actor.HasRole(UserRole.BUYER)
                && listing.Status == ListingStatus.ACTIVE
                && listing.AvailableUntil > DateTime.UtcNow);
        if (!canRead)
        {
            throw new MaterialOperationException(MaterialOperationError.NotFound, "Material listing was not found.");
        }

        return ToResponse(listing);
    }

    public async Task<IReadOnlyList<MaterialListingHistoryResponse>> GetListingHistoryAsync(
        Guid listingId,
        MaterialActor actor,
        CancellationToken cancellationToken)
    {
        var listing = await dbContext.Listings.AsNoTracking()
            .Where(item => item.Id == listingId)
            .Select(item => new { item.Id, item.SellerId })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new MaterialOperationException(MaterialOperationError.NotFound, "Material listing was not found.");

        if (!actor.HasRole(UserRole.MANAGER) && (!actor.HasRole(UserRole.SELLER) || listing.SellerId != actor.Id))
        {
            throw new MaterialOperationException(MaterialOperationError.NotFound, "Material listing was not found.");
        }

        return await dbContext.AuditLogs.AsNoTracking()
            .Where(audit => audit.EntityType == nameof(Listing) && audit.EntityId == listing.Id)
            .OrderByDescending(audit => audit.CreatedAtUtc)
            .Select(audit => new MaterialListingHistoryResponse(
                audit.Id,
                audit.ActorUserId,
                audit.Action,
                audit.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MaterialListingResponse>> GetMyListingsAsync(
        Guid sellerId,
        CancellationToken cancellationToken)
    {
        var listings = await dbContext.Listings
            .AsNoTracking()
            .Where(listing => listing.SellerId == sellerId)
            .Include(listing => listing.Seller)
            .Include(listing => listing.Category)
            .Include(listing => listing.Photos)
            .OrderByDescending(listing => listing.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return listings.Select(ToResponse).ToList();
    }

    public async Task<MaterialListingResponse> UpdateListingAsync(
        Guid sellerId,
        Guid listingId,
        UpdateMaterialListingRequest request,
        CancellationToken cancellationToken)
    {
        ValidateListingRequest(request);
        var listing = await GetOwnedListingAsync(sellerId, listingId, cancellationToken);
        if (listing.Status is not ListingStatus.DRAFT and not ListingStatus.REJECTED)
        {
            throw new MaterialOperationException(
                MaterialOperationError.Conflict,
                "Only DRAFT or REJECTED listings can be edited. Publish the updated listing for manager verification.");
        }

        await EnsureCategoryUnitAsync(request.CategoryId, request.Unit, cancellationToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            listing.CategoryId = request.CategoryId;
            ApplyListingRequest(listing, request);
            listing.Photos.Clear();
            AddAudit(sellerId, listing.Id, "LISTING_UPDATED");
            await dbContext.SaveChangesAsync(cancellationToken);

            // Listings use PostgreSQL xmin optimistic concurrency. EF keeps the original
            // xmin on this tracked parent after the first save, so detach it before adding
            // dependents to guarantee that the second save is photo inserts only.
            dbContext.Entry(listing).State = EntityState.Detached;
            AddReplacementPhotos(listing.Id, request.Photos);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw new MaterialOperationException(
                MaterialOperationError.Conflict,
                "The material listing changed while it was being updated. Reload it and try again.");
        }

        return await GetResponseAsync(listing.Id, cancellationToken);
    }

    public async Task DeleteListingAsync(Guid sellerId, Guid listingId, CancellationToken cancellationToken)
    {
        var listing = await GetOwnedListingAsync(sellerId, listingId, cancellationToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"Listings\" WHERE \"Id\" = {listingId} FOR UPDATE", cancellationToken);
        await dbContext.Entry(listing).ReloadAsync(cancellationToken);
        var protectedStock = await dbContext.Reservations.AnyAsync(item => item.ListingId == listing.Id &&
                item.Status != ReservationStatus.RELEASED && item.Status != ReservationStatus.CANCELLED, cancellationToken)
            || await dbContext.Transactions.AnyAsync(item => item.Offer.MaterialMatch.ListingId == listing.Id &&
                (item.Status == TransactionStatus.APPROVED || item.Status == TransactionStatus.HANDED_OVER || item.Status == TransactionStatus.COMPLETED), cancellationToken);
        if (protectedStock)
            throw new MaterialOperationException(MaterialOperationError.Conflict, "Approved, reserved, or completed stock cannot be deleted.");

        // Keep candidate/audit references durable while making the material unavailable
        // everywhere. This is a seller-visible deletion, not a destructive cascade.
        listing.Status = ListingStatus.CLOSED;
        AddAudit(sellerId, listing.Id, "LISTING_DELETED");
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<MaterialListingResponse> PublishListingAsync(
        Guid sellerId,
        Guid listingId,
        CancellationToken cancellationToken)
    {
        var listing = await GetOwnedListingAsync(sellerId, listingId, cancellationToken);
        if (listing.Status is not ListingStatus.DRAFT and not ListingStatus.REJECTED)
        {
            throw new MaterialOperationException(
                MaterialOperationError.Conflict,
                "Only DRAFT or REJECTED listings can be submitted for verification.");
        }
        if (listing.AvailableUntil <= DateTime.UtcNow)
        {
            throw new MaterialOperationException(MaterialOperationError.Validation, "AvailableUntil must be in the future.");
        }

        listing.Status = ListingStatus.PENDING_VERIFICATION;
        AddAudit(sellerId, listing.Id, "LISTING_SUBMITTED_FOR_VERIFICATION");
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetResponseAsync(listing.Id, cancellationToken);
    }

    public async Task<MaterialListingResponse> VerifyListingAsync(
        Guid managerId,
        Guid listingId,
        VerifyListingRequest request,
        CancellationToken cancellationToken)
    {
        var listing = await dbContext.Listings.SingleOrDefaultAsync(item => item.Id == listingId, cancellationToken)
            ?? throw new MaterialOperationException(MaterialOperationError.NotFound, "Material listing was not found.");
        if (listing.Status != ListingStatus.PENDING_VERIFICATION)
        {
            throw new MaterialOperationException(
                MaterialOperationError.Conflict,
                "Only PENDING_VERIFICATION listings can be verified or rejected.");
        }

        if (request.Approved && listing.AvailableUntil <= DateTime.UtcNow)
            throw new MaterialOperationException(MaterialOperationError.Validation, "An expired listing cannot be verified.");

        listing.Status = request.Approved ? ListingStatus.ACTIVE : ListingStatus.REJECTED;
        if (request.Approved)
        {
            // Durable invalidation notice only: no hidden background matching or
            // changes to approved outcomes. The next explicit run re-evaluates.
            var staleIds = await dbContext.Matches.Where(x => x.ListingId == listingId &&
                (x.MaterialRequest.Status == BuyerRequestStatus.OPEN || x.MaterialRequest.Status == BuyerRequestStatus.MATCHING) &&
                !dbContext.Reservations.Any(r => r.MaterialRequestId == x.MaterialRequestId) &&
                !dbContext.AgentWorkflows.Any(w => w.MaterialRequestId == x.MaterialRequestId &&
                    (w.Status == AgentWorkflowStatus.PENDING_APPROVAL || w.Status == AgentWorkflowStatus.APPROVED ||
                     w.Status == AgentWorkflowStatus.COMPLETED)))
                .Select(x => x.Id).ToListAsync(cancellationToken);
            foreach (var id in staleIds)
                dbContext.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), EntityType = nameof(MaterialMatch),
                    EntityId = id, ActorUserId = managerId, Action = "STALE_LISTING_VERIFIED" });
        }

        AddAudit(managerId, listing.Id, request.Approved ? "LISTING_VERIFIED" : "LISTING_REJECTED");
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetResponseAsync(listing.Id, cancellationToken);
    }

    public async Task<MaterialAnalyticsSummaryResponse> GetAnalyticsSummaryAsync(
        MaterialAnalyticsQuery query,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var expiringBefore = now.AddDays(query.ExpiringWithinDays);
        var lowRemainingFraction = query.LowRemainingPercent / 100m;
        var listings = dbContext.Listings.AsNoTracking();
        var activeListings = listings.Where(listing =>
            listing.Status == ListingStatus.ACTIVE && listing.AvailableUntil > now);

        var activeCount = await activeListings.CountAsync(cancellationToken);
        var categoryNames = await dbContext.Categories.AsNoTracking()
            .ToDictionaryAsync(category => category.Id, category => category.Name, cancellationToken);
        var categoryCounts = await listings
            .GroupBy(listing => listing.CategoryId)
            .Select(group => new { CategoryId = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);
        var listingsByCategory = categoryCounts
            .Select(group => new MaterialAnalyticsCountResponse(categoryNames[group.CategoryId], group.Count))
            .OrderBy(item => item.Key)
            .ToList();
        var statusCounts = await listings
            .GroupBy(listing => listing.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);
        var listingsByStatus = statusCounts
            .Select(group => new MaterialAnalyticsCountResponse(group.Status.ToString(), group.Count))
            .OrderBy(item => item.Key)
            .ToList();
        var expiringListings = await activeListings
            .Where(listing => listing.AvailableUntil <= expiringBefore)
            .Include(listing => listing.Seller)
            .Include(listing => listing.Category)
            .Include(listing => listing.Photos)
            .OrderBy(listing => listing.AvailableUntil)
            .ToListAsync(cancellationToken);
        var lowRemainingQuantityListings = await activeListings
            .Where(listing => listing.Quantity - listing.ReservedQuantity <= listing.Quantity * lowRemainingFraction)
            .Include(listing => listing.Seller)
            .Include(listing => listing.Category)
            .Include(listing => listing.Photos)
            .OrderBy(listing => listing.AvailableUntil)
            .ToListAsync(cancellationToken);

        return new MaterialAnalyticsSummaryResponse(
            activeCount,
            listingsByCategory,
            listingsByStatus,
            expiringListings.Select(ToResponse).ToList(),
            lowRemainingQuantityListings.Select(ToResponse).ToList());
    }

    public async Task<IReadOnlyList<MaterialCategoryResponse>> GetCategoriesAsync(CancellationToken cancellationToken) =>
        await dbContext.Categories.AsNoTracking()
            .OrderBy(category => category.Name)
            .Select(category => ToResponse(category))
            .ToListAsync(cancellationToken);

    public Task<IReadOnlyList<string>> GetUnitCatalogAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<string>>(MaterialUnits.Catalog);

    public async Task<IReadOnlyList<string>> GetCategoryUnitsAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        var units = await dbContext.Categories.AsNoTracking().Where(category => category.Id == categoryId)
            .Select(category => category.AllowedUnits).SingleOrDefaultAsync(cancellationToken);
        return MaterialUnits.Distinct(units ?? []);
    }

    public async Task<IReadOnlyList<string>> GetActiveUnitsAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        var units = await dbContext.Listings.AsNoTracking()
            .Where(listing => listing.CategoryId == categoryId && listing.Status == ListingStatus.ACTIVE)
            .Select(listing => listing.Unit)
            .ToListAsync(cancellationToken);

        return units.Select(unit => unit.Trim())
            .Where(unit => unit.Length > 0)
            .GroupBy(unit => unit, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Key.ToLowerInvariant())
            .OrderBy(unit => unit, StringComparer.Ordinal)
            .ToArray();
    }

    public async Task<MaterialCategoryResponse> CreateCategoryAsync(MaterialCategoryRequest request, CancellationToken cancellationToken)
    {
        var name = NormalizedName(request.Name);
        if (await CategoryNameExistsAsync(name, null, cancellationToken))
        {
            throw new MaterialOperationException(MaterialOperationError.Conflict, "A category with this name already exists.");
        }

        var units = MaterialUnits.ValidateAllowed(request.AllowedUnits, await GetUnitCatalogAsync(cancellationToken));
        var category = new Category { Id = Guid.NewGuid(), Name = name, AllowedUnits = units };
        dbContext.Categories.Add(category);
        await SaveCategoryChangesAsync(cancellationToken);
        return ToResponse(category);
    }

    public async Task<MaterialCategoryResponse> UpdateCategoryAsync(
        Guid categoryId,
        MaterialCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories.SingleOrDefaultAsync(item => item.Id == categoryId, cancellationToken)
            ?? throw new MaterialOperationException(MaterialOperationError.NotFound, "Material category was not found.");
        var name = NormalizedName(request.Name);
        if (await CategoryNameExistsAsync(name, categoryId, cancellationToken))
        {
            throw new MaterialOperationException(MaterialOperationError.Conflict, "A category with this name already exists.");
        }

        category.AllowedUnits = MaterialUnits.ValidateAllowed(request.AllowedUnits, await GetUnitCatalogAsync(cancellationToken));
        category.Name = name;
        await SaveCategoryChangesAsync(cancellationToken);
        return ToResponse(category);
    }

    public async Task DeleteCategoryAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories.SingleOrDefaultAsync(item => item.Id == categoryId, cancellationToken)
            ?? throw new MaterialOperationException(MaterialOperationError.NotFound, "Material category was not found.");
        var inUse = await dbContext.Listings.AnyAsync(item => item.CategoryId == categoryId, cancellationToken);
        if (inUse)
        {
            throw new MaterialOperationException(MaterialOperationError.Conflict, "This category cannot be deleted because material listings are using it.");
        }

        if (await dbContext.BuyerRequests.AnyAsync(item => item.CategoryId == categoryId, cancellationToken))
            throw new MaterialOperationException(MaterialOperationError.Conflict, "A category used by a material request cannot be deleted.");

        dbContext.Categories.Remove(category);
        await SaveCategoryChangesAsync(cancellationToken);
    }

    private static IQueryable<Listing> RestrictToReadableListings(IQueryable<Listing> listings, MaterialActor actor)
    {
        if (actor.HasRole(UserRole.MANAGER)) return listings;
        var seller = actor.HasRole(UserRole.SELLER);
        var buyer = actor.HasRole(UserRole.BUYER);
        return listings.Where(listing => (seller && listing.SellerId == actor.Id)
            || (buyer && listing.Status == ListingStatus.ACTIVE && listing.AvailableUntil > DateTime.UtcNow));
    }

    private async Task<Listing> GetOwnedListingAsync(Guid sellerId, Guid listingId, CancellationToken cancellationToken) =>
        await dbContext.Listings.Include(listing => listing.Photos)
            .SingleOrDefaultAsync(item => item.Id == listingId && item.SellerId == sellerId, cancellationToken)
        ?? throw new MaterialOperationException(MaterialOperationError.NotFound, "Material listing was not found.");

    private async Task EnsureCategoryUnitAsync(Guid categoryId, string unit, CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories.SingleOrDefaultAsync(category => category.Id == categoryId, cancellationToken);
        if (category is null)
        {
            throw new MaterialOperationException(MaterialOperationError.Validation, "CategoryId must reference an existing material category.");
        }
        if (!MaterialUnits.Distinct(category.AllowedUnits).Contains(MaterialUnits.Normalize(unit)))
            throw new MaterialOperationException(MaterialOperationError.Validation, "Select an allowed unit for this category.");
    }

    private async Task<Listing?> GetListingWithDetailsAsync(Guid listingId, CancellationToken cancellationToken) =>
        await dbContext.Listings.AsNoTracking()
            .Include(listing => listing.Seller)
            .Include(listing => listing.Category)
            .Include(listing => listing.Photos)
            .SingleOrDefaultAsync(listing => listing.Id == listingId, cancellationToken);

    private async Task<MaterialListingResponse> GetResponseAsync(Guid listingId, CancellationToken cancellationToken)
    {
        var listing = await GetListingWithDetailsAsync(listingId, cancellationToken)
            ?? throw new InvalidOperationException("Saved material listing could not be reloaded.");
        return ToResponse(listing);
    }

    private static void ValidateListingRequest(CreateMaterialListingRequest request)
    {
        if (request.AvailableUntil is null || request.AvailableUntil <= DateTime.UtcNow)
        {
            throw new MaterialOperationException(MaterialOperationError.Validation, "AvailableUntil must be in the future.");
        }
        if (request.Latitude.HasValue != request.Longitude.HasValue)
        {
            throw new MaterialOperationException(MaterialOperationError.Validation, "Latitude and Longitude must be supplied together.");
        }
        if (request.Photos.Count > 10 || request.Photos.GroupBy(photo => photo.SortOrder).Any(group => group.Count() > 1))
        {
            throw new MaterialOperationException(MaterialOperationError.Validation, "Photos must have unique SortOrder values and contain at most ten items.");
        }
    }

    private static void ApplyListingRequest(Listing listing, CreateMaterialListingRequest request)
    {
        listing.Title = request.Title.Trim();
        listing.Description = request.Description.Trim();
        listing.Quantity = request.Quantity;
        listing.Unit = MaterialUnits.Normalize(request.Unit);
        listing.Condition = Enum.Parse<MaterialCondition>(request.Condition, ignoreCase: false);
        listing.UnitPrice = request.UnitPrice;
        listing.Latitude = request.Latitude;
        listing.Longitude = request.Longitude;
        listing.AvailableUntil = request.AvailableUntil!.Value.ToUniversalTime();
    }

    private static void ReplacePhotos(Listing listing, IReadOnlyList<ListingPhotoRequest> photos)
    {
        listing.Photos.Clear();
        foreach (var photo in photos.OrderBy(item => item.SortOrder))
        {
            listing.Photos.Add(new ListingPhoto
            {
                Id = Guid.NewGuid(),
                PhotoUrl = photo.PhotoUrl.Trim(),
                SortOrder = photo.SortOrder
            });
        }
    }

    private void AddReplacementPhotos(Guid listingId, IReadOnlyList<ListingPhotoRequest> photos)
    {
        foreach (var photo in photos.OrderBy(item => item.SortOrder))
        {
            dbContext.ListingPhotos.Add(new ListingPhoto
            {
                Id = Guid.NewGuid(),
                ListingId = listingId,
                PhotoUrl = photo.PhotoUrl.Trim(),
                SortOrder = photo.SortOrder
            });
        }
    }
    private void AddAudit(Guid actorUserId, Guid listingId, string action) => dbContext.AuditLogs.Add(new AuditLog
    {
        Id = Guid.NewGuid(),
        ActorUserId = actorUserId,
        EntityType = nameof(Listing),
        EntityId = listingId,
        Action = action
    });

    private static string NormalizedName(string name) => string.Join(" ", name.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private async Task<bool> CategoryNameExistsAsync(string name, Guid? excludedId, CancellationToken ct)
    {
        var names = await dbContext.Categories.Where(x => x.Id != excludedId).Select(x => x.Name).ToListAsync(ct);
        return names.Any(existing => string.Equals(NormalizedName(existing), name, StringComparison.OrdinalIgnoreCase));
    }

    private async Task SaveCategoryChangesAsync(CancellationToken ct)
    {
        try { await dbContext.SaveChangesAsync(ct); }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "UX_Categories_Name" })
        {
            throw new MaterialOperationException(MaterialOperationError.Conflict, "A category with this name already exists.");
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation, ConstraintName: "FK_Listings_Categories_CategoryId" })
        {
            throw new MaterialOperationException(MaterialOperationError.Conflict, "This category cannot be deleted because material listings are using it.");
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation, ConstraintName: "FK_MaterialRequests_Categories_CategoryId" })
        {
            throw new MaterialOperationException(MaterialOperationError.Conflict, "A category used by a material request cannot be deleted.");
        }
    }

    private static MaterialListingResponse ToResponse(Listing listing) => new(
        listing.Id,
        listing.SellerId,
        listing.CategoryId,
        listing.Category?.Name ?? string.Empty,
        listing.Title,
        listing.Description,
        listing.Quantity,
        listing.ReservedQuantity,
        listing.Unit,
        listing.Condition.ToString(),
        listing.UnitPrice,
        listing.Latitude,
        listing.Longitude,
        listing.AvailableUntil,
        listing.Status.ToString(),
        listing.CreatedAtUtc,
        listing.UpdatedAtUtc,
        listing.Photos.OrderBy(photo => photo.SortOrder)
            .Select(photo => new ListingPhotoResponse(photo.Id, photo.PhotoUrl, photo.SortOrder))
            .ToList(),
        listing.Seller is null ? null : new SellerContactResponse(listing.Seller.FullName,
            listing.Seller.BusinessName, listing.Seller.Email, listing.Seller.PhoneNumber));

    private static MaterialCategoryResponse ToResponse(Category category) =>
        new(category.Id, category.Name, category.CreatedAtUtc, category.UpdatedAtUtc, MaterialUnits.Distinct(category.AllowedUnits));
}






