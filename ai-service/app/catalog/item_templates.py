"""Authoritative Construction Item Template Catalog for SurplusLink matching workflows.

Provides deterministic item definitions and phrase mapping for valid quantity
modes, units, and classifications.
"""

from __future__ import annotations

import re
from dataclasses import dataclass
from typing import Any, Mapping, Sequence


@dataclass(frozen=True)
class ItemTemplateDefinition:
    id: str
    name: str
    item_class: str  # MATERIAL, TOOL, EQUIPMENT, FIXTURE, TEMPORARY_WORK, OTHER_CONSTRUCTION
    quantity_mode: str  # PACKAGE, PIECE, CONTINUOUS_BULK, LENGTH, AREA, VOLUME
    base_unit: str
    package_type: str | None
    allowed_units: tuple[str, ...]
    allowed_package_sizes: tuple[float, ...]
    price_basis: str
    attribute_fields: tuple[str, ...]


# Canonical templates synchronized with ASP.NET backend catalog seed
CONSTRUCTION_ITEM_TEMPLATES: Sequence[ItemTemplateDefinition] = (
    ItemTemplateDefinition(
        id="00000000-0000-0000-0000-000000000201",
        name="Paint",
        item_class="MATERIAL",
        quantity_mode="PACKAGE",
        base_unit="L",
        package_type="CAN",
        allowed_units=("L", "can"),
        allowed_package_sizes=(1.0, 4.0, 10.0, 20.0),
        price_basis="PER_PACKAGE",
        attribute_fields=("brand", "paintType", "colour", "finish"),
    ),
    ItemTemplateDefinition(
        id="00000000-0000-0000-0000-000000000202",
        name="Generator",
        item_class="EQUIPMENT",
        quantity_mode="PIECE",
        base_unit="piece",
        package_type="PIECE",
        allowed_units=("piece", "unit"),
        allowed_package_sizes=(1.0,),
        price_basis="PER_UNIT",
        attribute_fields=("brand", "model", "capacityKva", "fuelType", "phase", "voltage", "runningHours"),
    ),
    ItemTemplateDefinition(
        id="00000000-0000-0000-0000-000000000203",
        name="Cement",
        item_class="MATERIAL",
        quantity_mode="PACKAGE",
        base_unit="kg",
        package_type="BAG",
        allowed_units=("kg", "bag"),
        allowed_package_sizes=(50.0, 25.0),
        price_basis="PER_PACKAGE",
        attribute_fields=("brand", "cementType", "grade"),
    ),
    ItemTemplateDefinition(
        id="00000000-0000-0000-0000-000000000204",
        name="Reinforcement Steel",
        item_class="MATERIAL",
        quantity_mode="PIECE",
        base_unit="piece",
        package_type="ROD",
        allowed_units=("piece", "rod", "kg", "ton"),
        allowed_package_sizes=(1.0,),
        price_basis="PER_PIECE",
        attribute_fields=("diameterMm", "lengthM", "grade"),
    ),
    ItemTemplateDefinition(
        id="00000000-0000-0000-0000-000000000205",
        name="Structural Steel",
        item_class="MATERIAL",
        quantity_mode="PIECE",
        base_unit="piece",
        package_type="PIECE",
        allowed_units=("piece", "length", "ton"),
        allowed_package_sizes=(1.0,),
        price_basis="PER_PIECE",
        attribute_fields=("sectionType", "dimensions", "lengthM", "grade"),
    ),
    ItemTemplateDefinition(
        id="00000000-0000-0000-0000-000000000206",
        name="Bricks",
        item_class="MATERIAL",
        quantity_mode="PIECE",
        base_unit="piece",
        package_type="PIECE",
        allowed_units=("piece",),
        allowed_package_sizes=(1.0,),
        price_basis="PER_PIECE",
        attribute_fields=("brickType", "dimensionsMm", "compressiveStrength"),
    ),
    ItemTemplateDefinition(
        id="00000000-0000-0000-0000-000000000207",
        name="Blocks",
        item_class="MATERIAL",
        quantity_mode="PIECE",
        base_unit="piece",
        package_type="PIECE",
        allowed_units=("piece", "block"),
        allowed_package_sizes=(1.0,),
        price_basis="PER_PIECE",
        attribute_fields=("blockType", "thicknessInches"),
    ),
    ItemTemplateDefinition(
        id="00000000-0000-0000-0000-000000000208",
        name="Sand",
        item_class="MATERIAL",
        quantity_mode="CONTINUOUS_BULK",
        base_unit="m3",
        package_type=None,
        allowed_units=("m3", "cube", "ton"),
        allowed_package_sizes=(),
        price_basis="PER_UNIT",
        attribute_fields=("sandType", "screeningStatus"),
    ),
    ItemTemplateDefinition(
        id="00000000-0000-0000-0000-000000000209",
        name="Aggregate",
        item_class="MATERIAL",
        quantity_mode="CONTINUOUS_BULK",
        base_unit="m3",
        package_type=None,
        allowed_units=("m3", "cube", "ton"),
        allowed_package_sizes=(),
        price_basis="PER_UNIT",
        attribute_fields=("aggregateSize", "aggregateType"),
    ),
    ItemTemplateDefinition(
        id="00000000-0000-0000-0000-00000000020B",
        name="Tiles",
        item_class="MATERIAL",
        quantity_mode="PACKAGE",
        base_unit="sqm",
        package_type="BOX",
        allowed_units=("sqm", "box"),
        allowed_package_sizes=(1.44, 1.0, 1.08, 1.92, 2.16),
        price_basis="PER_PACKAGE",
        attribute_fields=("material", "dimensionsMm", "piecesPerBox", "finish"),
    ),
    ItemTemplateDefinition(
        id="00000000-0000-0000-0000-000000000211",
        name="Roofing Sheets",
        item_class="MATERIAL",
        quantity_mode="PIECE",
        base_unit="sheet",
        package_type="SHEET",
        allowed_units=("sheet", "piece"),
        allowed_package_sizes=(1.0,),
        price_basis="PER_PIECE",
        attribute_fields=("material", "lengthM", "widthM", "thicknessMm", "profile"),
    ),
    ItemTemplateDefinition(
        id="00000000-0000-0000-0000-000000000212",
        name="PVC Pipes",
        item_class="MATERIAL",
        quantity_mode="PIECE",
        base_unit="piece",
        package_type="PIPE",
        allowed_units=("piece", "pipe", "length"),
        allowed_package_sizes=(1.0,),
        price_basis="PER_PIECE",
        attribute_fields=("diameterMm", "lengthPerPieceM", "pressureClass", "pipeType"),
    ),
    ItemTemplateDefinition(
        id="00000000-0000-0000-0000-00000000021A",
        name="Drill",
        item_class="TOOL",
        quantity_mode="PIECE",
        base_unit="piece",
        package_type="PIECE",
        allowed_units=("piece", "unit"),
        allowed_package_sizes=(1.0,),
        price_basis="PER_UNIT",
        attribute_fields=("brand", "model", "drillType", "powerSource", "wattage"),
    ),
    ItemTemplateDefinition(
        id="00000000-0000-0000-0000-00000000021B",
        name="Angle Grinder",
        item_class="TOOL",
        quantity_mode="PIECE",
        base_unit="piece",
        package_type="PIECE",
        allowed_units=("piece", "unit"),
        allowed_package_sizes=(1.0,),
        price_basis="PER_UNIT",
        attribute_fields=("brand", "model", "discDiameterMm", "wattage", "powerSource"),
    ),
    ItemTemplateDefinition(
        id="00000000-0000-0000-0000-00000000021E",
        name="Air Compressor",
        item_class="EQUIPMENT",
        quantity_mode="PIECE",
        base_unit="piece",
        package_type="PIECE",
        allowed_units=("piece", "unit"),
        allowed_package_sizes=(1.0,),
        price_basis="PER_UNIT",
        attribute_fields=("brand", "model", "tankCapacityL", "maxPressureBar", "motorPowerHp", "powerSource"),
    ),
    ItemTemplateDefinition(
        id="00000000-0000-0000-0000-00000000021F",
        name="Concrete Mixer",
        item_class="EQUIPMENT",
        quantity_mode="PIECE",
        base_unit="piece",
        package_type="PIECE",
        allowed_units=("piece", "unit"),
        allowed_package_sizes=(1.0,),
        price_basis="PER_UNIT",
        attribute_fields=("brand", "drumCapacityL", "powerSource", "isTowable"),
    ),
    ItemTemplateDefinition(
        id="00000000-0000-0000-0000-000000000217",
        name="Scaffolding",
        item_class="TEMPORARY_WORK",
        quantity_mode="PIECE",
        base_unit="set",
        package_type="PIECE",
        allowed_units=("set", "piece"),
        allowed_package_sizes=(1.0,),
        price_basis="PER_PIECE",
        attribute_fields=("scaffoldType", "material", "componentsIncluded"),
    ),
    ItemTemplateDefinition(
        id="00000000-0000-0000-0000-000000000099",
        name="Custom Construction Item",
        item_class="OTHER_CONSTRUCTION",
        quantity_mode="PIECE",
        base_unit="piece",
        package_type=None,
        allowed_units=("piece", "kg", "m", "sqm", "m3", "bag", "box", "can", "unit"),
        allowed_package_sizes=(),
        price_basis="PER_UNIT",
        attribute_fields=("itemName", "soldAs", "specifications"),
    ),
)

_TEMPLATES_BY_NAME: Mapping[str, ItemTemplateDefinition] = {
    t.name.lower(): t for t in CONSTRUCTION_ITEM_TEMPLATES
}


def resolve_phrase_to_template(phrase: str) -> ItemTemplateDefinition | None:
    """Map user/buyer search phrases to the authoritative construction item template.

    Examples:
        '5kVA diesel generator' -> Generator
        'paint 10L'             -> Paint
        '12mm steel rods'       -> Reinforcement Steel
        'sand 5 m3'             -> Sand
    """
    if not phrase or not phrase.strip():
        return None

    clean = phrase.strip().lower()

    if "generator" in clean or "genset" in clean or "kva" in clean:
        return _TEMPLATES_BY_NAME.get("generator")

    if "paint" in clean or "emulsion" in clean or "enamel" in clean:
        return _TEMPLATES_BY_NAME.get("paint")

    if (
        "steel rod" in clean
        or "rebar" in clean
        or "steel bar" in clean
        or "reinforcement" in clean
        or re.search(r"\b\d+mm\b.*steel", clean)
        or re.search(r"steel.*\b\d+mm\b", clean)
    ):
        return _TEMPLATES_BY_NAME.get("reinforcement steel")

    if "cement" in clean or "opc" in clean or "ppc" in clean:
        return _TEMPLATES_BY_NAME.get("cement")

    if "tile" in clean and "adhesive" not in clean:
        return _TEMPLATES_BY_NAME.get("tiles")

    if "compressor" in clean or "air compressor" in clean:
        return _TEMPLATES_BY_NAME.get("air compressor")

    if "grinder" in clean or "angle grinder" in clean:
        return _TEMPLATES_BY_NAME.get("angle grinder")

    if "pvc" in clean or "pipe" in clean:
        return _TEMPLATES_BY_NAME.get("pvc pipes")

    if "roofing" in clean or "zinc alum" in clean or "corrugated" in clean:
        return _TEMPLATES_BY_NAME.get("roofing sheets")

    if "sand" in clean:
        return _TEMPLATES_BY_NAME.get("sand")

    if "aggregate" in clean:
        return _TEMPLATES_BY_NAME.get("aggregate")

    if "scaffold" in clean:
        return _TEMPLATES_BY_NAME.get("scaffolding")

    if "mixer" in clean:
        return _TEMPLATES_BY_NAME.get("concrete mixer")

    # Direct substring matches
    for name, template in _TEMPLATES_BY_NAME.items():
        if name in clean:
            return template

    return None


def get_catalog_tool_definitions() -> list[dict[str, Any]]:
    """Export catalog templates for deterministic workflow consumers."""
    return [
        {
            "id": t.id,
            "name": t.name,
            "itemClass": t.item_class,
            "quantityMode": t.quantity_mode,
            "baseUnit": t.base_unit,
            "packageType": t.package_type,
            "allowedUnits": list(t.allowed_units),
            "allowedPackageSizes": list(t.allowed_package_sizes),
            "priceBasis": t.price_basis,
            "attributes": list(t.attribute_fields),
        }
        for t in CONSTRUCTION_ITEM_TEMPLATES
    ]
