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

    model_config = ConfigDict(extra="ignore", populate_by_name=True)

    raw_message: str
    intent: Intent = Intent.UNKNOWN
    confidence: float = Field(default=0.9, ge=0.0, le=1.0)
    language: str = "en"  # "en", "si", "si-Latn", "ta"

    item_candidate: Optional[str] = Field(default=None, alias="item")
    resolved_item: Optional[str] = None
    catalog_item_id: Optional[str] = None
    is_custom_item: bool = False

    quantity: Optional[QuantitySlots] = None
    unit: Optional[str] = None
    package_count: Optional[int] = Field(default=None, ge=1)
    package_size: Optional[float] = Field(default=None, gt=0)
    package_unit: Optional[str] = None

    dimensions: Optional[ItemDimensions] = None
    coverage_area: Optional[float] = Field(default=None, ge=0.0)  # Always in m2 / sqm

    location_text: Optional[str] = Field(default=None, alias="location")
    structured_location: Optional[Dict[str, Any]] = None  # {latitude, longitude, city, address}

    preferences: Dict[str, str] = Field(default_factory=dict)
    estimation_context: Dict[str, Any] = Field(default_factory=dict)
    live_data_request: Optional[Dict[str, Any]] = None

    is_follow_up: bool = False
    topic_switch: bool = False
    referenced_previous_context: bool = False
    is_question: bool = False
    is_purchase_request: bool = False
    retrieval_query: Optional[str] = None

    ambiguities: List[str] = Field(default_factory=list)
    needs_clarification: bool = False
    clarification_question: Optional[str] = None

    # Backward compatibility location property
    @property
    def location(self) -> Optional[str]:
        return self.location_text

    @location.setter
    def location(self, val: Optional[str]) -> None:
        self.location_text = val

    # camelCase Property Aliases required by Section B
    @property
    def itemCandidate(self) -> Optional[str]:
        return self.item_candidate

    @property
    def resolvedItem(self) -> Optional[str]:
        return self.resolved_item

    @property
    def catalogItemId(self) -> Optional[str]:
        return self.catalog_item_id

    @property
    def isCustomItem(self) -> bool:
        return self.is_custom_item

    @property
    def packageCount(self) -> Optional[int]:
        return self.package_count

    @property
    def packageSize(self) -> Optional[float]:
        return self.package_size

    @property
    def coverageArea(self) -> Optional[float]:
        return self.coverage_area

    @property
    def locationText(self) -> Optional[str]:
        return self.location_text

    @property
    def structuredLocation(self) -> Optional[Dict[str, Any]]:
        return self.structured_location

    @property
    def estimationContext(self) -> Dict[str, Any]:
        return self.estimation_context

    @property
    def liveDataRequest(self) -> Optional[Dict[str, Any]]:
        return self.live_data_request

    @property
    def followUp(self) -> bool:
        return self.is_follow_up

    @property
    def topicSwitch(self) -> bool:
        return self.topic_switch
