using Microsoft.EntityFrameworkCore;
using SurplusLink.Api.Models;

namespace SurplusLink.Api.Requirements;

internal static class RequirementQueryBuilder
{
    public static IQueryable<BuyerRequest> Filter(IQueryable<BuyerRequest> query, RequirementQuery input)
    {
        if (!string.IsNullOrWhiteSpace(input.Search))
        {
            // Escape LIKE wildcards so a search for "%" or "_" is a literal substring search.
            var term = input.Search.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
            var pattern = "%" + term + "%";
            query = query.Where(x => EF.Functions.ILike(x.Category.Name, pattern, "\\") ||
                                     EF.Functions.ILike(x.Notes, pattern, "\\"));
        }
        if (input.Status is not null)
        {
            var status = Enum.Parse<BuyerRequestStatus>(input.Status, true);
            query = query.Where(x => x.Status == status);
        }
        if (input.CategoryId is Guid categoryId) query = query.Where(x => x.CategoryId == categoryId);
        if (input.DeadlineFrom is DateTimeOffset from)
        {
            var utc = from.UtcDateTime;
            query = query.Where(x => x.Deadline >= utc);
        }
        if (input.DeadlineTo is DateTimeOffset to)
        {
            var utc = to.UtcDateTime;
            query = query.Where(x => x.Deadline <= utc);
        }
        return query;
    }

    public static IOrderedQueryable<BuyerRequest> Sort(IQueryable<BuyerRequest> query, RequirementQuery input)
    {
        var descending = input.SortDir.Equals("desc", StringComparison.OrdinalIgnoreCase);
        var ordered = input.Sort.ToLowerInvariant() switch
        {
            "deadline" => descending ? query.OrderByDescending(x => x.Deadline) : query.OrderBy(x => x.Deadline),
            "budget" => descending ? query.OrderByDescending(x => x.MaximumBudget) : query.OrderBy(x => x.MaximumBudget),
            _ => descending ? query.OrderByDescending(x => x.CreatedAtUtc) : query.OrderBy(x => x.CreatedAtUtc)
        };
        return ordered.ThenBy(x => x.Id);
    }
}
