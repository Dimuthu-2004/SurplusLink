namespace SurplusLink.Api.Reservations;

public sealed class ReservationRejectedException(string message, string? code = null) : InvalidOperationException(message)
{
    public string? Code { get; } = code;
}

public sealed class ReservationConflictException(string message, Exception innerException)
    : InvalidOperationException(message, innerException);
