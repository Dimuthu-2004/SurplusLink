namespace SurplusLink.Api.Models;

public enum ListingStatus
{
    DRAFT,
    PENDING_VERIFICATION,
    ACTIVE,
    REJECTED,

    // Retained for compatibility with listings created by the shared scaffold.
    AVAILABLE,
    RESERVED,
    SOLD,
    CLOSED
}

public enum MaterialCondition
{
    NEW,
    EXCELLENT,
    GOOD,
    FAIR,
    POOR
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
