from __future__ import annotations

import logging
from enum import Enum
from typing import Any, Dict, List, Optional, Set
from pydantic import BaseModel, ConfigDict, Field

from app.assistant.canonical_request import CanonicalUserRequest
from app.assistant.semantic_schemas import Intent, RequirementDraft

logger = logging.getLogger(__name__)


class AssistantState(str, Enum):
    IDLE = "IDLE"
    KNOWLEDGE_QA = "KNOWLEDGE_QA"
    ESTIMATION_COLLECTING_INPUTS = "ESTIMATION_COLLECTING_INPUTS"
    REQUIREMENT_COLLECTING_INPUTS = "REQUIREMENT_COLLECTING_INPUTS"
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
    AssistantState.ESTIMATION_COLLECTING_INPUTS: StateConfiguration(
        state=AssistantState.ESTIMATION_COLLECTING_INPUTS,
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
    AssistantState.REQUIREMENT_COLLECTING_INPUTS: StateConfiguration(
        state=AssistantState.REQUIREMENT_COLLECTING_INPUTS,
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
    and field merging across turns.
    """

    model_config = ConfigDict(extra="ignore")

    current_state: AssistantState = AssistantState.IDLE
    active_requirement_draft: Optional[RequirementDraft] = None
    known_fields: Dict[str, Any] = Field(default_factory=dict)
    missing_fields: List[str] = Field(default_factory=list)

    last_item_candidate: Optional[str] = None
    last_intent: Optional[Intent] = None
    last_tool_context: Optional[Dict[str, Any]] = None

    def process_request(self, canonical_req: CanonicalUserRequest) -> AssistantState:
        """
        Processes a canonical user request, performs topic-switch detection,
        merges new slots into state, and returns the updated state.
        """
        # 1. Topic Switch Detection: If current request is a broad query or unrelated intent, reset context
        if self._is_topic_switch(canonical_req):
            logger.info("Topic switch detected for intent %s. Resetting context.", canonical_req.intent)
            self._reset_item_context()

        # 2. State Merging
        self._merge_canonical_request(canonical_req)

        # 3. State Transition
        new_state = self._determine_next_state(canonical_req)
        self.current_state = new_state
        self.last_intent = canonical_req.intent
        if canonical_req.item_candidate:
            self.last_item_candidate = canonical_req.item_candidate

        return self.current_state

    def _is_topic_switch(self, canonical_req: CanonicalUserRequest) -> bool:
        """Determines if the new request represents a topic switch away from current context."""
        # Broad marketplace queries ("sellers lage thiyena items monada") should not be constrained by active draft item
        if canonical_req.intent == Intent.LIVE_MARKETPLACE_QUERY and not canonical_req.is_follow_up:
            return True

        if self.current_state == AssistantState.IDLE and not self.active_requirement_draft:
            return False

        # If user explicitly specifies a different item (e.g. active is cement, user asks about tiles transport)
        if (
            canonical_req.item_candidate
            and self.last_item_candidate
            and canonical_req.item_candidate.lower() != self.last_item_candidate.lower()
            and not canonical_req.is_follow_up
        ):
            return True

        # Platform help or general knowledge questions during requirement drafting
        if canonical_req.intent in {Intent.PLATFORM_HELP, Intent.SECURITY_REFUSAL}:
            return True

        return False

    def _reset_item_context(self) -> None:
        """Clears active draft and item-specific known fields."""
        self.active_requirement_draft = None
        self.known_fields.clear()
        self.missing_fields.clear()
        self.last_item_candidate = None

    def _merge_canonical_request(self, canonical_req: CanonicalUserRequest) -> None:
        """
        Merges new canonical information into existing state WITHOUT overwriting
        unrelated previously collected fields.
        """
        # Item candidate
        if canonical_req.item_candidate:
            self.known_fields["item"] = canonical_req.item_candidate

        # Location text (only if valid)
        if canonical_req.location:
            self.known_fields["location_text"] = canonical_req.location

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

    def _determine_next_state(self, canonical_req: CanonicalUserRequest) -> AssistantState:
        intent = canonical_req.intent

        if intent in {Intent.CREATE_REQUIREMENT_DRAFT, Intent.CONTINUE_REQUIREMENT_DRAFT}:
            if self.active_requirement_draft and self.active_requirement_draft.ready_for_review:
                return AssistantState.REQUIREMENT_READY
            return AssistantState.REQUIREMENT_COLLECTING_INPUTS

        if intent == Intent.MATERIAL_ESTIMATION:
            return AssistantState.ESTIMATION_COLLECTING_INPUTS

        if intent == Intent.LIVE_MARKETPLACE_QUERY:
            return AssistantState.LIVE_QUERY

        if intent in {Intent.LIVE_DATA_QUERY, Intent.TRANSACTION_QUERY, Intent.MATCH_EXPLANATION}:
            return AssistantState.TRANSACTION_QUERY

        if intent in {Intent.CONSTRUCTION_KNOWLEDGE, Intent.PRICE_INFORMATION, Intent.PLATFORM_HELP}:
            return AssistantState.KNOWLEDGE_QA

        return AssistantState.IDLE
