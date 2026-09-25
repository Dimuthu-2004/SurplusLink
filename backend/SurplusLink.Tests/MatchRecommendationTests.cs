using SurplusLink.Api.Matching;
using SurplusLink.Api.Models;

namespace SurplusLink.Tests;

public sealed class MatchRecommendationTests
{
    [Fact]
    public void Two_valid_matches_have_one_real_winner_and_reranking_changes_it()
    {
        var (request, first, second) = Candidates();
        var matches = new[] { first, second };
        var winner = MatchRecommendation.Choose(request, matches, DateTime.UtcNow);
        Assert.Equal(first.Id, winner?.Id);
        Assert.Single(matches, x => x.Id == winner?.Id);
        second.Score = .95m;
        Assert.Equal(second.Id, MatchRecommendation.Choose(request, matches, DateTime.UtcNow)?.Id);
    }

    [Theory]
    [InlineData("REJECTED")]
    [InlineData("ROUTE_FAILED")]
    [InlineData("INACTIVE")]
    [InlineData("EXPIRED")]
    [InlineData("INSUFFICIENT")]
    [InlineData("MISSING_ROUTE")]
    public void Invalid_candidates_cannot_win(string reason)
    {
        var (request, first, second) = Candidates();
        switch (reason)
        {
            case "REJECTED": first.Status = MatchStatus.REJECTED; break;
            case "ROUTE_FAILED": first.Status = MatchStatus.ROUTE_FAILED; break;
            case "INACTIVE": first.Listing.Status = ListingStatus.DRAFT; break;
            case "EXPIRED": first.Listing.AvailableUntil = DateTime.UtcNow.AddDays(-1); break;
            case "INSUFFICIENT": first.Listing.Quantity = 0; break;
            case "MISSING_ROUTE": first.DurationMinutes = null; break;
        }
        Assert.NotNull(MatchRecommendation.InvalidReason(request, first, DateTime.UtcNow));
        Assert.Equal(second.Id, MatchRecommendation.Choose(request, [first, second], DateTime.UtcNow)?.Id);
    }

    [Fact]
    public void Ties_use_condition_cost_distance_and_stable_id_in_order()
    {
        var (request, first, second) = Candidates();
        first.Score = second.Score;
        second.Listing.Condition = MaterialCondition.EXCELLENT;
        Assert.Equal(second.Id, MatchRecommendation.Choose(request, [first, second], DateTime.UtcNow)?.Id);
        first.Listing.Condition = second.Listing.Condition;
        first.Listing.UnitPrice = 1;
        Assert.Equal(first.Id, MatchRecommendation.Choose(request, [second, first], DateTime.UtcNow)?.Id);
        second.Listing.UnitPrice = first.Listing.UnitPrice;
        second.Distance = 1;
        Assert.Equal(second.Id, MatchRecommendation.Choose(request, [first, second], DateTime.UtcNow)?.Id);
        first.Distance = second.Distance;
        var expected = new[] { first, second }.OrderBy(x => x.ListingId.ToString(), StringComparer.Ordinal).First();
        Assert.Equal(expected.Id, MatchRecommendation.Choose(request, [second, first], DateTime.UtcNow)?.Id);
    }

    private static (BuyerRequest, MaterialMatch, MaterialMatch) Candidates()
    {
        var request = new BuyerRequest { Id = Guid.NewGuid(), BuyerId = Guid.NewGuid(),
            CategoryId = Guid.NewGuid(), RequiredQuantity = 2, MaximumBudget = 1000,
            Deadline = DateTime.UtcNow.AddDays(1), Unit = "kg" };
        MaterialMatch Candidate(decimal score) => new()
        {
            Id = Guid.NewGuid(), ListingId = Guid.NewGuid(), Score = score, Status = MatchStatus.ROUTED,
            Distance = 10, DurationMinutes = 20, EstimatedTransportCost = 100,
            Listing = new Listing { SellerId = Guid.NewGuid(), CategoryId = request.CategoryId,
                Status = ListingStatus.ACTIVE, Condition = MaterialCondition.GOOD, Quantity = 10,
                Unit = "kg", UnitPrice = 10, AvailableUntil = DateTime.UtcNow.AddDays(3) }
        };
        return (request, Candidate(.9m), Candidate(.8m));
    }
}
