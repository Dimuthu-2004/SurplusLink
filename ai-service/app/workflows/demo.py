"""Offline demonstration using explicitly labelled fixtures, never production fallback."""
import asyncio
from datetime import datetime, timedelta, timezone

from app.workflows.orchestration import WorkflowOrchestrator, WorkflowRequest


def demo_request():
    deadline = (datetime.now(timezone.utc) + timedelta(days=7)).isoformat()
    expiry = (datetime.now(timezone.utc) + timedelta(days=30)).isoformat()
    return WorkflowRequest.model_validate({
        "workflowId": "00000000-0000-0000-0000-000000000001",
        "buyerRequest": {
            "id": "00000000-0000-0000-0000-000000000002",
            "buyerId": "00000000-0000-0000-0000-000000000003",
            "categoryId": "00000000-0000-0000-0000-000000000004",
            "requiredQuantity": "10", "unit": "kg", "maximumBudget": "2000", "deadline": deadline,
            "latitude": "6.9271", "longitude": "79.8612", "status": "MATCHING", "notes": "Demo fixture",
        },
        "listings": [{
            "matchId": "00000000-0000-0000-0000-000000000005",
            "listingId": "00000000-0000-0000-0000-000000000006",
            "sellerId": "00000000-0000-0000-0000-000000000007",
            "categoryId": "00000000-0000-0000-0000-000000000004",
            "availableQuantity": "20", "unit": "kg", "unitPrice": "100", "condition": "GOOD",
            "status": "ACTIVE", "availableUntil": expiry, "latitude": "6.9", "longitude": "79.8",
            "distanceKm": "10", "durationMinutes": "30", "transportCost": "500",
        }],
    })


async def main():
    print((await WorkflowOrchestrator().run(demo_request())).model_dump_json(indent=2))


if __name__ == "__main__":
    asyncio.run(main())
