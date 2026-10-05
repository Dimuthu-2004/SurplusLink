from __future__ import annotations

import logging
import re
from enum import Enum
from typing import Any, Dict, List, Optional, Set
from pydantic import BaseModel, ConfigDict, Field

from app.assistant.canonical_request import CanonicalUserRequest
from app.assistant.semantic_schemas import Intent, RequirementDraft
from app.assistant.validation_gate import ValidationGate

logger = logging.getLogger(__name__)

# Conversational stop words/phrases in Sinhala/English that must NEVER be parsed as delivery locations
LOCATION_STOP_WORDS = {
    "kohomada", "kohomada?", "kohoma", "koma", "machan", "macho", "bro", "sir", "hi", "hello",
    "how", "how much", "price", "cost", "store", "use", "apply", "what", "where", "why", "when"
}


class AssistantState(str, Enum):
    IDLE = "IDLE"
    KNOWLEDGE_QA = "KNOWLEDGE_QA"
    ESTIMATION_COLLECTING = "ESTIMATION_COLLECTING"
    ESTIMATION_COLLECTING_INPUTS = "ESTIMATION_COLLECTING"  # Alias for backward compatibility
    REQUIREMENT_COLLECTING = "REQUIREMENT_COLLECTING"
    REQUIREMENT_COLLECTING_INPUTS = "REQUIREMENT_COLLECTING"  # Alias for backward compatibility
    REQUIREMENT_READY = "REQUIREMENT_READY"
    LIVE_QUERY = "LIVE_QUERY"
    TRANSACTION_QUERY = "TRANSACTION_QUERY"


class StateConfiguration(BaseModel):
    state: AssistantState
    allowed_next_intents: Set[Intent]
    can_inherit_context: bool = True
    should_clear_context_on_topic_switch: bool = True


STATE_CONFIGS: Dict[AssistantState, StateConfiguration] = {
    AssistantState.IDLE: StateConfiguration(
        state=AssistantState.IDLE,
        allowed_next_intents=set(Intent),
        can_inherit_context=False,
        should_clear_context_on_topic_switch=False,
    ),
    AssistantState.KNOWLEDGE_QA: StateConfiguration(
        state=AssistantState.KNOWLEDGE_QA,
        allowed_next_intents=set(Intent),
        can_inherit_context=False,
        should_clear_context_on_topic_switch=False,
    ),
    AssistantState.ESTIMATION_COLLECTING: StateConfiguration(
        state=AssistantState.ESTIMATION_COLLECTING,
        allowed_next_intents={
            Intent.MATERIAL_ESTIMATION,
            Intent.CLARIFICATION,
            Intent.CREATE_REQUIREMENT_DRAFT,
            Intent.LIVE_MARKETPLACE_QUERY,
            Intent.CONSTRUCTION_KNOWLEDGE,
        },
        can_inherit_context=True,
        should_clear_context_on_topic_switch=True,
    ),
    AssistantState.REQUIREMENT_COLLECTING: StateConfiguration(
        state=AssistantState.REQUIREMENT_COLLECTING,
        allowed_next_intents={
            Intent.CREATE_REQUIREMENT_DRAFT,
            Intent.CONTINUE_REQUIREMENT_DRAFT,
            Intent.CLARIFICATION,
            Intent.MATERIAL_ESTIMATION,
            Intent.LIVE_MARKETPLACE_QUERY,
            Intent.CONSTRUCTION_KNOWLEDGE,
        },
        can_inherit_context=True,
        should_clear_context_on_topic_switch=True,
    ),
    AssistantState.REQUIREMENT_READY: StateConfiguration(
        state=AssistantState.REQUIREMENT_READY,
        allowed_next_intents=set(Intent),
        can_inherit_context=True,
        should_clear_context_on_topic_switch=True,
    ),
    AssistantState.LIVE_QUERY: StateConfiguration(
        state=AssistantState.LIVE_QUERY,
        allowed_next_intents=set(Intent),
        can_inherit_context=False,
        should_clear_context_on_topic_switch=False,
    ),
    AssistantState.TRANSACTION_QUERY: StateConfiguration(
        state=AssistantState.TRANSACTION_QUERY,
        allowed_next_intents=set(Intent),
        can_inherit_context=False,
        should_clear_context_on_topic_switch=False,
    ),
}


class ConversationStateMachine(BaseModel):
    """
    Authoritative state machine governing conversation context, state transitions,
    multiple requirement drafts, and slot merging across turns.
    """

    model_config = ConfigDict(extra="ignore")

    current_state: AssistantState = AssistantState.IDLE
    drafts: List[RequirementDraft] = Field(default_factory=list)
    active_draft_id: Optional[str] = None
    active_requirement_draft: Optional[RequirementDraft] = None

    known_fields: Dict[str, Any] = Field(default_factory=dict)
    missing_fields: List[str] = Field(default_factory=list)
    structured_location: Optional[Dict[str, Any]] = None

    last_item_candidate: Optional[str] = None
    last_intent: Optional[Intent] = None
    last_tool_context: Optional[Dict[str, Any]] = None

    def process_request(self, canonical_req: CanonicalUserRequest) -> AssistantState:
        """
        Processes a canonical user request, performs topic-switch & additive draft detection,
        merges new slots into state, and returns the updated state.
        """
        # 1. Topic Switch Detection: If current request is a broad query or unrelated intent, reset context
        if self._is_topic_switch(canonical_req):
            logger.info("Topic switch detected for intent %s. Clearing active item context.", canonical_req.intent)
            self._reset_item_context()

        # 2. Location Filtering & Processing
        self._process_location(canonical_req)

        # 3. State Merging & Additive Draft Handling
        self._merge_canonical_request(canonical_req)

        # 4. State Transition
        new_state = self._determine_next_state(canonical_req)
        self.current_state = new_state
        self.last_intent = canonical_req.intent
        if canonical_req.item_candidate:
            self.last_item_candidate = canonical_req.item_candidate

        return self.current_state

    def _process_location(self, canonical_req: CanonicalUserRequest) -> None:
        """Filters out non-location strings like 'kohomada' and reuses structured location."""
        if canonical_req.location_text:
            cleaned = canonical_req.location_text.strip().lower()
            if cleaned in LOCATION_STOP_WORDS or any(sw in cleaned for sw in ["kohomada", "how to"]):
                canonical_req.location_text = None

        raw = canonical_req.raw_message.lower()
        if ValidationGate.is_current_location_request(raw) and self.structured_location:
            canonical_req.structured_location = self.structured_location
            if not canonical_req.location_text and self.structured_location.get("city"):
                canonical_req.location_text = str(self.structured_location.get("city"))
            elif not canonical_req.location_text and self.structured_location.get("address"):
                canonical_req.location_text = str(self.structured_location.get("address"))

        if canonical_req.structured_location:
            self.structured_location = canonical_req.structured_location

    def _is_topic_switch(self, canonical_req: CanonicalUserRequest) -> bool:
        """Determines if the new request represents a broad topic switch away from current context."""
        # Broad marketplace queries ("sellers lage thiyena items monada") should not inherit previous item
        if canonical_req.intent in {Intent.SEARCH_MATERIAL, Intent.LIVE_MARKETPLACE_QUERY} and not canonical_req.is_follow_up:
            return True

        if self.current_state == AssistantState.IDLE and not self.active_requirement_draft:
            return False

        # Explicit non-additive new item while active draft is ready -> topic switch / new context
        if (
            canonical_req.item_candidate
            and self.last_item_candidate
            and canonical_req.item_candidate.casefold() != self.last_item_candidate.casefold()
            and not self._is_additive_request(canonical_req)
            and not canonical_req.is_follow_up
        ):
            return True

        # Knowledge QA or security refusal during drafting does NOT clear drafts, but switches state
        if canonical_req.intent in {Intent.PLATFORM_HELP, Intent.SECURITY_REFUSAL}:
            return True

        return False

    def _is_additive_request(self, canonical_req: CanonicalUserRequest) -> bool:
        """Detects phrases like 'also need a water pump', 'and I need tiles', 'add a generator too'."""
        raw = canonical_req.raw_message.lower()
        additive_markers = ["also need", "also want", "and i need", "add a", "another requirement", "in addition"]
        return any(marker in raw for marker in additive_markers)

    def _reset_item_context(self) -> None:
        """Clears active draft pointer and item-specific known fields while keeping drafts list intact."""
        self.active_requirement_draft = None
        self.active_draft_id = None
        self.known_fields.clear()
        self.missing_fields.clear()
        self.last_item_candidate = None

    def _merge_canonical_request(self, canonical_req: CanonicalUserRequest) -> None:
        """
        Merges new canonical information into existing state WITHOUT overwriting
        unrelated previously collected fields. Supports additive requirement creation.
        """
        # If additive request ("also need a generator"), detach current active draft pointer
        if self._is_additive_request(canonical_req):
            self.active_requirement_draft = None
            self.active_draft_id = None
            self.known_fields.clear()

        # Item candidate
        if canonical_req.item_candidate:
            self.known_fields["item"] = canonical_req.item_candidate

        # Location text & structured location
        if canonical_req.location_text:
            self.known_fields["location_text"] = canonical_req.location_text
        if canonical_req.structured_location:
            self.known_fields["structured_location"] = canonical_req.structured_location

        # Quantities & Package details
        if canonical_req.package_count is not None:
            self.known_fields["package_count"] = canonical_req.package_count
        if canonical_req.package_size is not None:
            self.known_fields["package_size"] = canonical_req.package_size
        if canonical_req.package_unit:
            self.known_fields["package_unit"] = canonical_req.package_unit
        if canonical_req.coverage_area is not None:
            self.known_fields["coverage_area"] = canonical_req.coverage_area

        if canonical_req.quantity and canonical_req.quantity.value is not None:
            self.known_fields["quantity_value"] = canonical_req.quantity.value
            if canonical_req.quantity.unit:
                self.known_fields["quantity_unit"] = canonical_req.quantity.unit

        # Dimensions
        if canonical_req.dimensions:
            self.known_fields["dimensions"] = canonical_req.dimensions.model_dump()

        # Preferences
        if canonical_req.preferences:
            existing_prefs = self.known_fields.get("preferences", {})
            existing_prefs.update(canonical_req.preferences)
            self.known_fields["preferences"] = existing_prefs

        # Estimation context
        if canonical_req.estimation_context:
            existing_est = self.known_fields.get("estimation_context", {})
            existing_est.update(canonical_req.estimation_context)
            self.known_fields["estimation_context"] = existing_est

    def set_active_draft(self, draft: RequirementDraft) -> None:
        """Stores/updates draft in drafts list and sets it as active."""
        self.active_requirement_draft = draft
        self.active_draft_id = draft.id
        # Update or append in drafts list
        idx = next((i for i, d in enumerate(self.drafts) if d.id == draft.id), None)
        if idx is not None:
            self.drafts[idx] = draft
        else:
            self.drafts.append(draft)

    def _determine_next_state(self, canonical_req: CanonicalUserRequest) -> AssistantState:
        intent = canonical_req.intent

        if intent in {Intent.CREATE_REQUIREMENT, Intent.UPDATE_REQUIREMENT,
                      Intent.CREATE_REQUIREMENT_DRAFT, Intent.CONTINUE_REQUIREMENT_DRAFT}:
            if self.active_requirement_draft and self.active_requirement_draft.ready_for_review:
                return AssistantState.REQUIREMENT_READY
            return AssistantState.REQUIREMENT_COLLECTING

        if intent in {Intent.ASK_QUANTITY_ESTIMATION, Intent.MATERIAL_ESTIMATION}:
            return AssistantState.ESTIMATION_COLLECTING

        if intent in {Intent.SEARCH_MATERIAL, Intent.LIVE_MARKETPLACE_QUERY}:
            return AssistantState.LIVE_QUERY

        if intent in {Intent.LIVE_DATA_QUERY, Intent.TRANSACTION_QUERY, Intent.MATCH_EXPLANATION}:
            return AssistantState.TRANSACTION_QUERY

        if intent in {
            Intent.ASK_CONSTRUCTION_KNOWLEDGE,
            Intent.ASK_STORAGE_KNOWLEDGE,
            Intent.GENERAL_PLATFORM_QUESTION,
            Intent.CONSTRUCTION_KNOWLEDGE,
            Intent.PRICE_INFORMATION,
            Intent.PLATFORM_HELP,
        }:
            return AssistantState.KNOWLEDGE_QA

        return AssistantState.IDLE
