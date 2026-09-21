from __future__ import annotations

import asyncio
from datetime import datetime, timezone
from decimal import Decimal
from time import perf_counter
from typing import Awaitable, Callable, Literal, Protocol
from uuid import UUID

from pydantic import AwareDatetime, Field, StrictBool, model_validator

from app.agents.logistics_schemas import Contract, Measurement


ALLOWED_TOOLS = (
    "check_listing_active", "check_listing_not_expired", "check_available_quantity",
    "check_budget", "check_match_data_complete", "check_transaction_threshold",
)
ToolName = Literal[
    "check_listing_active", "check_listing_not_expired", "check_available_quantity",
    "check_budget", "check_match_data_complete", "check_transaction_threshold",
]


class ValidationInput(Contract):
    matchId: UUID
    listingId: UUID
    buyerId: UUID
    sellerId: UUID
    categoryMatches: StrictBool
    unitMatches: StrictBool
    listingStatus: str = Field(max_length=32)
    availableUntil: AwareDatetime
    deadline: AwareDatetime
    quantity: Measurement = Field(gt=0)
    availableQuantity: Measurement
    unitPrice: Measurement = Field(gt=0)
    maximumBudget: Measurement = Field(gt=0)
    distanceKm: Measurement | None = None
    durationMinutes: Measurement | None = None
    transportCost: Measurement | None = None
    deliveryFeasible: StrictBool | None = None


class CheckResult(Contract):
    passed: StrictBool
    code: str = Field(pattern=r"^[A-Z_]{1,80}$")
    warning: str | None = Field(default=None, pattern=r"^[A-Z_]{1,80}$")


class ValidationResult(Contract):
    valid: StrictBool
    requiresApproval: StrictBool
    recommendedMatchId: UUID | None
    violations: tuple[str, ...] = ()
    warnings: tuple[str, ...] = ()

    @model_validator(mode="after")
    def consistent(self):
        if self.valid != self.requiresApproval or self.valid != (self.recommendedMatchId is not None):
            raise ValueError("Only valid results can request approval or recommend a match.")
        if self.valid == bool(self.violations):
            raise ValueError("Valid results have no violations; invalid results require a reason.")
        return self


class ToolTrace(Contract):
    toolName: ToolName
    status: Literal["COMPLETED", "FAILED"]
    output: CheckResult | None = None
    errorCode: str | None = None
    retryCount: int = Field(ge=0, le=3)
    startedAtUtc: AwareDatetime
    completedAtUtc: AwareDatetime
    durationMilliseconds: int = Field(ge=0)


class ValidationTools(Protocol):
    async def check_listing_active(self, value: ValidationInput) -> CheckResult: ...
    async def check_listing_not_expired(self, value: ValidationInput) -> CheckResult: ...
    async def check_available_quantity(self, value: ValidationInput) -> CheckResult: ...
    async def check_budget(self, value: ValidationInput) -> CheckResult: ...
    async def check_match_data_complete(self, value: ValidationInput) -> CheckResult: ...
    async def check_transaction_threshold(self, value: ValidationInput) -> CheckResult: ...


class DeterministicValidationTools:
    """The entire validation tool surface: pure checks over an API-owned snapshot.

    Threshold is an escalation warning, never permission to auto-approve. All valid
    workflows require manager approval, including those below the threshold.
    """

    def __init__(self, threshold: Decimal = Decimal("100000"), *, clock=None):
        if not threshold.is_finite() or threshold <= 0:
            raise ValueError("A positive finite transaction threshold is required.")
        self.threshold = threshold
        self.clock = clock or (lambda: datetime.now(timezone.utc))

    async def check_listing_active(self, value):
        return self._result(value.listingStatus == "ACTIVE", "LISTING_NOT_ACTIVE")

    async def check_listing_not_expired(self, value):
        return self._result(value.availableUntil > self.clock() and value.availableUntil >= value.deadline,
                            "LISTING_EXPIRED_OR_EXPIRES_BEFORE_DELIVERY")

    async def check_available_quantity(self, value):
        return self._result(value.availableQuantity >= value.quantity, "INSUFFICIENT_QUANTITY")

    async def check_budget(self, value):
        return self._result(value.transportCost is not None and
                            value.quantity * value.unitPrice + value.transportCost <= value.maximumBudget,
                            "TOTAL_COST_EXCEEDS_BUDGET_OR_UNKNOWN")

    async def check_match_data_complete(self, value):
        complete = (value.buyerId != value.sellerId and value.categoryMatches and value.unitMatches
                    and value.distanceKm is not None and value.durationMinutes is not None
                    and value.transportCost is not None and value.deliveryFeasible is True
                    and value.deadline > self.clock())
        return self._result(complete, "MATCH_DATA_INCOMPLETE_OR_INCONSISTENT")

    @staticmethod
    def _result(passed, failure_code):
        return CheckResult(passed=passed, code="CHECK_PASSED" if passed else failure_code)

    async def check_transaction_threshold(self, value):
        if value.transportCost is None:
            return CheckResult(passed=False, code="TRANSACTION_VALUE_UNKNOWN")
        exceeds = value.quantity * value.unitPrice + value.transportCost >= self.threshold
        return CheckResult(passed=True, code="THRESHOLD_CHECKED",
                           warning="TRANSACTION_THRESHOLD_REQUIRES_REVIEW" if exceeds else None)


class ValidationAgent:
    """No LLM-selected actions, mutations, database client or approval capability."""

    def __init__(self, tools: ValidationTools, *, timeout_seconds: float = 1, max_retries: int = 1):
        if not 0 < timeout_seconds <= 30 or type(max_retries) is not int or not 0 <= max_retries <= 3:
            raise ValueError("Invalid validation execution limits.")
        self.tools, self.timeout, self.retries = tools, timeout_seconds, max_retries

    async def validate(self, value: ValidationInput) -> tuple[ValidationResult, tuple[ToolTrace, ...]]:
        value = ValidationInput.model_validate(value.model_dump())
        violations, warnings, traces = [], [], []
        # Fixed allowlist; input, notes and future LLM preferences cannot change it.
        for name in ALLOWED_TOOLS:
            trace = await self._call(name, getattr(self.tools, name), value)
            traces.append(trace)
            if trace.errorCode:
                violations.append(trace.errorCode + ":" + name)
            elif trace.output is not None:
                if not trace.output.passed:
                    violations.append(trace.output.code)
                if trace.output.warning:
                    warnings.append(trace.output.warning)
        valid = not violations
        return ValidationResult(valid=valid, requiresApproval=valid,
            recommendedMatchId=value.matchId if valid else None,
            violations=tuple(violations), warnings=tuple(warnings)), tuple(traces)

    async def _call(self, name: ToolName, call: Callable[[ValidationInput], Awaitable[CheckResult]], value):
        started, tick = datetime.now(timezone.utc), perf_counter()
        output, code, attempt = None, None, 0
        for attempt in range(self.retries + 1):
            try:
                raw = await asyncio.wait_for(call(value), timeout=self.timeout)
                output = CheckResult.model_validate(raw.model_dump() if isinstance(raw, CheckResult) else raw)
                code = None
                break
            except TimeoutError:
                code = "TOOL_TIMEOUT"
            except (ConnectionError, OSError):
                code = "TOOL_UNAVAILABLE"
            except Exception:
                code = "INVALID_TOOL_RESPONSE"
                break  # Schema/programming failures are not transient.
        return ToolTrace(toolName=name, status="FAILED" if code else "COMPLETED", output=output,
            errorCode=code, retryCount=attempt, startedAtUtc=started, completedAtUtc=datetime.now(timezone.utc),
            durationMilliseconds=round((perf_counter() - tick) * 1000))
