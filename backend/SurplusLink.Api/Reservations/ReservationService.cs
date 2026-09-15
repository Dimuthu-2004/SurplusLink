using Microsoft.EntityFrameworkCore;
using SurplusLink.Api.Data;
using SurplusLink.Api.Models;

namespace SurplusLink.Api.Reservations;

public sealed class ReservationService(SurplusLinkDbContext dbContext) : IReservationService
{
    public async Task<Reservation> ReserveAsync(
        Guid listingId,
        Guid materialRequestId,
        decimal quantity,
        CancellationToken cancellationToken)
    {
        if (quantity <= 0)
        {
            throw new ReservationRejectedException("Reservation quantity must be positive.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var listing = await dbContext.Listings
            .SingleOrDefaultAsync(item => item.Id == listingId, cancellationToken)
            ?? throw new ReservationRejectedException("Listing was not found.");
        var request = await dbContext.MaterialRequests
            .SingleOrDefaultAsync(item => item.Id == materialRequestId, cancellationToken)
            ?? throw new ReservationRejectedException("Material request was not found.");

        if (listing.Status is not ListingStatus.ACTIVE and not ListingStatus.AVAILABLE and not ListingStatus.RESERVED)
        {
            throw new ReservationRejectedException("Listing is not available for reservation.");
        }

        if (request.Status is not MaterialRequestStatus.OPEN and not MaterialRequestStatus.MATCHED)
        {
            throw new ReservationRejectedException("Material request is not open for reservation.");
        }

        if (listing.CategoryId != request.CategoryId)
        {
            throw new ReservationRejectedException("Listing and material request categories do not match.");
        }

        if (listing.ReservedQuantity + quantity > listing.Quantity)
        {
            throw new ReservationRejectedException("Insufficient available quantity.");
        }

        listing.ReservedQuantity += quantity;
        if (listing.ReservedQuantity == listing.Quantity)
        {
            listing.Status = ListingStatus.RESERVED;
        }

        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            ListingId = listing.Id,
            MaterialRequestId = request.Id,
            Quantity = quantity,
            Status = ReservationStatus.ACTIVE
        };
        dbContext.Reservations.Add(reservation);
        dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorUserId = request.BuyerId,
            EntityType = nameof(Listing),
            EntityId = listing.Id,
            Action = "QUANTITY_RESERVED"
        });

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return reservation;
        }
        catch (DbUpdateConcurrencyException exception)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw new ReservationConflictException(
                "The listing changed during reservation. Reload it and try again.",
                exception);
        }
    }
}
