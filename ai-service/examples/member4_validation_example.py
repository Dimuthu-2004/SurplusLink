import asyncio, json
from app.agents.validation import DeterministicValidationTools, ValidationAgent, ValidationInput
from app.workflows.demo import demo_request

request = demo_request(); row = request.listings[0]
value = ValidationInput(matchId=row.matchId, listingId=row.listingId, buyerId=request.buyerRequest["buyerId"], sellerId=row.sellerId, categoryMatches=True, unitMatches=True, listingStatus="ACTIVE", availableUntil=row.availableUntil, deadline=request.buyerRequest["deadline"], quantity=10, availableQuantity=20, unitPrice=100, maximumBudget=2000, distanceKm=10, durationMinutes=30, transportCost=500, deliveryFeasible=True)
result, traces = asyncio.run(ValidationAgent(DeterministicValidationTools()).validate(value))
print("MEMBER 4 | Offers, Reservations, Transactions & Approval | ValidationAgent")
print("INPUT:\n" + json.dumps(value.model_dump(mode="json"), indent=2))
print("ALLOWED TOOLS: " + ", ".join(trace.toolName for trace in traces))
print("OUTPUT:")
print(json.dumps(result.model_dump(mode="json"), indent=2))
print("Result: validation can recommend PENDING_APPROVAL but cannot approve or reserve stock.")
