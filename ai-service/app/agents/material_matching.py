from __future__ import annotations

from datetime import datetime, timezone
from decimal import Decimal
from typing import Any, Literal, Mapping, TypedDict, cast

from langgraph.graph import END, START, StateGraph
from pydantic import ValidationError

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
    response: MatchingResponse


class MaterialMatchingAgent:
    """Read-only LangGraph workflow for ranked Material listing candidates."""

    def __init__(
        self,
        boundary: ActiveMaterialsReadBoundary,
        *,
        clock: callable[[], datetime] | None = None,
    ) -> None:
        self._tools = MaterialSearchTools(boundary)
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
        workflow.add_edge("build_candidates", END)
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
            required_quantity=request.requiredQuantity,
            unit=request.unit,
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
            if detail is None or not self._fits(detail, request):
                continue
            candidates.append(self._candidate(detail, request))

        # The repository order is not an input to policy.  Make equal scores stable
        # across providers and repeat executions before the orchestrator selects one.
        candidates.sort(key=lambda candidate: (-candidate.basicFitScore, candidate.listingId))
        if not candidates:
            return {
                "response": self._failure(
                    "no_candidate",
                    "NO_CANDIDATE",
                    "No active, verified, non-expired listing matches the requested material.",
                    exclusions,
                )
            }

        return {
            "response": MatchingResponse(
                status="ok",
                candidates=candidates,
                exclusions=exclusions,
            )
        }

    @staticmethod
    def _fits(listing: MaterialListingRecord, request: MatchingRequest) -> bool:
        if listing.seller_id == request.buyerUserId:
            return False
        if listing.available_quantity < request.requiredQuantity:
            return False
        if listing.unit.casefold() != request.unit.casefold():
            return False
        if listing.available_until.astimezone(timezone.utc).date() < request.deadline.astimezone(timezone.utc).date():
            return False
        if listing.unit_price * request.requiredQuantity > request.maximumBudget:
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
        total_cost = listing.unit_price * request.requiredQuantity
        budget_headroom = (request.maximumBudget - total_cost) / request.maximumBudget
        quantity_headroom = min(
            (listing.available_quantity - request.requiredQuantity) / request.requiredQuantity,
            Decimal("1"),
        )
        score = round(float(50 + (30 * budget_headroom) + (20 * quantity_headroom)), 2)
        return Candidate(
            listingId=listing.listing_id,
            availableQuantity=listing.available_quantity,
            unitPrice=listing.unit_price,
            condition=listing.condition,
            basicFitScore=score,
            reason=(
                f"Active verified {listing.condition.lower()} listing with "
                f"{listing.available_quantity} {listing.unit} available; "
                f"estimated cost {total_cost} is within the maximum budget."
            ),
        )

    @staticmethod
    def _failure(
        status: Literal["invalid_input", "search_unavailable", "no_candidate"],
        code: Literal["INVALID_INPUT", "SEARCH_UNAVAILABLE", "NO_CANDIDATE"],
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
