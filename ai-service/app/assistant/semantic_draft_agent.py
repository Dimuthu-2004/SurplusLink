from __future__ import annotations

import json
from typing import Any, Dict, List, Optional, Tuple

from app.assistant.item_resolver import ItemResolver, ResolvedItem
from app.assistant.semantic_schemas import ExtractedSlots, RequirementDraft


class CatalogDraftValidator:
    """
    Applies deterministic catalog & open-vocabulary custom item rules to semantic LLM slots.
    Never rejects custom items solely because a catalog template is absent.
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

        # 1. Custom Item Handling (e.g. Water Pump, Generator, Air Compressor)
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

            quantity = slots.quantity
            if quantity:
                if quantity.package_count is not None:
                    draft.input_mode = "PACKAGE_COUNT"
                    draft.package_count = quantity.package_count
                    draft.package_size = quantity.package_size or 1.0
                    draft.entered_quantity = float(quantity.package_count)
                    draft.entered_unit = quantity.package_unit or resolved.base_unit
                    draft.normalized_quantity = quantity.package_count * (quantity.package_size or 1.0)
                elif quantity.value is not None:
                    if not quantity.unit:
                        draft.normalized_quantity = None
                        return draft, "What unit should I use for that quantity?"
                    draft.input_mode = "BASE_QUANTITY"
                    draft.entered_quantity = quantity.value
                    draft.entered_unit = quantity.unit
                    draft.normalized_quantity = quantity.value

            if slots.preferences:
                draft.preferences.update(slots.preferences)

            if slots.location_text:
                draft.location_text = slots.location_text
            if slots.structured_location:
                draft.structured_location = slots.structured_location

            missing = []
            if not draft.entered_quantity and not draft.package_count:
                missing.append("quantity")
            if not draft.location_text:
                missing.append("delivery_location")

            draft.missing_required_fields = missing
            draft.ready_for_review = len(missing) == 0

            return draft, None

        # 2. Known Catalog Template Handling (Paint, Cement, Tiles, Steel, PVC, Sealant)
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
            missing = []
            if slots.quantity and slots.quantity.value:
                if not slots.quantity.unit:
                    draft.normalized_quantity = None
                    return draft, "What unit should I use for that quantity?"
                draft.entered_quantity = slots.quantity.value
                draft.entered_unit = slots.quantity.unit
                draft.normalized_quantity = slots.quantity.value
            else:
                missing.append("quantity")

            if slots.location_text:
                draft.location_text = slots.location_text
            else:
                missing.append("delivery_location")

            draft.missing_required_fields = missing
            draft.ready_for_review = len(missing) == 0
            return draft, None

        draft = current.model_copy(deep=True) if current and current.template_id == str(template["id"]) else RequirementDraft(
            template_id=str(template["id"]),
            category_id=str(template.get("categoryId", "cat_general")),
            item_name=str(template["name"]),
            display_name=str(template["name"]),
            is_custom_item=False,
            input_mode="BASE_QUANTITY",
            normalized_base_unit=str(template.get("baseUnit", "piece")),
        )

        quantity = slots.quantity
        if quantity:
            if quantity.package_count is not None:
                if quantity.package_size is None and template.get("allowedPackageSizes"):
                    allowed = [float(x) for x in template.get("allowedPackageSizes", [])]
                    if len(allowed) == 1:
                        quantity.package_size = allowed[0]
                    else:
                        draft.package_count = quantity.package_count
                        draft.missing_required_fields = ["package_size"]
                        draft.ready_for_review = False
                        return draft, f"What is the size of each package?"

                draft.input_mode = "PACKAGE_COUNT"
                draft.package_count = quantity.package_count
                draft.package_size = quantity.package_size or 1.0
                draft.entered_quantity = float(quantity.package_count)
                draft.entered_unit = str(template.get("packageType") or "PACKAGE")
                draft.normalized_quantity = quantity.package_count * (quantity.package_size or 1.0)
            elif quantity.value is not None:
                unit = quantity.unit
                if not unit:
                    draft.normalized_quantity = None
                    return draft, "What unit should I use for that quantity?"
                draft.input_mode = "BASE_QUANTITY"
                draft.entered_quantity = quantity.value
                draft.entered_unit = unit
                draft.normalized_quantity = quantity.value

        if slots.preferences:
            for k, v in slots.preferences.items():
                if isinstance(v, str):
                    draft.preferences[k] = v.upper() if len(v) <= 4 else v.title()
                else:
                    draft.preferences[k] = str(v)

        if slots.location_text:
            draft.location_text = slots.location_text
        if slots.structured_location:
            draft.structured_location = slots.structured_location

        missing = []
        if not draft.entered_quantity and not draft.package_count:
            missing.append("quantity")
        if not draft.location_text:
            missing.append("delivery_location")

        draft.missing_required_fields = missing
        draft.ready_for_review = len(missing) == 0

        return draft, None

    def next_question(self, draft: RequirementDraft, language: str = "en") -> str:
        if not draft.missing_required_fields:
            return "Your requirement details look complete! Would you like to review and submit?"
        field = draft.missing_required_fields[0]
        item = draft.display_name or draft.item_name
        if field == "quantity":
            return f"How many {item} do you need?"
        elif field == "package_size":
            return f"What package size do you need for {item}?"
        elif field == "delivery_location":
            return f"Where should the {item} be delivered?"
        return f"Please provide details for {field}."
