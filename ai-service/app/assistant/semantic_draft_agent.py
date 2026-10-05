from __future__ import annotations

import json
from typing import Any, Dict, List, Optional, Tuple

from app.assistant.item_resolver import ItemResolver, ResolvedItem
from app.assistant.material_estimation import MaterialEstimationEngine
from app.assistant.semantic_schemas import ExtractedSlots, RequirementDraft


class CatalogDraftValidator:
    """
    Applies deterministic catalog & open-vocabulary custom item rules to semantic LLM slots.
    Preserves multi-turn location, package details, and performs tile coverage estimation.
    """

    def __init__(self, item_resolver: Optional[ItemResolver] = None):
        self.item_resolver = item_resolver or ItemResolver()

    def build_or_update(
        self,
        slots: ExtractedSlots,
        catalog: List[Dict[str, Any]],
        current: Optional[RequirementDraft] = None,
    ) -> Tuple[Optional[RequirementDraft], Optional[str]]:
        item_text = slots.item
        resolved: Optional[ResolvedItem] = None

        if item_text:
            resolved = self.item_resolver.resolve(item_text, catalog_override=catalog)
        elif current:
            resolved = self.item_resolver.resolve(current.item_name, catalog_override=catalog)

        # If no item resolved and no current draft, ask for item
        if not resolved and not current:
            return None, "What construction item or equipment do you need?"

        if current and resolved and not slots.item:
            resolved = self.item_resolver.resolve(current.item_name, catalog_override=catalog)

        # Check for missing unit when simple numeric quantity is provided without unit or package_count
        if slots.quantity and slots.quantity.value is not None and not slots.quantity.unit and not slots.quantity.package_count:
            draft = current.model_copy(deep=True) if current else RequirementDraft(
                template_id=resolved.catalog_item_id or "CUSTOM_ITEM" if resolved else "CUSTOM_ITEM",
                category_id=resolved.category_id or "GENERAL" if resolved else "GENERAL",
                item_name=resolved.display_name if resolved else (item_text or "Item"),
                display_name=resolved.display_name if resolved else (item_text or "Item"),
                is_custom_item=resolved.is_custom_item if resolved else True,
                input_mode="BASE_QUANTITY",
                normalized_base_unit=resolved.base_unit if resolved else "piece",
                entered_quantity=slots.quantity.value,
                normalized_quantity=None,
            )
            return draft, "What unit should I use for that quantity?"

        # 1. Custom Item vs Catalog Template Draft Creation
        if resolved and resolved.is_custom_item:
            draft = current.model_copy(deep=True) if current and current.is_custom_item else RequirementDraft(
                template_id="CUSTOM_ITEM",
                category_id=resolved.category_id or "GENERAL_CONSTRUCTION",
                item_name=resolved.display_name,
                display_name=resolved.display_name,
                is_custom_item=True,
                input_mode="PACKAGE_COUNT" if slots.quantity and slots.quantity.package_count else "BASE_QUANTITY",
                normalized_base_unit=resolved.base_unit,
            )
            template = None
        else:
            template = None
            if resolved and resolved.catalog_item_id:
                template = next((row for row in catalog if str(row.get("id")) == resolved.catalog_item_id), None)
                if not template:
                    template = {
                        "id": resolved.catalog_item_id,
                        "categoryId": resolved.category_id,
                        "name": resolved.display_name,
                        "baseUnit": resolved.base_unit,
                        "allowedUnits": resolved.allowed_units,
                        "allowedPackageSizes": resolved.allowed_package_sizes,
                        "packageType": resolved.package_type or "PACKAGE",
                    }

            if template is None:
                item_name = resolved.display_name if resolved else (item_text or "Item")
                draft = RequirementDraft(
                    template_id="CUSTOM_ITEM",
                    category_id="GENERAL",
                    item_name=item_name,
                    display_name=item_name,
                    is_custom_item=True,
                    input_mode="BASE_QUANTITY",
                    normalized_base_unit="piece",
                )
            else:
                draft = current.model_copy(deep=True) if current and current.template_id == str(template["id"]) else RequirementDraft(
                    template_id=str(template["id"]),
                    category_id=str(template.get("categoryId", "cat_general")),
                    item_name=str(template["name"]),
                    display_name=str(template["name"]),
                    is_custom_item=False,
                    input_mode="BASE_QUANTITY",
                    normalized_base_unit=str(template.get("baseUnit", "piece")),
                )

        # 2. Merge slots and locations
        self._merge_slots_and_locations(draft, slots, current, template=template)

        # 3. Tile Coverage Deterministic Calculation
        if "tile" in draft.item_name.lower() or "tile" in (draft.display_name or "").lower():
            if draft.coverage_area and draft.dimensions:
                w_mm = draft.dimensions.get("width", 600)
                l_mm = draft.dimensions.get("length", 600)
                # Catalog package sizes for tiles are coverage areas (m² per
                # box), not the number of physical tiles in a box. Use an
                # explicitly supplied tile package size when available and
                # retain the established 4-tiles-per-box default otherwise.
                t_per_box = (
                    int(draft.package_size)
                    if draft.package_size is not None and draft.package_size > 0
                    else 4
                )

                est = MaterialEstimationEngine.estimate_tiles({
                    "area_sqm": draft.coverage_area,
                    "tile_width_mm": w_mm,
                    "tile_length_mm": l_mm,
                    "package_size": t_per_box,
                })
                draft.calculated_physical_quantity = est.calculated_physical_quantity
                draft.calculated_package_count = est.calculated_package_count
                if est.calculated_package_count:
                    draft.package_count = est.calculated_package_count
                    draft.package_unit = "box"
                draft.input_mode = "BASE_QUANTITY"
                draft.entered_quantity = float(est.calculated_physical_quantity or 37)
                draft.entered_unit = "piece"
                draft.normalized_quantity = est.calculated_value
                draft.notes = f"Required coverage: {draft.coverage_area:g} m² | Tile size: {w_mm:g}×{l_mm:g} mm | Calculated: {est.calculated_physical_quantity} tiles (approx. {est.calculated_package_count} boxes)"

        # 4. Evaluate missing required fields
        missing = []
        if not draft.entered_quantity and not draft.package_count and not draft.calculated_physical_quantity:
            missing.append("quantity")
        if draft.package_count is not None and draft.package_size is None:
            missing.append("package_size")
        if (not draft.location_text and not draft.location_source) or draft.location_pending:
            missing.append("delivery_location")
        if draft.location_source == "CURRENT_DEVICE_LOCATION" and (draft.latitude is None or draft.longitude is None):
            missing.append("delivery_coordinates")

        draft.missing_required_fields = missing
        draft.ready_for_review = len(missing) == 0

        return draft, None

    def _merge_slots_and_locations(
        self,
        draft: RequirementDraft,
        slots: ExtractedSlots,
        current: Optional[RequirementDraft],
        template: Optional[Dict[str, Any]] = None,
    ) -> None:
        """Merges new slots into draft while preserving previously collected multi-turn fields."""

        # 1. Quantity & Package Details
        quantity = slots.quantity
        if quantity:
            if quantity.package_count is not None:
                draft.input_mode = "PACKAGE_COUNT"
                draft.package_count = quantity.package_count
                if quantity.package_size is not None:
                    draft.package_size = quantity.package_size
                elif current and current.package_size is not None:
                    draft.package_size = current.package_size
                elif template and template.get("allowedPackageSizes"):
                    sizes = template.get("allowedPackageSizes")
                    if isinstance(sizes, list) and len(sizes) == 1 and isinstance(sizes[0], (int, float)):
                        draft.package_size = float(sizes[0])

                if draft.package_size is not None:
                    draft.normalized_quantity = quantity.package_count * draft.package_size
                else:
                    draft.normalized_quantity = None

                draft.entered_quantity = float(quantity.package_count)
                draft.entered_unit = (
                    quantity.package_unit
                    or (template.get("packageType") if template else None)
                    or draft.normalized_base_unit
                    or "bag"
                )
            elif quantity.value is not None:
                # If package_count was previously collected, user giving "50kg" supplies package_size!
                if current and current.package_count is not None:
                    draft.input_mode = "PACKAGE_COUNT"
                    draft.package_count = current.package_count
                    draft.package_size = quantity.value
                    draft.package_unit = quantity.unit or current.package_unit or "kg"
                    draft.entered_quantity = float(current.package_count)
                    draft.entered_unit = current.entered_unit or current.package_unit or "bag"
                    draft.normalized_quantity = current.package_count * quantity.value
                else:
                    draft.input_mode = "BASE_QUANTITY"
                    draft.entered_quantity = quantity.value
                    draft.entered_unit = quantity.unit or draft.normalized_base_unit
                    draft.normalized_quantity = quantity.value
        elif current:
            # Retain quantity fields from current draft
            if current.package_count is not None:
                draft.package_count = current.package_count
            if current.package_size is not None:
                draft.package_size = current.package_size
            if current.entered_quantity is not None:
                draft.entered_quantity = current.entered_quantity
            if current.entered_unit is not None:
                draft.entered_unit = current.entered_unit
            if current.normalized_quantity is not None:
                draft.normalized_quantity = current.normalized_quantity

        # 2. Location Preservation & Handoff Contract
        if slots.location_source == "CURRENT_DEVICE_LOCATION" or (slots.location_text and "current location" in slots.location_text.lower()):
            draft.location_source = "CURRENT_DEVICE_LOCATION"
            if slots.structured_location or (slots.location_text and "current location" not in slots.location_text.lower()):
                draft.location_pending = False
                if slots.location_text and "current location" not in slots.location_text.lower():
                    draft.location_text = slots.location_text
            else:
                draft.location_pending = True
                draft.location_text = None
        elif slots.location_text:
            draft.location_text = slots.location_text
            draft.location_source = "USER_TEXT"
            draft.location_pending = False
        elif current and (current.location_text or current.location_source):
            # PRESERVE location from current turn!
            draft.location_text = current.location_text
            draft.structured_location = current.structured_location
            draft.location_source = current.location_source
            draft.location_pending = current.location_pending
            draft.latitude = current.latitude
            draft.longitude = current.longitude
            draft.resolved_address = current.resolved_address

        if slots.structured_location:
            draft.structured_location = slots.structured_location
            if slots.structured_location.get("latitude") is not None:
                draft.latitude = float(slots.structured_location["latitude"])
            if slots.structured_location.get("longitude") is not None:
                draft.longitude = float(slots.structured_location["longitude"])
            if slots.structured_location.get("address") or slots.structured_location.get("resolved_address"):
                draft.resolved_address = str(slots.structured_location.get("address") or slots.structured_location.get("resolved_address"))
            if slots.structured_location.get("city") and not draft.location_text:
                draft.location_text = str(slots.structured_location["city"])
            if draft.latitude is not None and draft.longitude is not None:
                draft.location_pending = False

        # 3. Dimensions & Coverage Area
        if slots.dimensions:
            draft.dimensions = slots.dimensions
        elif current and current.dimensions:
            draft.dimensions = current.dimensions

        if slots.coverage_area is not None:
            draft.coverage_area = slots.coverage_area
        elif current and current.coverage_area is not None:
            draft.coverage_area = current.coverage_area

        # 3. Preferences
        if slots.preferences:
            for k, v in slots.preferences.items():
                if isinstance(v, str):
                    draft.preferences[k] = v.upper() if len(v) <= 4 else v.title()
                else:
                    draft.preferences[k] = str(v)
        elif current and current.preferences:
            draft.preferences.update(current.preferences)

        # 4. Notes
        if slots.notes:
            draft.notes = slots.notes
        elif current and current.notes:
            draft.notes = current.notes

        if slots.maximum_budget is not None:
            draft.maximum_budget = slots.maximum_budget
        elif current and current.maximum_budget is not None:
            draft.maximum_budget = current.maximum_budget
        if slots.deadline:
            draft.deadline = slots.deadline
        elif current and current.deadline:
            draft.deadline = current.deadline
        if slots.delivery_required is not None:
            draft.delivery_required = slots.delivery_required

    def next_question(self, draft: RequirementDraft, language: str = "en") -> str:
        if not draft.missing_required_fields:
            return "Your requirement details look complete! Would you like to review and submit?"
        field = draft.missing_required_fields[0]
        item = draft.display_name or draft.item_name
        if field == "quantity":
            return f"How many {item} do you need?"
        elif field == "package_size":
            return f"What is the size of each bag for {item} (e.g., 50kg)?"
        elif field == "delivery_location":
            if draft.location_pending:
                return f"Requesting your device location for {item} delivery..."
            return f"Where should the {item} be delivered?"
        elif field == "delivery_coordinates":
            return "Please share your device location so I can use the exact delivery point."
        elif field == "maximum_budget":
            return "What is your maximum budget in LKR?"
        elif field == "deadline":
            return "When do you need it delivered?"
        return f"Please provide details for {field}."
