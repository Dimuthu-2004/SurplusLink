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
        # Area in sqm or sqft
        area_sqm = params.get("area_sqm")
        if area_sqm is None:
            area_sqft = params.get("area_sqft")
            if area_sqft is not None:
                area_sqm = UnitNormalizer.sqft_to_sqm(area_sqft)

        # Handle room dimensions: width, length, height
        if area_sqm is None and all(k in params for k in ("width", "length")):
            w = float(params["width"])
            l = float(params["length"])
            h = float(params.get("height", 0))
            if h > 0:
                # 4 walls area
                area_sqm = 2 * (w + l) * h
                # Optionally add ceiling
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
        # Coverage rate per coat (default 10 m2/L per coat for standard emulsion)
        coverage_per_coat = float(params.get("coverage_rate", 10.0))
        wastage_pct = float(params.get("wastage_pct", 10.0)) / 100.0

        # Math calculation
        total_surface = area_sqm * coats
        base_litres = total_surface / coverage_per_coat
        total_litres = base_litres * (1 + wastage_pct)
        total_litres_rounded = math.ceil(total_litres * 10) / 10.0

        # Standard container recommendation (1L, 4L, 10L, 20L)
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

        # Tile size in cm (default 60x60cm = 0.36 m2 per tile)
        tile_w_cm = float(params.get("tile_width_cm", 60))
        tile_h_cm = float(params.get("tile_height_cm", 60))
        tile_area_sqm = (tile_w_cm / 100.0) * (tile_h_cm / 100.0)

        wastage_pct = float(params.get("wastage_pct", 10.0)) / 100.0
        total_area_needed = area_sqm * (1 + wastage_pct)

        tiles_needed = math.ceil(total_area_needed / tile_area_sqm)
        # Standard box count (e.g. 4 tiles per box for 60x60cm)
        tiles_per_box = int(params.get("tiles_per_box", 4))
        boxes_needed = math.ceil(tiles_needed / tiles_per_box)

        explanation = (
            f"For {area_sqm:.1f} m² floor/wall area using {tile_w_cm:g}x{tile_h_cm:g}cm tiles:\n"
            f"• Area per tile: {tile_area_sqm:.3f} m²\n"
            f"• Wastage & cutting allowance: 10%\n"
            f"• **Tiles Needed: {tiles_needed} pieces** ({boxes_needed} boxes @ {tiles_per_box} pcs/box)"
        )

        return EstimationResult(
            material="Tiles",
            input_summary=f"{area_sqm:.1f} m² area, {tile_w_cm:g}x{tile_h_cm:g}cm tiles",
            calculated_value=float(tiles_needed),
            unit="pcs",
            recommended_package=f"{boxes_needed} boxes",
            explanation=explanation,
            assumptions="10% cutting and wastage allowance included.",
        )

    @classmethod
    def estimate_cement(cls, params: Dict[str, Any]) -> EstimationResult:
        # Plastering area or concrete volume
        plaster_sqm = params.get("plaster_sqm")
        concrete_m3 = params.get("concrete_m3")

        if concrete_m3 is not None and float(concrete_m3) > 0:
            vol = float(concrete_m3)
            # Standard 1:2:4 concrete mix requires approx 6.5 bags (50kg) per m3
            bags = math.ceil(vol * 6.5)
            total_kg = bags * 50
            explanation = (
                f"For {vol:.2f} m³ Grade 20 concrete (1:2:4 mix):\n"
                f"• Cement required: ~6.5 bags per m³\n"
                f"• **Cement Bags: {bags} bags (50kg each = {total_kg} kg total)**"
            )
            return EstimationResult(
                material="Cement",
                input_summary=f"{vol:.2f} m³ concrete",
                calculated_value=float(bags),
                unit="bags",
                recommended_package=f"{bags} bags (50kg)",
                explanation=explanation,
            )

        if plaster_sqm is not None and float(plaster_sqm) > 0:
            area = float(plaster_sqm)
            # Standard 12mm plastering (1:4 mix) requires ~0.12 bags (50kg) per m2
            bags = math.ceil(area * 0.12)
            explanation = (
                f"For {area:.1f} m² wall plastering (12mm thickness, 1:4 mortar mix):\n"
                f"• **Cement Bags: {bags} bags (50kg each)**"
            )
            return EstimationResult(
                material="Cement",
                input_summary=f"{area:.1f} m² plaster",
                calculated_value=float(bags),
                unit="bags",
                recommended_package=f"{bags} bags (50kg)",
                explanation=explanation,
            )

        return EstimationResult(
            material="Cement",
            input_summary="Missing plaster area or concrete volume",
            calculated_value=0.0,
            unit="bags",
            explanation="Please specify plaster area in m² or concrete volume in m³ to calculate cement bags.",
        )

    @classmethod
    def estimate_blocks(cls, params: Dict[str, Any]) -> EstimationResult:
        wall_sqm = params.get("wall_sqm")
        if wall_sqm is None and all(k in params for k in ("length", "height")):
            wall_sqm = float(params["length"]) * float(params["height"])

        if wall_sqm is None or wall_sqm <= 0:
            return EstimationResult(
                material="Cement Blocks",
                input_summary="Missing wall dimensions",
                calculated_value=0.0,
                unit="blocks",
                explanation="Please provide the wall length and height (or total wall area in m²) to calculate cement block count.",
            )

        # Standard cement block size in Sri Lanka: 16" x 8" x 4"/6" (400mm x 200mm face)
        # ~12.5 blocks per m2 of wall including mortar joints
        blocks_per_sqm = 12.5
        wastage_pct = float(params.get("wastage_pct", 5.0)) / 100.0

        raw_blocks = wall_sqm * blocks_per_sqm
        total_blocks = math.ceil(raw_blocks * (1 + wastage_pct))

        explanation = (
            f"For {wall_sqm:.1f} m² wall area using standard cement masonry blocks (16\" x 8\" face):\n"
            f"• Standard rate: ~12.5 blocks/m²\n"
            f"• Wastage allowance: 5%\n"
            f"• **Blocks Required: {total_blocks} blocks**"
        )

        return EstimationResult(
            material="Cement Blocks",
            input_summary=f"{wall_sqm:.1f} m² wall area",
            calculated_value=float(total_blocks),
            unit="blocks",
            recommended_package=f"{total_blocks} blocks",
            explanation=explanation,
        )

    @classmethod
    def estimate_concrete(cls, params: Dict[str, Any]) -> EstimationResult:
        v_m3 = float(params.get("volume_m3", 0))
        if v_m3 <= 0 and all(k in params for k in ("length", "width", "thickness")):
            v_m3 = float(params["length"]) * float(params["width"]) * float(params["thickness"])

        if v_m3 <= 0:
            return EstimationResult(
                material="Concrete",
                input_summary="Missing volume parameters",
                calculated_value=0.0,
                unit="m3",
                explanation="Please provide length, width, and thickness to calculate concrete volume.",
            )

        bags_cement = math.ceil(v_m3 * 6.5)
        sand_m3 = v_m3 * 0.45
        gravel_m3 = v_m3 * 0.90

        explanation = (
            f"For {v_m3:.2f} m³ Grade 20 Concrete (1:2:4 ratio):\n"
            f"• Cement: {bags_cement} bags (50kg)\n"
            f"• Sand: ~{sand_m3:.2f} m³ ({sand_m3*35.315:.1f} cu.ft)\n"
            f"• Coarse Aggregate (Gravel/Metal): ~{gravel_m3:.2f} m³ ({gravel_m3*35.315:.1f} cu.ft)"
        )

        return EstimationResult(
            material="Concrete Mix",
            input_summary=f"{v_m3:.2f} m³ concrete",
            calculated_value=v_m3,
            unit="m3",
            explanation=explanation,
        )

    @classmethod
    def estimate_sealant(cls, params: Dict[str, Any]) -> EstimationResult:
        length_m = float(params.get("joint_length_m", 0))
        width_mm = float(params.get("joint_width_mm", 6.0))
        depth_mm = float(params.get("joint_depth_mm", 6.0))

        if length_m <= 0:
            return EstimationResult(
                material="Sealant",
                input_summary="Missing joint length",
                calculated_value=0.0,
                unit="cartridges",
                explanation="Please provide total joint length in meters to calculate sealant cartridge requirement.",
            )

        # Volume per meter in ml = width(mm) * depth(mm) * 1(m)
        ml_per_meter = width_mm * depth_mm
        total_ml = length_m * ml_per_meter * 1.15  # 15% wastage
        # Standard silicone cartridge is 300ml
        cartridges = math.ceil(total_ml / 300.0)

        explanation = (
            f"For {length_m:.1f}m joint length ({width_mm:g}mm width x {depth_mm:g}mm depth):\n"
            f"• Total volume: {total_ml:.0f} ml (includes 15% wastage)\n"
            f"• **Sealant Cartridges Needed: {cartridges} cartridges (300ml each)**"
        )

        return EstimationResult(
            material="Sealant",
            input_summary=f"{length_m:.1f}m joint length",
            calculated_value=float(cartridges),
            unit="cartridges",
            recommended_package=f"{cartridges} cartridges (300ml)",
            explanation=explanation,
        )
