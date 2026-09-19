from __future__ import annotations

from collections.abc import Callable, Mapping
from datetime import datetime, timezone
from decimal import Decimal
from typing import Any, Protocol, TypedDict

from langgraph.graph import END, START, StateGraph
from pydantic import BaseModel, ValidationError

from app.agents.logistics_schemas import (
    CandidateLogistics, ListingLocationInput, ListingLocationOutput, LogisticsRequest,
    LogisticsResponse, LogisticsWarning, RouteInput, RouteOutput, TransportInput, TransportOutput,
)


class LogisticsTools(Protocol):
    """Only the approved ASP.NET Core tool boundary; no database or maps SDK access."""

    def get_listing_location(self, request: ListingLocationInput) -> Mapping[str, Any]: ...
    def get_route_estimate(self, request: RouteInput) -> Mapping[str, Any]: ...
    def calculate_transport_estimate(self, request: TransportInput) -> Mapping[str, Any]: ...


class LogisticsState(TypedDict, total=False):
    raw_input: Any
    now: datetime
    request: LogisticsRequest
    response: LogisticsResponse


class LogisticsAgent:
    """Deterministic read-only logistics; input/tool text never selects actions."""

    def __init__(self, tools: LogisticsTools, *, max_retries: int = 1,
                 clock: Callable[[], datetime] | None = None) -> None:
        if type(max_retries) is not int or not 0 <= max_retries <= 3:
            raise ValueError("max_retries must be an integer between zero and three.")
        self._tools = tools
        self._max_attempts = max_retries + 1
        self._clock = clock or (lambda: datetime.now(timezone.utc))
        graph = StateGraph(LogisticsState)
        graph.add_node("validate", self._validate)
        graph.add_node("evaluate", self._evaluate)
        graph.add_edge(START, "validate")
        graph.add_conditional_edges("validate", lambda s: "finish" if "response" in s else "evaluate",
                                    {"finish": END, "evaluate": "evaluate"})
        graph.add_edge("evaluate", END)
        self._graph = graph.compile()

    def assess(self, raw_input: Any) -> LogisticsResponse:
        state = self._graph.invoke({"raw_input": raw_input, "now": self._now()})
        return LogisticsResponse.model_validate(state["response"].model_dump())

    def assess_json(self, raw_input: Any) -> str:
        return self.assess(raw_input).model_dump_json()

    def _now(self) -> datetime:
        value = self._clock()
        if value.tzinfo is None or value.utcoffset() is None:
            raise ValueError("Logistics clock must be timezone-aware.")
        return value

    @staticmethod
    def _validate(state: LogisticsState) -> LogisticsState:
        try:
            request = LogisticsRequest.model_validate(state["raw_input"], context={"now": state["now"]})
            return {"request": request}
        except ValidationError:
            # Never reflect raw input, provider errors, credentials or instruction text.
            return {"response": LogisticsResponse(status="invalid_input", failure=LogisticsWarning(code="INVALID_INPUT"))}

    def _call(self, name, request, response_type):
        request = type(request).model_validate(request.model_dump())
        for attempt in range(1, self._max_attempts + 1):
            try:
                raw = getattr(self._tools, name)(request)
                if isinstance(raw, BaseModel):
                    raw = raw.model_dump()
                result = response_type.model_validate(raw)
            except ValidationError:
                return None, LogisticsWarning(code="INVALID_TOOL_RESPONSE", tool=name, attempts=attempt), attempt
            except TimeoutError:
                code = "TOOL_TIMEOUT"
            except (ConnectionError, OSError):
                code = "TOOL_UNAVAILABLE"
            except Exception:
                return None, LogisticsWarning(code="TOOL_UNAVAILABLE", tool=name, attempts=attempt), attempt
            else:
                code = getattr(result, "errorCode", None)
                if code is None:
                    return result, None, attempt
                if code not in ("ROUTING_TIMEOUT", "ROUTING_UNAVAILABLE", "TRANSPORT_UNAVAILABLE"):
                    return None, LogisticsWarning(code=code, tool=name, attempts=attempt,
                        retryAfterSeconds=getattr(result, "retryAfterSeconds", None)), attempt
            if attempt == self._max_attempts:
                return None, LogisticsWarning(code=code, tool=name, attempts=attempt), attempt
        raise AssertionError("Unreachable retry state")

    def _evaluate(self, state: LogisticsState) -> LogisticsState:
        request = state["request"]
        candidates = tuple(self._candidate(request, listing_id) for listing_id in request.candidateListingIds)
        successes = sum(x.estimatedTransportCost is not None for x in candidates)
        status = "ok" if successes == len(candidates) else "partial" if successes else "failed"
        return {"response": LogisticsResponse(status=status, requirementId=request.requirementId, candidates=candidates)}

    def _candidate(self, request, listing_id):
        def failed(warning, route=None, feasible=None):
            return CandidateLogistics(listingId=listing_id, reason=warning.code, warnings=(warning,),
                distanceKm=route.distanceKm if route else None, durationMinutes=route.durationMinutes if route else None,
                deliveryFeasible=feasible)

        buyer = request.buyerLocation
        if buyer is None or not buyer.complete:
            return failed(LogisticsWarning(code="MISSING_BUYER_COORDINATES"))
        location, warning, attempts = self._call("get_listing_location", ListingLocationInput(
            requirementId=request.requirementId, listingId=listing_id), ListingLocationOutput)
        if warning:
            return failed(warning)
        if location.listingId != listing_id:
            return failed(LogisticsWarning(code="INVALID_TOOL_RESPONSE", tool="get_listing_location", attempts=attempts))
        if not location.complete:
            return failed(LogisticsWarning(code="MISSING_SELLER_COORDINATES", tool="get_listing_location", attempts=attempts))
        route, warning, _ = self._call("get_route_estimate", RouteInput(
            requirementId=request.requirementId, listingId=listing_id,
            sellerLatitude=location.latitude, sellerLongitude=location.longitude,
            buyerLatitude=buyer.latitude, buyerLongitude=buyer.longitude), RouteOutput)
        if warning:
            return failed(warning)
        transport, warning, _ = self._call("calculate_transport_estimate", TransportInput(
            requirementId=request.requirementId, listingId=listing_id,
            distanceKm=route.distanceKm, durationMinutes=route.durationMinutes), TransportOutput)
        # Compare durations rather than constructing a potentially overflowing timedelta.
        remaining = request.deadline - self._now()
        seconds = Decimal(remaining.days * 86400 + remaining.seconds) + Decimal(remaining.microseconds) / 1_000_000
        feasible = route.durationMinutes * 60 <= seconds
        if warning:
            return failed(warning, route, feasible)
        return CandidateLogistics(listingId=listing_id, distanceKm=route.distanceKm,
            durationMinutes=route.durationMinutes, estimatedTransportCost=transport.estimatedTransportCost,
            deliveryFeasible=feasible, reason="DELIVERY_WITHIN_DEADLINE" if feasible else "DEADLINE_EXCEEDED")
