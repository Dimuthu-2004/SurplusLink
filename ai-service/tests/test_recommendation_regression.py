import unittest
from uuid import UUID

from app.workflows.demo import demo_request
from app.workflows.orchestration import WorkflowOrchestrator, WorkflowRequest


class RecommendationRegressionTests(unittest.IsolatedAsyncioTestCase):
    async def test_reranking_changes_real_match_id_and_returns_one_recommendation(self):
        raw = demo_request().model_dump(mode="json")
        first = raw["listings"][0]
        second = dict(first, listingId=str(UUID(int=100)), matchId=str(UUID(int=101)), condition="POOR")
        first["condition"] = "EXCELLENT"
        raw["listings"] = [first, second]
        for expected in (first["matchId"], second["matchId"]):
            result = await WorkflowOrchestrator().run(WorkflowRequest.model_validate(raw))
            self.assertEqual(str(result.validation.recommendedMatchId), expected)
            self.assertEqual(result.validation.recommendedMatchId, result.recommendation.matchId)
            self.assertEqual(sum(row["matchId"] == expected for row in raw["listings"]), 1)
            first["condition"], second["condition"] = "POOR", "EXCELLENT"

    async def test_no_routed_candidate_reports_exact_reason(self):
        raw = demo_request().model_dump(mode="json")
        raw["listings"][0].update(distanceKm=None, durationMinutes=None, transportCost=None,
                                  routingError="ROUTE_UNAVAILABLE")
        result = await WorkflowOrchestrator().run(WorkflowRequest.model_validate(raw))
        self.assertIsNone(result.validation.recommendedMatchId)
        self.assertEqual(result.errorCode, "NO_VALID_SELECTABLE_CANDIDATE")
        self.assertIn("MATCH_DATA_INCOMPLETE_OR_INCONSISTENT", result.validation.violations)
