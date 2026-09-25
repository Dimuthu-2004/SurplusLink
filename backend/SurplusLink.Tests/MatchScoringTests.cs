using System.Text.Json;
using SurplusLink.Api.Matching;
using SurplusLink.Api.Models;
using SurplusLink.Api.Workflows;

namespace SurplusLink.Tests;

public sealed class MatchScoringTests
{
    [Fact]
    public void Equal_scores_sort_by_condition_total_cost_distance_then_listing_id()
    {
        MaterialMatch Row(int id, MaterialCondition condition, decimal transport, decimal distance) => new()
        {
            Id = Guid.NewGuid(), ListingId = Guid.Parse($"00000000-0000-0000-0000-{id:000000000000}"),
            Score = .6m, Listing = new Listing { Condition = condition, UnitPrice = 100 },
            MaterialRequest = new BuyerRequest { RequiredQuantity = 10 },
            EstimatedTransportCost = transport, Distance = distance
        };
        var rows = new[] { Row(1, MaterialCondition.POOR, 0, 0), Row(2, MaterialCondition.GOOD, 501, 0),
            Row(3, MaterialCondition.GOOD, 500, 11), Row(5, MaterialCondition.GOOD, 500, 10),
            Row(4, MaterialCondition.GOOD, 500, 10) };
        var sorted = MatchQueryBuilder.Sort(rows.AsQueryable(), new()).ToArray();
        Assert.Equal(new[] { rows[4], rows[3], rows[2], rows[1], rows[0] }, sorted);
    }

    [Fact]
    public void Condition_orders_equal_base_candidates_and_missing_routes_have_no_score()
    {
        var scores = new[] { "EXCELLENT", "GOOD", "FAIR", "POOR" }
            .Select(c => MatchScoring.Score(c, 1000, 2000, 10, 500)).ToArray();
        Assert.Equal(new[] { .7568m, .6318m, .5068m, .3818m }, scores);
        Assert.Equal(0, MatchScoring.Score("EXCELLENT", 1000, 2000, null, 500));
        Assert.Equal(0, MatchScoring.Score("EXCELLENT", 1000, 2000, 10, null));
    }

    [Theory]
    [InlineData("POOR", "EXCELLENT", 500, 500)]
    [InlineData("GOOD", "GOOD", 500.01, 500)]
    public void Exactly_one_deterministic_recommendation_is_accepted(string firstCondition,
        string secondCondition, decimal firstCost, decimal secondCost)
    {
        var request = new BuyerRequest { BuyerId = Guid.NewGuid(), CategoryId = Guid.NewGuid(),
            RequiredQuantity = 10, Unit = "kg", MaximumBudget = 2000, Deadline = DateTime.UtcNow.AddDays(2) };
        var first = new WorkflowListingSnapshot(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), request.CategoryId,
            20, "kg", 100, firstCondition, "ACTIVE", DateTime.UtcNow.AddDays(5), 6, 79, 10, 30, firstCost, null);
        var second = first with { MatchId = Guid.NewGuid(), ListingId = Guid.NewGuid(), Condition = secondCondition,
            TransportCost = secondCost };
        var snapshot = new WorkflowRunRequest(Guid.NewGuid(), new { }, [first, second]);
        WorkflowRunResult Result(WorkflowListingSnapshot row) => new(snapshot.WorkflowId, "MATCH_FOUND",
            new(true, true, row.MatchId, [], []), new(row.MatchId, row.ListingId,
                MatchScoring.Score(row.Condition, 1000, 2000, row.DistanceKm, row.TransportCost),
                row.DistanceKm!.Value, row.TransportCost!.Value), [], null);
        WorkflowQueueProcessor.ValidateSelection(request, snapshot, Result(second));
        Assert.Throws<JsonException>(() => WorkflowQueueProcessor.ValidateSelection(request, snapshot, Result(first)));
        // Even stale positive measurements cannot make a failed route recommendable.
        snapshot = snapshot with { Listings = [first, second with { RoutingError = "ROUTING_UNAVAILABLE" }] };
        Assert.Throws<JsonException>(() => WorkflowQueueProcessor.ValidateSelection(request, snapshot, Result(second)));
        WorkflowQueueProcessor.ValidateSelection(request, snapshot, Result(first));
    }
}
