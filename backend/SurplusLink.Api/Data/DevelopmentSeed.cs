using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SurplusLink.Api.Models;

namespace SurplusLink.Api.Data;

public static class DevelopmentSeed
{
    public static async Task SeedAsync(IServiceProvider services, IWebHostEnvironment environment)
    {
        if (!environment.IsDevelopment())
        {
            return;
        }

        var dbContext = services.GetRequiredService<SurplusLinkDbContext>();
        var passwordHasher = services.GetRequiredService<IPasswordHasher<User>>();
        var users = new[]
        {
            new { Email = "seller@test.local", Password = "Seller123!", Roles = new[] { UserRole.SELLER } },
            new { Email = "buyer@test.local", Password = "Buyer123!", Roles = new[] { UserRole.BUYER } },
            new { Email = "dual@test.local", Password = "Dual123!", Roles = new[] { UserRole.SELLER, UserRole.BUYER } },
            new { Email = "manager@test.local", Password = "Manager123!", Roles = new[] { UserRole.MANAGER } }
        };

        foreach (var seed in users)
        {
            var email = seed.Email.ToLowerInvariant();
            if (await dbContext.Users.AnyAsync(user => user.Email == email))
            {
                continue;
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                EmailVerified = true,
                RoleAssignments = seed.Roles.Select(role => new UserRoleAssignment { Role = role }).ToList(),
                CreatedAtUtc = DateTime.UtcNow
            };
            user.PasswordHash = passwordHasher.HashPassword(user, seed.Password);
            dbContext.Users.Add(user);
        }

        await dbContext.SaveChangesAsync();
    }
}
