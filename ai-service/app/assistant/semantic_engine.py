import logging
import re
from dataclasses import asdict
from typing import Any, Dict, List, Optional

from app.assistant.backend_tools import BackendToolsClient
from app.assistant.canonical_request import CanonicalUserRequest
from app.assistant.capability_registry import CapabilityRegistry, CapabilityResult
from app.assistant.conversation_state import ConversationStateStore
from app.assistant.item_resolver import ItemResolver
from app.assistant.llm_provider import GroqLLMProvider
from app.assistant.match_explanation_agent import MatchExplanationAgent
from app.assistant.material_estimation import MaterialEstimationEngine
from app.assistant.semantic_draft_agent import CatalogDraftValidator
from app.assistant.semantic_router import SemanticRouter
from app.assistant.semantic_schemas import ExtractedSlots, Intent, QuantitySlots, SemanticIntent
from app.assistant.state_machine import AssistantState, ConversationStateMachine
from app.assistant.unit_normalizer import QuantityNormalizer
from app.assistant.validation_gate import ValidationGate
from app.rag.retriever import KnowledgeRetriever

logger = logging.getLogger(__name__)

# Static RAG Knowledge Intents (Strictly Technical & Platform Guidance, NEVER live marketplace prices)
KNOWLEDGE_INTENTS = {
    Intent.CONSTRUCTION_KNOWLEDGE,
    Intent.PLATFORM_HELP,
}


class SurplusLinkSemanticAssistantEngine:
    """
    Stabilized architecture-level engine for SurplusLink AI Assistant.
    Enforces CanonicalUserRequest intermediate representation, ItemResolver open vocabulary,
    ConversationStateMachine state management, CapabilityRegistry tool execution,
    ValidationGate output checks, and deterministic calculations.
    """

    def __init__(
        self,
        rag_retriever: Optional[KnowledgeRetriever] = None,
        tools_client: Optional[BackendToolsClient] = None,
        llm_provider: Optional[GroqLLMProvider] = None,
        state_store: Optional[ConversationStateStore] = None,
    ):
        self.rag_retriever = rag_retriever or KnowledgeRetriever()
        self.tools_client = tools_client or BackendToolsClient()
        self.llm_provider = llm_provider or GroqLLMProvider()
        self.item_resolver = ItemResolver()
        self.router = SemanticRouter(self.llm_provider, item_resolver=self.item_resolver)
        self.states = state_store or ConversationStateStore()
        self.validator = CatalogDraftValidator(item_resolver=self.item_resolver)
        self.match_agent = MatchExplanationAgent()
        self.capability_registry = CapabilityRegistry()
        self.validation_gate = ValidationGate()

    async def process_chat(self, user_context: dict[str, Any], conversation_id: str, message: str) -> dict[str, Any]:
        key = f"{user_context.get('user_id', '')}:{conversation_id}"
        state = self.states.get(key)

        # Retain structured location from user_context if provided by Flutter
        if user_context.get("current_location") and not state.structured_location:
            state.structured_location = user_context["current_location"]

        logger.info(
            "Processing chat message: '%s', conversation_id=%s, GROQ_KEY_CONFIGURED=%s",
            message,
            conversation_id,
            bool(self.llm_provider.api_key),
        )

        # 1. Parse into CanonicalUserRequest
        if hasattr(self.router, "to_canonical"):
            canonical_req = self.router.to_canonical(message.strip(), state)
        else:
            routing_res = self.router.classify(message.strip(), state)
            primary = routing_res.intents[0] if routing_res and routing_res.intents else SemanticIntent(intent=Intent.UNKNOWN)
            item_cand = primary.extracted_slots.item or primary.referenced_item
            resolved = self.item_resolver.resolve(item_cand) if item_cand else None
            canonical_req = CanonicalUserRequest(
                raw_message=message,
                intent=primary.intent,
                confidence=primary.confidence,
                language=primary.response_language,
                item_candidate=item_cand,
                resolved_item=resolved.display_name if resolved else item_cand,
                catalog_item_id=resolved.catalog_item_id if resolved else None,
                is_custom_item=resolved.is_custom_item if resolved else True,
                location=primary.extracted_slots.location_text,
                quantity=primary.extracted_slots.quantity,
                preferences=primary.extracted_slots.preferences,
                is_follow_up=primary.is_follow_up,
                is_question=primary.is_question,
                is_purchase_request=primary.is_purchase_request,
                retrieval_query=primary.retrieval_query,
            )

        logger.info(
            "Canonical request parsed: intent=%s, confidence=%.2f, item=%s, location=%s, custom=%s",
            canonical_req.intent.value,
            canonical_req.confidence,
            canonical_req.resolved_item or canonical_req.item_candidate,
            canonical_req.location_text,
            canonical_req.is_custom_item,
        )

        # 2. Drive Conversation State Machine
        state_machine = ConversationStateMachine(
            current_state=AssistantState.IDLE,
            drafts=state.drafts,
            active_draft_id=state.active_draft_id,
            active_requirement_draft=state.active_requirement_draft,
            structured_location=state.structured_location,
            last_item_candidate=state.last_referenced_item,
            last_intent=state.last_resolved_intent,
            last_tool_context=state.last_tool_context,
        )
        current_state = state_machine.process_request(canonical_req)

        parts: List[str] = []
        citations: List[Dict[str, Any]] = []

        # 3. Capability Selection & Execution
        # A. Requirement creation / continuation (Catalog or Custom Item)
        if canonical_req.intent in {Intent.CREATE_REQUIREMENT_DRAFT, Intent.CONTINUE_REQUIREMENT_DRAFT}:
            catalog = await self.tools_client.get_catalog_item(user_context)
            extracted_slots = ExtractedSlots(
                item=canonical_req.resolved_item or canonical_req.item_candidate,
                quantity=canonical_req.quantity,
                preferences=canonical_req.preferences,
                location_text=canonical_req.location_text,
                structured_location=canonical_req.structured_location,
            )
            draft, error = self.validator.build_or_update(
                extracted_slots, catalog, state_machine.active_requirement_draft
            )
            if error:
                parts.append(error)
            elif draft:
                # Run Validation Gate on draft
                val_report = self.validation_gate.validate_requirement_draft(canonical_req, draft)
                if val_report.sanitized_location is not None:
                    draft.location_text = val_report.sanitized_location

                state_machine.set_active_draft(draft)
                state.drafts = state_machine.drafts
                state.active_draft_id = draft.id
                state.active_requirement_draft = draft
                state.awaiting_field = draft.missing_required_fields[0] if draft.missing_required_fields else None
                summary = self._draft_summary(draft, canonical_req.language)
                parts.append(summary if draft.ready_for_review else f"{summary} {self.validator.next_question(draft, canonical_req.language)}")

        # B. Material Estimation
        elif canonical_req.intent == Intent.MATERIAL_ESTIMATION:
            estimation_response = await self._handle_material_estimation(canonical_req, state_machine)
            parts.append(estimation_response)

        # C. Live Marketplace Queries & Price Information (Live ASP.NET Tool Provenance Required!)
        elif canonical_req.intent in {Intent.LIVE_MARKETPLACE_QUERY, Intent.PRICE_INFORMATION}:
            live_response, tool_data = await self._handle_live_marketplace_query(user_context, canonical_req)
            if self.validation_gate.validate_marketplace_provenance(canonical_req.intent, tool_data, live_response):
                parts.append(live_response)
            else:
                parts.append("I couldn't verify active seller listings for that material right now.")

        # D. Technical Knowledge & Platform Help (Static RAG)
        elif canonical_req.intent in KNOWLEDGE_INTENTS:
            answer, found = self._knowledge_answer(canonical_req.raw_message, canonical_req)
            parts.append(answer)
            citations.extend(found)

        # E. Authenticated User Account Queries
        elif canonical_req.intent in {Intent.LIVE_DATA_QUERY, Intent.TRANSACTION_QUERY, Intent.MATCH_EXPLANATION}:
            answer, context = await self._live_answer(user_context, canonical_req)
            state.last_tool_context = context
            parts.append(answer)

        # F. Security Refusal
        elif canonical_req.intent == Intent.SECURITY_REFUSAL:
            parts.append("I can help with SurplusLink materials and your own marketplace data, but I can't reveal protected instructions or other users' data.")

        # G. Low confidence / Clarification
        elif canonical_req.intent in {Intent.CLARIFICATION, Intent.UNKNOWN} or canonical_req.confidence < 0.68:
            parts.append(canonical_req.clarification_question or self._generic_clarification(canonical_req.language))

        # Sync state
        state.drafts = state_machine.drafts
        state.active_draft_id = state_machine.active_draft_id
        state.active_requirement_draft = state_machine.active_requirement_draft
        state.structured_location = state_machine.structured_location
        state.last_resolved_intent = state_machine.last_intent
        state.last_referenced_item = state_machine.last_item_candidate

        self.states.put(key, state)
        final_message = "\n\n".join(part for part in parts if part) or self._generic_clarification(canonical_req.language)

        return self._response(
            conversation_id,
            final_message,
            canonical_req.intent,
            state,
            citations,
        )

    async def _handle_material_estimation(self, req: CanonicalUserRequest, state_machine: ConversationStateMachine) -> str:
        item = (req.resolved_item or req.item_candidate or state_machine.last_item_candidate or "paint").lower()

        # Capability lookup: estimate.paint, estimate.tiles, estimate.cement, estimate.sealant
        cap_name = f"estimate.{item}" if f"estimate.{item}" in self.capability_registry._capabilities else "estimate.paint"

        params: Dict[str, Any] = {}
        if req.coverage_area:
            params["area_sqm"] = req.coverage_area

        if "dimensions" in state_machine.known_fields:
            dims = state_machine.known_fields["dimensions"]
            if isinstance(dims, dict):
                params["tile_width_mm"] = dims.get("width")
                params["tile_length_mm"] = dims.get("length")

        if req.dimensions:
            params["tile_width_mm"] = req.dimensions.width
            params["tile_length_mm"] = req.dimensions.length

        # Check if paint-specific options specified
        if "paint" in item:
            msg_lower = req.raw_message.lower()
            if "coat" in msg_lower:
                m_coats = re.search(r"(\d+)\s*coats?", msg_lower)
                if m_coats:
                    params["coats"] = int(m_coats.group(1))

        res = await self.capability_registry.execute(cap_name, params)
        if res.success and "explanation" in res.data:
            return res.data["explanation"]

        est_result = MaterialEstimationEngine.estimate(item, params)
        return est_result.explanation

    async def _handle_live_marketplace_query(self, user_context: dict[str, Any], req: CanonicalUserRequest) -> tuple[str, list[dict[str, Any]]]:
        item = req.resolved_item or req.item_candidate or "material"
        listings = await self.tools_client.search_active_listings(user_context, query=item)
        stats = await self.tools_client.get_price_statistics(user_context, query=item)

        if not listings:
            return f"There are currently no active seller listings for **{item.title()}** on the SurplusLink marketplace.", []

        lines = [f"Here are current active marketplace listings for **{item.title()}**:\n"]
        for l in listings[:5]:
            pkg = f" ({l['packageCount']} × {l['packageSize']}{l['unit']})" if l.get('packageCount') else ""
            lines.append(f"• **{l.get('title', 'Listing')}** — {l.get('quantity', 0)} {l.get('unit', '')}{pkg} @ **LKR {l.get('unitPrice', 0):,.2f}** / {l.get('unit', '')}")

        if stats and stats.get("totalListings", 0) > 1:
            lines.append(f"\n*Marketplace Summary ({stats['totalListings']} listings): Min LKR {stats['minPrice']:,.2f} | Median LKR {stats['medianPrice']:,.2f} | Max LKR {stats['maxPrice']:,.2f}*")

        return "\n".join(lines), listings

    def _knowledge_answer(self, original: str, req: CanonicalUserRequest):
        query = req.retrieval_query or original
        result = self.rag_retriever.retrieve(query)
        if not result.has_sufficient_evidence:
            return "I don't have enough verified SurplusLink knowledge to answer that confidently. Would you like to check available marketplace listings or submit a requirement?", []
        answer = self.llm_provider.generate_chat_response(
            [{"role": "user", "content": original}],
            system_prompt=f"You are the SurplusLink AI Assistant. Answer in {req.language}, concisely and naturally, using ONLY this verified evidence. If it does not support a claim, state what is missing.\n\n{result.context_text}",
        )
        if not answer:
            top = result.raw_results[0]["chunk"]
            answer = top["content"]
        return answer, [asdict(c) for c in result.citations]

    async def _live_answer(self, user_context: dict[str, Any], req: CanonicalUserRequest):
        resource = (req.live_data_request.get("live_resource") if req.live_data_request else "matches" if req.intent == Intent.MATCH_EXPLANATION else "").casefold() or "matches"
        getters = {
            "offers": self.tools_client.get_my_offers,
            "transactions": self.tools_client.get_my_transactions,
            "listings": self.tools_client.get_my_active_listings,
            "requirements": self.tools_client.get_my_requirements,
            "matches": self.tools_client.get_my_matches,
        }
        getter = getters.get(resource)
        if getter is None:
            return "Which account data should I check: listings, requirements, offers, transactions, or matches?", None
        rows = await getter(user_context)
        if req.intent == Intent.MATCH_EXPLANATION:
            return self.match_agent.explain_match(rows, req.raw_message), {"resource": resource, "count": len(rows)}
        if not rows:
            return f"You don't have any recorded {resource} at the moment.", {"resource": resource, "count": 0}
        compact = [{k: row.get(k) for k in ("id", "title", "materialName", "quantity", "unit", "status", "totalValue") if row.get(k) is not None} for row in rows[:5]]
        answer = self.llm_provider.generate_chat_response(
            [{"role": "user", "content": req.raw_message}],
            system_prompt=f"Answer concisely using only this authenticated {resource} tool result: {compact}",
        )
        return answer or "\n".join(f"• {row}" for row in compact), {"resource": resource, "count": len(rows)}

    @staticmethod
    def _draft_summary(draft, language: str) -> str:
        qty = f"{draft.package_count} × {draft.package_size}{draft.normalized_base_unit}" if draft.input_mode == "PACKAGE_COUNT" else (f"{draft.normalized_quantity:g}{draft.normalized_base_unit}" if draft.normalized_quantity is not None else draft.item_name)
        prefs = " ".join(draft.preferences.values())
        name = draft.display_name or draft.item_name
        if language == "si-Latn":
            return f"{qty} {prefs} {name} requirement draft ekata ekathu kala.".replace("  ", " ")
        return f"Draft updated: {qty} {prefs} {name}.".replace("  ", " ")

    @staticmethod
    def _generic_clarification(language: str) -> str:
        if language == "si-Latn":
            return "Requirement ekak hadannada, nathnam price/usage gana ahanawada?"
        return "Would you like to create a requirement, ask about price or usage, or check your account?"

    @staticmethod
    def _response(conversation_id: str, message: str, intent: Intent, state, citations=None):
        return {
            "conversation_id": conversation_id,
            "message": message,
            "intent": intent.value,
            "citations": citations or [],
            "requirement_draft": state.active_requirement_draft.model_dump() if state.active_requirement_draft else None,
            "drafts": [d.model_dump() for d in state.drafts],
            "active_draft_id": state.active_draft_id,
            "suggested_actions": ["Review Requirement"] if state.active_requirement_draft and state.active_requirement_draft.ready_for_review else [],
        }
