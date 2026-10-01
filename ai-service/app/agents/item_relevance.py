"""Read-only item identity gate. No category word or untrusted instruction grants eligibility."""
from __future__ import annotations

import re
from typing import Literal
from pydantic import BaseModel, ConfigDict, Field
from app.catalog.item_templates import CONSTRUCTION_ITEM_TEMPLATES


class ItemRelevanceResult(BaseModel):
    model_config = ConfigDict(extra="forbid", frozen=True)
    classification: Literal["SAME_ITEM", "COMPATIBLE_ALTERNATIVE", "RELATED_ONLY", "INCOMPATIBLE"]
    confidence: float = Field(ge=0, le=1)
    reasonCode: str
    evidence: list[str] = Field(default_factory=list)

    @property
    def eligible(self) -> bool:
        return self.classification == "SAME_ITEM" and self.confidence >= .95


def _normalize(value: str) -> str:
    return " " + re.sub(r"[^\w]+", " ", value.casefold()).strip() + " "


class ItemRelevanceAgent:
    """Conservative deterministic stage; returns evidence, never chain-of-thought."""
    def evaluate(self, request, listing) -> ItemRelevanceResult:
        requested = request.constructionItemTemplateId
        if requested and listing.template_id:
            same = requested == listing.template_id
            return ItemRelevanceResult(classification="SAME_ITEM" if same else "INCOMPATIBLE",
                confidence=1, reasonCode="EXACT_TEMPLATE" if same else "DIFFERENT_TEMPLATE")
        template = next((x for x in CONSTRUCTION_ITEM_TEMPLATES if x.id == requested), None)
        name = template.name if template else request.itemName or ""
        aliases = request.aliases or {
            "Generator": ["generator", "portable generator", "genset"],
            "Paint": ["paint", "wall paint", "emulsion paint"],
            "Air Compressor": ["air compressor", "compressor"],
        }.get(name, [name])
        broad = {"", "equipment", "finishes", "construction", "material", "materials", "other", "custom item"}
        title = _normalize(listing.title)
        evidence = [x for x in aliases if x.casefold() not in broad and _normalize(x) in title]
        competitors = [x for x in CONSTRUCTION_ITEM_TEMPLATES
            if x.name != name and x.name.casefold() not in broad and _normalize(x.name) in title]
        same = bool(evidence) and not competitors
        return ItemRelevanceResult(classification="SAME_ITEM" if same else "INCOMPATIBLE",
            confidence=.96 if same else 1, reasonCode="CUSTOM_ITEM_NAME_MATCH" if same else "ITEM_IDENTITY_NOT_ESTABLISHED",
            evidence=evidence if same else [])
