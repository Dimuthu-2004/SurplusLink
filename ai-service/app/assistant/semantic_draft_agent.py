from __future__ import annotations

import json
from typing import Any

from app.assistant.semantic_schemas import ExtractedSlots, RequirementDraft


class CatalogDraftValidator:
    """Applies only deterministic catalog/business rules to semantic LLM slots."""

    def resolve_template(self, item_text: str | None, catalog: list[dict[str, Any]]) -> tuple[dict[str, Any] | None, bool]:
        if not item_text:
            return None, False
        needle = item_text.casefold().strip()
        exact = [row for row in catalog if needle == str(row.get("name", "")).casefold() or needle in [str(a).casefold() for a in row.get("aliases", [])]]
        if len(exact) == 1:
            return exact[0], False
        contained = [row for row in catalog if needle in str(row.get("name", "")).casefold() or str(row.get("name", "")).casefold() in needle]
        return (contained[0], False) if len(contained) == 1 else (None, len(contained) > 1)

    def build_or_update(self, slots: ExtractedSlots, catalog: list[dict[str, Any]], current: RequirementDraft | None = None) -> tuple[RequirementDraft | None, str | None]:
        template = None
        if current:
            template = next((row for row in catalog if str(row.get("id")) == current.template_id), None)
        if slots.item:
            template, ambiguous = self.resolve_template(slots.item, catalog)
            if ambiguous:
                return None, "Which catalog item did you mean?"
        if template is None:
            return current, "Which construction item do you need?"

        draft = current.model_copy(deep=True) if current and current.template_id == str(template["id"]) else RequirementDraft(
            template_id=str(template["id"]), category_id=str(template["categoryId"]), item_name=str(template["name"]),
            input_mode="BASE_QUANTITY", normalized_base_unit=str(template["baseUnit"]),
        )
        quantity = slots.quantity
        if quantity:
            if quantity.package_count is not None:
                if quantity.package_size is None:
                    return draft, "What is the size of each package?"
                allowed_sizes = [float(x) for x in template.get("allowedPackageSizes", [])]
                if allowed_sizes and float(quantity.package_size) not in allowed_sizes:
                    return draft, f"Available package sizes are {', '.join(map(str, allowed_sizes))} {template['baseUnit']}. Which one should I use?"
                draft.input_mode = "PACKAGE_COUNT"
                draft.package_count = quantity.package_count
                draft.package_size = quantity.package_size
                draft.entered_quantity = float(quantity.package_count)
                draft.entered_unit = str(template.get("packageType") or "PACKAGE")
                draft.normalized_quantity = quantity.package_count * quantity.package_size
            elif quantity.value is not None:
                unit = quantity.unit
                allowed = [str(x) for x in template.get("allowedUnits", [])]
                canonical = next((x for x in allowed if x.casefold() == (unit or "").casefold()), None)
                if unit and not canonical:
                    return draft, f"{unit} is not valid for {template['name']}. Please use one of: {', '.join(allowed)}."
                if not unit:
                    return draft, "What unit should I use for that quantity?"
                draft.input_mode = "BASE_QUANTITY"
                draft.entered_quantity = quantity.value
                draft.entered_unit = canonical
                draft.normalized_quantity = quantity.value

        fields = self._fields(template.get("attributeSchema"))
        for key, value in slots.preferences.items():
            field = next((f for f in fields if str(f.get("id", "")).casefold() == key.casefold()), None)
            if not field or not field.get("buyerPreference", False):
                continue
            options = [str(x) for x in field.get("options", [])]
            canonical_value = next((x for x in options if x.casefold() == value.casefold()), value if not options or field.get("allowOther") else None)
            if canonical_value is not None:
                draft.preferences[str(field["id"])] = canonical_value
        if slots.location_text:
            draft.location_text = slots.location_text
        if slots.notes:
            draft.notes = slots.notes

        missing = []
        if draft.normalized_quantity is None:
            missing.append("quantity")
        for field in fields:
            required = field.get("required") or field.get("priority") in ("REQUIRED", "CORE_REQUIRED")
            if required and field.get("buyerPreference") and field.get("id") not in draft.preferences:
                missing.append(str(field.get("id")))
        if not draft.location_text:
            missing.append("delivery location")
        draft.missing_required_fields = missing
        draft.ready_for_review = not missing
        return draft, None

    @staticmethod
    def _fields(raw: Any) -> list[dict[str, Any]]:
        if isinstance(raw, list):
            return raw
        if isinstance(raw, str):
            try:
                value = json.loads(raw)
                return value if isinstance(value, list) else []
            except ValueError:
                pass
        return []

    @staticmethod
    def next_question(draft: RequirementDraft, language: str) -> str:
        field = draft.missing_required_fields[0]
        if language == "si-Latn":
            return "Delivery location eka koheda?" if field == "delivery location" else f"{field} eka kiyanna puluwanda?"
        if language == "si":
            return "බෙදාහැරීමේ ස්ථානය කොහේද?" if field == "delivery location" else f"{field} සඳහන් කරන්න පුළුවන්ද?"
        if language == "ta":
            return "விநியோக இடம் எங்கே?" if field == "delivery location" else f"{field} விவரத்தை சொல்ல முடியுமா?"
        return "Where should it be delivered?" if field == "delivery location" else f"What {field} would you prefer?"
