using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SurplusLink.Api.Data;
using SurplusLink.Api.Models;
using SurplusLink.Api.Transactions;

namespace SurplusLink.Tests;

public sealed class OfferPresentationTests
{
    [Fact]
    public void Projection_retains_identifiers_and_resolves_public_names_without_sensitive_fields()
    {
        var offer = new Offer {
            Id = Guid.NewGuid(), BuyerId = Guid.NewGuid(), SellerId = Guid.NewGuid(),
            Buyer = new User { FullName = "Nimal Perera", Email = "private-buyer@example.com", PasswordHash = "secret" },
            Seller = new User { FullName = "Kamal Silva", BusinessName = "ABC Materials", Email = "private-seller@example.com", Nic = "199912345678" },
            MaterialMatch = new MaterialMatch {
                Listing = new Listing { Title = "Tiles", Unit = "pcs" },
                MaterialRequest = new BuyerRequest { Title = "Bathroom tiles" }
            },
            Quantity = 20, TotalValue = 20000
        };
        var response = TransactionService.OfferProjection.Compile()(offer);
        Assert.Equal(offer.Id, response.Id);
        Assert.Equal(offer.BuyerId, response.BuyerId);
        Assert.Equal(offer.SellerId, response.SellerId);
        Assert.Equal("Nimal Perera", response.BuyerName);
        Assert.Equal("Kamal Silva", response.SellerName);
        Assert.Equal("ABC Materials", response.SellerBusinessName);
        Assert.Equal("Tiles", response.MaterialName);
        Assert.Equal("Bathroom tiles", response.RequirementTitle);
        Assert.Equal("pcs", response.Unit);
        var json = JsonSerializer.Serialize(response);
        Assert.DoesNotContain("private-", json);
        Assert.DoesNotContain("secret", json);
        Assert.DoesNotContain("199912345678", json);
        Assert.DoesNotContain("PasswordHash", json);
    }

    [Fact]
    public void Public_fields_are_projected_in_one_sql_query()
    {
        using var db = new SurplusLinkDbContext(new DbContextOptionsBuilder<SurplusLinkDbContext>()
            .UseNpgsql("Host=localhost;Database=projection_only;Username=test;Password=test").Options);
        var sql = db.Offers.Select(TransactionService.OfferProjection).ToQueryString();
        Assert.Contains("JOIN", sql);
        Assert.Contains("FullName", sql);
        Assert.Contains("BusinessName", sql);
        Assert.DoesNotContain("PasswordHash", sql);
        Assert.DoesNotContain("Email", sql);
        Assert.DoesNotContain("PhoneNumber", sql);
    }
}
