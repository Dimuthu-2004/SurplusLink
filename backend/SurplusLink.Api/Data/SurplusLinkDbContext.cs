using Microsoft.EntityFrameworkCore;

namespace SurplusLink.Api.Data;

/// <summary>EF Core/PostgreSQL persistence boundary. Entity sets arrive with vertical slices.</summary>
public sealed class SurplusLinkDbContext(DbContextOptions<SurplusLinkDbContext> options) : DbContext(options)
{
}
