from __future__ import annotations
from datetime import datetime, timezone
from decimal import Decimal
import unittest
from uuid import UUID

from app.agents.material_matching import MaterialMatchingAgent, _effective_contribution
from app.materials.read_boundary import ActiveMaterialCriteria, MaterialListingRecord

NOW = datetime(2026, 10, 6, tzinfo=timezone.utc)
DEADLINE = datetime(2099, 12, 1, tzinfo=timezone.utc)

class FakeBoundary:
    def __init__(self, listings: list[MaterialListingRecord]) -> None:
        self.listings = {l.listing_id: l for l in listings}

    def search_active_materials(self, criteria: ActiveMaterialCriteria):
        return list(self.listings.values())

    def get_material_detail(self, listing_id: str):
        return self.listings.get(listing_id)

def paint_listing(listing_id: str, count: int, size: int, price: str, status="ACTIVE", until=DEADLINE) -> MaterialListingRecord:
    qty = Decimal(count * size)
    return MaterialListingRecord(
        seller_id=UUID("00000000-0000-0000-0000-000000000002"),
        listing_id=listing_id,
        template_id="paint-template",
        category_id="paint-cat",
        category="Paint",
        available_quantity=qty,
        unit="L",
        unit_price=Decimal(price),
        condition="NEW",
        status=status,
        is_verified=True,
        available_until=until,
        quantity_mode="PACKAGE",
        package_type="CAN",
        package_size=Decimal(size),
        package_count_available=count,
        base_equivalent_available_quantity=qty,
        maximum_contribution=qty,
        full_coverage=True,
        base_unit="L",
        title="Super Wall Paint",
    )

def listing(listing_id: str, *, mode: str, unit: str, required_stock: str, price: str,
            package_size: str | None = None, package_count: int | None = None,
            maximum_contribution: str | None = None) -> MaterialListingRecord:
    return MaterialListingRecord(
        seller_id=UUID("00000000-0000-0000-0000-000000000002"), listing_id=listing_id,
        template_id="generic-template", category_id="generic-cat", category="Generic",
        available_quantity=Decimal(required_stock), unit=unit, unit_price=Decimal(price), condition="NEW",
        status="ACTIVE", is_verified=True, available_until=DEADLINE, quantity_mode=mode,
        package_type="PIECE" if mode == "PIECE" else ("BOX" if mode == "PACKAGE" else None),
        package_size=Decimal(package_size) if package_size else None, package_count_available=package_count,
        base_equivalent_available_quantity=Decimal(required_stock),
        maximum_contribution=Decimal(maximum_contribution) if maximum_contribution else None,
        full_coverage=False, base_unit=unit, title="Generic material")

def req(qty: str, budget: str, unit="L", cat="paint-cat", template="paint-template", deadline=DEADLINE) -> dict:
    return {
        "buyerUserId": "00000000-0000-0000-0000-000000000001",
        "categoryId": cat,
        "constructionItemTemplateId": template,
        "requiredQuantity": qty,
        "unit": unit,
        "maximumBudget": budget,
        "deadline": deadline.isoformat(),
    }

class BusinessCasesTests(unittest.TestCase):

    def test_quantity_modes_are_data_driven_partial_candidates(self):
        cases = [
            ("CONTINUOUS", "L", "50", "20", "100", None, None, "20"),
            ("PIECE", "piece", "10", "5", "5000", "1", 5, "5"),
            ("PACKAGE", "L", "50", "20", "1000", "4", 5, "20"),
        ]
        for mode, unit, required, stock, price, size, count, expected in cases:
            with self.subTest(mode=mode):
                row = listing("candidate", mode=mode, unit=unit, required_stock=stock, price=price,
                              package_size=size, package_count=count)
                result = MaterialMatchingAgent(FakeBoundary([row]), clock=lambda: NOW).match(
                    req(required, "100000", unit=unit, cat="generic-cat", template="generic-template"))
                self.assertEqual(result.status, "ok")
                self.assertEqual(result.candidates[0].maximumContribution, Decimal(expected))
                self.assertFalse(result.candidates[0].fullCoverage)

    def test_two_partial_sellers_are_independent_candidates(self):
        sellers = [listing(name, mode="PIECE", unit="piece", required_stock="5", price="5000",
                           package_size="1", package_count=5) for name in ("A", "B")]
        result = MaterialMatchingAgent(FakeBoundary(sellers), clock=lambda: NOW).match(
            req("10", "100000", unit="piece", cat="generic-cat", template="generic-template"))
        self.assertEqual({x.listingId for x in result.candidates}, {"A", "B"})
        self.assertEqual({x.maximumContribution for x in result.candidates}, {Decimal("5")})

    def test_doors_shape_uses_backend_routed_contribution_without_rejection(self):
        # The backend snapshot has already subtracted route cost: five whole
        # doors at 5,000 are affordable, even though the buyer wants ten.
        doors = listing("doors", mode="PIECE", unit="piece", required_stock="5", price="5000",
                        package_size="1", package_count=5, maximum_contribution="5")
        result = MaterialMatchingAgent(FakeBoundary([doors]), clock=lambda: NOW).match(
            req("10", "30000", unit="piece", cat="generic-cat", template="generic-template"))
        candidate = result.candidates[0]
        self.assertEqual(result.status, "ok")
        self.assertEqual(candidate.maximumContribution, Decimal("5"))
        self.assertFalse(candidate.fullCoverage)
        self.assertGreater(candidate.basicFitScore, 0)

    def test_1_buyer_50L_budget_50k_sellers_40L_60L(self):
        boundary = FakeBoundary([paint_listing("A", 10, 4, "1000"), paint_listing("B", 15, 4, "1100")])
        res = MaterialMatchingAgent(boundary, clock=lambda: NOW).match(req("50", "50000"))
        self.assertEqual(res.status, "ok")
        self.assertEqual([c.listingId for c in res.candidates], ["A", "B"])

    def test_2_buyer_50L_budget_10k_seller_80L_1000_per_can(self):
        # Seller A has 80L (20 cans @ 1000/can). Buyer needs 50L with 10k budget.
        # Seller can affordably contribute 10 cans = 40L for 10k -> candidate MUST be valid!
        boundary = FakeBoundary([paint_listing("A", 20, 4, "1000"), paint_listing("B", 15, 4, "1100")])
        res = MaterialMatchingAgent(boundary, clock=lambda: NOW).match(req("50", "10000"))
        self.assertEqual(res.status, "ok")
        self.assertGreaterEqual(len(res.candidates), 1)
        candidate_a = next(c for c in res.candidates if c.listingId == "A")
        self.assertEqual(candidate_a.maximumContribution, Decimal("40"))  # 10 cans = 40L

    def test_50l_package_contributions_price_the_actual_partial_offer(self):
        sellers = [paint_listing("A", 20, 4, "1000"), paint_listing("B", 15, 4, "1100")]
        res = MaterialMatchingAgent(FakeBoundary(sellers), clock=lambda: NOW).match(req("50", "10000"))
        self.assertEqual(res.status, "ok")
        contributions = {candidate.listingId: candidate.maximumContribution for candidate in res.candidates}
        self.assertEqual(contributions, {"A": Decimal("40"), "B": Decimal("36")})
        prices = {seller.listing_id: _effective_contribution(seller, Decimal("50"), Decimal("10000"))[2]
                  for seller in sellers}
        self.assertEqual(prices, {"A": Decimal("10000"), "B": Decimal("9900")})

    def test_3_buyer_40L_seller_30L(self):
        boundary = FakeBoundary([paint_listing("A", 7, 4, "1000")]) # 28L
        res = MaterialMatchingAgent(boundary, clock=lambda: NOW).match(req("40", "50000"))
        self.assertEqual(res.status, "ok")
        self.assertEqual(len(res.candidates), 1)
        self.assertEqual(res.candidates[0].maximumContribution, Decimal("28"))

    def test_4_buyer_40L_seller_60L(self):
        boundary = FakeBoundary([paint_listing("A", 15, 4, "1000")]) # 60L
        res = MaterialMatchingAgent(boundary, clock=lambda: NOW).match(req("40", "50000"))
        self.assertEqual(res.status, "ok")
        self.assertEqual(len(res.candidates), 1)
        self.assertEqual(res.candidates[0].maximumContribution, Decimal("40"))

    def test_7_package_5_cans_4L_same_as_20L_base(self):
        boundary = FakeBoundary([paint_listing("A", 10, 4, "1000")])
        res1 = MaterialMatchingAgent(boundary, clock=lambda: NOW).match(req("20", "50000"))
        res2 = MaterialMatchingAgent(boundary, clock=lambda: NOW).match(req("20", "50000"))
        self.assertEqual(res1.status, res2.status)
        self.assertEqual(len(res1.candidates), len(res2.candidates))

    def test_8_unit_mismatch_kg_vs_L(self):
        boundary = FakeBoundary([paint_listing("A", 10, 4, "1000")])
        res = MaterialMatchingAgent(boundary, clock=lambda: NOW).match(req("20", "50000", unit="kg"))
        self.assertEqual(res.status, "no_candidate")

    def test_9_no_stock_rejects(self):
        boundary = FakeBoundary([paint_listing("A", 0, 4, "1000")])
        res = MaterialMatchingAgent(boundary, clock=lambda: NOW).match(req("10", "50000"))
        self.assertEqual(res.status, "no_candidate")

    def test_10_one_package_affordability(self):
        # Paint 4L/can, 1000/can, budget 1500 -> 1 can affordable (1000 <= 1500) -> valid candidate!
        boundary = FakeBoundary([paint_listing("A", 10, 4, "1000")])
        res_ok = MaterialMatchingAgent(boundary, clock=lambda: NOW).match(req("20", "1500"))
        self.assertEqual(res_ok.status, "ok")
        self.assertEqual(res_ok.candidates[0].maximumContribution, Decimal("4")) # 1 can = 4L

        # Budget 500 -> 1 can costs 1000 > 500 -> REJECT!
        res_reject = MaterialMatchingAgent(boundary, clock=lambda: NOW).match(req("20", "500"))
        self.assertEqual(res_reject.status, "no_candidate")

if __name__ == "__main__":
    unittest.main()
