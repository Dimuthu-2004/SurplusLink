from __future__ import annotations

from typing import Any, Dict, List, Optional
from pydantic import BaseModel, ConfigDict, Field

from app.assistant.semantic_schemas import Intent, QuantitySlots


class ItemDimensions(BaseModel):
    model_config = ConfigDict(extra="ignore")

    width: Optional[float] = None
    length: Optional[float] = None
    height: Optional[float] = None
    unit: str = "mm"


class CanonicalUserRequest(BaseModel):
    """
    Authoritative canonical intermediate representation of a user message.
    All downstream components (state machine, capability registry, calculators,
    and response generator) MUST consume this canonical structure instead of
    re-parsing raw user strings.
    """

    model_config = ConfigDict(extra="ignore")

    raw_message: str
    intent: Intent = Intent.UNKNOWN
    confidence: float = Field(default=0.9, ge=0.0, le=1.0)
    language: str = "en"  # "en", "si", "si-Latn", "ta"

    item_candidate: Optional[str] = None
    catalog_item_id: Optional[str] = None
    location: Optional[str] = None  # Validated delivery city/district only

    quantity: Optional[QuantitySlots] = None
    package_count: Optional[int] = Field(default=None, ge=1)
    package_size: Optional[float] = Field(default=None, gt=0)
    package_unit: Optional[str] = None

    dimensions: Optional[ItemDimensions] = None
    coverage_area: Optional[float] = Field(default=None, ge=0.0)  # Always in m2

    preferences: Dict[str, str] = Field(default_factory=dict)
    estimation_context: Dict[str, Any] = Field(default_factory=dict)
    live_data_request: Optional[Dict[str, Any]] = None

    is_follow_up: bool = False
    referenced_previous_context: bool = False
    is_question: bool = False
    is_purchase_request: bool = False
    retrieval_query: Optional[str] = None

    ambiguities: List[str] = Field(default_factory=list)
    needs_clarification: bool = False
    clarification_question: Optional[str] = None
