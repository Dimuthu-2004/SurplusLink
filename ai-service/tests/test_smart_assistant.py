import pytest
import asyncio
from typing import Dict, Any

from app.assistant.semantic_schemas import Intent, SemanticIntent, ExtractedSlots
from app.assistant.unit_normalizer import UnitNormalizer
from app.assistant.material_estimation import MaterialEstimationEngine
from app.assistant.semantic_engine import SurplusLinkSemanticAssistantEngine

pytestmark = pytest.mark.live_gemini


def test_unit_normalizer_area():
    res1 = UnitNormalizer.parse_area("10m2")
    assert res1 is not None
    assert res1[0] == 10.0
    assert res1[1] == "m2"

    res2 = UnitNormalizer.parse_area("100 sqft")
    assert res2 is not None
    assert round(res2[0], 2) == 9.29
    assert res2[1] == "sqft"

    res3 = UnitNormalizer.parse_area("12ft x 10ft")
    assert res3 is not None
    assert round(res3[0], 2) == 11.15
    assert res3[1] == "sqft"


def test_unit_normalizer_packages():
    pkg1 = UnitNormalizer.parse_quantity_structure("10 x 50kg bags")
    assert pkg1 is not None
    assert pkg1.package_count == 10
    assert pkg1.package_size == 50.0
    assert pkg1.value == 500.0
    assert pkg1.unit == "kg"

    pkg2 = UnitNormalizer.parse_quantity_structure("2 cans of 4L")
    assert pkg2 is not None
    assert pkg2.package_count == 2
    assert pkg2.package_size == 4.0
    assert pkg2.value == 8.0
    assert pkg2.unit == "L"


def test_material_estimation_engine():
    # Paint estimation
    paint_res = MaterialEstimationEngine.estimate("Paint", {"area_sqm": 20.0, "coats": 2, "coverage_rate": 10.0})
    assert paint_res.calculated_value == 4.4
    assert paint_res.unit == "L"
    assert "4.4 Litres" in paint_res.explanation

    # Tiles estimation
    tiles_res = MaterialEstimationEngine.estimate("Tiles", {"area_sqm": 15.0, "tile_width_cm": 60, "tile_height_cm": 60})
    assert tiles_res.calculated_value > 0
    assert tiles_res.unit == "pcs"
    assert "Tiles Needed" in tiles_res.explanation

    # Cement estimation for concrete
    cement_res = MaterialEstimationEngine.estimate("Cement", {"concrete_m3": 2.0})
    assert cement_res.calculated_value == 13.0
    assert cement_res.unit == "bags"


@pytest.mark.asyncio
async def test_smart_assistant_scenarios():
    engine = SurplusLinkSemanticAssistantEngine()
    user_context = {"user_id": "test-user-smart-eval"}

    # Scenario 1: Material estimation question asking clarification
    res1 = await engine.process_chat(user_context, "eval-1", "10m2 area ekakata paint kochchara ooneda")
    assert res1["intent"] == "ASK_QUANTITY_ESTIMATION"
    assert "paint" in res1["message"].lower() or "area" in res1["message"].lower() or "litres" in res1["message"].lower()
    assert res1["requirement_draft"] is None

    # Scenario 2: Price information query (does not create draft)
    res2 = await engine.process_chat(user_context, "eval-2", "paint 10L price keeyada normally?")
    assert res2["intent"] == "PRICE_INFORMATION"
    assert res2["requirement_draft"] is None

    # Scenario 3: Live marketplace query
    res3 = await engine.process_chat(user_context, "eval-3", "site eke sellers lage paint price kohomada")
    assert res3["intent"] == "LIVE_MARKETPLACE_QUERY"
    assert "paint" in res3["message"].lower() or "marketplace" in res3["message"].lower() or "listings" in res3["message"].lower()

    # Scenario 4: Construction knowledge question (Tile transport)
    res4 = await engine.process_chat(user_context, "eval-4", "tiles break wenne nathi widiyata transport karanne kohomada")
    assert res4["intent"] == "CONSTRUCTION_KNOWLEDGE"
    assert res4["requirement_draft"] is None

    # Scenario 5: Platform help
    res5 = await engine.process_chat(user_context, "eval-5", "how do I create a listing?")
    assert res5["intent"] in ["PLATFORM_HELP", "CONSTRUCTION_KNOWLEDGE"]

    # Scenario 6: Requirement draft without specific location (does not store "site" as location)
    res6 = await engine.process_chat(user_context, "eval-6", "tomorrow site ekata cement ganna puluwanda")
    assert res6["intent"] == "CREATE_REQUIREMENT_DRAFT"
    if res6["requirement_draft"]:
        loc = res6["requirement_draft"].get("location_text")
        assert loc is None or loc.lower() not in ["site", "site eka"]
