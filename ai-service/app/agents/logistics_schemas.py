from __future__ import annotations

from datetime import datetime
from decimal import Decimal
from typing import Annotated, Literal
from uuid import UUID

from pydantic import BaseModel, BeforeValidator, ConfigDict, Field, ValidationInfo, model_validator


def numeric(value):
    if isinstance(value, bool):
        raise ValueError("Boolean is not a measurement.")
    return value


Measurement = Annotated[Decimal, BeforeValidator(numeric), Field(ge=0, le=Decimal("1e18"), allow_inf_nan=False)]
Latitude = Annotated[Decimal, BeforeValidator(numeric), Field(ge=-90, le=90, allow_inf_nan=False)]
Longitude = Annotated[Decimal, BeforeValidator(numeric), Field(ge=-180, le=180, allow_inf_nan=False)]
Identifier = Annotated[UUID, Field(description="Nonempty backend identifier")]


class Contract(BaseModel):
    model_config = ConfigDict(extra="forbid", frozen=True)

    @model_validator(mode="after")
    def nonempty_identifiers(self):
        for value in self.__dict__.values():
            if isinstance(value, UUID) and value.int == 0:
                raise ValueError("Empty identifier.")
        return self


class Location(Contract):
    latitude: Latitude | None = None
    longitude: Longitude | None = None

    @property
    def complete(self) -> bool:
        return self.latitude is not None and self.longitude is not None


class LogisticsRequest(Contract):
    requirementId: Identifier
    candidateListingIds: tuple[Identifier, ...] = Field(min_length=1, max_length=100)
    buyerLocation: Location | None = None
    deadline: datetime

    @model_validator(mode="after")
    def validate_request(self, info: ValidationInfo):
        if len(set(self.candidateListingIds)) != len(self.candidateListingIds) or any(x.int == 0 for x in self.candidateListingIds):
            raise ValueError("Candidate identifiers must be unique and nonempty.")
        if self.deadline.tzinfo is None or self.deadline.utcoffset() is None:
            raise ValueError("Deadline must include a timezone.")
        if info.context and self.deadline <= info.context["now"]:
            raise ValueError("Deadline must be in the future.")
        return self


class ListingLocationInput(Contract):
    requirementId: Identifier
    listingId: Identifier


class ListingLocationOutput(Location):
    listingId: Identifier


class RouteInput(ListingLocationInput):
    sellerLatitude: Latitude
    sellerLongitude: Longitude
    buyerLatitude: Latitude
    buyerLongitude: Longitude


ErrorCode = Literal["ROUTING_TIMEOUT", "ROUTING_RATE_LIMITED", "ROUTING_UNAVAILABLE",
    "ROUTING_INVALID_RESPONSE", "ROUTING_NOT_CONFIGURED", "INVALID_COORDINATES",
    "TRANSPORT_PRICING_NOT_CONFIGURED", "TRANSPORT_ESTIMATE_OUT_OF_RANGE", "TRANSPORT_UNAVAILABLE"]


class RouteOutput(Contract):
    distanceKm: Measurement | None = None
    durationMinutes: Measurement | None = None
    errorCode: ErrorCode | None = None
    retryAfterSeconds: int | None = Field(default=None, ge=0, le=86400, strict=True)
    success: bool = Field(strict=True)

    @model_validator(mode="after")
    def consistent(self):
        if self.success:
            if self.errorCode is not None or self.distanceKm is None or self.durationMinutes is None or self.retryAfterSeconds is not None:
                raise ValueError("Invalid route success.")
        elif self.errorCode is None or self.distanceKm is not None or self.durationMinutes is not None:
            raise ValueError("Invalid route failure.")
        if self.retryAfterSeconds is not None and self.errorCode != "ROUTING_RATE_LIMITED":
            raise ValueError("Retry delay only applies to rate limiting.")
        return self


class TransportInput(ListingLocationInput):
    distanceKm: Measurement
    durationMinutes: Measurement


class TransportOutput(Contract):
    estimatedTransportCost: Measurement | None = None
    errorCode: ErrorCode | None = None

    @model_validator(mode="after")
    def consistent(self):
        if (self.errorCode is None) != (self.estimatedTransportCost is not None):
            raise ValueError("Invalid transport result.")
        return self


ToolName = Literal["get_listing_location", "get_route_estimate", "calculate_transport_estimate"]
FailureCode = Literal["INVALID_INPUT", "MISSING_BUYER_COORDINATES", "MISSING_SELLER_COORDINATES",
    "INVALID_TOOL_RESPONSE", "TOOL_UNAVAILABLE", "TOOL_TIMEOUT", "ROUTING_TIMEOUT", "ROUTING_RATE_LIMITED",
    "ROUTING_UNAVAILABLE", "ROUTING_INVALID_RESPONSE", "ROUTING_NOT_CONFIGURED", "INVALID_COORDINATES",
    "TRANSPORT_PRICING_NOT_CONFIGURED", "TRANSPORT_ESTIMATE_OUT_OF_RANGE", "TRANSPORT_UNAVAILABLE"]


class LogisticsWarning(Contract):
    code: FailureCode
    tool: ToolName | None = None
    attempts: int = Field(default=0, ge=0, le=4, strict=True)
    retryAfterSeconds: int | None = Field(default=None, ge=0, le=86400, strict=True)


class CandidateLogistics(Contract):
    listingId: Identifier
    distanceKm: Measurement | None = None
    durationMinutes: Measurement | None = None
    estimatedTransportCost: Measurement | None = None
    deliveryFeasible: bool | None = Field(default=None, strict=True)
    reason: Literal["DELIVERY_WITHIN_DEADLINE", "DEADLINE_EXCEEDED"] | FailureCode
    warnings: tuple[LogisticsWarning, ...] = ()

    @model_validator(mode="after")
    def consistent(self):
        if (self.distanceKm is None) != (self.durationMinutes is None):
            raise ValueError("Partial route metrics.")
        if self.distanceKm is None and (self.estimatedTransportCost is not None or self.deliveryFeasible is not None):
            raise ValueError("Cannot infer logistics without route metrics.")
        if self.reason in ("DELIVERY_WITHIN_DEADLINE", "DEADLINE_EXCEEDED"):
            if self.estimatedTransportCost is None or self.deliveryFeasible != (self.reason == "DELIVERY_WITHIN_DEADLINE"):
                raise ValueError("Incomplete successful logistics result.")
        elif not self.warnings or self.estimatedTransportCost is not None or self.warnings[0].code != self.reason:
            raise ValueError("A failure must have a matching warning and no invented cost.")
        return self


class LogisticsResponse(Contract):
    status: Literal["ok", "partial", "failed", "invalid_input"]
    requirementId: Identifier | None = None
    candidates: tuple[CandidateLogistics, ...] = ()
    failure: LogisticsWarning | None = None

    @model_validator(mode="after")
    def consistent(self):
        if self.status == "invalid_input":
            if self.candidates or self.failure is None or self.failure.code != "INVALID_INPUT":
                raise ValueError("Invalid input envelope.")
        else:
            if not self.candidates or self.requirementId is None or self.failure is not None:
                raise ValueError("Incomplete response.")
            successes = sum(x.estimatedTransportCost is not None for x in self.candidates)
            expected = "ok" if successes == len(self.candidates) else "partial" if successes else "failed"
            if self.status != expected or len({x.listingId for x in self.candidates}) != len(self.candidates):
                raise ValueError("Inconsistent response.")
        return self
