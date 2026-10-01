namespace SurplusLink.Api.Notifications;

public static class NotificationTypes
{
    public const string ListingAwaitingApproval = "LISTING_AWAITING_APPROVAL";
    public const string ListingApproved = "LISTING_APPROVED";
    public const string ListingRejected = "LISTING_REJECTED";
    public const string RequirementSubmitted = "REQUIREMENT_SUBMITTED";
    public const string MatchesReady = "MATCHES_READY";
    public const string NoSuitableMatches = "NO_SUITABLE_MATCHES";
    public const string BuyerSelectionAwaitingApproval = "BUYER_SELECTION_AWAITING_APPROVAL";
    public const string SelectionApproved = "SELECTION_APPROVED";
    public const string StockReservedForBuyer = "STOCK_RESERVED_FOR_BUYER";
    public const string SelectionRejected = "SELECTION_REJECTED";
    public const string SelectionRevisionRequested = "SELECTION_REVISION_REQUESTED";
    public const string SelectionAvailabilityChanged = "SELECTION_AVAILABILITY_CHANGED";
    public const string TransactionHandoverConfirmed = "TRANSACTION_HANDOVER_CONFIRMED";
    public const string TransactionCompleted = "TRANSACTION_COMPLETED";
    public const string TransactionFollowUpRequired = "TRANSACTION_FOLLOW_UP_REQUIRED";
    public const string TransactionTimedOut = "TRANSACTION_TIMED_OUT";
    public const string TransactionResolvedByManager = "TRANSACTION_RESOLVED_BY_MANAGER";
}
