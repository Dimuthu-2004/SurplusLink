using SurplusLink.Api.Models;

namespace SurplusLink.Api.Materials;

internal static class MaterialListingQueryBuilder
{
    internal static IQueryable<Listing> ApplyFilters(IQueryable<Listing> listings, MaterialListingQuery query)
    {
        Validate(query);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToUpperInvariant();
            listings = listings.Where(listing =>
                listing.Title.ToUpper().Contains(search)
                || listing.Description.ToUpper().Contains(search)
                || listing.Category.Name.ToUpper().Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            var category = query.Category.Trim();
            if (Guid.TryParse(category, out var categoryId))
            {
                listings = listings.Where(listing => listing.CategoryId == categoryId);
            }
            else
            {
                var categoryName = category.ToUpperInvariant();
                listings = listings.Where(listing => listing.Category.Name.ToUpper().Contains(categoryName));
            }
        }

        if (TryParseEnum<ListingStatus>(query.Status, "Status", out var status))
        {
            listings = listings.Where(listing => listing.Status == status);
        }

        if (TryParseEnum<MaterialCondition>(query.Condition, "Condition", out var condition))
        {
            listings = listings.Where(listing => listing.Condition == condition);
        }

        if (query.MinPrice is not null)
        {
            listings = listings.Where(listing => listing.UnitPrice >= query.MinPrice.Value);
        }

        if (query.MaxPrice is not null)
        {
            listings = listings.Where(listing => listing.UnitPrice <= query.MaxPrice.Value);
        }

        return listings;
    }

    internal static IOrderedQueryable<Listing> ApplySort(IQueryable<Listing> listings, MaterialListingQuery query)
    {
        Validate(query);
        var ascending = string.Equals(query.SortDir, "asc", StringComparison.OrdinalIgnoreCase);

        return ParseSortBy(query.SortBy) switch
        {
            "unitprice" => ascending
                ? listings.OrderBy(listing => listing.UnitPrice).ThenBy(listing => listing.Id)
                : listings.OrderByDescending(listing => listing.UnitPrice).ThenBy(listing => listing.Id),
            "quantity" => ascending
                ? listings.OrderBy(listing => listing.Quantity).ThenBy(listing => listing.Id)
                : listings.OrderByDescending(listing => listing.Quantity).ThenBy(listing => listing.Id),
            "availableuntil" => ascending
                ? listings.OrderBy(listing => listing.AvailableUntil).ThenBy(listing => listing.Id)
                : listings.OrderByDescending(listing => listing.AvailableUntil).ThenBy(listing => listing.Id),
            _ => ascending
                ? listings.OrderBy(listing => listing.CreatedAtUtc).ThenBy(listing => listing.Id)
                : listings.OrderByDescending(listing => listing.CreatedAtUtc).ThenBy(listing => listing.Id)
        };
    }

    internal static void Validate(MaterialListingQuery query)
    {
        if (query.MinPrice is not null && query.MaxPrice is not null && query.MinPrice > query.MaxPrice)
        {
            throw new MaterialOperationException(MaterialOperationError.Validation, "MinPrice cannot exceed MaxPrice.");
        }

        _ = ParseSortBy(query.SortBy);
        if (!string.Equals(query.SortDir, "asc", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(query.SortDir, "desc", StringComparison.OrdinalIgnoreCase))
        {
            throw new MaterialOperationException(MaterialOperationError.Validation, "SortDir must be asc or desc.");
        }

        _ = TryParseEnum<ListingStatus>(query.Status, "Status", out _);
        _ = TryParseEnum<MaterialCondition>(query.Condition, "Condition", out _);
    }

    private static string ParseSortBy(string sortBy)
    {
        var normalized = sortBy.Trim().ToLowerInvariant();
        return normalized is "unitprice" or "quantity" or "createdat" or "availableuntil"
            ? normalized
            : throw new MaterialOperationException(MaterialOperationError.Validation, "SortBy must be unitPrice, quantity, createdAt, or availableUntil.");
    }

    private static bool TryParseEnum<TEnum>(string? value, string name, out TEnum parsed)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            parsed = default;
            return false;
        }

        if (Enum.TryParse(value, ignoreCase: true, out parsed) && Enum.IsDefined(parsed))
        {
            return true;
        }

        throw new MaterialOperationException(MaterialOperationError.Validation, $"{name} is not valid.");
    }
}