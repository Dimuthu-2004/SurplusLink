using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SurplusLink.Api.Models;
using SurplusLink.Api.Workflows;

namespace SurplusLink.Tests;

public sealed class AgentWorkflowIntegrationTests(RequirementsDatabase fixture) : IClassFixture<RequirementsDatabase>
{
    [PostgresFact]
    public async Task Manager_approval_reserves_once_and_decisions_are_audited()
    {
        var workflow = await SeedWorkflow(AgentWorkflowStatus.PENDING_APPROVAL);
        using var app = fixture.App();
        using var buyer = fixture.Client(app, fixture.Buyer, "BUYER");
        using var manager = fixture.Client(app, fixture.Manager, "MANAGER");

        Assert.Equal(HttpStatusCode.Forbidden, (await buyer.GetAsync($"/api/workflows/{workflow.Id}")).StatusCode);
        var approved = await manager.PostAsJsonAsync($"/api/workflows/{workflow.Id}/approve", new { note = "Approved after validation." });
        Assert.Equal(HttpStatusCode.OK, approved.StatusCode);
        var response = (await approved.Content.ReadFromJsonAsync<AgentWorkflowResponse>())!;
        Assert.Equal(AgentWorkflowStatus.APPROVED, response.Status);
        Assert.Single(response.Approvals);

        using var db = fixture.Context();
        Assert.Equal(1, await db.Reservations.CountAsync(x => x.MaterialRequestId == workflow.MaterialRequestId));
        Assert.Contains(await db.AuditLogs.Where(x => x.EntityId == workflow.Id).Select(x => x.Action).ToListAsync(),
            x => x == "WORKFLOW_APPROVED");
        Assert.Equal(HttpStatusCode.OK,
            (await manager.PostAsJsonAsync($"/api/workflows/{workflow.Id}/approve", new { note = "Idempotent." })).StatusCode);
        Assert.Equal(1, await db.Reservations.CountAsync(x => x.MaterialRequestId == workflow.MaterialRequestId));
    }

    [PostgresFact]
    public async Task Reject_and_revise_never_reserve_and_store_manager_decisions()
    {
        var rejected = await SeedWorkflow(AgentWorkflowStatus.PENDING_APPROVAL);
        var revised = await SeedWorkflow(AgentWorkflowStatus.PENDING_APPROVAL);
        using var app = fixture.App();
        using var manager = fixture.Client(app, fixture.Manager, "MANAGER");

        var reject = await manager.PostAsJsonAsync($"/api/workflows/{rejected.Id}/reject", new { note = "Validation failed." });
        Assert.Equal(HttpStatusCode.OK, reject.StatusCode);
        var revise = await manager.PostAsJsonAsync($"/api/workflows/{revised.Id}/revise", new { note = "Correct the quantity." });
        Assert.Equal(HttpStatusCode.OK, revise.StatusCode);
        var revisedResponse = (await revise.Content.ReadFromJsonAsync<AgentWorkflowResponse>())!;
        Assert.Equal(AgentWorkflowStatus.REVISION_REQUESTED, revisedResponse.Status);
        Assert.Contains(revisedResponse.Steps, x => x.Stage == "REVISION");

        using var db = fixture.Context();
        Assert.Equal(0, await db.Reservations.CountAsync(x => x.MaterialRequestId == rejected.MaterialRequestId));
        Assert.Equal(0, await db.Reservations.CountAsync(x => x.MaterialRequestId == revised.MaterialRequestId));
        Assert.Equal(2, await db.Approvals.CountAsync(x => x.AgentWorkflowId == rejected.Id || x.AgentWorkflowId == revised.Id));
    }

    private async Task<AgentWorkflow> SeedWorkflow(AgentWorkflowStatus status)
    {
        using var db = fixture.Context();
        var category = await db.Categories.FirstAsync();
        var request = new BuyerRequest
        {
            Id = Guid.NewGuid(), BuyerId = fixture.Buyer, CategoryId = category.Id, Title = "Workflow request",
            RequiredQuantity = 2, MaximumBudget = 1000, Unit = "kg", Deadline = DateTime.UtcNow.AddDays(5),
            Status = BuyerRequestStatus.MATCH_FOUND
        };
        var listing = new Listing
        {
            Id = Guid.NewGuid(), SellerId = fixture.Seller, CategoryId = category.Id, Title = "Workflow listing",
            Quantity = 10, ReservedQuantity = 0, Unit = "kg", UnitPrice = 10, AvailableUntil = DateTime.UtcNow.AddDays(5),
            Status = ListingStatus.ACTIVE, Condition = MaterialCondition.GOOD
        };
        var match = new MaterialMatch { Id = Guid.NewGuid(), MaterialRequestId = request.Id, ListingId = listing.Id, Status = MatchStatus.GENERATED };
        var workflow = new AgentWorkflow
        {
            Id = Guid.NewGuid(), MaterialRequestId = request.Id, MaterialMatchId = match.Id, Status = status,
            CurrentStage = "REVIEW", InputJson = "{\"source\":\"test\"}", OutputJson = "{\"quantity\":2}",
            ValidationJson = "{\"valid\":true}", StartedAtUtc = DateTime.UtcNow
        };
        db.AddRange(request, listing, match, workflow);
        await db.SaveChangesAsync();
        return workflow;
    }
}
