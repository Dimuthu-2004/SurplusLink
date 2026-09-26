using System.ComponentModel.DataAnnotations;

namespace SurplusLink.Api.Handoffs;

public sealed class CreateMobileHandoffRequest
{
    [Required]
    public Guid CategoryId { get; init; }

    [MaxLength(64)]
    public string Source { get; init; } = "REACT_MARKETPLACE";
}

public sealed record MobileHandoffResponse(
    Guid Id,
    string Code,
    string DeepLink,
    Guid CategoryId,
    string CategoryName,
    string Source,
    DateTime CreatedAt,
    DateTime ExpiresAt);

public sealed class RedeemMobileHandoffRequest
{
    [Required, MaxLength(128)]
    public string Code { get; init; } = string.Empty;
}

public sealed record RedeemMobileHandoffResponse(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string Source,
    DateTime RedeemedAt);

public sealed record MobileHandoffStatusResponse(
    string Code,
    Guid CategoryId,
    string CategoryName,
    string Source,
    bool IsExpired,
    bool IsRedeemed,
    DateTime ExpiresAt);

public enum MobileHandoffErrorCode
{
    Validation,
    NotFound,
    Forbidden,
    Expired,
    AlreadyRedeemed
}

public sealed class MobileHandoffException : Exception
{
    public MobileHandoffErrorCode ErrorCode { get; }
    public int StatusCode { get; }

    public MobileHandoffException(MobileHandoffErrorCode code, string message, int statusCode)
        : base(message)
    {
        ErrorCode = code;
        StatusCode = statusCode;
    }
}
