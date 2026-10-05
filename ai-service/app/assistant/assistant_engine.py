import logging
import re
from typing import Any, Dict, List, Optional

from app.assistant.backend_tools import BackendToolsClient
from app.assistant.llm_provider import GeminiLLMProvider
from app.assistant.match_explanation_agent import MatchExplanationAgent
from app.assistant.requirement_draft_agent import RequirementDraft, RequirementDraftAgent
from app.rag.retriever import Citation, KnowledgeRetriever

logger = logging.getLogger(__name__)


class SurplusLinkAssistantEngine:
    def __init__(
        self,
        rag_retriever: Optional[KnowledgeRetriever] = None,
        tools_client: Optional[BackendToolsClient] = None,
        llm_provider: Optional[GeminiLLMProvider] = None,
    ):
        self.rag_retriever = rag_retriever or KnowledgeRetriever()
        self.tools_client = tools_client or BackendToolsClient()
        self.llm_provider = llm_provider or GeminiLLMProvider()
        self.draft_agent = RequirementDraftAgent()
        self.match_agent = MatchExplanationAgent()

    async def process_chat(
        self,
        user_context: Dict[str, Any],
        conversation_id: str,
        message: str,
    ) -> Dict[str, Any]:
        raw_msg = message.strip()
        msg_lower = raw_msg.lower()

        # 1. Security Check: Block Prompt Injection & System Key Leaks
        if self._is_prompt_injection_attempt(msg_lower):
            return {
                "conversation_id": conversation_id,
                "message": "I am the SurplusLink AI Assistant. I can help you with construction materials, platform guidance, requirement drafting, and checking your transactions.",
                "intent": "SECURITY_REFUSAL",
                "citations": [],
                "requirement_draft": None,
                "suggested_actions": ["Find materials", "How does matching work?", "My transactions"],
            }

        # 2. Intent Classification
        intent = self._classify_intent(msg_lower)

        # 3. Route to Intent Handlers
        if intent in ("CREATE_REQUIREMENT_DRAFT", "BUYER_REQUIREMENT_HELP") or self._looks_like_requirement_request(msg_lower):
            return await self._handle_requirement_draft(user_context, conversation_id, raw_msg)

        if intent in ("MY_OFFERS", "MY_TRANSACTIONS", "MY_LISTINGS", "MY_REQUIREMENTS", "MY_MATCHES", "MATCH_EXPLANATION"):
            return await self._handle_live_account_query(user_context, conversation_id, raw_msg, intent)

        if intent in ("CONSTRUCTION_KNOWLEDGE", "GENERAL_PLATFORM_HELP", "SELLER_LISTING_HELP", "QUANTITY_HELP", "PACKAGE_HELP", "LOCATION_HELP"):
            return await self._handle_rag_query(user_context, conversation_id, raw_msg, intent)

        # Mixed / Unknown Intent
        return await self._handle_mixed_or_unknown(user_context, conversation_id, raw_msg)

    def _is_prompt_injection_attempt(self, msg_lower: str) -> bool:
        injection_patterns = [
            r"ignore (all )?previous instructions",
            r"reveal (api|system) key",
            r"show system prompt",
            r"dump (all )?users",
            r"drop table",
            r"select \* from",
            r"another user'?s (data|transaction|record)",
        ]
        for pattern in injection_patterns:
            if re.search(pattern, msg_lower):
                return True
        return False

    def _classify_intent(self, msg_lower: str) -> str:
        # Live Account Tools
        if any(w in msg_lower for w in ["my offer", "mage offer", "my offers", "mage offers"]):
            return "MY_OFFERS"

        if any(w in msg_lower for w in ["my transaction", "mage transaction", "my transactions", "handover", "received"]):
            return "MY_TRANSACTIONS"

        if any(w in msg_lower for w in ["my listing", "mage listing", "my listings", "my stock"]):
            return "MY_LISTINGS"

        if any(w in msg_lower for w in ["my requirement", "mage requirement", "my requirements"]):
            return "MY_REQUIREMENTS"

        if any(w in msg_lower for w in ["why was this", "why recommended", "match score", "why match"]):
            return "MATCH_EXPLANATION"

        if any(w in msg_lower for w in ["my match", "my matches", "mage match"]):
            return "MY_MATCHES"

        # Requirement Creation (English, Sinhala, Tamil, Romanized Sinhala)
        if any(w in msg_lower for w in ["i need", "i want", "one", "ඕන", "வேண்டும்", "buy", "looking for"]):
            return "CREATE_REQUIREMENT_DRAFT"

        # Quantity / Package / Listing Help
        if any(w in msg_lower for w in ["package size", "can size", "bag size", "how to list", "quantity mode"]):
            return "PACKAGE_HELP"

        # Platform / Construction RAG Knowledge
        if any(w in msg_lower for w in ["how to store", "how does matching work", "what is surpluslink", "cement storage", "paint coverage", "sls", "standards"]):
            return "CONSTRUCTION_KNOWLEDGE"

        return "UNKNOWN"

    def _looks_like_requirement_request(self, msg_lower: str) -> bool:
        materials = ["paint", "cement", "tile", "tiles", "rebar", "steel", "timber", "wood", "generator", "compressor", "pvc", "සිමෙන්ති", "පේන්ට්", "ටයිල්"]
        has_material = any(m in msg_lower for m in materials)
        has_qty_or_need = any(w in msg_lower for w in ["need", "want", "one", "litre", "l", "kg", "bags", "cans", "boxes", "10k", "5k", "50kg", "ඕන"])
        return has_material and has_qty_or_need

    async def _handle_requirement_draft(
        self,
        user_context: Dict[str, Any],
        conversation_id: str,
        message: str,
    ) -> Dict[str, Any]:
        catalog_items = await self.tools_client.get_catalog_item(user_context)
        draft, reply_text = self.draft_agent.extract_draft(message, catalog_items)

        if not draft:
            return {
                "conversation_id": conversation_id,
                "message": reply_text,
                "intent": "BUYER_REQUIREMENT_HELP",
                "citations": [],
                "requirement_draft": None,
                "suggested_actions": ["I need paint", "I need 50 bags of cement", "Browse materials"],
            }

        actions = ["Review Requirement"] if draft.ready_for_review else ["Specify delivery location", "Change quantity"]

        return {
            "conversation_id": conversation_id,
            "message": reply_text,
            "intent": "CREATE_REQUIREMENT_DRAFT",
            "citations": [],
            "requirement_draft": draft.to_dict(),
            "suggested_actions": actions,
        }

    async def _handle_live_account_query(
        self,
        user_context: Dict[str, Any],
        conversation_id: str,
        message: str,
        intent: str,
    ) -> Dict[str, Any]:
        if intent == "MY_OFFERS":
            offers = await self.tools_client.get_my_offers(user_context)
            if not offers:
                reply = "You don't have any active offers at the moment."
            else:
                lines = ["Here are your active SurplusLink offers:\n"]
                for o in offers[:5]:
                    lines.append(f"• **{o.get('materialName', 'Material')}** — {o.get('quantity', 0)} {o.get('unit', '')} (LKR {o.get('totalValue', 0):,.2f}) — Status: `{o.get('status', 'PENDING')}`")
                reply = "\n".join(lines)

            return {
                "conversation_id": conversation_id,
                "message": reply,
                "intent": intent,
                "citations": [],
                "requirement_draft": None,
                "suggested_actions": ["My transactions", "Create a requirement"],
            }

        if intent == "MY_TRANSACTIONS":
            txs = await self.tools_client.get_my_transactions(user_context)
            if not txs:
                reply = "You don't have any recorded transactions currently."
            else:
                lines = ["Here are your recent SurplusLink transactions:\n"]
                for t in txs[:5]:
                    status = t.get("status", "PENDING")
                    ref = t.get("referenceNumber", t.get("id", "")[:8])
                    lines.append(f"• **Ref #{ref}** — {t.get('materialName', 'Material')} — Status: `{status}`")
                reply = "\n".join(lines)

            return {
                "conversation_id": conversation_id,
                "message": reply,
                "intent": intent,
                "citations": [],
                "requirement_draft": None,
                "suggested_actions": ["My offers", "My requirements"],
            }

        if intent == "MY_LISTINGS":
            listings = await self.tools_client.get_my_active_listings(user_context)
            if not listings:
                reply = "You don't have any active seller listings currently on the marketplace."
            else:
                lines = ["Here are your active seller material listings:\n"]
                for l in listings[:5]:
                    lines.append(f"• **{l.get('title', 'Listing')}** — {l.get('quantity', 0)} {l.get('unit', '')} available @ LKR {l.get('unitPrice', 0):,.2f}/{l.get('unit', '')}")
                reply = "\n".join(lines)

            return {
                "conversation_id": conversation_id,
                "message": reply,
                "intent": intent,
                "citations": [],
                "requirement_draft": None,
                "suggested_actions": ["How to list materials", "My offers"],
            }

        if intent in ("MY_MATCHES", "MATCH_EXPLANATION"):
            matches = await self.tools_client.get_my_matches(user_context)
            reply = self.match_agent.explain_match(matches, message)
            return {
                "conversation_id": conversation_id,
                "message": reply,
                "intent": intent,
                "citations": [],
                "requirement_draft": None,
                "suggested_actions": ["My requirements", "My transactions"],
            }

        # Fallback to requirements
        reqs = await self.tools_client.get_my_requirements(user_context)
        if not reqs:
            reply = "You don't have any active buyer requirements currently."
        else:
            lines = ["Here are your submitted buyer requirements:\n"]
            for r in reqs[:5]:
                lines.append(f"• **{r.get('title', 'Requirement')}** — {r.get('quantity', 0)} {r.get('unit', '')} — Status: `{r.get('status', 'OPEN')}`")
            reply = "\n".join(lines)

        return {
            "conversation_id": conversation_id,
            "message": reply,
            "intent": intent,
            "citations": [],
            "requirement_draft": None,
            "suggested_actions": ["Create a requirement", "My offers"],
        }

    async def _handle_rag_query(
        self,
        user_context: Dict[str, Any],
        conversation_id: str,
        message: str,
        intent: str,
    ) -> Dict[str, Any]:
        rag_res = self.rag_retriever.retrieve(message)

        if not rag_res.has_sufficient_evidence:
            return {
                "conversation_id": conversation_id,
                "message": "I don't have enough verified SurplusLink knowledge to answer that confidently. Would you like to check available materials or submit a requirement?",
                "intent": intent,
                "citations": [],
                "requirement_draft": None,
                "suggested_actions": ["Find materials", "How does matching work?", "Contact support"],
            }

        # Try Groq synthesis if LLM provider is available
        llm_reply = None
        if self.llm_provider.is_available():
            sys_prompt = (
                "You are the knowledgeable SurplusLink AI Assistant for Sri Lanka construction material reuse.\n"
                "Answer the user's question accurately using ONLY the provided verified context.\n"
                "Do NOT hallucinate or invent facts not present in the context.\n"
                "Keep the response practical, short, clear, and mobile-friendly."
            )
            messages = [
                {"role": "system", "content": f"Verified Context:\n{rag_res.context_text}"},
                {"role": "user", "content": message},
            ]
            llm_reply = self.llm_provider.generate_chat_response(messages, system_prompt=sys_prompt)

        if not llm_reply:
            # Fallback deterministic answer from retrieved top chunk
            top_raw = rag_res.raw_results[0]["chunk"]
            llm_reply = f"{top_raw['content']}\n\n*Source: {top_raw['title']} ({top_raw['source']})*"

        citations_list = [asdict(c) for c in rag_res.citations]

        return {
            "conversation_id": conversation_id,
            "message": llm_reply,
            "intent": intent,
            "citations": citations_list,
            "requirement_draft": None,
            "suggested_actions": ["Create a requirement", "My active listings"],
        }

    async def _handle_mixed_or_unknown(
        self,
        user_context: Dict[str, Any],
        conversation_id: str,
        message: str,
    ) -> Dict[str, Any]:
        # Try RAG retrieval first
        rag_res = self.rag_retriever.retrieve(message)
        if rag_res.has_sufficient_evidence:
            return await self._handle_rag_query(user_context, conversation_id, message, "CONSTRUCTION_KNOWLEDGE")

        # Friendly fallback assistant response
        return {
            "conversation_id": conversation_id,
            "message": "I am your SurplusLink Construction Reuse Assistant! I can help you find materials, create requirement drafts, explain matching, or check your offers and transactions.",
            "intent": "GENERAL_PLATFORM_HELP",
            "citations": [],
            "requirement_draft": None,
            "suggested_actions": ["Find materials", "Create a requirement", "My offers", "My transactions"],
        }
