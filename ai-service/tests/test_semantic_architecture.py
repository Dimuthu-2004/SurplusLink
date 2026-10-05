import pytest

from app.assistant.semantic_draft_agent import CatalogDraftValidator
from app.assistant.semantic_schemas import ExtractedSlots, QuantitySlots
from app.assistant.semantic_schemas import Intent, SemanticIntent, SemanticRoutingResult
from app.assistant.semantic_engine import SurplusLinkSemanticAssistantEngine
from evaluation_corpus import CORPUS, NEGATIVE_REQUIREMENT_CASES


def test_held_out_corpus_has_25_utterances_per_important_intent():
    assert len(CORPUS) >= 8
    assert all(len(utterances) >= 25 for utterances in CORPUS.values())
    assert sum(map(len, CORPUS.values())) >= 200
    assert len(NEGATIVE_REQUIREMENT_CASES) >= 8


def test_catalog_validator_package_arithmetic_and_dynamic_preferences():
    catalog = [{
        "id": "template-cement", "categoryId": "category", "name": "Cement",
        "baseUnit": "kg", "packageType": "BAG", "allowedUnits": ["kg", "bag"],
        "allowedPackageSizes": [25, 50],
        "attributeSchema": '[{"id":"grade","buyerPreference":true,"priority":"REQUIRED","type":"select","options":["OPC","PPC"]}]',
    }]
    slots = ExtractedSlots(
        item="Cement",
        quantity=QuantitySlots(package_count=7, package_size=50, package_unit="kg"),
        preferences={"grade": "opc"},
        location_text="SLIIT laga",
        structured_location={"latitude": 6.9147, "longitude": 79.9729},
        maximum_budget=100000,
        deadline="2030-01-01T00:00:00Z",
        delivery_required=True,
    )
    draft, error = CatalogDraftValidator().build_or_update(slots, catalog)
    assert error is None
    assert draft is not None
    assert draft.normalized_quantity == 350
    assert draft.preferences == {"grade": "OPC"}
    assert draft.ready_for_review


def test_catalog_validator_does_not_infer_missing_unit():
    catalog = [{
        "id": "paint", "categoryId": "category", "name": "Paint", "baseUnit": "L",
        "packageType": "CAN", "allowedUnits": ["L", "can"], "allowedPackageSizes": [1, 4, 10],
        "attributeSchema": "[]",
    }]
    draft, error = CatalogDraftValidator().build_or_update(
        ExtractedSlots(item="Paint", quantity=QuantitySlots(value=10)), catalog,
    )
    assert draft is not None
    assert error == "What unit should I use for that quantity?"
    assert draft.normalized_quantity is None


class _Tools:
    async def get_catalog_item(self, _context):
        return [{
            "id": "paint", "categoryId": "finishes", "name": "Paint", "baseUnit": "L",
            "packageType": "CAN", "allowedUnits": ["L", "can"], "allowedPackageSizes": [1, 4, 10],
            "attributeSchema": "[]",
        }]


class _Router:
    def __init__(self, decisions):
        self.decisions = iter(decisions)

    def classify(self, *_args, **_kwargs):
        return SemanticRoutingResult(intents=[next(self.decisions)])


def _decision(intent, **kwargs):
    return SemanticIntent(
        intent=intent, confidence=.99, is_question=False,
        is_purchase_request=intent in {Intent.CREATE_REQUIREMENT_DRAFT, Intent.CONTINUE_REQUIREMENT_DRAFT},
        is_follow_up=intent == Intent.CONTINUE_REQUIREMENT_DRAFT,
        **kwargs,
    )


@pytest.mark.asyncio
async def test_structured_state_continues_same_draft_after_an_unrelated_turn():
    engine = SurplusLinkSemanticAssistantEngine(tools_client=_Tools())
    engine.router = _Router([
        _decision(Intent.CREATE_REQUIREMENT_DRAFT, extracted_slots=ExtractedSlots(
            item="Paint", quantity=QuantitySlots(value=6, unit="L"),
            maximum_budget=100000, deadline="2030-01-01T00:00:00Z",
            delivery_required=True,
        )),
        _decision(Intent.CONTINUE_REQUIREMENT_DRAFT, extracted_slots=ExtractedSlots(
            location_text="Malabe",
            structured_location={"latitude": 6.9147, "longitude": 79.9729},
        ), response_language="si-Latn"),
    ])
    first = await engine.process_chat({"user_id": "buyer"}, "conversation", "first turn")
    second = await engine.process_chat({"user_id": "buyer"}, "conversation", "follow up")
    assert "delivery" in first["requirement_draft"]["missing_required_fields"][0]
    assert second["requirement_draft"]["template_id"] == "paint"
    assert second["requirement_draft"]["normalized_quantity"] == 6
    assert second["requirement_draft"]["location_text"] == "Malabe"
    assert second["requirement_draft"]["ready_for_review"] is True
