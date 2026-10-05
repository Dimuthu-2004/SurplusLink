import math
from dataclasses import dataclass
from typing import Any, Dict, Optional, Tuple

from app.assistant.unit_normalizer import UnitNormalizer


@dataclass
class EstimationResult:
    material: str
    input_summary: str
    calculated_value: float
    unit: str
    recommended_package: Optional[str] = None
    explanation: str = ""
    assumptions: str = ""
    calculated_package_count: Optional[int] = None
    calculated_physical_quantity: Optional[int] = None


class MaterialEstimationEngine:
    """Deterministic material calculator for construction materials."""

    @classmethod
    def estimate(cls, material_type: str, params: Dict[str, Any]) -> EstimationResult:
        mat = material_type.lower().strip()
        if "paint" in mat:
            return cls.estimate_paint(params)
        elif "tile" in mat:
            return cls.estimate_tiles(params)
        elif "cement" in mat:
            return cls.estimate_cement(params)
        elif "block" in mat or "brick" in mat:
            return cls.estimate_blocks(params)
        elif "concrete" in mat or "mortar" in mat:
            return cls.estimate_concrete(params)
        elif "sealant" in mat or "silicone" in mat:
            return cls.estimate_sealant(params)
        else:
            return EstimationResult(
                material=material_type,
                input_summary=str(params),
                calculated_value=0.0,
                unit="units",
                explanation=f"Material estimation for '{material_type}' requires specific dimension and specification inputs.",
            )

    @classmethod
    def estimate_paint(cls, params: Dict[str, Any]) -> EstimationResult:
        area_sqm = params.get("area_sqm")
        if area_sqm is None:
            area_sqft = params.get("area_sqft")
            if area_sqft is not None:
                area_sqm = UnitNormalizer.sqft_to_sqm(area_sqft)

        if area_sqm is None and all(k in params for k in ("width", "length")):
            w = float(params["width"])
            l = float(params["length"])
            h = float(params.get("height", 0))
            if h > 0:
                area_sqm = 2 * (w + l) * h
                if params.get("include_ceiling", False):
                    area_sqm += (w * l)
            else:
                area_sqm = w * l

        if area_sqm is None or area_sqm <= 0:
            return EstimationResult(
                material="Paint",
                input_summary="Missing area dimensions",
                calculated_value=0.0,
                unit="L",
                explanation="Please provide the surface area (e.g. 10m² or 12ft x 10ft wall height 9ft) to calculate required paint volume.",
            )

        coats = int(params.get("coats", 2))
        coverage_per_coat = float(params.get("coverage_rate", 10.0))
        wastage_pct = float(params.get("wastage_pct", 10.0)) / 100.0

        total_surface = area_sqm * coats
        base_litres = total_surface / coverage_per_coat
        total_litres = base_litres * (1 + wastage_pct)
        total_litres_rounded = math.ceil(total_litres * 10) / 10.0

        recommended_pkg = ""
        if total_litres_rounded <= 4:
            recommended_pkg = f"1 × 4L can (or 4 × 1L cans)"
        elif total_litres_rounded <= 10:
            recommended_pkg = f"1 × 10L bucket"
        else:
            buckets_10l = math.ceil(total_litres_rounded / 10.0)
            recommended_pkg = f"{buckets_10l} × 10L buckets"

        input_desc = f"{area_sqm:.1f} m² surface area, {coats} coat(s)"
        explanation = (
            f"For {area_sqm:.1f} m² with {coats} coat(s):\n"
            f"• Total surface coverage: {total_surface:.1f} m²\n"
            f"• Estimated coverage rate: ~{coverage_per_coat:g} m²/L per coat\n"
            f"• Wastage allowance: 10%\n"
            f"• **Estimated Paint Required: {total_litres_rounded:g} Litres** ({recommended_pkg})"
        )

        return EstimationResult(
            material="Paint",
            input_summary=input_desc,
            calculated_value=total_litres_rounded,
            unit="L",
            recommended_package=recommended_pkg,
            explanation=explanation,
            assumptions="Standard coverage rate 10 m²/L per coat on prepared substrate with 10% wastage.",
        )

    @classmethod
    def estimate_tiles(cls, params: Dict[str, Any]) -> EstimationResult:
        area_sqm = params.get("area_sqm")
        if area_sqm is None and params.get("area_sqft") is not None:
            area_sqm = UnitNormalizer.sqft_to_sqm(float(params["area_sqft"]))

        if area_sqm is None and all(k in params for k in ("width", "length")):
            w = float(params["width"])
            l = float(params["length"])
            area_sqm = w * l

        if area_sqm is None or area_sqm <= 0:
            return EstimationResult(
                material="Tiles",
                input_summary="Missing floor/wall area",
                calculated_value=0.0,
                unit="pieces",
                explanation="Please provide the floor or wall area (e.g. 15m² or 10ft x 12ft) to calculate tile requirements.",
            )

        # Parse tile dimensions in mm or cm (default 600x600 mm = 60x60 cm)
        if "tile_width_mm" in params and "tile_length_mm" in params:
            tile_w_mm = float(params["tile_width_mm"])
            tile_h_mm = float(params["tile_length_mm"])
            tile_w_cm = tile_w_mm / 10.0
            tile_h_cm = tile_h_mm / 10.0
        else:
            tile_w_cm = float(params.get("tile_width_cm", 60))
            tile_h_cm = float(params.get("tile_height_cm", 60))
            tile_w_mm = tile_w_cm * 10.0
            tile_h_mm = tile_h_cm * 10.0

        tile_area_sqm = (tile_w_cm / 100.0) * (tile_h_cm / 100.0)
        wastage_pct = float(params.get("wastage_pct", 10.0)) / 100.0
        total_area_needed = area_sqm * (1 + wastage_pct)

        tiles_needed = math.ceil(total_area_needed / tile_area_sqm)
        package_size = params.get("package_size", params.get("tiles_per_box", 4))
        tiles_per_package = int(package_size)
        if tiles_per_package <= 0:
            tiles_per_package = 4
        boxes_needed = math.ceil(tiles_needed / tiles_per_package)

        explanation = (
            f"Calculated tile requirement for {area_sqm:g} m² coverage using {tile_w_mm:g}×{tile_h_mm:g} mm tiles:\n"
            f"• Area per tile: {tile_area_sqm:.3f} m²\n"
            f"• Wastage & cutting allowance: 10%\n"
            f"• **Tiles Needed: {tiles_needed} pieces** ({boxes_needed} boxes @ {tiles_per_package} pcs/box)"
        )

        return EstimationResult(
            material="Tiles",
            input_summary=f"{area_sqm:g} m² area, {tile_w_mm:g}×{tile_h_mm:g} mm tiles",
            calculated_value=float(tiles_needed),
            unit="pcs",
            recommended_package=f"{boxes_needed} boxes",
            explanation=explanation,
            assumptions="10% cutting and wastage allowance included.",
            calculated_package_count=boxes_needed,
            calculated_physical_quantity=tiles_needed,
        )

    @classmethod
    def estimate_cement(cls, params: Dict[str, Any]) -> EstimationResult:
        conc_m3 = params.get("concrete_m3") or params.get("volume_m3")
        if conc_m3 is not None:
            bags_needed = math.ceil(float(conc_m3) * 6.5)
            return EstimationResult(
                material="Cement",
                input_summary=f"{conc_m3:g} m³ concrete volume",
                calculated_value=float(bags_needed),
                unit="bags",
                recommended_package=f"{bags_needed} × 50kg bags",
                explanation=f"Estimated {bags_needed} bags (50kg each) for {conc_m3:g} m³ concrete.",
                calculated_package_count=bags_needed,
                calculated_physical_quantity=bags_needed,
            )

        area_sqm = params.get("area_sqm", 10.0)
        bags_needed = math.ceil(area_sqm * 0.4)
        return EstimationResult(
            material="Cement",
            input_summary=f"{area_sqm:g} m² coverage",
            calculated_value=float(bags_needed),
            unit="bags",
            recommended_package=f"{bags_needed} × 50kg bags",
            explanation=f"Estimated {bags_needed} bags (50kg each) for {area_sqm:g} m² application.",
            calculated_package_count=bags_needed,
            calculated_physical_quantity=bags_needed,
        )

    @classmethod
    def estimate_blocks(cls, params: Dict[str, Any]) -> EstimationResult:
        area_sqm = params.get("area_sqm", 10.0)
        blocks_needed = math.ceil(area_sqm * 12.5 * 1.05)
        return EstimationResult(
            material="Blocks",
            input_summary=f"{area_sqm:g} m² wall area",
            calculated_value=float(blocks_needed),
            unit="piece",
            explanation=f"Estimated {blocks_needed} blocks for {area_sqm:g} m² wall area (includes 5% wastage).",
            calculated_physical_quantity=blocks_needed,
        )

    @classmethod
    def estimate_concrete(cls, params: Dict[str, Any]) -> EstimationResult:
        vol = params.get("volume_m3", 1.0)
        return EstimationResult(
            material="Concrete",
            input_summary=f"{vol:g} m³ volume",
            calculated_value=float(vol),
            unit="m3",
            explanation=f"Estimated {vol:g} m³ readymix or batch concrete.",
        )

    @classmethod
    def estimate_sealant(cls, params: Dict[str, Any]) -> EstimationResult:
        meters = params.get("linear_meters", 10.0)
        cartridges = math.ceil(meters / 3.0)
        return EstimationResult(
            material="Sealant",
            input_summary=f"{meters:g} linear meters joint",
            calculated_value=float(cartridges),
            unit="cartridge",
            explanation=f"Estimated {cartridges} cartridges (300ml) for {meters:g}m joint.",
            calculated_package_count=cartridges,
            calculated_physical_quantity=cartridges,
        )
