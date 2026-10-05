import asyncio
from pathlib import Path
from dotenv import load_dotenv

ENV_FILE = Path(__file__).resolve().parent.parent.parent / ".env.local"
if ENV_FILE.exists():
    load_dotenv(dotenv_path=ENV_FILE, override=True)

import pytest
from app.assistant.semantic_engine import SurplusLinkSemanticAssistantEngine
from app.assistant.semantic_schemas import Intent

pytestmark = pytest.mark.live_gemini


@pytest.fixture
def engine():
    return SurplusLinkSemanticAssistantEngine()


@pytest.fixture
def user_ctx():
    return {"user_id": "eval-user-001", "roles": ["BUYER"]}


@pytest.mark.asyncio
async def test_eval_requirement_utterances(engine, user_ctx):
    utterances = [
        "I need a generator",
        "I need two generators",
        "mata generator dekak oone",
        "generator 2k one",
    ]
    for idx, text in enumerate(utterances):
        cid = f"conv-req-{idx}"
        res = await engine.process_chat(user_ctx, cid, text)
        assert res["intent"] in {"CREATE_REQUIREMENT", "CREATE_REQUIREMENT_DRAFT"}
        draft = res["requirement_draft"]
        assert draft is not None
        assert "generator" in draft["item_name"].lower()
        assert res["message"] is not None
        # State persistence verification
        assert draft["id"] in [d["id"] for d in res["drafts"]]


@pytest.mark.asyncio
async def test_eval_package_quantity_utterances(engine, user_ctx):
    utterances = [
        ("I need 10 bags of cement in Malabe", 10, "Malabe"),
        ("Malabe walata cement bags 10k one", 10, "Malabe"),
        ("cement 10 bags", 10, None),
        ("need ten cement bags", 10, None),
    ]
    for idx, (text, expected_count, expected_loc) in enumerate(utterances):
        cid = f"conv-pkg-{idx}"
        res = await engine.process_chat(user_ctx, cid, text)
        assert res["intent"] in {"CREATE_REQUIREMENT", "CREATE_REQUIREMENT_DRAFT"}
        draft = res["requirement_draft"]
        assert draft is not None
        assert draft["package_count"] == expected_count
        assert draft["package_size"] is None or draft["package_size"] == 0  # Must ask for package size
        assert draft["ready_for_review"] is False  # Cannot be ready without package size
        if expected_loc:
            assert draft["location_text"] is not None and expected_loc.lower() in draft["location_text"].lower()


@pytest.mark.asyncio
async def test_eval_location_utterances(engine, user_ctx):
    # Turn 1: create draft
    cid = "conv-loc-eval"
    res1 = await engine.process_chat(user_ctx, cid, "I need 2 generators")
    draft1 = res1["requirement_draft"]
    assert draft1 is not None

    loc_utterances = [
        "deliver it to where I am",
        "send it to my current location",
        "mata innathanata genna",
        "my current place",
    ]
    for text in loc_utterances:
        cid_loc = f"conv-loc-{hash(text)}"
        # Start generator draft
        await engine.process_chat(user_ctx, cid_loc, "I need 2 generators")
        # Turn 2: current location phrase
        res2 = await engine.process_chat(user_ctx, cid_loc, text)
        draft2 = res2["requirement_draft"]
        assert draft2 is not None
        assert draft2["location_source"] == "CURRENT_DEVICE_LOCATION"
        assert res2["client_action"] is not None
        assert res2["client_action"]["type"] == "REQUEST_DEVICE_LOCATION"
        assert draft2["ready_for_review"] is False  # Must not be ready before coordinates arrive


@pytest.mark.asyncio
async def test_eval_seller_count_utterances(engine, user_ctx):
    utterances = [
        "how many sellers are in the system?",
        "how many sellers do you have?",
        "how many sellers are there?",
        "okkoma categories wala sellers la keeyak innawada?",
        "system eke sellers gana keeyak innawada?",
    ]
    for idx, text in enumerate(utterances):
        cid = f"conv-sellers-{idx}"
        res = await engine.process_chat(user_ctx, cid, text)
        assert res["intent"] in {"SELLER_COUNT", "MARKETPLACE_STATS"}
        # Must not invent fake item "Material"
        draft = res["requirement_draft"]
        assert draft is None or "material" not in draft.get("item_name", "").lower()
        assert "seller" in res["message"].lower() or " sellers " in res["message"].lower() or "innawada" in res["message"].lower()


@pytest.mark.asyncio
async def test_eval_knowledge_utterances(engine, user_ctx):
    utterances = [
        "how should paint be stored?",
        "paint store karanne kohomada?",
    ]
    for idx, text in enumerate(utterances):
        cid = f"conv-know-{idx}"
        res = await engine.process_chat(user_ctx, cid, text)
        assert res["intent"] in {"ASK_STORAGE_KNOWLEDGE", "ASK_CONSTRUCTION_KNOWLEDGE", "CONSTRUCTION_KNOWLEDGE"}
        assert len(res["message"]) > 10


@pytest.mark.asyncio
async def test_eval_live_marketplace_search(engine, user_ctx):
    utterances = [
        "find cement sellers near Malabe",
        "cement Malabe walin hoyala meya",
        "cement Malabe walin hoyala denna",
    ]
    for idx, text in enumerate(utterances):
        cid = f"conv-search-{idx}"
        res = await engine.process_chat(user_ctx, cid, text)
        assert res["intent"] in {"SEARCH_MATERIAL", "LIVE_MARKETPLACE_QUERY"}
        assert "cement" in res["message"].lower() or "marketplace" in res["message"].lower() or "seller" in res["message"].lower()


@pytest.mark.asyncio
async def test_eval_estimation_utterances(engine, user_ctx):
    utterances = [
        "how much paint for a 10 sqm wall?",
        "10m2 wall ekakata paint kochchara one da?",
    ]
    for idx, text in enumerate(utterances):
        cid = f"conv-est-{idx}"
        res = await engine.process_chat(user_ctx, cid, text)
        assert res["intent"] in {"ASK_QUANTITY_ESTIMATION", "MATERIAL_ESTIMATION"}
        assert "paint" in res["message"].lower() or "liters" in res["message"].lower() or "l" in res["message"].lower() or "coat" in res["message"].lower()
