from __future__ import annotations

import re
import uuid
from enum import Enum
from typing import Any, Dict, List, Optional

from pydantic import BaseModel, ConfigDict, Field, field_validator


class Intent(str, Enum):
    CONSTRUCTION_KNOWLEDGE = "CONSTRUCTION_KNOWLEDGE"
    MATERIAL_ESTIMATION = "MATERIAL_ESTIMATION"
    PRICE_INFORMATION = "PRICE_INFORMATION"
    CREATE_REQUIREMENT_DRAFT = "CREATE_REQUIREMENT_DRAFT"
    CONTINUE_REQUIREMENT_DRAFT = "CONTINUE_REQUIREMENT_DRAFT"
    PLATFORM_HELP = "PLATFORM_HELP"
    LIVE_MARKETPLACE_QUERY = "LIVE_MARKETPLACE_QUERY"
    LIVE_DATA_QUERY = "LIVE_DATA_QUERY"
    TRANSACTION_QUERY = "TRANSACTION_QUERY"
    MATCH_EXPLANATION = "MATCH_EXPLANATION"
    UNIT_CONVERSION = "UNIT_CONVERSION"
    CLARIFICATION = "CLARIFICATION"
    SECURITY_REFUSAL = "SECURITY_REFUSAL"
    UNKNOWN = "UNKNOWN"


class QuantitySlots(BaseModel):
    model_config = ConfigDict(extra="ignore")

    value: float | None = None
    unit: str | None = None
    approximate: bool = False
    package_count: int | None = Field(default=None, ge=1)
    package_size: float | None = Field(default=None, gt=0)
    package_unit: str | None = None


class ExtractedSlots(BaseModel):
    model_config = ConfigDict(extra="ignore")

    item: str | None = None
    quantity: QuantitySlots | None = None
    preferences: dict[str, str] = Field(default_factory=dict)
    location_text: str | None = None
    structured_location: dict[str, Any] | None = None
    location_source: str | None = None
    location_pending: bool = False
    dimensions: dict[str, Any] | None = None
    coverage_area: float | None = None
    notes: str | None = None
    live_resource: str | None = None
    referenced_id: str | None = None

    @field_validator("quantity", mode="before")
    @classmethod
    def parse_quantity(cls, v):
        if v is None:
            return None
        if isinstance(v, (int, float)):
            return QuantitySlots(value=float(v))
        if isinstance(v, str):
            m = re.match(r"^([0-9.]+)\s*([a-zA-Z]+)?$", v.strip())
            if m:
                val = float(m.group(1))
                unit = m.group(2)
                return QuantitySlots(value=val, unit=unit)
            return QuantitySlots(value=None, unit=v)
        return v


class SemanticIntent(BaseModel):
    model_config = ConfigDict(extra="ignore")

    intent: Intent
    confidence: float = Field(default=0.9, ge=0, le=1)
    is_question: bool = False
    is_purchase_request: bool = False
    is_follow_up: bool = False
    referenced_item: str | None = None
    extracted_slots: ExtractedSlots = Field(default_factory=ExtractedSlots)
    needs_clarification: bool = False
    clarification_question: str | None = None
    retrieval_query: str | None = None
    response_language: str = "en"

    @field_validator("intent", mode="before")
    @classmethod
    def parse_intent_enum(cls, v):
        if isinstance(v, str):
            v_upper = v.upper().strip()
            aliases = {
                "BUYER_REQUIREMENT": "CREATE_REQUIREMENT_DRAFT",
                "REQUIREMENT_DRAFT": "CREATE_REQUIREMENT_DRAFT",
                "BUY_REQUIREMENT": "CREATE_REQUIREMENT_DRAFT",
                "PRICE_INQUIRY": "PRICE_INFORMATION",
                "MATERIAL_ESTIMATION_QUESTION": "MATERIAL_ESTIMATION",
                "KNOWLEDGE": "CONSTRUCTION_KNOWLEDGE",
                "CONSTRUCTION_KNOWLEDGE_QUESTION": "CONSTRUCTION_KNOWLEDGE",
            }
            target_str = aliases.get(v_upper, v_upper)
            try:
                return Intent(target_str)
            except ValueError:
                return Intent.UNKNOWN
        return v


class SemanticRoutingResult(BaseModel):
    intents: list[SemanticIntent] = Field(min_length=1, max_length=3)


class RequirementDraft(BaseModel):
    id: str = Field(default_factory=lambda: f"draft_{uuid.uuid4().hex[:8]}")
    template_id: str | None = None
    category_id: str | None = None
    item_name: str
    display_name: str | None = None
    is_custom_item: bool = False
    input_mode: str = "BASE_QUANTITY"
    entered_quantity: float | None = None
    entered_unit: str | None = None
    package_count: int | None = None
    package_size: float | None = None
    package_unit: str | None = None
    normalized_quantity: float | None = None
    normalized_base_unit: str = "piece"
    dimensions: dict[str, Any] | None = None
    coverage_area: float | None = None
    calculated_physical_quantity: int | None = None
    calculated_package_count: int | None = None
    location_text: str | None = None
    structured_location: dict[str, Any] | None = None
    location_source: str | None = None  # "CURRENT_DEVICE_LOCATION" | "USER_TEXT"
    location_pending: bool = False
    latitude: float | None = None
    longitude: float | None = None
    resolved_address: str | None = None
    preferences: dict[str, str] = Field(default_factory=dict)
    notes: str | None = None
    missing_required_fields: list[str] = Field(default_factory=list)
    ready_for_review: bool = False


class ClientAction(BaseModel):
    model_config = ConfigDict(extra="ignore")

    type: str
    draft_id: str | None = None
    params: dict[str, Any] = Field(default_factory=dict)



class ConversationState(BaseModel):
    drafts: list[RequirementDraft] = Field(default_factory=list)
    active_draft_id: str | None = None
    active_requirement_draft: RequirementDraft | None = None
    structured_location: dict[str, Any] | None = None
    awaiting_field: str | None = None
    last_resolved_intent: Intent | None = None
    last_referenced_item: str | None = None
    last_tool_context: dict[str, Any] | None = None
