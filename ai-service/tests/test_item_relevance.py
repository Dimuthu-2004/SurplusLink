from dataclasses import replace
from app.agents.item_relevance import ItemRelevanceAgent, ItemRelevanceResult
from app.agents.matching_schemas import MatchingRequest
from app.agents.material_matching import MaterialMatchingAgent
from test_material_matching_agent import listing, request, FakeActiveMaterialsBoundary

GENERATOR = "00000000-0000-0000-0000-000000000202"
PAINT = "00000000-0000-0000-0000-000000000201"
COMPRESSOR = "00000000-0000-0000-0000-000000000222"


def test_exact_template_and_different_template_contract():
    criteria = MatchingRequest.model_validate(dict(request(), constructionItemTemplateId=GENERATOR))
    agent = ItemRelevanceAgent()
    item = replace(listing("item", quantity="10", unit_price="1"), template_id=GENERATOR)
    assert agent.evaluate(criteria, item).eligible
    result = agent.evaluate(criteria, replace(item, template_id=COMPRESSOR))
    assert result.classification == "INCOMPATIBLE"
    assert ItemRelevanceResult.model_validate(result.model_dump()).confidence == 1


def test_custom_identity_requires_item_evidence_not_category():
    criteria = MatchingRequest.model_validate(dict(request(), constructionItemTemplateId=PAINT))
    item = replace(listing("item", quantity="10", unit_price="1"), template_id=None)
    agent = ItemRelevanceAgent()
    assert agent.evaluate(criteria, replace(item, title="Exterior Wall Paint")).eligible
    assert not agent.evaluate(criteria, replace(item, title="Random Finishes item")).eligible
    assert not agent.evaluate(criteria, replace(item, title="Tile Adhesive")).eligible


def test_ineligible_candidates_never_reach_scoring():
    item = replace(listing("item", quantity="10", unit_price="1"), template_id=COMPRESSOR)
    agent = MaterialMatchingAgent(FakeActiveMaterialsBoundary([item]))
    agent._candidate = lambda *_: (_ for _ in ()).throw(AssertionError("wrong item reached scoring"))
    result = agent.match(dict(request(), constructionItemTemplateId=GENERATOR))
    assert result.candidates == []
    assert result.exclusions[0].code == "ITEM_MISMATCH"
