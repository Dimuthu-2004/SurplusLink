using System.Text.Json;
using SurplusLink.Api.Matching;
using SurplusLink.Api.Models;
using SurplusLink.Api.Workflows;

namespace SurplusLink.Tests;

public sealed class MatchScoringTests
{
    [Fact]
    public void No_selected_preferences_preserves_the_legacy_formula()
    {
        Assert.Equal(.7568m, MatchScoring.Score("EXCELLENT", 1000, 2000, 10, 500));
        Assert.Equal(.7568m, MatchScoring.Score("EXCELLENT", 1000, 2000, 10, 500, 0, 0));
    }

    [Fact]
    public void Soft_preferences_change_the_final_score_and_rank()
    {
        var wrongButExcellent = MatchScoring.Score("EXCELLENT", 1000, 2000, 10, 500, 0, 2);
        var matchingButGood = MatchScoring.Score("GOOD", 1000, 2000, 10, 500, 2, 2);
        var halfMatching = MatchScoring.Score("GOOD", 1000, 2000, 10, 500, 1, 2);

        Assert.Equal(.4989m, wrongButExcellent);
        Assert.Equal(.7239m, matchingButGood);
        Assert.Equal(.5739m, halfMatching);
        Assert.True(matchingButGood > wrongButExcellent);
    }

    [Fact]
    public void Informational_fields_do_not_change_the_preference_score_and_hard_mismatches_remain_rejected()
    {
        var buyerId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var template = new ConstructionItemTemplate
        {
            Id = Guid.NewGuid(), CategoryId = categoryId, Name = "Paint",
            AttributeSchema = """
                [{"id":"colour","label":"Colour","buyerPreference":true,"matchBehavior":"SOFT_PREFERENCE"},
                 {"id":"finish","label":"Finish","buyerPreference":true,"matchBehavior":"HARD_REQUIREMENT"},
                 {"id":"note","label":"Note","buyerPreference":true,"matchBehavior":"INFORMATIONAL"}]
                """
        };
        var request = new BuyerRequest { BuyerId = buyerId, CategoryId = categoryId, ConstructionItemTemplateId = template.Id,
            BuyerPreferencesJson = """{"colour":"Red","finish":"Matt","note":"Anything"}""", Deadline = DateTime.UtcNow.AddDays(1) };
        var listing = new Listing { SellerId = Guid.NewGuid(), CategoryId = categoryId, ConstructionItemTemplateId = template.Id,
            ConstructionItemTemplate = template, Title = "Paint", Status = ListingStatus.ACTIVE, AvailableUntil = DateTime.UtcNow.AddDays(2),
            SpecificationsJson = """{"colour":"Red","finish":"Satin","note":"Different"}""" };
        var result = PreferenceCompatibility.Evaluate(request, listing);

        Assert.Equal(1, result.MatchedCount);
        Assert.Equal(2, result.ConsideredCount);
        Assert.True(result.HasHardMismatch);
        var rejection = MatchService.EligibilityReason(request, listing);
        Assert.Equal("REQUIRED_SPECIFICATION_MISMATCH", rejection);
        var finalScore = rejection is null
            ? MatchScoring.Score("GOOD", 1000, 2000, 10, 500, result.MatchedCount, result.ConsideredCount) : 0m;
        Assert.Equal(0m, finalScore);
    }

    [Fact]
    public void Equal_scores_sort_by_condition_total_cost_distance_then_listing_id()
    {
        MaterialMatch Row(int id, MaterialCondition condition, decimal transport, decimal distance) => new()
        {
            Id = Guid.NewGuid(), ListingId = Guid.Parse($"00000000-0000-0000-0000-{id:000000000000}"),
            Score = .6m, Listing = new Listing { ConstructionItemTemplateId = Guid.Parse("00000000-0000-0000-0000-000000000203"), Condition = condition, UnitPrice = 100 },
            MaterialRequest = new BuyerRequest { ConstructionItemTemplateId = Guid.Parse("00000000-0000-0000-0000-000000000203"), RequiredQuantity = 10 },
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

    [Fact]
    public void Partial_stock_candidate_remains_eligible_and_scored()
    {
        var request = new BuyerRequest { ConstructionItemTemplateId = Guid.Parse("00000000-0000-0000-0000-000000000203"), BuyerId = Guid.NewGuid(), CategoryId = Guid.NewGuid(),
            RequiredQuantity = 50, Unit = "pcs", MaximumBudget = 2000, Deadline = DateTime.UtcNow.AddDays(2),
            Status = BuyerRequestStatus.MATCHING };
        var listing = new Listing { ConstructionItemTemplateId = Guid.Parse("00000000-0000-0000-0000-000000000203"), SellerId = Guid.NewGuid(), CategoryId = request.CategoryId,
            Quantity = 20, ReservedQuantity = 0, Unit = "pcs", UnitPrice = 10,
            Status = ListingStatus.ACTIVE, AvailableUntil = DateTime.UtcNow.AddDays(5) };
        Assert.True(WorkflowQueueProcessor.StillEligible(request, listing, 50));

        var row = new WorkflowListingSnapshot(Guid.NewGuid(), Guid.NewGuid(), listing.SellerId,
            request.CategoryId, 20, "pcs", 10, "GOOD", "ACTIVE", listing.AvailableUntil,
            6, 79, 10, 30, 50, null, ConstructionItemTemplateId: request.ConstructionItemTemplateId) { MaximumContribution = 20 };
        var input = new WorkflowRunRequest(Guid.NewGuid(), new { }, [row]);
        var score = MatchScoring.Score(row.Condition, row.UnitPrice * row.MaximumContribution!.Value,
            request.MaximumBudget, row.DistanceKm, row.TransportCost);
        var result = new WorkflowRunResult(input.WorkflowId, "MATCH_FOUND",
            new(true, true, row.MatchId, [], []), new(row.MatchId, row.ListingId, score,
                row.DistanceKm!.Value, row.TransportCost!.Value), [], null);
        WorkflowQueueProcessor.ValidateSelection(request, input, result);
        Assert.True(score > 0);
    }

    [Theory]
    [InlineData("POOR", "EXCELLENT", 500, 500)]
    [InlineData("GOOD", "GOOD", 500.01, 500)]
    public void Exactly_one_deterministic_recommendation_is_accepted(string firstCondition,
        string secondCondition, decimal firstCost, decimal secondCost)
    {
        var request = new BuyerRequest { ConstructionItemTemplateId = Guid.Parse("00000000-0000-0000-0000-000000000203"), BuyerId = Guid.NewGuid(), CategoryId = Guid.NewGuid(),
            RequiredQuantity = 10, Unit = "kg", MaximumBudget = 2000, Deadline = DateTime.UtcNow.AddDays(2) };
        var first = new WorkflowListingSnapshot(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), request.CategoryId,
            20, "kg", 100, firstCondition, "ACTIVE", DateTime.UtcNow.AddDays(5), 6, 79, 10, 30, firstCost, null, ConstructionItemTemplateId: request.ConstructionItemTemplateId) { MaximumContribution = 10 };
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
