import re
from dataclasses import asdict, dataclass
from typing import Any, Dict, List, Optional, Tuple


@dataclass
class RequirementDraft:
    template_id: Optional[str]
    item_name: str
    input_mode: str  # BASE_QUANTITY or PACKAGE
    entered_quantity: float
    entered_unit: str
    normalized_quantity: float
    normalized_base_unit: str
    preferences: Optional[Dict[str, str]]
    location_text: Optional[str]
    notes: Optional[str]
    missing_required_fields: List[str]
    ready_for_review: bool

    def to_dict(self) -> Dict[str, Any]:
        return asdict(self)


class RequirementDraftAgent:
    """Extracts structured requirement draft from multilingual natural language queries."""

    # Common location detection in Sri Lanka
    SRI_LANKA_CITIES = [
        "colombo", "negombo", "gampaha", "kandy", "galle", "jaffna", "kurunegala",
        "kalutara", "matara", "ratnapura", "badulla", "anuradhapura", "trincomalee",
        "batticaloa", "nuwara eliya", "panadura", "moratuwa", "kelaniya", "wattala"
    ]

    # Color detection
    COLORS = ["yellow", "white", "brilliant white", "off white", "blue", "red", "green", "grey", "black"]

    def extract_draft(self, message: str, catalog_items: Optional[List[Dict[str, Any]]] = None) -> Tuple[Optional[RequirementDraft], str]:
        text_lower = message.lower().strip()

        # 1. Identify Item
        item_name = self._detect_item(text_lower, catalog_items)
        if not item_name:
            return None, "I'm not sure which material you need. Could you specify if you need Paint, Cement, Tiles, Rebar, Timber, or a specific tool?"

        # 2. Extract Quantity, Unit, and Package info
        qty_info = self._parse_quantity_and_unit(text_lower, item_name)
        if not qty_info["has_quantity"]:
            return None, f"How much {item_name} do you need?"
        if qty_info["is_ambiguous"]:
            return None, f"5 what — bags, kilograms, litres, pieces, or another unit?"

        # 3. Detect Preferences (e.g. Colour)
        preferences = {}
        for color in self.COLORS:
            if color in text_lower:
                preferences["colour"] = color.title()

        # 4. Detect Location
        location_text = None
        for city in self.SRI_LANKA_CITIES:
            if city in text_lower:
                location_text = city.title()
                break

        # 5. Resolve catalog template ID if available
        template_id = None
        if catalog_items:
            for item in catalog_items:
                if item.get("name", "").lower() in item_name.lower() or item_name.lower() in item.get("name", "").lower():
                    template_id = item.get("id")
                    break

        missing = []
        if not location_text:
            missing.append("Delivery location")

        ready = len(missing) == 0

        draft = RequirementDraft(
            template_id=template_id,
            item_name=item_name,
            input_mode=qty_info["input_mode"],
            entered_quantity=qty_info["entered_quantity"],
            entered_unit=qty_info["entered_unit"],
            normalized_quantity=qty_info["normalized_quantity"],
            normalized_base_unit=qty_info["normalized_base_unit"],
            preferences=preferences if preferences else None,
            location_text=location_text,
            notes=None,
            missing_required_fields=missing,
            ready_for_review=ready,
        )

        # Build user-friendly response message
        msg_parts = [f"I've prepared a requirement draft for **{item_name}**:\n"]
        if qty_info["input_mode"] == "PACKAGE":
            msg_parts.append(f"• **Quantity**: {int(qty_info['entered_quantity'])} {qty_info['entered_unit']} (Total: {qty_info['normalized_quantity']} {qty_info['normalized_base_unit']})")
        else:
            msg_parts.append(f"• **Quantity**: {qty_info['normalized_quantity']} {qty_info['normalized_base_unit']}")

        if preferences:
            for k, v in preferences.items():
                msg_parts.append(f"• **{k.title()}**: {v}")

        if location_text:
            msg_parts.append(f"• **Delivery Area**: {location_text}")
        else:
            msg_parts.append("• **Delivery Area**: *Not specified*")

        if ready:
            msg_parts.append("\nYour requirement is ready to review. Tap **[ Review Requirement ]** below to double-check and submit.")
        else:
            msg_parts.append("\nPlease let me know your delivery location so we can finalize the draft.")

        response_text = "\n".join(msg_parts)

        return draft, response_text

    def _detect_item(self, text: str, catalog_items: Optional[List[Dict[str, Any]]]) -> Optional[str]:
        # Item mapping dictionary (multilingual English, Sinhala, Romanized Sinhala, Tamil)
        item_map = {
            "paint": "Paint",
            "පේන්ට්": "Paint",
            "තීන්ත": "Paint",
            "cement": "Cement",
            "සිමෙන්ති": "Cement",
            "சிமெண்டு": "Cement",
            "tiles": "Floor Tiles",
            "tile": "Floor Tiles",
            "ටයිල්": "Floor Tiles",
            "rebar": "Steel Rebar",
            "steel": "Steel Rebar",
            "යකඩ": "Steel Rebar",
            "timber": "Timber",
            "wood": "Timber",
            "ලී": "Timber",
            "generator": "Diesel Generator",
            "ජෙනරේටර්": "Diesel Generator",
            "compressor": "Air Compressor",
            "pvc": "PVC Pipes",
            "pipe": "PVC Pipes",
        }

        for kw, canonical in item_map.items():
            if kw in text:
                return canonical

        # Check catalog items
        if catalog_items:
            for item in catalog_items:
                name = item.get("name", "").lower()
                if name and name in text:
                    return item.get("name")

        return None

    def _parse_quantity_and_unit(self, text: str, item_name: str) -> Dict[str, Any]:
        # Check package pattern e.g. "7 cement bags 50kg", "7 bags 50kg", "3 cans 4l", "2 cans of 4L paint"
        pkg_match = re.search(r"(\d+(?:\.\d+)?)\s*(?:[a-z]+\s*)*(bags?|cans?|buckets?|boxes?)\s*(?:of\s*)?(\d+(?:\.\d+)?)\s*(kg|l|litres?|sqm)", text)
        if pkg_match:
            count = float(pkg_match.group(1))
            pkg_type = pkg_match.group(2).rstrip("s")
            size = float(pkg_match.group(3))
            base_unit = pkg_match.group(4).lower()
            if base_unit.startswith("litre"):
                base_unit = "L"
            elif base_unit == "l":
                base_unit = "L"

            normalized = round(count * size, 2)
            return {
                "has_quantity": True,
                "is_ambiguous": False,
                "input_mode": "PACKAGE",
                "entered_quantity": count,
                "entered_unit": f"{pkg_type}s ({size}{base_unit})",
                "normalized_quantity": normalized,
                "normalized_base_unit": base_unit,
            }

        # Check simple pattern e.g. "10 litres", "10 l", "10L", "50 bags", "10k", "1 unit", "1 pc"
        # 1. Litres
        l_match = re.search(r"(\d+(?:\.\d+)?)\s*(?:litre|litres|l)\b", text)
        if l_match:
            val = float(l_match.group(1))
            return {
                "has_quantity": True,
                "is_ambiguous": False,
                "input_mode": "BASE_QUANTITY",
                "entered_quantity": val,
                "entered_unit": "L",
                "normalized_quantity": val,
                "normalized_base_unit": "L",
            }

        # 2. Bags / Kilograms
        bag_match = re.search(r"(\d+(?:\.\d+)?)\s*(?:bags?|බෑග්)\b", text)
        if bag_match:
            val = float(bag_match.group(1))
            normalized_kg = val * 50.0 if "cement" in item_name.lower() else val
            base_u = "kg" if "cement" in item_name.lower() else "bags"
            return {
                "has_quantity": True,
                "is_ambiguous": False,
                "input_mode": "PACKAGE" if "cement" in item_name.lower() else "BASE_QUANTITY",
                "entered_quantity": val,
                "entered_unit": "bags",
                "normalized_quantity": normalized_kg,
                "normalized_base_unit": base_u,
            }

        # 3. Unit / Piece / count e.g. "1 unit", "1 pc", "2 pieces", "10k"
        unit_match = re.search(r"(\d+(?:\.\d+)?)\s*(?:units?|pcs?|pieces?)\b", text)
        if unit_match:
            val = float(unit_match.group(1))
            return {
                "has_quantity": True,
                "is_ambiguous": False,
                "input_mode": "BASE_QUANTITY",
                "entered_quantity": val,
                "entered_unit": "unit",
                "normalized_quantity": val,
                "normalized_base_unit": "unit",
            }

        # 4. Romanized Sinhala "10k" or number followed by "one" / "wenne" / "wage"
        num_k_match = re.search(r"(\d+)\s*k\b", text)
        if num_k_match:
            val = float(num_k_match.group(1))
            default_unit = "L" if "paint" in item_name.lower() else "bags" if "cement" in item_name.lower() else "pcs"
            return {
                "has_quantity": True,
                "is_ambiguous": False,
                "input_mode": "BASE_QUANTITY",
                "entered_quantity": val,
                "entered_unit": default_unit,
                "normalized_quantity": val,
                "normalized_base_unit": default_unit,
            }

        # Bare number detection (e.g. "cement 5" -> ambiguous)
        bare_num_match = re.search(r"\b(\d+)\b", text)
        if bare_num_match:
            val = float(bare_num_match.group(1))
            return {
                "has_quantity": True,
                "is_ambiguous": True,
                "input_mode": "BASE_QUANTITY",
                "entered_quantity": val,
                "entered_unit": "unknown",
                "normalized_quantity": val,
                "normalized_base_unit": "unknown",
            }

        return {"has_quantity": False, "is_ambiguous": False}
