using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using SurplusLink.Api.Models;

namespace SurplusLink.Tests;

public sealed class UserRoleMigrationTests(RequirementsDatabase fixture) : IClassFixture<RequirementsDatabase>
{
    [PostgresFact]
    public async Task Migration_round_trips_single_roles_rejects_duplicates_and_refuses_lossy_rollback()
    {
        using var db = fixture.Context();
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20260918072804_AddUserContactProfile");
        var role = await db.Database.SqlQuery<string>($"""SELECT "Role" AS "Value" FROM "Users" WHERE "Id" = {fixture.Buyer}""").SingleAsync();
        Assert.Equal("BUYER", role);
        await migrator.MigrateAsync();
        Assert.Equal(UserRole.BUYER, (await db.Set<UserRoleAssignment>().SingleAsync(x => x.UserId == fixture.Buyer)).Role);
        var duplicate = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "UserRoleAssignments" ("UserId", "Role") VALUES ({fixture.Buyer}, 'BUYER');
            """));
        Assert.Equal(PostgresErrorCodes.UniqueViolation, duplicate.SqlState);
        db.Set<UserRoleAssignment>().Add(new() { UserId = fixture.Buyer, Role = UserRole.SELLER });
        await db.SaveChangesAsync();
        var rollback = await Assert.ThrowsAsync<PostgresException>(() => migrator.MigrateAsync("20260918072804_AddUserContactProfile"));
        Assert.Contains("every user must have exactly one role", rollback.MessageText);
        Assert.Equal(2, await db.Set<UserRoleAssignment>().CountAsync(x => x.UserId == fixture.Buyer));
        Assert.Equal(fixture.Buyer, (await db.BuyerRequests.FindAsync(fixture.LegacyRequest))!.BuyerId);
    }
}
