namespace SurplusLink.Api.Reservations;

public sealed class ReservationRejectedException(string message) : InvalidOperationException(message);

public sealed class ReservationConflictException(string message, Exception innerException)
    : InvalidOperationException(message, innerException);
