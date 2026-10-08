"""Member 1 focused MaterialMatchingAgent tests using an in-memory API snapshot."""
from app.agents.material_matching import MaterialMatchingAgent
from app.workflows.demo import demo_request
from app.workflows.orchestration import SnapshotTools
from uuid import UUID


def _criteria(request):
    row = request.buyerRequest
    return {"buyerUserId": row["buyerId"], "categoryId": row["categoryId"],
            "requiredQuantity": row["requiredQuantity"], "unit": row["unit"],
            "maximumBudget": row["maximumBudget"], "deadline": row["deadline"],
            "constructionItemTemplateId": row["constructionItemTemplateId"]}


def test_partial_candidate_has_structured_contribution_contract():
    request = demo_request()
    row = request.listings[0].model_copy(update={"availableQuantity": 5})
    result = MaterialMatchingAgent(SnapshotTools((row,))).match(_criteria(request))
    assert result.status == "ok"
    candidate = result.candidates[0]
    assert candidate.maximumContribution == 5
    assert candidate.fullCoverage is False
    assert candidate.availableQuantity == 5


def test_self_owned_listing_is_excluded_and_no_candidate_is_safe():
    request = demo_request()
    row = request.listings[0].model_copy(update={"sellerId": UUID(request.buyerRequest["buyerId"])})
    result = MaterialMatchingAgent(SnapshotTools((row,))).match(_criteria(request))
    assert result.status == "no_candidate"
    assert result.failure.code == "NO_CANDIDATE"
    assert result.exclusions[0].code == "SELF_MATCH_NOT_ALLOWED"


def test_inactive_listing_is_safely_excluded():
    request = demo_request()
    inactive = request.listings[0].model_copy(update={"status": "DRAFT"})
    result = MaterialMatchingAgent(SnapshotTools((inactive,))).match(_criteria(request))
    assert result.status == "no_candidate"
    assert result.failure.code == "NO_CANDIDATE"
