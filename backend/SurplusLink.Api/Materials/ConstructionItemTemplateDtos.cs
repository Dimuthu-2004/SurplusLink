using System.ComponentModel.DataAnnotations;

namespace SurplusLink.Api.Materials;

public sealed record ConstructionItemTemplateResponse(
    Guid Id,
    string Name,
    Guid CategoryId,
    string CategoryName,
    string ItemClass,
    string QuantityMode,
    string BaseUnit,
    string? PackageType,
    IReadOnlyList<string> AllowedUnits,
    IReadOnlyList<decimal> AllowedPackageSizes,
    string AttributeSchema,
    string PriceBasis,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public class CreateConstructionItemTemplateRequest
{
    [Required, MaxLength(160)]
    public string Name { get; init; } = string.Empty;

    [Required]
    public Guid CategoryId { get; init; }

    [Required, RegularExpression("^(MATERIAL|TOOL|EQUIPMENT|FIXTURE|TEMPORARY_WORK|OTHER_CONSTRUCTION)$")]
    public string ItemClass { get; init; } = "MATERIAL";

    [Required, RegularExpression("^(PACKAGE|PIECE|CONTINUOUS_BULK|LENGTH|AREA|VOLUME)$")]
    public string QuantityMode { get; init; } = "PACKAGE";

    [Required, MaxLength(32)]
    public string BaseUnit { get; init; } = "piece";

    [RegularExpression("^(CAN|BAG|BOX|CARTRIDGE|ROLL|SHEET|ROD|PIPE|PACK|PIECE|OTHER)?$")]
    public string? PackageType { get; init; }

    [Required]
    public string[] AllowedUnits { get; init; } = [];

    public decimal[] AllowedPackageSizes { get; init; } = [];

    public string AttributeSchema { get; init; } = "[]";

    [MaxLength(40)]
    public string PriceBasis { get; init; } = "PER_UNIT";
}

public sealed class UpdateConstructionItemTemplateRequest : CreateConstructionItemTemplateRequest;

public sealed class ToggleTemplateStatusRequest
{
    public bool IsActive { get; init; }
}

public sealed record TemplateMatchResolutionResponse(
    Guid? TemplateId,
    string? TemplateName,
    string? ItemClass,
    string? QuantityMode,
    string? BaseUnit,
    string? PackageType,
    decimal Confidence);
