using Microsoft.EntityFrameworkCore;
using SurplusLink.Api.Models;

namespace SurplusLink.Api.Data;

/// <summary>EF Core/PostgreSQL persistence boundary. Entity sets arrive with vertical slices.</summary>
public sealed class SurplusLinkDbContext(DbContextOptions<SurplusLinkDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(user => user.Id);
            entity.Property(user => user.Email).HasMaxLength(320).IsRequired();
            entity.HasIndex(user => user.Email).IsUnique();
            entity.Property(user => user.PasswordHash).IsRequired();
            entity.Property(user => user.Role).HasConversion<string>().HasMaxLength(20).IsRequired();
        });
    }
}
