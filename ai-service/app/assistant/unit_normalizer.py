from __future__ import annotations

import re
from dataclasses import dataclass
from typing import Dict, Optional, Tuple, Any

from app.assistant.canonical_request import ItemDimensions


@dataclass
class NormalizedQuantity:
    value: float
    unit: str
    base_value: Optional[float] = None
    base_unit: Optional[str] = None
    package_count: Optional[int] = None
    package_size: Optional[float] = None
    package_unit: Optional[str] = None
    physical_sellable_count: Optional[int] = None
    coverage_area_m2: Optional[float] = None
    dimensions: Optional[ItemDimensions] = None
    display_text: str = ""


class QuantityNormalizer:
    """
    Authoritative centralized unit normalizer for SurplusLink AI Assistant.
    No downstream component may manually interpret or parse raw unit strings.
    """

    UNIT_ALIASES: Dict[str, str] = {
        "kg": "kg",
        "kgs": "kg",
        "kilogram": "kg",
        "kilograms": "kg",
        "g": "g",
        "gram": "g",
        "grams": "g",
        "l": "L",
        "ltr": "L",
        "ltrs": "L",
        "liter": "L",
        "liters": "L",
        "litre": "L",
        "litres": "L",
        "ml": "ml",
        "milliliter": "ml",
        "milliliters": "ml",
        "m": "m",
        "meter": "m",
        "meters": "m",
        "cm": "cm",
        "centimeter": "cm",
        "centimeters": "cm",
        "mm": "mm",
        "millimeter": "mm",
        "millimeters": "mm",
        "m2": "m2",
        "m²": "m2",
        "sqm": "m2",
        "square meter": "m2",
        "square meters": "m2",
        "sqft": "sqft",
        "sq ft": "sqft",
        "square feet": "sqft",
        "square foot": "sqft",
        "bag": "bag",
        "bags": "bag",
        "box": "box",
        "boxes": "box",
        "can": "can",
        "cans": "can",
        "bucket": "bucket",
        "buckets": "bucket",
        "piece": "piece",
        "pieces": "piece",
        "pcs": "piece",
        "tile": "piece",
        "tiles": "piece",
        "cartridge": "cartridge",
        "cartridges": "cartridge",
        "rs": "LKR",
        "rs.": "LKR",
        "lkr": "LKR",
    }

    @classmethod
    def parse_k_number(cls, val_str: str, is_reverse_suffix: bool = False) -> float:
        """
        Parses numbers including 'k' or 'K' suffix.
        Disambiguates between numeric multiplier 'k' (e.g., 10k = 10,000 in '10k bags' or 'Rs 10k')
        and Romanized Sinhala count suffix '-k' (e.g., 'bags 10k' = 10 bags, 'cans 3k' = 3 cans, 'tiles 4k' = 4 tiles).
        """
        cleaned = val_str.strip().lower()
        if cleaned.endswith("kg"):
            cleaned = cleaned[:-2].strip()
        elif cleaned.endswith("g") and not cleaned.endswith("kg"):
            cleaned = cleaned[:-1].strip()

        if cleaned.endswith("k"):
            val_part = cleaned[:-1].strip()
            try:
                val_num = float(val_part)
                if is_reverse_suffix:
                    # In Romanized Sinhala reverse count syntax (e.g. "bags 10k", "cans 3k", "tiles 4k"),
                    # 'k' is the Sinhala count suffix -k / -ak (meaning 10 bags, 3 cans, 4 tiles).
                    return val_num
                else:
                    # Standard numeric multiplier (e.g. 10k = 10,000)
                    return val_num * 1000.0
            except ValueError:
                pass
        return float(cleaned)

    @classmethod
    def parse_dimensions(cls, text: str) -> Optional[ItemDimensions]:
        """
        Parses physical item dimensions like '600x600', '600 x 600 mm', '2x2 ft', '300x600mm'.
        NEVER collapses dimensions into coverage area.
        """
        dim_match = re.search(
            r"(\d+(?:\.\d+)?\s*k?)\s*[xX*×]\s*(\d+(?:\.\d+)?\s*k?)(?:\s*[xX*×]\s*(\d+(?:\.\d+)?\s*k?))?\s*(mm|cm|m|meters|ft|feet|in|inches)?",
            text,
            re.IGNORECASE,
        )
        if not dim_match:
            return None

        # Exclude coverage area expressions like "12m x 10m area" if clearly room dimensions
        if re.search(r"area|room|wall|floor", text, re.IGNORECASE) and not re.search(r"mm|cm|tile|block", text, re.IGNORECASE):
            return None

        w = cls.parse_k_number(dim_match.group(1))
        l = cls.parse_k_number(dim_match.group(2))
        h = cls.parse_k_number(dim_match.group(3)) if dim_match.group(3) else None
        u_raw = (dim_match.group(4) or "mm").lower()

        unit = "mm"
        if u_raw in ["cm", "centimeter", "centimeters"]:
            unit = "cm"
        elif u_raw in ["m", "meter", "meters"]:
            unit = "m"
        elif u_raw in ["ft", "feet"]:
            unit = "ft"

        return ItemDimensions(width=w, length=l, height=h, unit=unit)

    @classmethod
    def parse_area(cls, text: str) -> Optional[Tuple[float, str]]:
        """Parses coverage area from text returning (value_in_m2, original_unit)."""
        area_match = re.search(
            r"(\d+(?:\.\d+)?\s*k?)\s*(m2|sqm|square\s*meters?|m²|sqft|square\s*feet|sq\s*ft)",
            text,
            re.IGNORECASE,
        )
        if area_match:
            val = cls.parse_k_number(area_match.group(1))
            u_str = area_match.group(2).lower()
            if "ft" in u_str:
                return (val * 0.092903, "sqft")
            return (val, "m2")

        dim_match = re.search(
            r"(\d+(?:\.\d+)?)\s*(ft|feet|m|meters)\s*[xX*×]\s*(\d+(?:\.\d+)?)\s*(ft|feet|m|meters)",
            text,
            re.IGNORECASE,
        )
        if dim_match:
            v1 = float(dim_match.group(1))
            v2 = float(dim_match.group(3))
            u1 = dim_match.group(2).lower()
            if "m" in u1:
                return (v1 * v2, "m2")
            else:
                sqft = v1 * v2
                return (sqft * 0.092903, "sqft")

        return None

    @classmethod
    def normalize_unit_name(cls, raw_unit: Optional[str]) -> str:
        if not raw_unit:
            return ""
        cleaned = raw_unit.strip().lower()
        return cls.UNIT_ALIASES.get(cleaned, cleaned)

    @classmethod
    def parse_quantity_structure(cls, text: str) -> Optional[NormalizedQuantity]:
        """
        Parses complex quantity structures including noisy Sri Lankan inputs:
        - '10k bags' -> 10,000 bags (multiplier k)
        - 'bags 10k' -> 10 bags (Romanized Sinhala count suffix -k)
        - 'cans 3k' -> 3 cans (Romanized Sinhala count suffix -k)
        - '4L cans 3k' -> package_count=3, package_size=4L, total=12L
        - 'tiles 4k' -> 4 tiles
        - 'Rs 10k' / 'budget 50k' -> 10,000 / 50,000 LKR
        - '10 x 50kg bags' -> package_count=10, package_size=50kg, total=500kg
        """
        if not text:
            return None
        text_str = text.strip()

        dims = cls.parse_dimensions(text_str)
        area_res = cls.parse_area(text_str)
        cov_m2 = area_res[0] if area_res else None

        # 0. Check monetary expressions: "Rs 10k", "budget 50k", "10k LKR"
        money_match = re.search(
            r"(rs\.?|lkr|budget|price|cost)\s*(\d+(?:\.\d+)?\s*k?)|(\d+(?:\.\d+)?\s*k?)\s*(rs\.?|lkr)",
            text_str,
            re.IGNORECASE,
        )
        if money_match:
            num_str = money_match.group(2) or money_match.group(3)
            val = cls.parse_k_number(num_str, is_reverse_suffix=False)
            return NormalizedQuantity(
                value=val,
                unit="LKR",
                base_value=val,
                base_unit="LKR",
                coverage_area_m2=cov_m2,
                dimensions=dims,
                display_text=f"LKR {val:,.2f}",
            )

        # 1. Check "4L cans 3k" pattern (Reverse noun-first with package size)
        pkg_rev_sz_match = re.search(
            r"(\d+(?:\.\d+)?)\s*(L|ltr|litres?|liter?|kg|g)?\s*(cans?|bags?|boxes?|buckets?|packs?)\s*(\d+(?:\.\d+)?\s*k?)",
            text_str,
            re.IGNORECASE,
        )
        if pkg_rev_sz_match:
            size = float(pkg_rev_sz_match.group(1))
            u_raw = (pkg_rev_sz_match.group(2) or "L").lower()
            unit = cls.normalize_unit_name(u_raw)
            count = int(cls.parse_k_number(pkg_rev_sz_match.group(4), is_reverse_suffix=True))
            total = count * size
            return NormalizedQuantity(
                value=total,
                unit=unit,
                base_value=total,
                base_unit=unit,
                package_count=count,
                package_size=size,
                package_unit=unit,
                physical_sellable_count=count,
                coverage_area_m2=cov_m2,
                dimensions=dims,
                display_text=f"{count} × {size:g}{unit} ({total:g}{unit})",
            )

        # 2. Package structure: "10 x 50kg", "10 bags of 50kg", "2 cans of 4L"
        pkg_match = re.search(
            r"(\d+(?:\.\d+)?\s*k?)\s*(?:x|\*|×|bags?|cans?|boxes?|packs?|buckets?)\s*(?:of)?\s*(\d+(?:\.\d+)?)\s*(kg|g|l|litres?|liter?|ltrs?|m2|sqm|pcs|pieces)?",
            text_str,
            re.IGNORECASE,
        )
        if pkg_match:
            count = int(cls.parse_k_number(pkg_match.group(1), is_reverse_suffix=False))
            size = cls.parse_k_number(pkg_match.group(2))
            u_raw = (pkg_match.group(3) or "kg").lower()
            unit = cls.normalize_unit_name(u_raw)
            total = count * size
            return NormalizedQuantity(
                value=total,
                unit=unit,
                base_value=total,
                base_unit=unit,
                package_count=count,
                package_size=size,
                package_unit=unit,
                physical_sellable_count=count,
                coverage_area_m2=cov_m2,
                dimensions=dims,
                display_text=f"{count} × {size:g}{unit} ({total:g}{unit})",
            )

        # 3. Reverse noisy syntax: "bags 10k", "cans 3k", "tiles 4k" (Romanized Sinhala suffix -k)
        rev_match = re.search(
            r"(bags?|cans?|boxes?|buckets?|pieces?|pcs|tiles?)\s*(\d+(?:\.\d+)?\s*k?)",
            text_str,
            re.IGNORECASE,
        )
        if rev_match:
            u_name = cls.normalize_unit_name(rev_match.group(1))
            val = cls.parse_k_number(rev_match.group(2), is_reverse_suffix=True)
            return NormalizedQuantity(
                value=val,
                unit=u_name,
                base_value=val,
                base_unit=u_name,
                package_count=int(val),
                physical_sellable_count=int(val),
                coverage_area_m2=cov_m2,
                dimensions=dims,
                display_text=f"{val:g} {u_name}",
            )

        # 4. Simple quantity with unit or number: "10k bags", "50kg", "6ltr", "12m2" (English number-first syntax)
        qty_match = re.search(
            r"(\d+(?:\.\d+)?\s*k?)\s*(kg|g|l|litres?|liter?|ltrs?|m2|sqm|sqft|bags?|cans?|boxes?|pcs|pieces|buckets?|cartridges?|tiles?)?",
            text_str,
            re.IGNORECASE,
        )
        if qty_match:
            val = cls.parse_k_number(qty_match.group(1), is_reverse_suffix=False)
            u_raw = qty_match.group(2)
            base_unit = cls.normalize_unit_name(u_raw) if u_raw else ""

            pkg_cnt = int(val) if base_unit in ["bag", "can", "box", "bucket", "piece", "cartridge"] else None

            return NormalizedQuantity(
                value=val,
                unit=base_unit,
                base_value=val,
                base_unit=base_unit,
                package_count=pkg_cnt,
                physical_sellable_count=pkg_cnt,
                coverage_area_m2=cov_m2,
                dimensions=dims,
                display_text=f"{val:g} {base_unit}".strip(),
            )

        if dims or cov_m2:
            return NormalizedQuantity(
                value=0.0,
                unit="",
                coverage_area_m2=cov_m2,
                dimensions=dims,
                display_text="",
            )

        return None

    @staticmethod
    def sqft_to_sqm(sqft: float) -> float:
        return sqft * 0.092903

    @staticmethod
    def sqm_to_sqft(sqm: float) -> float:
        return sqm * 10.7639


# Backwards compatibility alias
UnitNormalizer = QuantityNormalizer
