namespace SurplusLink.Api.Models;

public enum ListingStatus
{
    DRAFT,
    AVAILABLE,
    RESERVED,
    SOLD,
    CLOSED
}

public enum MaterialRequestStatus
{
    OPEN,
    MATCHED,
    FULFILLED,
    CANCELLED,
    EXPIRED
}

public enum WorkflowStatus
{
    PENDING,
    ACCEPTED,
    IN_PROGRESS,
    COMPLETED,
    CANCELLED
}

public enum ReservationStatus
{
    ACTIVE,
    CONFIRMED,
    RELEASED,
    CANCELLED
}
