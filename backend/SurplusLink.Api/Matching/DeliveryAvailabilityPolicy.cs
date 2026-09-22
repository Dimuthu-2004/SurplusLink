namespace SurplusLink.Api.Matching;

/// <summary>Delivery availability is a calendar-day commitment in UTC.</summary>
internal static class DeliveryAvailabilityPolicy
{
    internal static bool IsAvailableThrough(DateTime availableUntil, DateTime deadline) =>
        UtcDate(availableUntil) >= UtcDate(deadline);

    internal static DateOnly UtcDate(DateTime value) => DateOnly.FromDateTime(value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    });
}
