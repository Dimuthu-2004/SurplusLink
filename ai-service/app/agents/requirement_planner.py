from __future__ import annotations

from collections.abc import Callable
from datetime import datetime, timezone
from typing import Any, Literal, TypedDict

from langgraph.graph import END, START, StateGraph
from pydantic import ValidationError

from app.agents.agentic_schemas import PlannerModelOutput
from app.assistant.llm_provider import GeminiLLMProvider, LLMProvider
from app.agents.planner_schemas import (
    NormalizedCriteria, PlannerFailure, PlannerIssue, PlannerRequest, PlannerResponse,
    PlannerSuccess, planner_response_adapter, validated_plan,
)


class PlannerState(TypedDict, total=False):
    raw_input: Any
    now: datetime
    request: PlannerRequest
    response: PlannerResponse


class RequirementPlannerAgent:
    """Gemini-backed, tool-free planner with a code-owned capability allow-list.

    The caller supplies the stored backend DTO after enforcing authentication and
    ownership. Objective/notes are data, never routing or policy instructions.
    """

    def __init__(self, *, llm: LLMProvider | None = None,
                 clock: Callable[[], datetime] | None = None) -> None:
        self._clock = clock or (lambda: datetime.now(timezone.utc))
        self._llm = llm or GeminiLLMProvider()
        graph = StateGraph(PlannerState)
        graph.add_node("validate_requirement", self._validate)
        graph.add_node("build_plan", self._build_plan)
        graph.add_edge(START, "validate_requirement")
        graph.add_conditional_edges(
            "validate_requirement",
            lambda state: "finish" if "response" in state else "plan",
            {"finish": END, "plan": "build_plan"},
        )
        graph.add_edge("build_plan", END)
        self._graph = graph.compile()

    def plan(self, raw_input: Any) -> PlannerResponse:
        """Return a Pydantic success/failure envelope, never free-form prose."""
        now = self._clock()
        if now.tzinfo is None:
            raise ValueError("Planner clock must be timezone-aware.")
        state = self._graph.invoke({"raw_input": raw_input, "now": now})
        # Revalidate at the output boundary even if future graph nodes are replaced.
        return planner_response_adapter.validate_python(state["response"].model_dump())

    def plan_json(self, raw_input: Any) -> str:
        """JSON only. Decimal values serialize as strings to preserve precision."""
        return self.plan(raw_input).model_dump_json()

    @staticmethod
    def _validate(state: PlannerState) -> PlannerState:
        try:
            request = PlannerRequest.model_validate(state["raw_input"], context={"now": state["now"]})
        except ValidationError as error:
            # Do not reflect raw values or arbitrary attacker-controlled extra-key names.
            fields = set(PlannerRequest.model_fields) | {
                "id", "buyerId", "categoryId", "category", "requiredQuantity", "unit",
                "maximumBudget", "deadline", "latitude", "longitude", "notes",
                "status", "createdAt", "updatedAt",
            }
            issues = []
            for item in error.errors(include_input=False, include_url=False, include_context=False):
                path = ".".join(str(part) if part in fields else "[unknown]" for part in item["loc"]) or "input"
                code = "MISSING_FIELD" if item["type"] == "missing" else "INVALID_FIELD"
                message = "Required field is missing." if code == "MISSING_FIELD" else "Field is invalid."
                issues.append(PlannerIssue(field=path, code=code, message=message))
            return {"response": PlannerFailure(issues=tuple(issues))}
        return {"request": request}

    def _build_plan(self, state: PlannerState) -> PlannerState:
        request = state["request"]
        stored = request.buyerRequest
        criteria = NormalizedCriteria(
            buyerUserId=stored.buyerId,
            categoryId=stored.categoryId, category=stored.category,
            requiredQuantity=stored.requiredQuantity, unit=stored.unit,
            maximumBudget=stored.maximumBudget, deadline=stored.deadline,
            targetLatitude=stored.latitude, targetLongitude=stored.longitude, notes=stored.notes,
            deliveryRequired=stored.deliveryRequired,
        )
        warnings: list[Literal["NOTES_ARE_UNTRUSTED_DATA"]] = []
        if stored.notes:
            warnings.append("NOTES_ARE_UNTRUSTED_DATA")
        proposal = self._llm.generate_structured(
            [
                {"role": "user", "content": (
                    "Create a concise procurement execution plan for this confirmed requirement. "
                    "Use only MATCHING, LOGISTICS, VALIDATION in that exact safe order. "
                    "MATCHING and VALIDATION are mandatory. Include LOGISTICS only when deliveryRequired is true. "
                    "Do not calculate quantities, prices, routes, or make approval decisions. "
                    f"Objective: {request.objective or stored.itemName or stored.category or 'confirmed material requirement'}\n"
                    f"Confirmed requirement: category={stored.category or stored.categoryId}; item={stored.itemName}; "
                    f"quantity={stored.requiredQuantity} {stored.unit}; budget={stored.maximumBudget}; "
                    f"deadline={stored.deadline.isoformat()}; deliveryRequired={stored.deliveryRequired}."
                )}
            ],
            PlannerModelOutput,
            temperature=0,
            max_tokens=700,
        )
        if proposal is None:
            return {"response": PlannerFailure(issues=(PlannerIssue(
                field="planner", code="LLM_UNAVAILABLE", message="Structured planning is unavailable."),))}
        try:
            plan = validated_plan(proposal, stored.deliveryRequired)
        except ValueError:
            return {"response": PlannerFailure(issues=(PlannerIssue(
                field="planSteps", code="INVALID_LLM_PLAN", message="Structured planning is invalid."),))}
        return {"response": PlannerSuccess(
            buyerRequestId=stored.id, normalizedCriteria=criteria,
            planSteps=plan, reasoningSummary=proposal.reasoningSummary,
            constraints=proposal.constraints, warnings=tuple(warnings),
        )}
