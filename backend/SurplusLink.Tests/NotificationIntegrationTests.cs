using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SurplusLink.Api.Data;
using SurplusLink.Api.Models;
using SurplusLink.Api.Notifications;

namespace SurplusLink.Tests;

public sealed class NotificationIntegrationTests(RequirementsDatabase fixture) : IClassFixture<RequirementsDatabase>
{
    [PostgresFact]
    public async Task Notifications_are_private_filterable_pageable_deduplicated_and_readable()
    {
        using var app = fixture.App();
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SurplusLinkDbContext>();
            await db.Notifications.Where(item => item.UserId == fixture.Buyer || item.UserId == fixture.OtherBuyer)
                .ExecuteDeleteAsync();
            var service = scope.ServiceProvider.GetRequiredService<INotificationService>();
            var first = await service.CreateAsync(new(
                fixture.Buyer, "MATCH_READY", "Match ready", "A match is ready for review.",
                NotificationContext.BUYER, NotificationPriority.ACTION_REQUIRED,
                "MaterialRequest", Guid.NewGuid(), "/requirements/one", "match-ready:one"));
            var duplicate = await service.CreateAsync(new(
                fixture.Buyer, "MATCH_READY", "Changed retry title", "Changed retry message.",
                NotificationContext.BUYER, NotificationPriority.ACTION_REQUIRED,
                "MaterialRequest", Guid.NewGuid(), "/requirements/retry", "match-ready:one"));
            Assert.Equal(first.Id, duplicate.Id);

            await service.CreateAsync(new(
                fixture.Buyer, "SYSTEM_NEWS", "System news", "Maintenance is scheduled.",
                NotificationContext.SYSTEM, NotificationPriority.INFO,
                DeduplicationKey: "system-news:one"));
            await service.CreateAsync(new(
                fixture.Buyer, "STOCK_WARNING", "Stock warning", "Stock needs attention.",
                NotificationContext.SELLER, NotificationPriority.WARNING,
                DeduplicationKey: "stock-warning:one"));
            await service.CreateAsync(new(
                fixture.OtherBuyer, "PRIVATE", "Other user", "Must remain private.",
                NotificationContext.BUYER, NotificationPriority.CRITICAL,
                DeduplicationKey: "private:other"));
        }

        using var buyer = fixture.Client(app, fixture.Buyer);
        using var other = fixture.Client(app, fixture.OtherBuyer);

        var page = (await buyer.GetFromJsonAsync<NotificationPage>("/api/notifications?page=1&pageSize=2"))!;
        Assert.Equal(3, page.Total);
        Assert.Equal(2, page.Items.Count);
        Assert.Equal(2, page.TotalPages);
        Assert.DoesNotContain(page.Items, item => item.Title == "Other user");

        var secondPage = (await buyer.GetFromJsonAsync<NotificationPage>("/api/notifications?page=2&pageSize=2"))!;
        Assert.Single(secondPage.Items);

        var filtered = (await buyer.GetFromJsonAsync<NotificationPage>(
            "/api/notifications?unread=true&context=BUYER&priority=ACTION_REQUIRED"))!;
        var selected = Assert.Single(filtered.Items);
        Assert.Equal("Match ready", selected.Title);

        var unread = (await buyer.GetFromJsonAsync<UnreadNotificationCount>("/api/notifications/unread-count"))!;
        Assert.Equal(3, unread.Count);

        var forbiddenRead = await other.PostAsync($"/api/notifications/{selected.Id}/read", null);
        Assert.Equal(HttpStatusCode.NotFound, forbiddenRead.StatusCode);

        var readResponse = await buyer.PostAsync($"/api/notifications/{selected.Id}/read", null);
        Assert.Equal(HttpStatusCode.OK, readResponse.StatusCode);
        var read = (await readResponse.Content.ReadFromJsonAsync<NotificationResponse>())!;
        Assert.True(read.IsRead);
        Assert.NotNull(read.ReadAt);
        Assert.Equal(2, (await buyer.GetFromJsonAsync<UnreadNotificationCount>(
            "/api/notifications/unread-count"))!.Count);

        var markAll = await buyer.PostAsync("/api/notifications/read-all", null);
        Assert.Equal(HttpStatusCode.OK, markAll.StatusCode);
        Assert.Equal(0, (await markAll.Content.ReadFromJsonAsync<UnreadNotificationCount>())!.Count);
        Assert.Equal(0, (await buyer.GetFromJsonAsync<UnreadNotificationCount>(
            "/api/notifications/unread-count"))!.Count);
        Assert.Empty((await buyer.GetFromJsonAsync<NotificationPage>(
            "/api/notifications?unread=true"))!.Items);
    }

    [Fact]
    public void Notification_model_has_user_scoped_partial_unique_deduplication_index()
    {
        using var db = new SurplusLinkDbContext(new DbContextOptionsBuilder<SurplusLinkDbContext>()
            .UseNpgsql("Host=localhost;Database=model_only;Username=model;Password=model").Options);
        var entity = db.Model.FindEntityType(typeof(Notification))!;
        var index = entity.GetIndexes().Single(item => item.IsUnique &&
            item.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(Notification.UserId), nameof(Notification.DeduplicationKey)]));
        Assert.Equal("\"DeduplicationKey\" IS NOT NULL", index.GetFilter());
    }
}
