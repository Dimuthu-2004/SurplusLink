from __future__ import annotations

from datetime import datetime, timezone
from decimal import Decimal, ROUND_CEILING
from typing import Any, Literal, Mapping, TypedDict, cast

from langgraph.graph import END, START, StateGraph
from pydantic import ValidationError

from app.agents.agentic_schemas import SemanticMatchingOutput
from app.assistant.llm_provider import GeminiLLMProvider, LLMProvider
from app.agents.match_scoring import condition_rank
from app.agents.item_relevance import ItemRelevanceAgent
from app.agents.matching_schemas import (
    Candidate,
    CandidateExclusion,
    MatchingFailure,
    MatchingRequest,
    MatchingResponse,
)
from app.materials.read_boundary import (
    ActiveMaterialCriteria,
    ActiveMaterialsReadBoundary,
    MaterialListingRecord,
    MaterialSearchTools,
)


class MatchingState(TypedDict, total=False):
    raw_input: Mapping[str, Any]
    request: MatchingRequest
    search_results: list[MaterialListingRecord]
    deterministic_response: MatchingResponse
    response: MatchingResponse


class MaterialMatchingAgent:
    """Read-only LangGraph workflow for ranked Material listing candidates."""

    def __init__(
        self,
        boundary: ActiveMaterialsReadBoundary,
        *,
        llm: LLMProvider | None = None,
        clock: callable[[], datetime] | None = None,
    ) -> None:
        self._tools = MaterialSearchTools(boundary)
        self._llm = llm or GeminiLLMProvider()
        self._clock = clock or (lambda: datetime.now(timezone.utc))
        self._graph = self._build_graph()

    def match(self, raw_input: Mapping[str, Any]) -> MatchingResponse:
        """Validate input and return candidates or a safe structured failure."""

        state = self._graph.invoke({"raw_input": dict(raw_input)})
        return MatchingResponse.model_validate(state["response"])

    def _build_graph(self):
        workflow = StateGraph(MatchingState)
        workflow.add_node("validate_input", self._validate_input)
        workflow.add_node("search_active_materials", self._search_active_materials)
        workflow.add_node("build_candidates", self._build_candidates)
        workflow.add_node("semantic_reasoning", self._semantic_reasoning)
        workflow.add_edge(START, "validate_input")
        workflow.add_conditional_edges(
            "validate_input",
            self._after_validation,
            {
                "search": "search_active_materials",
                "finish": END,
            },
        )
        workflow.add_conditional_edges(
            "search_active_materials",
            self._after_search,
            {
                "candidates": "build_candidates",
                "finish": END,
            },
        )
        workflow.add_conditional_edges("build_candidates", self._after_candidates,
            {"semantic": "semantic_reasoning", "finish": END})
        workflow.add_edge("semantic_reasoning", END)
        return workflow.compile()

    def _validate_input(self, state: MatchingState) -> MatchingState:
        try:
            request = MatchingRequest.model_validate(state["raw_input"])
        except ValidationError as error:
            return {
                "response": self._failure(
                    "invalid_input",
                    "INVALID_INPUT",
                    "Matching input is invalid: " + self._validation_message(error),
                )
            }
        return {"request": request}

    @staticmethod
    def _after_validation(state: MatchingState) -> Literal["search", "finish"]:
        return "finish" if "response" in state else "search"

    def _search_active_materials(self, state: MatchingState) -> MatchingState:
        request = state["request"]
        criteria = ActiveMaterialCriteria(
            buyer_user_id=request.buyerUserId,
            category_id=request.categoryId,
            category=request.category,
            required_quantity=request.normalizedRequiredQuantity or request.requiredQuantity,
            unit=request.normalizedBaseUnit or request.baseUnit or request.unit,
            maximum_budget=request.maximumBudget,
            deadline=request.deadline,
            requested_at=self._clock(),
        )
        try:
            return {
                "search_results": self._tools.search_active_materials(criteria),
            }
        except Exception:
            return {
                "response": self._failure(
                    "search_unavailable",
                    "SEARCH_UNAVAILABLE",
                    "Material search is temporarily unavailable. Please try again later.",
                )
            }

    @staticmethod
    def _after_search(state: MatchingState) -> Literal["candidates", "finish"]:
        return "finish" if "response" in state else "candidates"

    def _build_candidates(self, state: MatchingState) -> MatchingState:
        request = state["request"]
        requested_at = self._clock()
        candidates: list[Candidate] = []
        exclusions: list[CandidateExclusion] = []
        relevance_results = {}

        for search_result in state.get("search_results", []):
            if search_result.seller_id == request.buyerUserId:
                exclusions.append(CandidateExclusion(listingId=search_result.listing_id))
                continue
            try:
                detail = self._tools.get_material_detail(
                    search_result.listing_id,
                    requested_at=requested_at,
                )
            except Exception:
                continue
            if detail is not None and detail.seller_id == request.buyerUserId:
                exclusions.append(CandidateExclusion(listingId=detail.listing_id))
                continue
            if detail is None:
                continue
            relevance = ItemRelevanceAgent().evaluate(request, detail)
            relevance_results[detail.listing_id] = relevance
            if not relevance.eligible:
                exclusions.append(CandidateExclusion(listingId=detail.listing_id, code="ITEM_MISMATCH"))
                continue
            if not self._fits(detail, request):
                continue
            candidates.append(self._candidate(detail, request))

        # The repository order is not an input to policy.  Make equal scores stable
        # across providers and repeat executions before the orchestrator selects one.
        candidates.sort(key=lambda candidate: (-candidate.basicFitScore, -condition_rank(candidate.condition), candidate.unitPrice, candidate.listingId))
        if not candidates:
            return {
                "response": self._failure(
                    "no_candidate",
                    "NO_CANDIDATE",
                    "No active, verified, non-expired listing matches the requested material.",
                    exclusions,
                ).model_copy(update={"itemRelevance": relevance_results})
            }

        return {
            "deterministic_response": MatchingResponse(
                status="ok",
                candidates=candidates,
                exclusions=exclusions,
                itemRelevance=relevance_results,
            )
        }

    @staticmethod
    def _after_candidates(state: MatchingState) -> Literal["semantic", "finish"]:
        return "semantic" if "deterministic_response" in state else "finish"

    def _semantic_reasoning(self, state: MatchingState) -> MatchingState:
        """Ask Gemini to explain only candidates that already passed hard gates."""
        deterministic = state["deterministic_response"]
        request = state["request"]
        rows = {row.listing_id: row for row in state["search_results"]}
        payload = []
        for candidate in deterministic.candidates:
            row = rows.get(candidate.listingId)
            payload.append({"listingId": candidate.listingId, "itemName": row.title if row else "",
                            "description": row.description if row else "", "condition": candidate.condition})
        output = self._llm.generate_structured(
            [{"role": "user", "content": (
                "For each already eligible marketplace listing, explain semantic suitability to the buyer. "
                "Return exactly one explanation for every supplied listing ID. Do not add listings. "
                "Do not claim stock, price, route, eligibility, or approval facts; those are verified elsewhere. "
                f"Buyer item={request.itemName or request.constructionItemTemplateId or request.category}; "
                f"buyer aliases={request.aliases}; buyer unit={request.unit}. Candidates={payload}"
            )}], SemanticMatchingOutput, temperature=0, max_tokens=1200)
        expected = {candidate.listingId for candidate in deterministic.candidates}
        if output is None or {item.listingId for item in output.explanations} != expected or len(output.explanations) != len(expected):
            return {"response": self._failure("semantic_unavailable", "SEMANTIC_REASONING_UNAVAILABLE",
                "Semantic matching is temporarily unavailable. Please try again later.", deterministic.exclusions)
                .model_copy(update={"itemRelevance": deterministic.itemRelevance})}
        explanations = {item.listingId: item for item in output.explanations}
        return {"response": deterministic.model_copy(update={
            "candidates": [candidate.model_copy(update={"semanticExplanation": explanations[candidate.listingId]})
                           for candidate in deterministic.candidates]
        })}

    @staticmethod
    def _fits(listing: MaterialListingRecord, request: MatchingRequest) -> bool:
        if listing.seller_id == request.buyerUserId:
            return False
        # Partial inventory is a valid candidate: allocation selection decides
        # how much to take. Only unavailable stock is a hard exclusion.
        if listing.available_quantity <= 0:
            return False
        # Unit conversion is deliberately not reimplemented in Python. The
        # backend sends canonical base measurements; package type never enters
        # this comparison because it is a physical selling form, not a unit.
        required_quantity = request.normalizedRequiredQuantity or request.requiredQuantity
        required_unit = request.normalizedBaseUnit or request.baseUnit or request.unit
        if (listing.base_unit or listing.unit).casefold() != required_unit.casefold():
            return False
        if listing.available_until.astimezone(timezone.utc).date() < request.deadline.astimezone(timezone.utc).date():
            return False
        required_packages = (required_quantity / listing.package_size).to_integral_value(rounding=ROUND_CEILING) if listing.quantity_mode in {"PACKAGE", "PIECE"} and listing.package_size else None
        expected_cost = listing.unit_price * (required_packages if required_packages is not None else required_quantity)
        if expected_cost > request.maximumBudget:
            return False
        if request.categoryId and listing.category_id != request.categoryId:
            return False
        return not (
            request.category
            and not request.categoryId
            and (listing.category or "").casefold() != request.category.casefold()
        )

    @staticmethod
    def _candidate(listing: MaterialListingRecord, request: MatchingRequest) -> Candidate:
        required_quantity = request.normalizedRequiredQuantity or request.requiredQuantity
        required_packages = (required_quantity / listing.package_size).to_integral_value(rounding=ROUND_CEILING) if listing.quantity_mode in {"PACKAGE", "PIECE"} and listing.package_size else None
        total_cost = listing.unit_price * (required_packages if required_packages is not None else required_quantity)
        budget_headroom = (request.maximumBudget - total_cost) / request.maximumBudget
        # Preliminary score only: routing is required for the final score.
        score = round(float(Decimal("50") * condition_rank(listing.condition) / 4
                            + Decimal("30") * budget_headroom), 2)
        return Candidate(
            listingId=listing.listing_id,
            availableQuantity=listing.available_quantity,
            unitPrice=listing.unit_price,
            condition=listing.condition,
            basicFitScore=score,
            reason=(
                (f"Seller has {listing.package_count_available} {listing.package_type.lower() if listing.package_type else 'packages'} "
                 f"of {listing.package_size}{listing.unit}; can contribute {listing.maximum_contribution or listing.available_quantity} {listing.unit}. "
                 if listing.quantity_mode in {"PACKAGE", "PIECE"} else "") +
                f"Active verified {listing.condition.lower()} listing with {listing.available_quantity} {listing.unit} available; "
                f"estimated cost {total_cost} is within the maximum budget."
            ),
            quantityMode=listing.quantity_mode, packageType=listing.package_type, packageSize=listing.package_size,
            packageCountAvailable=listing.package_count_available,
            baseEquivalentAvailableQuantity=listing.base_equivalent_available_quantity or listing.available_quantity,
            maximumContribution=listing.maximum_contribution or min(listing.available_quantity, required_quantity),
            fullCoverage=listing.full_coverage if listing.full_coverage is not None else listing.available_quantity >= required_quantity,
            baseUnit=listing.base_unit or listing.unit,
            minimumSellableIncrement=listing.minimum_sellable_increment,
            decimalPrecision=listing.decimal_precision,
        )

    @staticmethod
    def _failure(
        status: Literal["invalid_input", "search_unavailable", "no_candidate", "semantic_unavailable"],
        code: Literal["INVALID_INPUT", "SEARCH_UNAVAILABLE", "NO_CANDIDATE", "SEMANTIC_REASONING_UNAVAILABLE"],
        message: str,
        exclusions: list[CandidateExclusion] | None = None,
    ) -> MatchingResponse:
        return MatchingResponse(
            status=status,
            candidates=[],
            failure=MatchingFailure(code=code, message=message),
            exclusions=exclusions or [],
        )

    @staticmethod
    def _validation_message(error: ValidationError) -> str:
        return "; ".join(
            f"{'.'.join(str(part) for part in item['loc'])}: {item['msg']}"
            for item in error.errors()
        )
