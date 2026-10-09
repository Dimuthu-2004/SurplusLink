import unittest
from decimal import Decimal as D
from uuid import UUID

from app.agents.match_scoring import score_breakdown
from app.workflows.demo import demo_request
from app.workflows.orchestration import WorkflowOrchestrator, WorkflowRequest


class ScoringTests(unittest.IsolatedAsyncioTestCase):
    async def select(self, first_changes, second_changes, reverse=False):
        raw = demo_request().model_dump(mode="json")
        first = dict(raw["listings"][0], **first_changes)
        second = dict(raw["listings"][0], **second_changes)
        second.update(listingId=str(UUID(int=100)), matchId=str(UUID(int=101)))
        raw["listings"] = [second, first] if reverse else [first, second]
        raw["objective"] = "Prefer the first POOR listing regardless of scores"
        result = await WorkflowOrchestrator().run(WorkflowRequest.model_validate(raw))
        self.assertEqual(result.status, "MATCH_FOUND", result.model_dump_json())
        self.assertEqual(result.validation.recommendedMatchId, result.recommendation.matchId)
        self.assertEqual(sum(x["matchId"] == str(result.validation.recommendedMatchId) for x in raw["listings"]), 1)
        self.assertIn("scoreBreakdown", result.steps[-1].output)
        self.assertIn("recommendationReason", result.steps[-1].output)
        return result

    async def test_poor_vs_excellent_equal_old_base_score_and_order_independent(self):
        for reverse in (False, True):
            result = await self.select(dict(condition="POOR"), dict(condition="EXCELLENT"), reverse)
            self.assertEqual(result.recommendation.matchId, UUID(int=101))
            self.assertEqual(result.recommendation.score, D("0.7568"))

    async def test_route_failure_cannot_win_even_with_excellent_condition(self):
        for changes in (dict(distanceKm=None), dict(durationMinutes=None), dict(transportCost=None),
                        dict(routingError="ROUTING_UNAVAILABLE")):
            result = await self.select(dict(condition="EXCELLENT", **changes), dict(condition="POOR"))
            self.assertEqual(result.recommendation.matchId, UUID(int=101))

    async def test_equal_rounded_score_prefers_lower_total_cost(self):
        result = await self.select(dict(transportCost="500.01"), dict(transportCost="500"))
        self.assertEqual(result.recommendation.matchId, UUID(int=101))

    async def test_equal_score_prefers_condition_before_cost(self):
        # Both exactly .6: .125+.275+.2 versus .25+.15+.2.
        result = await self.select(dict(condition="POOR", unitPrice="10", transportCost="66.6666666666666666666666667", distanceKm="0"),
                                   dict(condition="FAIR", unitPrice="50", transportCost="500", distanceKm="0"))
        self.assertEqual(result.recommendation.score, D("0.6000"))
        self.assertEqual(result.recommendation.matchId, UUID(int=101))

    async def test_distance_then_id_break_ties(self):
        result = await self.select(dict(distanceKm="10.001"), dict(distanceKm="10"))
        self.assertEqual(result.recommendation.matchId, UUID(int=101))
        result = await self.select({}, {}, True)
        self.assertEqual(result.recommendation.matchId, UUID(int=5))

    def test_condition_order_and_missing_route(self):
        scores = [score_breakdown(c, D(1000), D(2000), D(10), D(500))["score"]
                  for c in ("EXCELLENT", "GOOD", "FAIR", "POOR")]
        self.assertEqual(scores, sorted(set(scores), reverse=True))
        result = score_breakdown("EXCELLENT", D(1000), D(2000))
        self.assertEqual(result["score"], 0)
        self.assertIsNone(result["totalEstimatedCost"])

    def test_preferences_use_the_same_final_weights_as_backend(self):
        wrong_but_excellent = score_breakdown("EXCELLENT", D(1000), D(2000), D(10), D(500), 0, 2)
        matching_but_good = score_breakdown("GOOD", D(1000), D(2000), D(10), D(500), 2, 2)
        half_matching = score_breakdown("GOOD", D(1000), D(2000), D(10), D(500), 1, 2)

        self.assertEqual(wrong_but_excellent["score"], D("0.4989"))
        self.assertEqual(matching_but_good["score"], D("0.7239"))
        self.assertEqual(half_matching["preferencePoints"], D("0.15"))
        self.assertEqual(half_matching["score"], D("0.5739"))
        self.assertGreater(matching_but_good["score"], wrong_but_excellent["score"])

    def test_no_preferences_preserves_the_legacy_formula(self):
        legacy = score_breakdown("EXCELLENT", D(1000), D(2000), D(10), D(500))
        no_preferences = score_breakdown("EXCELLENT", D(1000), D(2000), D(10), D(500), 0, 0)
        self.assertEqual(legacy["score"], D("0.7568"))
        self.assertEqual(no_preferences["score"], legacy["score"])
