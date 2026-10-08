"""Member 4 focused ValidationAgent and orchestration approval-barrier tests."""
import asyncio
from decimal import Decimal
from app.agents.validation import ALLOWED_TOOLS, DeterministicValidationTools, ValidationAgent, ValidationInput
from app.workflows.demo import demo_request
from app.workflows.orchestration import WorkflowOrchestrator


def _value():
    request = demo_request(); row = request.listings[0]
    return ValidationInput(matchId=row.matchId, listingId=row.listingId, buyerId=request.buyerRequest["buyerId"], sellerId=row.sellerId,
        categoryMatches=True, unitMatches=True, listingStatus="ACTIVE", availableUntil=row.availableUntil,
        deadline=request.buyerRequest["deadline"], quantity=10, availableQuantity=20, unitPrice=100,
        maximumBudget=2000, distanceKm=10, durationMinutes=30, transportCost=500, deliveryFeasible=True)


def test_validation_is_read_only_allowlisted_and_requires_manager_approval():
    value = _value(); before = value.model_dump_json()
    result, traces = asyncio.run(ValidationAgent(DeterministicValidationTools()).validate(value))
    assert value.model_dump_json() == before and result.valid and result.requiresApproval
    assert tuple(trace.toolName for trace in traces) == ALLOWED_TOOLS


def test_invalid_stock_fails_closed_and_workflow_never_reserves():
    invalid, _ = asyncio.run(ValidationAgent(DeterministicValidationTools()).validate(_value().model_copy(update={"availableQuantity": Decimal("0")})))
    workflow = asyncio.run(WorkflowOrchestrator().run(demo_request()))
    assert not invalid.valid and "INSUFFICIENT_QUANTITY" in invalid.violations
    assert workflow.status == "MATCH_FOUND" and workflow.validation.requiresApproval
    assert "reservation" not in workflow.model_dump_json().lower()


def test_budget_incomplete_data_and_tool_failure_fail_closed():
    budget, _ = asyncio.run(ValidationAgent(DeterministicValidationTools()).validate(_value().model_copy(update={"maximumBudget": Decimal("100")})))
    incomplete, _ = asyncio.run(ValidationAgent(DeterministicValidationTools()).validate(_value().model_copy(update={"transportCost": None})))
    class Unavailable(DeterministicValidationTools):
        async def check_budget(self, value): raise ConnectionError("provider unavailable")
    failed, traces = asyncio.run(ValidationAgent(Unavailable(), max_retries=0).validate(_value()))
    assert "TOTAL_COST_EXCEEDS_BUDGET" in budget.violations
    assert "TOTAL_COST_UNKNOWN" in incomplete.violations
    assert not failed.valid and traces[3].errorCode == "TOOL_UNAVAILABLE"
