from __future__ import annotations

import logging
import re
from typing import Any, Dict, List, Optional, Tuple
from pydantic import BaseModel, ConfigDict, Field

logger = logging.getLogger(__name__)


class ResolvedItem(BaseModel):
    model_config = ConfigDict(extra="ignore")

    raw_candidate: str
    display_name: str
    catalog_item_id: Optional[str] = None
    category_id: Optional[str] = None
    is_custom_item: bool = False
    resolution_source: str = "UNKNOWN"  # "CATALOG_EXACT", "CATALOG_ALIAS", "CATALOG_SEMANTIC", "CUSTOM_KNOWN", "CUSTOM_OPEN_VOCABULARY"
    base_unit: str = "piece"
    allowed_units: List[str] = Field(default_factory=lambda: ["piece", "bag", "box", "can", "kg", "L", "sqm", "m2", "item"])
    allowed_package_sizes: List[float] = Field(default_factory=list)
    package_type: Optional[str] = None
    confidence: float = 1.0


# Default catalog templates used as fallback when DB catalog is unreachable
BUILTIN_CATALOG_TEMPLATES: List[Dict[str, Any]] = [
    {
        "id": "paint_template",
        "categoryId": "cat_paint",
        "name": "Paint",
        "aliases": ["paint", "emulsion", "wall paint", "exterior paint", "weather-shield", "primer", "enamel", "gloss paint"],
        "baseUnit": "L",
        "allowedUnits": ["L", "litre", "litres", "liter", "liters", "ltr", "can", "cans", "bucket", "buckets"],
        "allowedPackageSizes": [1.0, 4.0, 10.0, 20.0],
        "packageType": "CAN",
    },
    {
        "id": "cement_template",
        "categoryId": "cat_cement",
        "name": "Cement",
        "aliases": ["cement", "portland cement", "masonry cement", "slc cement", "concrete mix"],
        "baseUnit": "kg",
        "allowedUnits": ["kg", "kilogram", "kilograms", "bag", "bags", "pack", "packs"],
        "allowedPackageSizes": [25.0, 50.0],
        "packageType": "BAG",
    },
    {
        "id": "tiles_template",
        "categoryId": "cat_tiles",
        "name": "Tiles",
        "aliases": ["tiles", "tile", "ceramic tile", "ceramic tiles", "porcelain tile", "porcelain tiles", "floor tile", "wall tile"],
        "baseUnit": "m2",
        "allowedUnits": ["m2", "sqm", "square meters", "sqft", "box", "boxes", "piece", "pieces"],
        "allowedPackageSizes": [1.44, 1.0, 2.0],
        "packageType": "BOX",
    },
    {
        "id": "steel_template",
        "categoryId": "cat_steel",
        "name": "Steel Rebar",
        "aliases": ["steel", "rebar", "steel rebar", "tmt bar", "steel rod", "iron rod"],
        "baseUnit": "kg",
        "allowedUnits": ["kg", "ton", "tons", "piece", "pieces", "bar", "bars"],
        "allowedPackageSizes": [50.0, 1000.0],
        "packageType": "BUNDLE",
    },
    {
        "id": "pvc_template",
        "categoryId": "cat_pvc",
        "name": "PVC Pipes",
        "aliases": ["pvc", "pvc pipe", "pvc pipes", "pipe", "conduit"],
        "baseUnit": "piece",
        "allowedUnits": ["piece", "pieces", "meter", "meters", "m", "length"],
        "allowedPackageSizes": [4.0, 6.0],
        "packageType": "PIECE",
    },
    {
        "id": "sealant_template",
        "categoryId": "cat_sealant",
        "name": "Sealant & Adhesive",
        "aliases": ["sealant", "silicone sealant", "tile adhesive", "waterproofing sealant", "grout"],
        "baseUnit": "cartridge",
        "allowedUnits": ["cartridge", "cartridges", "kg", "can", "cans", "bag", "bags"],
        "allowedPackageSizes": [0.3, 1.0, 5.0, 20.0],
        "packageType": "CARTRIDGE",
    },
]

# Known common open-vocabulary construction / reuse items
KNOWN_CUSTOM_ITEMS: Dict[str, Dict[str, Any]] = {
    "water pump": {"display_name": "Water Pump", "base_unit": "piece"},
    "generator": {"display_name": "Generator", "base_unit": "piece"},
    "air compressor": {"display_name": "Air Compressor", "base_unit": "piece"},
    "concrete mixer": {"display_name": "Concrete Mixer", "base_unit": "piece"},
    "marble": {"display_name": "Marble", "base_unit": "sqm"},
    "marble slab": {"display_name": "Marble Slab", "base_unit": "piece"},
    "roofing sheet": {"display_name": "Roofing Sheet", "base_unit": "piece"},
    "scaffolding": {"display_name": "Scaffolding", "base_unit": "piece"},
    "bricks": {"display_name": "Bricks", "base_unit": "piece"},
    "cement blocks": {"display_name": "Cement Blocks", "base_unit": "piece"},
    "plywood": {"display_name": "Plywood", "base_unit": "piece"},
    "sand": {"display_name": "Sand", "base_unit": "m3"},
    "gravel": {"display_name": "Gravel", "base_unit": "m3"},
    "timber": {"display_name": "Timber", "base_unit": "m"},
}


class ItemResolver:
    """
    Authoritative Open-Vocabulary Item Resolver for SurplusLink.
    
    Resolution order:
    1. Exact catalog template match
    2. Catalog aliases match
    3. Semantic catalog match
    4. Existing custom listing/item names
    5. Valid unseen construction/reuse item candidate (e.g. "Water Pump", "Generator")
    6. Clarification only when genuinely ambiguous
    """

    def __init__(self, custom_catalog: Optional[List[Dict[str, Any]]] = None):
        self.catalog = custom_catalog if custom_catalog is not None else BUILTIN_CATALOG_TEMPLATES

    def resolve(self, candidate_text: Optional[str], catalog_override: Optional[List[Dict[str, Any]]] = None) -> Optional[ResolvedItem]:
        if not candidate_text:
            return None

        clean_text = candidate_text.strip()
        if not clean_text or len(clean_text) < 2:
            return None

        needle = clean_text.casefold()
        catalog_to_use = catalog_override if catalog_override is not None else self.catalog

        # 1. Exact catalog name match
        for row in catalog_to_use:
            c_name = str(row.get("name", "")).casefold()
            if needle == c_name:
                return ResolvedItem(
                    raw_candidate=clean_text,
                    display_name=str(row.get("name")),
                    catalog_item_id=str(row.get("id")),
                    category_id=str(row.get("categoryId", "GENERAL")),
                    is_custom_item=False,
                    resolution_source="CATALOG_EXACT",
                    base_unit=str(row.get("baseUnit", "piece")),
                    allowed_units=[str(u) for u in row.get("allowedUnits", [])],
                    allowed_package_sizes=[float(s) for s in row.get("allowedPackageSizes", [])],
                    package_type=row.get("packageType"),
                    confidence=1.0,
                )

        # 2. Catalog aliases match
        for row in catalog_to_use:
            aliases = [str(a).casefold() for a in row.get("aliases", [])]
            if needle in aliases or any(alias in needle for alias in aliases):
                return ResolvedItem(
                    raw_candidate=clean_text,
                    display_name=str(row.get("name")),
                    catalog_item_id=str(row.get("id")),
                    category_id=str(row.get("categoryId", "GENERAL")),
                    is_custom_item=False,
                    resolution_source="CATALOG_ALIAS",
                    base_unit=str(row.get("baseUnit", "piece")),
                    allowed_units=[str(u) for u in row.get("allowedUnits", [])],
                    allowed_package_sizes=[float(s) for s in row.get("allowedPackageSizes", [])],
                    package_type=row.get("packageType"),
                    confidence=0.95,
                )

        # 3. Partial / Semantic catalog match
        for row in catalog_to_use:
            c_name = str(row.get("name", "")).casefold()
            if c_name in needle or needle in c_name:
                return ResolvedItem(
                    raw_candidate=clean_text,
                    display_name=str(row.get("name")),
                    catalog_item_id=str(row.get("id")),
                    category_id=str(row.get("categoryId", "GENERAL")),
                    is_custom_item=False,
                    resolution_source="CATALOG_SEMANTIC",
                    base_unit=str(row.get("baseUnit", "piece")),
                    allowed_units=[str(u) for u in row.get("allowedUnits", [])],
                    allowed_package_sizes=[float(s) for s in row.get("allowedPackageSizes", [])],
                    package_type=row.get("packageType"),
                    confidence=0.90,
                )

        # 4. Known custom items lookup
        if needle in KNOWN_CUSTOM_ITEMS:
            info = KNOWN_CUSTOM_ITEMS[needle]
            return ResolvedItem(
                raw_candidate=clean_text,
                display_name=info["display_name"],
                catalog_item_id=None,
                category_id="CUSTOM_EQUIPMENT",
                is_custom_item=True,
                resolution_source="CUSTOM_KNOWN",
                base_unit=info.get("base_unit", "piece"),
                allowed_units=["piece", "pieces", "unit", "units", "set", "sets", "box", "boxes", "kg", "m2", "item"],
                confidence=0.90,
            )

        # 5. Open-vocabulary valid construction/reuse item candidate
        # Filter out purely generic question/greeting words
        stop_words = {"item", "items", "material", "materials", "thing", "things", "product", "products", "stuff", "surplus", "requirement"}
        if needle in stop_words:
            return None

        # Titlecase user's item candidate as display name
        display_title = clean_text.title()
        return ResolvedItem(
            raw_candidate=clean_text,
            display_name=display_title,
            catalog_item_id=None,
            category_id="CUSTOM_GENERAL",
            is_custom_item=True,
            resolution_source="CUSTOM_OPEN_VOCABULARY",
            base_unit="piece",
            allowed_units=["piece", "pieces", "unit", "units", "set", "sets", "bag", "bags", "can", "cans", "box", "boxes", "kg", "L", "m2", "item"],
            confidence=0.85,
        )

