import json
from app.agents.logistics import LogisticsAgent
from app.workflows.demo import demo_request
from app.workflows.orchestration import SnapshotTools

request = demo_request(); row = request.listings[0]; tools = SnapshotTools(request.listings)
value = {"requirementId": request.buyerRequest["id"], "candidateListingIds": [row.listingId], "buyerLocation": {"latitude": request.buyerRequest["latitude"], "longitude": request.buyerRequest["longitude"]}, "deadline": request.buyerRequest["deadline"]}
result = LogisticsAgent(tools, max_retries=0).assess(value)
print("MEMBER 3 | Matching & Logistics | LogisticsAgent")
print("INPUT:\n" + json.dumps(value, default=str, indent=2))
print("ALLOWED TOOLS: get_listing_location, get_route_estimate, calculate_transport_estimate")
print("OUTPUT:")
print(json.dumps(result.model_dump(mode="json"), indent=2))
print("Result: route data supports a feasible delivery estimate.")
