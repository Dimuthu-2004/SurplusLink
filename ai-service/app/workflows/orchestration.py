from __future__ import annotations

import asyncio
import os
from datetime import datetime, timezone
from decimal import Decimal
from time import perf_counter
from typing import Any, Literal, TypedDict
from uuid import UUID

from langgraph.graph import END, START, StateGraph
from pydantic import AwareDatetime, Field, model_validator

from app.agents.logistics import LogisticsAgent
from app.agents.logistics_schemas import Contract, Latitude, Longitude, Measurement
from app.agents.material_matching import MaterialMatchingAgent
from app.agents.requirement_planner import RequirementPlannerAgent
from app.agents.validation import DeterministicValidationTools, ToolTrace, ValidationAgent, ValidationInput, ValidationResult
from app.materials.read_boundary import MaterialListingRecord


class Limits(Contract):
    stageTimeoutSeconds: float = Field(default=5, gt=0, le=30)
    workflowTimeoutSeconds: float = Field(default=25, gt=0, le=120)
    toolTimeoutSeconds: float = Field(default=1, gt=0, le=10)
    maxRetries: int = Field(default=1, ge=0, le=3, strict=True)
    transactionThreshold: Measurement = Field(default=Decimal("100000"), gt=0)

    @classmethod
    def from_env(cls):
        return cls(stageTimeoutSeconds=float(os.getenv("WORKFLOW_STAGE_TIMEOUT_SECONDS", "5")),
                   workflowTimeoutSeconds=float(os.getenv("WORKFLOW_TIMEOUT_SECONDS", "25")),
                   toolTimeoutSeconds=float(os.getenv("WORKFLOW_TOOL_TIMEOUT_SECONDS", "1")),
                   maxRetries=int(os.getenv("WORKFLOW_MAX_RETRIES", "1")),
                   transactionThreshold=os.getenv("WORKFLOW_TRANSACTION_THRESHOLD", "100000"))


class ListingSnapshot(Contract):
    matchId: UUID
    listingId: UUID
    sellerId: UUID
    categoryId: UUID
    availableQuantity: Measurement
    unit: str = Field(min_length=1, max_length=32)
    unitPrice: Measurement = Field(gt=0)
    condition: str = Field(min_length=1, max_length=32)
    status: str = Field(min_length=1, max_length=32)
    availableUntil: AwareDatetime
    latitude: Latitude | None = None
    longitude: Longitude | None = None
    distanceKm: Measurement | None = None
    durationMinutes: Measurement | None = None
    transportCost: Measurement | None = None
    routingError: str | None = Field(default=None, max_length=80)


class WorkflowRequest(Contract):
    workflowId: UUID
    # Validated by the planner so malformed stored data gives a revision result.
    buyerRequest: dict[str, Any]
    listings: tuple[ListingSnapshot, ...] = Field(max_length=20)

    @model_validator(mode="after")
    def unique_ids(self):
        if len({x.listingId for x in self.listings}) != len(self.listings) or len({x.matchId for x in self.listings}) != len(self.listings):
            raise ValueError("Duplicate snapshot identifiers.")
        return self


class StepTrace(Contract):
    sequence: int
    stage: Literal["PLANNER", "MATCHING", "LOGISTICS", "VALIDATION"]
    status: Literal["COMPLETED", "FAILED"]
    output: dict[str, Any]
    errorCode: str | None = None
    retryCount: int = 0
    startedAtUtc: AwareDatetime
    completedAtUtc: AwareDatetime
    durationMilliseconds: int
    toolCalls: tuple[ToolTrace, ...] = ()


class Recommendation(Contract):
    matchId: UUID
    listingId: UUID
    score: Measurement = Field(le=1)
    distanceKm: Measurement
    transportCost: Measurement


class WorkflowResponse(Contract):
    workflowId: UUID
    status: Literal["PENDING_APPROVAL", "REVISION_REQUESTED", "REJECTED", "FAILED"]
    validation: ValidationResult
    recommendation: Recommendation | None = None
    steps: tuple[StepTrace, ...]
    errorCode: str | None = None

    @model_validator(mode="after")
    def approval_gate(self):
        pending = self.status == "PENDING_APPROVAL"
        if pending != self.validation.valid or pending != self.validation.requiresApproval:
            raise ValueError("Invalid approval gate.")
        if pending:
            if self.recommendation is None or self.validation.recommendedMatchId != self.recommendation.matchId:
                raise ValueError("Missing validated recommendation.")
        elif self.recommendation is not None or self.validation.recommendedMatchId is not None:
            raise ValueError("Invalid result must not recommend a match.")
        return self


class SnapshotTools:
    """Read-only adapters over the authenticated API snapshot, no network or DB.

    Real route/price values are supplied by ASP.NET's routing provider. Missing
    values fail closed; no straight-line estimate is invented by these adapters.
    """

    def __init__(self, listings):
        self.listings = {str(x.listingId): x for x in listings}

    @staticmethod
    def _record(x):
        return MaterialListingRecord(seller_id=x.sellerId, listing_id=str(x.listingId),
            category_id=str(x.categoryId), category=None, available_quantity=x.availableQuantity,
            unit=x.unit, unit_price=x.unitPrice, condition=x.condition, status=x.status,
            is_verified=x.status == "ACTIVE", available_until=x.availableUntil)

    def search_active_materials(self, criteria):
        return [self._record(x) for x in self.listings.values()]

    def get_material_detail(self, listing_id):
        row = self.listings.get(str(listing_id))
        return self._record(row) if row else None

    def get_listing_location(self, request):
        row = self.listings[str(request.listingId)]
        return dict(listingId=row.listingId, latitude=row.latitude, longitude=row.longitude)

    def get_route_estimate(self, request):
        row = self.listings[str(request.listingId)]
        if row.routingError or row.distanceKm is None or row.durationMinutes is None:
            return dict(success=False, errorCode="ROUTING_UNAVAILABLE")
        return dict(success=True, distanceKm=row.distanceKm, durationMinutes=row.durationMinutes)

    def calculate_transport_estimate(self, request):
        row = self.listings[str(request.listingId)]
        return dict(estimatedTransportCost=row.transportCost) if row.transportCost is not None else dict(errorCode="TRANSPORT_UNAVAILABLE")


class State(TypedDict, total=False):
    request: WorkflowRequest
    planner: Any
    matching: Any
    logistics: Any
    validation: ValidationResult
    recommendation: Recommendation
    status: str
    errorCode: str


class WorkflowOrchestrator:
    """One invocation per instance. Topology and policy are code, never model text."""

    def __init__(self, limits: Limits | None = None, *, validation_tools=None):
        self.limits = limits or Limits()
        self.validation_tools = validation_tools or DeterministicValidationTools(self.limits.transactionThreshold)
        self.steps: list[StepTrace] = []
        graph = StateGraph(State)
        nodes = (("PLANNER", self._planner), ("MATCHING", self._matching),
                 ("LOGISTICS", self._logistics), ("VALIDATION", self._validation))
        for name, node in nodes:
            graph.add_node(name, self._bounded(name, node))
        graph.add_edge(START, "PLANNER")
        for current, following in zip(nodes, nodes[1:]):
            graph.add_conditional_edges(current[0], lambda state: "stop" if "status" in state else "next",
                                        {"stop": END, "next": following[0]})
        graph.add_edge("VALIDATION", END)
        self.graph = graph.compile()

    async def run(self, request: WorkflowRequest):
        self.steps = []
        try:
            state = await asyncio.wait_for(self.graph.ainvoke({"request": request}, {"recursion_limit": 8}),
                                           timeout=self.limits.workflowTimeoutSeconds)
        except TimeoutError:
            state = dict(status="FAILED", errorCode="WORKFLOW_TIMEOUT")
        except Exception:
            state = dict(status="FAILED", errorCode="WORKFLOW_FAILED")
        validation = state.get("validation") or ValidationResult(valid=False, requiresApproval=False,
            recommendedMatchId=None, violations=(state.get("errorCode", "WORKFLOW_INCOMPLETE"),))
        return WorkflowResponse(workflowId=request.workflowId, status=state.get("status", "FAILED"),
            validation=validation, recommendation=state.get("recommendation"), steps=tuple(self.steps),
            errorCode=state.get("errorCode"))

    def _bounded(self, stage, node):
        async def execute(state):
            started, tick = datetime.now(timezone.utc), perf_counter()
            result, output, tools, error, attempt = {}, {}, (), None, 0
            for attempt in range(self.limits.maxRetries + 1):
                try:
                    result, output, tools = await asyncio.wait_for(node(state), self.limits.stageTimeoutSeconds)
                    error = None
                    break
                except TimeoutError:
                    # Do not start another sync agent while a timed-out read may still run.
                    error = "STAGE_TIMEOUT"
                    break
                except (ConnectionError, OSError):
                    error = "STAGE_UNAVAILABLE"
                except Exception:
                    error = "INVALID_STAGE_RESULT"
                    break
            if error:
                result = dict(status="FAILED", errorCode=error)
            self.steps.append(StepTrace(sequence=len(self.steps) + 1, stage=stage,
                status="FAILED" if error or result.get("status") in ("FAILED", "REJECTED", "REVISION_REQUESTED") else "COMPLETED",
                output=output, errorCode=error or result.get("errorCode"), retryCount=attempt,
                startedAtUtc=started, completedAtUtc=datetime.now(timezone.utc),
                durationMilliseconds=round((perf_counter() - tick) * 1000), toolCalls=tools))
            return result
        return execute

    async def _planner(self, state):
        result = await asyncio.to_thread(RequirementPlannerAgent().plan, {"buyerRequest": state["request"].buyerRequest})
        return (dict(planner=result) if result.status == "ok" else dict(status="REVISION_REQUESTED", errorCode="INVALID_REQUIREMENT"),
                result.model_dump(mode="json"), ())

    async def _matching(self, state):
        fields = state["planner"].normalizedCriteria.model_dump(mode="json")
        criteria = {k: fields[k] for k in ("buyerUserId", "categoryId", "category", "requiredQuantity", "unit", "maximumBudget", "deadline")}
        result = await asyncio.to_thread(MaterialMatchingAgent(SnapshotTools(state["request"].listings)).match, criteria)
        return (dict(matching=result) if result.status == "ok" else dict(status="REVISION_REQUESTED", errorCode="NO_MATCHING_CANDIDATE"),
                result.model_dump(mode="json"), ())

    async def _logistics(self, state):
        criteria = state["planner"].normalizedCriteria
        result = await asyncio.to_thread(LogisticsAgent(SnapshotTools(state["request"].listings), max_retries=0).assess, dict(
            requirementId=state["planner"].buyerRequestId, candidateListingIds=[x.listingId for x in state["matching"].candidates],
            buyerLocation=dict(latitude=criteria.targetLatitude, longitude=criteria.targetLongitude), deadline=criteria.deadline))
        # Validation must see incomplete/failed logistics rather than accepting a model preference.
        return dict(logistics=result), result.model_dump(mode="json"), ()

    async def _validation(self, state):
        candidate = state["matching"].candidates[0]  # Matching order, stable input order breaks ties.
        row = next(x for x in state["request"].listings if str(x.listingId) == candidate.listingId)
        route = next((x for x in state["logistics"].candidates if x.listingId == row.listingId), None)
        criteria = state["planner"].normalizedCriteria
        value = ValidationInput(matchId=row.matchId, listingId=row.listingId, buyerId=criteria.buyerUserId,
            sellerId=row.sellerId, categoryMatches=row.categoryId == criteria.categoryId,
            unitMatches=row.unit.casefold() == criteria.unit.casefold(), listingStatus=row.status,
            availableUntil=row.availableUntil, deadline=criteria.deadline, quantity=criteria.requiredQuantity,
            availableQuantity=row.availableQuantity, unitPrice=row.unitPrice, maximumBudget=criteria.maximumBudget,
            distanceKm=route.distanceKm if route else None, durationMinutes=route.durationMinutes if route else None,
            transportCost=route.estimatedTransportCost if route else None, deliveryFeasible=route.deliveryFeasible if route else None)
        validation, calls = await ValidationAgent(self.validation_tools, timeout_seconds=self.limits.toolTimeoutSeconds,
                                                  max_retries=self.limits.maxRetries).validate(value)
        result = dict(validation=validation, status="PENDING_APPROVAL" if validation.valid else "REVISION_REQUESTED")
        if any(call.errorCode for call in calls):
            result.update(status="FAILED", errorCode="VALIDATION_TOOLS_FAILED")
        if validation.valid:
            result["recommendation"] = Recommendation(matchId=row.matchId, listingId=row.listingId,
                score=Decimal(str(candidate.basicFitScore)) / 100, distanceKm=value.distanceKm, transportCost=value.transportCost)
        return result, validation.model_dump(mode="json"), calls
