import pytest

from app.assistant.semantic_engine import SurplusLinkSemanticAssistantEngine
from app.assistant.semantic_schemas import ExtractedSlots, Intent, QuantitySlots, SemanticIntent, SemanticRoutingResult


class FakeRouter:
    def __init__(self, decisions):
        self.decisions = iter(decisions)

    def classify(self, *_args, **_kwargs):
        return SemanticRoutingResult(intents=[next(self.decisions)])


class FakeTools:
    async def get_catalog_item(self, _context):
        return []


def decision(intent, **kwargs):
    return SemanticIntent(intent=intent, confidence=0.99, **kwargs)


@pytest.mark.asyncio
async def test_requirement_draft_is_created_and_preserved_without_gemini():
    engine = SurplusLinkSemanticAssistantEngine(tools_client=FakeTools())
    engine.router = FakeRouter([
        decision(
            Intent.CREATE_REQUIREMENT,
            is_purchase_request=True,
            extracted_slots=ExtractedSlots(
                item="Generator",
                quantity=QuantitySlots(value=2, unit="piece"),
            ),
        ),
        decision(
            Intent.UPDATE_REQUIREMENT,
            is_follow_up=True,
            extracted_slots=ExtractedSlots(location_text="Malabe"),
        ),
    ])

    first = await engine.process_chat({"user_id": "offline"}, "draft", "first")
    second = await engine.process_chat({"user_id": "offline"}, "draft", "second")

    assert first["intent"] == "CREATE_REQUIREMENT"
    assert first["requirement_draft"]["item_name"] == "Generator"
    assert second["intent"] == "UPDATE_REQUIREMENT"
    assert second["requirement_draft"]["item_name"] == "Generator"
    assert second["requirement_draft"]["location_text"] == "Malabe"
    assert second["requirement_draft"]["ready_for_review"] is True


@pytest.mark.asyncio
async def test_location_callback_continues_existing_draft_without_router_intent():
    engine = SurplusLinkSemanticAssistantEngine(tools_client=FakeTools())
    engine.router = FakeRouter([
        decision(
            Intent.CREATE_REQUIREMENT,
            is_purchase_request=True,
            extracted_slots=ExtractedSlots(
                item="Generator",
                quantity=QuantitySlots(value=2, unit="piece"),
            ),
        ),
        decision(Intent.UNKNOWN),
    ])

    await engine.process_chat({"user_id": "offline"}, "location", "first")
    result = await engine.process_chat(
        {"user_id": "offline"},
        "location",
        "Location acquired",
        structured_location={
            "latitude": 6.9271,
            "longitude": 79.8612,
            "resolved_address": "Malabe",
            "location_source": "CURRENT_DEVICE_LOCATION",
        },
    )

    assert result["intent"] == "UPDATE_REQUIREMENT"
    assert result["requirement_draft"]["location_pending"] is False
    assert result["requirement_draft"]["latitude"] == 6.9271
    assert result["requirement_draft"]["longitude"] == 79.8612


def test_gemini_schema_adapter_removes_unsupported_developer_api_keywords():
    from app.assistant.llm_provider import GeminiLLMProvider
    from app.assistant.semantic_schemas import SemanticRoutingResult

    schema = GeminiLLMProvider._gemini_schema(SemanticRoutingResult.model_json_schema())

    def assert_supported(value):
        if isinstance(value, dict):
            assert "additionalProperties" not in value
            assert "exclusiveMinimum" not in value
            assert "exclusiveMaximum" not in value
            assert "minimum" not in value
            assert "maximum" not in value
            for child in value.values():
                assert_supported(child)
        elif isinstance(value, list):
            for child in value:
                assert_supported(child)

    assert_supported(schema)


def test_tile_packages_are_derived_from_physical_quantity_and_package_size():
    from app.assistant.material_estimation import MaterialEstimationEngine

    result = MaterialEstimationEngine.estimate_tiles(
        {
            "area_sqm": 12,
            "tile_width_mm": 600,
            "tile_length_mm": 600,
            "package_size": 4,
        }
    )

    assert result.calculated_physical_quantity == 37
    assert result.calculated_package_count == 10
