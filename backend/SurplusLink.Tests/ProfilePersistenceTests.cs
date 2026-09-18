using System.Net;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using SurplusLink.Api.Auth;
using SurplusLink.Api.Materials;

namespace SurplusLink.Tests;

public sealed class ProfilePersistenceTests(RequirementsDatabase fixture) : IClassFixture<RequirementsDatabase>
{
    [PostgresFact]
    public async Task Registration_and_self_profile_update_persist_contact_details()
    {
        using var app = fixture.App();
        using var client = app.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new {
            email = Guid.NewGuid() + "@profile.test", password = "Password123!", roles = new[] { "BUYER" },
            fullName = "  New Buyer  ", phoneNumber = "0771234567", businessName = "Buyer Business", address = "Colombo"
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var session = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        Assert.Equal("New Buyer", session.User.FullName);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);
        var before = (await client.GetFromJsonAsync<UserResponse>("/api/auth/me"))!;
        Assert.Equal("Colombo", before.Address);
        var updated = await client.PutAsJsonAsync("/api/auth/me", new ProfileRequest {
            FullName = "Updated Buyer", PhoneNumber = "0771112233", BusinessName = "Updated Business", Address = "Kandy"
        });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        using var db = fixture.Context();
        var user = await db.Users.Include(x => x.RoleAssignments).SingleAsync(x => x.Id == session.User.Id);
        Assert.Equal("Updated Buyer", user!.FullName);
        Assert.Equal("Kandy", user.Address);
        Assert.Equal("BUYER", Assert.Single(user.RoleAssignments).Role.ToString());
    }

    [PostgresFact]
    public async Task Buyer_sees_seller_contact_on_listing_without_private_address_or_password()
    {
        using var app = fixture.App();
        using var seller = fixture.Client(app, fixture.Seller, "SELLER");
        var updated = await seller.PutAsJsonAsync("/api/auth/me", new ProfileRequest {
            FullName = "Supplier Name", PhoneNumber = "0771112233", BusinessName = "Supplier Ltd", Address = "Private home address"
        });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var listings = (await seller.GetFromJsonAsync<PagedMaterialListingsResponse>("/api/materials"))!;
        var id = Assert.Single(listings.Items).Id;
        using var buyer = fixture.Client(app, fixture.Buyer);
        var json = await buyer.GetFromJsonAsync<JsonElement>($"/api/materials/{id}");
        Assert.Equal("Supplier Name", json.GetProperty("seller").GetProperty("fullName").GetString());
        Assert.Equal("0771112233", json.GetProperty("seller").GetProperty("phoneNumber").GetString());
        Assert.DoesNotContain("Private home address", json.ToString());
        Assert.DoesNotContain("password", json.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}
