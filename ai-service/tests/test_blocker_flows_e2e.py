import pytest
import asyncio
from pathlib import Path
from dotenv import load_dotenv

ENV_FILE = Path(__file__).resolve().parent.parent.parent / ".env.local"
if ENV_FILE.exists():
    load_dotenv(dotenv_path=ENV_FILE, override=True)

from app.assistant.semantic_engine import SurplusLinkSemanticAssistantEngine
from app.assistant.semantic_schemas import Intent, RequirementDraft

@pytest.mark.asyncio
async def test_blocker_1_current_location_client_action_flow():
    engine = SurplusLinkSemanticAssistantEngine()
    user_ctx = {"user_id": "test_user_1", "email": "test@surpluslink.lk"}
    conv_id = "conv_blocker_1"

    # Turn 1: "I want a generator"
    res1 = await engine.process_chat(user_ctx, conv_id, "I want a generator")
    draft1 = res1.get("requirement_draft")
    assert draft1 is not None
    assert draft1["item_name"].lower() == "generator"
    assert draft1["ready_for_review"] is False
    assert "quantity" in draft1["missing_required_fields"]

    # Turn 2: "2"
    res2 = await engine.process_chat(user_ctx, conv_id, "2")
    draft2 = res2.get("requirement_draft")
    assert draft2 is not None
    assert draft2["entered_quantity"] == 2.0
    assert draft2["ready_for_review"] is False
    assert "delivery_location" in draft2["missing_required_fields"]

    # Turn 3: "to my current location"
    res3 = await engine.process_chat(user_ctx, conv_id, "to my current location")
    draft3 = res3.get("requirement_draft")
    client_act = res3.get("client_action")
    assert client_act is not None
    assert client_act["type"] == "REQUEST_DEVICE_LOCATION"
    assert draft3["location_pending"] is True
    assert draft3["location_source"] == "CURRENT_DEVICE_LOCATION"
    assert draft3["ready_for_review"] is False

    # Turn 4: Flutter responds with GPS coordinates
    loc_payload = {
        "latitude": 6.9271,
        "longitude": 79.8612,
        "resolved_address": "Malabe, Sri Lanka",
        "location_source": "CURRENT_DEVICE_LOCATION",
    }
    res4 = await engine.process_chat(user_ctx, conv_id, "Location acquired", structured_location=loc_payload)
    draft4 = res4.get("requirement_draft")
    assert draft4 is not None
    assert draft4["location_pending"] is False
    assert draft4["latitude"] == 6.9271
    assert draft4["longitude"] == 79.8612
    assert draft4["resolved_address"] == "Malabe, Sri Lanka"
    assert draft4["ready_for_review"] is True
    assert len(draft4["missing_required_fields"]) == 0


@pytest.mark.asyncio
async def test_blocker_2_cement_package_semantics_flow():
    engine = SurplusLinkSemanticAssistantEngine()
    user_ctx = {"user_id": "test_user_2", "email": "test@surpluslink.lk"}
    conv_id = "conv_blocker_2"

    # Turn 1: "I need 10 bags of mortar in Malabe" (custom item without fixed catalog package size)
    res1 = await engine.process_chat(user_ctx, conv_id, "I need 10 bags of mortar in Malabe")
    draft1 = res1.get("requirement_draft")
    assert draft1 is not None
    assert "mortar" in draft1["item_name"].lower()
    assert draft1["package_count"] == 10
    assert draft1["package_size"] is None
    assert draft1["normalized_quantity"] is None
    assert draft1["location_text"] == "Malabe"
    assert draft1["ready_for_review"] is False
    assert "package_size" in draft1["missing_required_fields"]

    # Turn 2: "50kg"
    res2 = await engine.process_chat(user_ctx, conv_id, "50kg")
    draft2 = res2.get("requirement_draft")
    assert draft2 is not None
    assert draft2["package_count"] == 10
    assert draft2["package_size"] == 50.0
    assert draft2["normalized_quantity"] == 500.0
    assert draft2["location_text"] == "Malabe"  # Location PRESERVED across turn!
    assert draft2["ready_for_review"] is True
    assert len(draft2["missing_required_fields"]) == 0


@pytest.mark.asyncio
async def test_blocker_3_tile_coverage_physical_quantity_flow():
    engine = SurplusLinkSemanticAssistantEngine()
    user_ctx = {"user_id": "test_user_3", "email": "test@surpluslink.lk"}
    conv_id = "conv_blocker_3"

    # Turn 1: "I need 600x600mm tiles for 12m2"
    res1 = await engine.process_chat(user_ctx, conv_id, "I need 600x600mm tiles for 12m2")
    draft1 = res1.get("requirement_draft")
    assert draft1 is not None
    assert "tile" in draft1["item_name"].lower()
    assert draft1["coverage_area"] == 12.0
    assert draft1["dimensions"]["width"] == 600.0
    assert draft1["dimensions"]["length"] == 600.0
    assert draft1["dimensions"]["unit"] == "mm"
    assert draft1["calculated_physical_quantity"] == 37
    assert draft1["calculated_package_count"] == 10
    assert draft1["ready_for_review"] is False
    assert "delivery_location" in draft1["missing_required_fields"]

    # Turn 2: "to Malabe"
    res2 = await engine.process_chat(user_ctx, conv_id, "to Malabe")
    draft2 = res2.get("requirement_draft")
    assert draft2 is not None
    assert draft2["location_text"] == "Malabe"
    assert draft2["coverage_area"] == 12.0
    assert draft2["calculated_physical_quantity"] == 37
    assert draft2["calculated_package_count"] == 10
    assert draft2["ready_for_review"] is True
    assert len(draft2["missing_required_fields"]) == 0
