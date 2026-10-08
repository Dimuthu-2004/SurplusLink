"""Member 3 focused LogisticsAgent tests with controlled read-only route tools."""
from app.agents.logistics import LogisticsAgent
from app.workflows.demo import demo_request
from app.workflows.orchestration import SnapshotTools


def _input(request):
    row = request.listings[0]
    return {"requirementId": request.buyerRequest["id"], "candidateListingIds": [row.listingId],
            "buyerLocation": {"latitude": request.buyerRequest["latitude"], "longitude": request.buyerRequest["longitude"]},
            "deadline": request.buyerRequest["deadline"]}


def test_route_distance_duration_cost_and_allowlisted_tool_order():
    request = demo_request(); tools = SnapshotTools(request.listings)
    result = LogisticsAgent(tools, max_retries=0).assess(_input(request))
    item = result.candidates[0]
    assert result.status == "ok" and item.distanceKm == 10 and item.durationMinutes == 30
    assert item.estimatedTransportCost == 500 and item.deliveryFeasible is True
    assert [trace.toolName for trace in tools.traces] == ["get_listing_location", "get_route_estimate", "calculate_transport_estimate"]


def test_missing_route_returns_structured_safe_failure():
    request = demo_request(); broken = request.listings[0].model_copy(update={"distanceKm": None})
    result = LogisticsAgent(SnapshotTools((broken,)), max_retries=0).assess(_input(request))
    assert result.status == "failed"
    assert result.candidates[0].reason == "ROUTING_UNAVAILABLE"
    assert result.candidates[0].estimatedTransportCost is None


def test_missing_coordinates_and_retry_are_bounded():
    request = demo_request(); row = request.listings[0].model_copy(update={"distanceKm": None})
    tools = SnapshotTools((row,))
    absent = _input(request); absent["buyerLocation"] = {"latitude": None, "longitude": None}
    assert LogisticsAgent(tools).assess(absent).candidates[0].reason == "MISSING_BUYER_COORDINATES"
    result = LogisticsAgent(tools, max_retries=1).assess(_input(request))
    assert result.candidates[0].warnings[0].attempts == 2
