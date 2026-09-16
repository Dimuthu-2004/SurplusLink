using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SurplusLink.Api.Models;

namespace SurplusLink.Api.Requirements;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class SaveRequirementRequest : IValidatableObject
{
    public Guid CategoryId { get; init; }
    [Range(typeof(decimal), "0.001", "999999999999999.999")]
    public decimal RequiredQuantity { get; init; }
    [Required, StringLength(32)]
    public string Unit { get; init; } = "";
    [Range(typeof(decimal), "0.01", "9999999999999999.99")]
    public decimal MaximumBudget { get; init; }
    [Required]
    public DateTimeOffset? Deadline { get; init; }
    [Required, Range(typeof(decimal), "-90", "90")]
    public decimal? Latitude { get; init; }
    [Required, Range(typeof(decimal), "-180", "180")]
    public decimal? Longitude { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CategoryId == Guid.Empty)
            yield return new("Category is required.", [nameof(CategoryId)]);
        if (decimal.Round(RequiredQuantity, 3) != RequiredQuantity)
            yield return new("Quantity supports at most three decimal places.", [nameof(RequiredQuantity)]);
        if (decimal.Round(MaximumBudget, 2) != MaximumBudget)
            yield return new("Budget supports at most two decimal places.", [nameof(MaximumBudget)]);
        if (Latitude is decimal lat && decimal.Round(lat, 6) != lat ||
            Longitude is decimal lon && decimal.Round(lon, 6) != lon)
            yield return new("Coordinates support at most six decimal places.", [nameof(Latitude), nameof(Longitude)]);
    }
}

public sealed record RequirementResponse(
    Guid Id, Guid BuyerId, Guid CategoryId, decimal RequiredQuantity, string Unit,
    decimal MaximumBudget, DateTime Deadline, decimal? Latitude, decimal? Longitude,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] BuyerRequestStatus Status,
    DateTime CreatedAt, DateTime UpdatedAt)
{
    public static RequirementResponse From(BuyerRequest request) => new(
        request.Id, request.BuyerId, request.CategoryId, request.RequiredQuantity, request.Unit,
        request.MaximumBudget, request.Deadline, request.Latitude, request.Longitude,
        request.Status, request.CreatedAtUtc, request.UpdatedAtUtc);
}

public sealed record RequirementPage(IReadOnlyList<RequirementResponse> Items, int Total, int Page, int PageSize);
public sealed record StartMatchingResponse(RequirementResponse Requirement, Guid WorkflowId);
public sealed class RequirementException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
