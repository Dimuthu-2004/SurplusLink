namespace SurplusLink.Api.Models;

public sealed class ConstructionItemTemplate : AuditableEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public Guid CategoryId { get; set; }

    public Category Category { get; set; } = null!;

    public string ItemClass { get; set; } = "MATERIAL"; // MATERIAL, TOOL, EQUIPMENT, FIXTURE, TEMPORARY_WORK, OTHER_CONSTRUCTION

    public string QuantityMode { get; set; } = "PACKAGE"; // PACKAGE, PIECE, CONTINUOUS_BULK, LENGTH, AREA, VOLUME

    public string BaseUnit { get; set; } = "piece";

    public string? PackageType { get; set; } // CAN, BAG, BOX, CARTRIDGE, ROLL, SHEET, ROD, PIPE, PACK, PIECE, OTHER

    public string[] AllowedUnits { get; set; } = [];

    public decimal[] AllowedPackageSizes { get; set; } = [];

    public string AttributeSchema { get; set; } = "[]"; // JSON array of attribute field definitions

    public string PriceBasis { get; set; } = "PER_UNIT"; // PER_UNIT, PER_PACKAGE, PER_PIECE, PER_DAY

    public bool IsActive { get; set; } = true;
}
