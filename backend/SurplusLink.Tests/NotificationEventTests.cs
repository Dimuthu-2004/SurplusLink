using Microsoft.EntityFrameworkCore;
using SurplusLink.Api.Materials;
using SurplusLink.Api.Models;
using SurplusLink.Api.Notifications;
using SurplusLink.Api.Requirements;
using SurplusLink.Api.Workflows;

namespace SurplusLink.Tests;

public sealed class NotificationEventTests(RequirementsDatabase fixture) : IClassFixture<RequirementsDatabase>
{
    [PostgresFact]
    public async Task Listing_submission_approval_rejection_and_requirement_submission_create_expected_notifications()
    {
        using var db = fixture.Context();
        var categoryId = (await db.Categories.FirstAsync()).Id;
        var approvedListing = Listing(categoryId, "Approval notification");
        var rejectedListing = Listing(categoryId, "Rejection notification");
        var requirement = new BuyerRequest
        {
            Id = Guid.NewGuid(), BuyerId = fixture.Buyer, CategoryId = categoryId,
            Title = "Notification requirement", RequiredQuantity = 2, Unit = "kg",
            MaximumBudget = 500, Deadline = DateTime.UtcNow.AddDays(5),
            Status = BuyerRequestStatus.DRAFT
        };
        db.AddRange(approvedListing, rejectedListing, requirement);
        await db.SaveChangesAsync();

        var notificationService = new NotificationService(db);
        var materials = new MaterialInventoryService(db, notificationService);
        await materials.PublishListingAsync(fixture.Seller, approvedListing.Id, default);
        await materials.PublishListingAsync(fixture.Seller, rejectedListing.Id, default);

        var managerNotifications = await db.Notifications.AsNoTracking()
            .Where(item => item.UserId == fixture.Manager &&
                item.Type == NotificationTypes.ListingAwaitingApproval &&
                (item.EntityId == approvedListing.Id || item.EntityId == rejectedListing.Id))
            .ToListAsync();
        Assert.Equal(2, managerNotifications.Count);
        Assert.All(managerNotifications, item =>
        {
            Assert.Equal(NotificationContext.MANAGER, item.Context);
            Assert.Equal(NotificationPriority.ACTION_REQUIRED, item.Priority);
            Assert.Equal(nameof(Listing), item.EntityType);
            Assert.NotNull(item.DeduplicationKey);
        });

        await materials.VerifyListingAsync(fixture.Manager, approvedListing.Id, new() { Approved = true }, default);
        await materials.VerifyListingAsync(fixture.Manager, rejectedListing.Id, new() { Approved = false }, default);

        var sellerNotifications = await db.Notifications.AsNoTracking()
            .Where(item => item.UserId == fixture.Seller &&
                (item.EntityId == approvedListing.Id || item.EntityId == rejectedListing.Id))
            .ToListAsync();
        Assert.Contains(sellerNotifications, item => item.EntityId == approvedListing.Id &&
            item.Type == NotificationTypes.ListingApproved &&
            item.Title == "Your material listing was approved" &&
            item.Priority == NotificationPriority.SUCCESS);
        var rejected = Assert.Single(sellerNotifications, item => item.EntityId == rejectedListing.Id);
        Assert.Equal(NotificationTypes.ListingRejected, rejected.Type);
        Assert.Equal("Your material listing was rejected", rejected.Title);
        Assert.DoesNotContain("password", rejected.Message, StringComparison.OrdinalIgnoreCase);

        var requirements = new RequirementService(db, new DeferredRequirementWorkflowStarter(), notificationService);
        await requirements.SubmitAsync(requirement.Id, fixture.Buyer, default);
        var submitted = await db.Notifications.AsNoTracking().SingleAsync(item =>
            item.UserId == fixture.Buyer && item.EntityId == requirement.Id &&
            item.Type == NotificationTypes.RequirementSubmitted);
        Assert.Equal("Requirement submitted", submitted.Title);
        Assert.Equal(nameof(BuyerRequest), submitted.EntityType);
        Assert.NotNull(submitted.DeduplicationKey);
    }

    private Listing Listing(Guid categoryId, string title) => new()
    {
        Id = Guid.NewGuid(), SellerId = fixture.Seller, CategoryId = categoryId,
        Title = title, Description = "Notification event test", Quantity = 10,
        ReservedQuantity = 0, Unit = "kg", UnitPrice = 25,
        AvailableUntil = DateTime.UtcNow.AddDays(10), Status = ListingStatus.DRAFT,
        Condition = MaterialCondition.GOOD
    };
}
