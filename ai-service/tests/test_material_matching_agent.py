from __future__ import annotations

from datetime import datetime, timezone
from decimal import Decimal
import unittest
from uuid import UUID
from dataclasses import replace
import re

from app.agents.material_matching import MaterialMatchingAgent
from app.materials.read_boundary import ActiveMaterialCriteria, MaterialListingRecord


NOW = datetime(2026, 9, 15, tzinfo=timezone.utc)
DEADLINE = datetime(2099, 10, 1, tzinfo=timezone.utc)


class MatchingLlm:
    def is_available(self): return True
    def generate_chat_response(self, *args, **kwargs): return None
    def generate_structured(self, messages, response_model, **_kwargs):
        ids = re.findall(r"'listingId': '([^']+)'", messages[0]["content"])
        return response_model.model_validate({"explanations": [
            {"listingId": listing_id, "semanticFit": "HIGH", "reason": "Terminology is semantically suitable.",
             "matchedAttributes": ["material description"], "concerns": []}
            for listing_id in ids
        ]})


def matching_agent(boundary, *, clock=None):
    return MaterialMatchingAgent(boundary, llm=MatchingLlm(), clock=clock)


class FakeActiveMaterialsBoundary:
    def __init__(self, listings: list[MaterialListingRecord]) -> None:
        self.listings = {listing.listing_id: listing for listing in listings}
        self.search_criteria: ActiveMaterialCriteria | None = None
        self.detail_calls: list[str] = []

    def search_active_materials(
        self, criteria: ActiveMaterialCriteria
    ) -> list[MaterialListingRecord]:
        self.search_criteria = criteria
        return list(self.listings.values())

    def get_material_detail(self, listing_id: str) -> MaterialListingRecord | None:
        self.detail_calls.append(listing_id)
        return self.listings.get(listing_id)


class MaterialMatchingAgentTests(unittest.TestCase):
    def test_returns_ranked_normal_candidates(self) -> None:
        boundary = FakeActiveMaterialsBoundary(
            [
                listing("standard", quantity="10", unit_price="10"),
                listing("better-value", quantity="10", unit_price="8"),
            ]
        )
        response = matching_agent(boundary, clock=lambda: NOW).match(request())

        self.assertEqual(response.status, "ok")
        self.assertIsNone(response.failure)
        self.assertEqual(
            [candidate.listingId for candidate in response.candidates],
            ["better-value", "standard"],
        )
        self.assertEqual(response.candidates[0].availableQuantity, Decimal("10"))
        self.assertEqual(response.candidates[0].unitPrice, Decimal("8"))
        self.assertGreater(response.candidates[0].basicFitScore, 0)
        self.assertIn("Active verified", response.candidates[0].reason)
        self.assertEqual(boundary.search_criteria.category_id, "steel-category")

    def test_excludes_expired_and_inactive_listings(self) -> None:
        boundary = FakeActiveMaterialsBoundary(
            [
                listing("active", quantity="10", unit_price="8"),
                listing("inactive", quantity="10", unit_price="8", status="DRAFT"),
                listing("unverified", quantity="10", unit_price="8", verified=False),
                listing(
                    "expired",
                    quantity="10",
                    unit_price="8",
                    available_until=datetime(2026, 9, 14, tzinfo=timezone.utc),
                ),
            ]
        )
        response = matching_agent(boundary, clock=lambda: NOW).match(request())

        self.assertEqual(response.status, "ok")
        self.assertEqual([candidate.listingId for candidate in response.candidates], ["active"])
        self.assertEqual(boundary.detail_calls, ["active"])

    def test_returns_safe_no_candidate_result(self) -> None:
        boundary = FakeActiveMaterialsBoundary(
            [listing("unavailable", quantity="0", unit_price="8")]
        )
        response = matching_agent(boundary, clock=lambda: NOW).match(request())

        self.assertEqual(response.status, "no_candidate")
        self.assertEqual(response.candidates, [])
        self.assertIsNotNone(response.failure)
        self.assertEqual(response.failure.code, "NO_CANDIDATE")
        self.assertIn("No active, verified, non-expired", response.failure.message)

    def test_partial_stock_is_a_ranked_candidate(self) -> None:
        boundary = FakeActiveMaterialsBoundary(
            [listing("partial", quantity="2", unit_price="8")]
        )
        response = matching_agent(boundary, clock=lambda: NOW).match(request())
        self.assertEqual(response.status, "ok")
        self.assertEqual([candidate.listingId for candidate in response.candidates], ["partial"])
        self.assertEqual(response.candidates[0].availableQuantity, Decimal("2"))

    def test_package_type_is_not_compared_with_base_measurement(self) -> None:
        canned_paint = replace(
            listing("paint-cans", quantity="5", unit_price="100"),
            unit="CAN", base_unit="L", quantity_mode="PACKAGE", package_type="CAN",
            package_size=Decimal("1"), package_count_available=5,
            base_equivalent_available_quantity=Decimal("5"),
        )
        raw = request()
        raw.update({"unit": "L", "baseUnit": "L", "requiredQuantity": "2", "maximumBudget": "300"})
        response = matching_agent(FakeActiveMaterialsBoundary([canned_paint]), clock=lambda: NOW).match(raw)
        self.assertEqual(response.status, "ok")
        self.assertEqual(response.candidates[0].packageType, "CAN")
        self.assertEqual(response.candidates[0].baseUnit, "L")

    def test_self_match_excluded_while_other_seller_matches(self):
        own = replace(listing("own", quantity="10", unit_price="8"),
                      seller_id=UUID(str(request()["buyerUserId"])))
        other = listing("other", quantity="10", unit_price="8")
        agent = matching_agent(FakeActiveMaterialsBoundary([own, other]), clock=lambda: NOW)
        result = agent.match(request())
        self.assertEqual([c.listingId for c in result.candidates], ["other"])
        self.assertEqual(result.exclusions[0].code, "SELF_MATCH_NOT_ALLOWED")
        self.assertNotIn("seller_id", result.model_dump_json())
        only_self = matching_agent(FakeActiveMaterialsBoundary([own]), clock=lambda: NOW).match(request())
        self.assertEqual(only_self.status, "no_candidate")
        self.assertEqual(only_self.exclusions[0].code, "SELF_MATCH_NOT_ALLOWED")

    def test_identity_required_and_detail_rechecked(self):
        raw = request()
        del raw["buyerUserId"]
        boundary = FakeActiveMaterialsBoundary([listing("changed", quantity="10", unit_price="8")])
        self.assertEqual(matching_agent(boundary).match(raw).status, "invalid_input")
        detail = replace(boundary.listings["changed"], seller_id=UUID(str(request()["buyerUserId"])))
        boundary.get_material_detail = lambda _: detail
        result = matching_agent(boundary, clock=lambda: NOW).match(request())
        self.assertEqual(result.candidates, [])
        self.assertEqual(result.exclusions[0].code, "SELF_MATCH_NOT_ALLOWED")


def request() -> dict[str, object]:
    return {
        "buyerUserId": "00000000-0000-0000-0000-000000000001",
        "categoryId": "steel-category",
        "constructionItemTemplateId": "00000000-0000-0000-0000-000000000204",
        "requiredQuantity": "5",
        "unit": "kg",
        "maximumBudget": "100",
        "deadline": DEADLINE.isoformat(),
    }


def listing(
    listing_id: str,
    *,
    quantity: str,
    unit_price: str,
    status: str = "ACTIVE",
    verified: bool = True,
    available_until: datetime = datetime(2099, 12, 1, tzinfo=timezone.utc),
) -> MaterialListingRecord:
    return MaterialListingRecord(
        seller_id=UUID("00000000-0000-0000-0000-000000000002"),
        listing_id=listing_id,
        template_id="00000000-0000-0000-0000-000000000204",
        category_id="steel-category",
        category="Steel",
        available_quantity=Decimal(quantity),
        unit="kg",
        unit_price=Decimal(unit_price),
        condition="GOOD",
        status=status,
        is_verified=verified,
        available_until=available_until,
    )


if __name__ == "__main__":
    unittest.main()
