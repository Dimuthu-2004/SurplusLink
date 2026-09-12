using SurplusLink.Api.Models;

namespace SurplusLink.Api.Reservations;

public interface IReservationService
{
    Task<Reservation> ReserveAsync(
        Guid listingId,
        Guid materialRequestId,
        decimal quantity,
        CancellationToken cancellationToken);
}
