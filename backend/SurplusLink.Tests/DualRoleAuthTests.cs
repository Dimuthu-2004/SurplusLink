using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SurplusLink.Api.Auth;
using SurplusLink.Api.Materials;
using SurplusLink.Api.Models;
using SurplusLink.Api.Requirements;
using SurplusLink.Api.Reservations;

namespace SurplusLink.Tests;

public sealed class DualRoleAuthTests(RequirementsDatabase fixture) : IClassFixture<RequirementsDatabase>
{
    public static IEnumerable<object[]> InvalidRoles() => new string[][]
    {
        [], ["MANAGER"], ["SELLER", "MANAGER"], ["BUYER", "MANAGER"],
        ["SELLER", "BUYER", "MANAGER"], ["SELLER", "SELLER"], ["BUYER", "BUYER"],
        ["UNKNOWN"], ["0"], ["seller"], ["SELLER,BUYER"], [null!]
    }.Select(roles => new object[] { roles });

    [Theory, MemberData(nameof(InvalidRoles))]
    public async Task Public_registration_rejects_invalid_roles_before_database_access(string[] roles)
    {
        using var app = new ApiWebApplicationFactory();
        using var client = app.CreateClient();
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PostAsJsonAsync("/api/auth/register", Registration(roles))).StatusCode);
    }

    [Fact]
    public async Task Missing_and_null_roles_are_rejected()
    {
        using var app = new ApiWebApplicationFactory();
        using var client = app.CreateClient();
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PostAsJsonAsync("/api/auth/register", Registration(null!))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PostAsJsonAsync("/api/auth/register", new { email = "x@test.local", password = "Password123!",
                fullName = "Test User", phoneNumber = "0771234567", address = "Colombo" })).StatusCode);
    }

    [PostgresFact]
    public async Task Registration_login_me_and_permissions_cover_all_marketplace_role_combinations()
    {
        using var app = fixture.App();
        var registrationNumber = 0;
        foreach (var roles in new[] { new[] { "SELLER" }, new[] { "BUYER" }, new[] { "SELLER", "BUYER" } })
        {
            using var client = app.CreateClient();
            var registration = Registration(roles, $"2000000000{++registrationNumber:D2}");
            var response = await client.PostAsJsonAsync("/api/auth/register", registration);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var registered = (await response.Content.ReadFromJsonAsync<RegistrationResponse>())!;
            Assert.Equal(registration.Email, registered.Email);
            Assert.True(registered.EmailVerificationRequired);
            using (var db = fixture.Context())
            {
                var user = await db.Users.SingleAsync(user => user.Email == registration.Email);
                user.EmailVerified = true;
                await db.SaveChangesAsync();
            }
            var login = await client.PostAsJsonAsync("/api/auth/login", new { registration.Email, registration.Password });
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            var session = (await login.Content.ReadFromJsonAsync<AuthResponse>())!;
            AssertRoles(session, roles);
            client.DefaultRequestHeaders.Authorization = new("Bearer", session.Token);
            var me = (await client.GetFromJsonAsync<UserResponse>("/api/auth/me"))!;
            Assert.Equal(session.User.Id, me.Id);
            Assert.Equal(roles.Order(), me.Roles.Order());

            var material = await client.PostAsJsonAsync("/api/materials", Material());
            Assert.Equal(roles.Contains("SELLER") ? HttpStatusCode.Created : HttpStatusCode.Forbidden, material.StatusCode);
            var requirement = await client.PostAsJsonAsync("/api/requirements", Requirement());
            Assert.Equal(roles.Contains("BUYER") ? HttpStatusCode.Created : HttpStatusCode.Forbidden, requirement.StatusCode);
            if (roles.Length == 2)
            {
                var listing = (await material.Content.ReadFromJsonAsync<MaterialListingResponse>())!;
                var request = (await requirement.Content.ReadFromJsonAsync<RequirementResponse>())!;
                Assert.Equal(me.Id, listing.SellerId);
                Assert.Equal(me.Id, request.BuyerId);
                Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/materials/{listing.Id}", Material())).StatusCode);
                Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/requirements/{request.Id}", Requirement())).StatusCode);
                Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/materials/{listing.Id}/history")).StatusCode);
                var own = (await client.GetFromJsonAsync<PagedMaterialListingsResponse>("/api/materials?mineOnly=true"))!;
                Assert.All(own.Items, x => Assert.Equal(me.Id, x.SellerId));
                using var other = fixture.Client(app, fixture.OtherBuyer, "SELLER", "BUYER");
                Assert.Equal(HttpStatusCode.NotFound, (await other.PutAsJsonAsync($"/api/materials/{listing.Id}", Material())).StatusCode);
                Assert.Equal(HttpStatusCode.Forbidden, (await other.PutAsJsonAsync($"/api/requirements/{request.Id}", Requirement())).StatusCode);
                Assert.Equal(HttpStatusCode.Forbidden, (await client.PatchAsJsonAsync($"/api/materials/{listing.Id}/verify", new { approved = true })).StatusCode);
                using var db = fixture.Context();
                var error = await Assert.ThrowsAsync<ReservationRejectedException>(() =>
                    new ReservationService(db).ReserveAsync(listing.Id, request.Id, 1, default));
                Assert.Equal(MarketplaceMatchPolicy.SelfMatchNotAllowed, error.Code);
                Assert.Equal(HttpStatusCode.OK, (await client.PatchAsync($"/api/materials/{listing.Id}/publish", null)).StatusCode);
                using var manager = fixture.Client(app, fixture.Manager, "MANAGER");
                Assert.Equal(HttpStatusCode.OK, (await manager.PatchAsJsonAsync($"/api/materials/{listing.Id}/verify", new { approved = true })).StatusCode);
            }
            foreach (var path in new[] { "/api/materials/analytics/summary", "/api/requirements/analytics/summary", "/api/requirements" })
                Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(path)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/material-categories", new { name = "forbidden" })).StatusCode);
        }
    }

    [PostgresFact]
    public async Task Migrated_users_keep_ids_roles_ownership_and_manager_login()
    {
        using var db = fixture.Context();
        foreach (var (id, role) in new[] { (fixture.Seller, UserRole.SELLER), (fixture.Buyer, UserRole.BUYER), (fixture.Manager, UserRole.MANAGER) })
        {
            var user = await db.Users.Include(x => x.RoleAssignments).SingleAsync(x => x.Id == id);
            Assert.Equal(role, Assert.Single(user.RoleAssignments).Role);
        }
        Assert.Equal(fixture.Buyer, (await db.BuyerRequests.FindAsync(fixture.LegacyRequest))!.BuyerId);
        Assert.True(await db.Listings.AnyAsync(x => x.SellerId == fixture.Seller));
        var manager = await db.Users.Include(x => x.RoleAssignments).SingleAsync(x => x.Id == fixture.Manager);
        manager.PasswordHash = new PasswordHasher<User>().HashPassword(manager, "ManagerTest123!");
        await db.SaveChangesAsync();
        using var app = fixture.App();
        using var client = app.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new { manager.Email, password = "ManagerTest123!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var session = (await login.Content.ReadFromJsonAsync<AuthResponse>())!;
        AssertRoles(session, ["MANAGER"]);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);
        Assert.Equal(new[] { "MANAGER" }, (await client.GetFromJsonAsync<UserResponse>("/api/auth/me"))!.Roles);
        foreach (var path in new[] { "/api/materials/analytics/summary", "/api/requirements/analytics/summary", "/api/requirements" })
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(path)).StatusCode);
    }

    [Fact]
    public void Shared_self_match_policy_rejects_owner_and_allows_other_seller()
    {
        var buyer = Guid.NewGuid();
        Assert.Equal("SELF_MATCH_NOT_ALLOWED", MarketplaceMatchPolicy.RejectionReason(buyer, buyer));
        Assert.Null(MarketplaceMatchPolicy.RejectionReason(buyer, Guid.NewGuid()));
    }

    private static void AssertRoles(AuthResponse session, string[] roles)
    {
        Assert.Equal(roles.Order(), session.User.Roles.Order());
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(session.Token);
        Assert.Equal(roles.Order(), jwt.Claims.Where(x => x.Type == ClaimTypes.Role).Select(x => x.Value).Order());
    }

    private static RegisterRequest Registration(string[] roles, string nic = "200000000001") => new()
    {
        Email = Guid.NewGuid() + "@dual.test", Password = "Password123!", Roles = roles,
        FullName = "Test User", Nic = nic, PhoneNumber = "0771234567", Address = "Colombo"
    };
    private static object Material() => new
    {
        categoryId = "00000000-0000-0000-0000-000000000101", title = "Test material", description = "Test stock",
        quantity = 10, unit = "kg", condition = "GOOD", unitPrice = 5, availableUntil = DateTime.UtcNow.AddDays(30)
    };
    private static object Requirement() => new
    {
        categoryId = "00000000-0000-0000-0000-000000000101", requiredQuantity = 2, unit = "kg",
        maximumBudget = 100, deadline = DateTime.UtcNow.AddDays(7), latitude = 6.9, longitude = 79.8
    };
}
