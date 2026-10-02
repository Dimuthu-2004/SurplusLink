from __future__ import annotations

import logging
from dataclasses import dataclass
from typing import Any, Callable, Dict, List, Optional
from pydantic import BaseModel, ConfigDict, Field

from app.assistant.canonical_request import CanonicalUserRequest
from app.assistant.material_estimation import MaterialEstimationEngine
from app.assistant.unit_normalizer import QuantityNormalizer

logger = logging.getLogger(__name__)


class CapabilityResult(BaseModel):
    model_config = ConfigDict(extra="ignore")

    success: bool
    capability_name: str
    data: Dict[str, Any] = Field(default_factory=dict)
    summary: str = ""
    error: Optional[str] = None
    provenance: Optional[str] = None  # e.g. "ASP.NET Core /api/ai-internal/tools/*", "RAG Retriever", "MaterialEstimationEngine"


@dataclass
class Capability:
    name: str
    required_inputs: List[str]
    optional_inputs: List[str]
    handler: Callable[..., Any]
    provenance: str

    def validate_inputs(self, inputs: Dict[str, Any]) -> List[str]:
        missing = [inp for inp in self.required_inputs if inp not in inputs or inputs[inp] is None]
        return missing


class CapabilityRegistry:
    """
    Authoritative registry of executable capabilities.
    All capability execution flows through this registry without scattered if/else branches.
    """

    def __init__(self):
        self._capabilities: Dict[str, Capability] = {}
        self._register_default_capabilities()

    def register(self, capability: Capability) -> None:
        self._capabilities[capability.name] = capability

    def get(self, name: str) -> Optional[Capability]:
        return self._capabilities.get(name)

    async def execute(self, name: str, inputs: Dict[str, Any]) -> CapabilityResult:
        cap = self.get(name)
        if not cap:
            return CapabilityResult(
                success=False,
                capability_name=name,
                error=f"Capability '{name}' is not registered in CapabilityRegistry.",
            )

        missing = cap.validate_inputs(inputs)
        if missing:
            return CapabilityResult(
                success=False,
                capability_name=name,
                error=f"Missing required inputs for '{name}': {', '.join(missing)}",
                provenance=cap.provenance,
            )

        try:
            res = await cap.handler(**inputs) if self._is_async(cap.handler) else cap.handler(**inputs)
            if isinstance(res, CapabilityResult):
                return res
            return CapabilityResult(
                success=True,
                capability_name=name,
                data=res if isinstance(res, dict) else {"result": res},
                summary=str(res.get("summary", "")) if isinstance(res, dict) else str(res),
                provenance=cap.provenance,
            )
        except Exception as ex:
            logger.exception("Capability '%s' execution failed: %s", name, str(ex))
            return CapabilityResult(
                success=False,
                capability_name=name,
                error=str(ex),
                provenance=cap.provenance,
            )

    @staticmethod
    def _is_async(fn: Callable) -> bool:
        import inspect

        return inspect.iscoroutinefunction(fn)

    def _register_default_capabilities(self) -> None:
        # 1. Paint Estimator
        self.register(
            Capability(
                name="estimate.paint",
                required_inputs=[],
                optional_inputs=["area_sqm", "area_sqft", "coats", "paint_type", "finish"],
                handler=lambda **kwargs: MaterialEstimationEngine.estimate("paint", kwargs),
                provenance="MaterialEstimationEngine",
            )
        )

        # 2. Tile Estimator
        self.register(
            Capability(
                name="estimate.tiles",
                required_inputs=[],
                optional_inputs=["area_sqm", "area_sqft", "tile_width_mm", "tile_length_mm", "wastage_percent"],
                handler=lambda **kwargs: MaterialEstimationEngine.estimate("tiles", kwargs),
                provenance="MaterialEstimationEngine",
            )
        )

        # 3. Cement Estimator
        self.register(
            Capability(
                name="estimate.cement",
                required_inputs=[],
                optional_inputs=["concrete_volume_m3", "plaster_area_sqm", "bag_weight_kg"],
                handler=lambda **kwargs: MaterialEstimationEngine.estimate("cement", kwargs),
                provenance="MaterialEstimationEngine",
            )
        )

        # 4. Sealant Estimator
        self.register(
            Capability(
                name="estimate.sealant",
                required_inputs=[],
                optional_inputs=["joint_length_m", "joint_width_mm", "joint_depth_mm"],
                handler=lambda **kwargs: MaterialEstimationEngine.estimate("sealant", kwargs),
                provenance="MaterialEstimationEngine",
            )
        )

        # 5. Quantity Conversion
        self.register(
            Capability(
                name="quantity.convert",
                required_inputs=["value", "from_unit", "to_unit"],
                optional_inputs=[],
                handler=self._convert_units,
                provenance="QuantityNormalizer",
            )
        )

    @staticmethod
    def _convert_units(value: float, from_unit: str, to_unit: str) -> Dict[str, Any]:
        u1 = QuantityNormalizer.normalize_unit_name(from_unit)
        u2 = QuantityNormalizer.normalize_unit_name(to_unit)
        if u1 == u2:
            return {"value": value, "unit": u2}
        if u1 == "sqft" and u2 == "m2":
            res = QuantityNormalizer.sqft_to_sqm(value)
            return {"value": res, "unit": "m2"}
        if u1 == "m2" and u2 == "sqft":
            res = QuantityNormalizer.sqm_to_sqft(value)
            return {"value": res, "unit": "sqft"}
        if u1 == "g" and u2 == "kg":
            return {"value": value / 1000.0, "unit": "kg"}
        if u1 == "kg" and u2 == "g":
            return {"value": value * 1000.0, "unit": "g"}
        if u1 == "ml" and u2 == "L":
            return {"value": value / 1000.0, "unit": "L"}
        if u1 == "L" and u2 == "ml":
            return {"value": value * 1000.0, "unit": "ml"}
        raise ValueError(f"Unsupported unit conversion from {from_unit} to {to_unit}")
