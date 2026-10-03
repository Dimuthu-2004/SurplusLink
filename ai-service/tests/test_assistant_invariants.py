import pytest
from app.assistant.canonical_request import CanonicalUserRequest, ItemDimensions
from app.assistant.capability_registry import CapabilityRegistry
from app.assistant.item_resolver import ItemResolver
from app.assistant.semantic_schemas import Intent, QuantitySlots, RequirementDraft
from app.assistant.state_machine import AssistantState, ConversationStateMachine
from app.assistant.unit_normalizer import QuantityNormalizer
from app.assistant.validation_gate import ValidationGate


def test_invariant_area_preservation():
    """Invariant 1: If user area = 12m², normalized required area remains 12m²."""
    res = QuantityNormalizer.parse_area("12m2 area")
    assert res is not None
    m2_val, unit = res
    assert m2_val == pytest.approx(12.0)
    assert unit in {"m2", "sqm"}


def test_invariant_package_math():
    """Invariant 2: If package_count=10 and package_size=50kg, total_quantity must equal 500kg."""
    qty = QuantityNormalizer.parse_quantity_structure("10 x 50kg bags")
    assert qty is not None
    assert qty.package_count == 10
    assert qty.package_size == 50.0
    assert qty.value == 500.0
    assert qty.unit == "kg"


def test_invariant_dimensions_never_overwrite_coverage_area():
    """Invariant 3: Physical tile dimensions (600x600 mm) must never collapse into coverage area (m2)."""
    dims = QuantityNormalizer.parse_dimensions("600x600 mm")
    assert dims is not None
    assert dims.width == 600
    assert dims.length == 600
    assert dims.unit == "mm"

    # Area parsing on physical dimensions without explicit area term returns None
    area = QuantityNormalizer.parse_area("600x600 mm tile")
    assert area is None


def test_invariant_price_info_never_creates_requirement():
    """Invariant 4: A PRICE_INFORMATION intent must never create or update a requirement draft."""
    sm = ConversationStateMachine()
    req = CanonicalUserRequest(
        raw_message="paint 10L price keeyada?",
        intent=Intent.PRICE_INFORMATION,
        is_question=True,
    )
    sm.process_request(req)
    assert sm.active_requirement_draft is None
    assert sm.current_state == AssistantState.KNOWLEDGE_QA


def test_invariant_knowledge_question_never_mutates_quantity():
    """Invariant 5: A KNOWLEDGE question must never mutate requirement quantity."""
    sm = ConversationStateMachine()
    sm.known_fields["quantity_value"] = 50.0

    req = CanonicalUserRequest(
        raw_message="paint cans store karanne kohomada?",
        intent=Intent.CONSTRUCTION_KNOWLEDGE,
        is_question=True,
    )
    sm.process_request(req)
    assert sm.known_fields.get("quantity_value") == 50.0
    assert sm.current_state == AssistantState.KNOWLEDGE_QA


def test_invariant_broad_marketplace_query_resets_context():
    """Invariant 6: A broad marketplace query must not inherit an old item automatically."""
    sm = ConversationStateMachine()
    sm.last_item_candidate = "Cement"
    sm.active_requirement_draft = RequirementDraft(
        template_id="TEMPL_CEMENT",
        category_id="CAT_CEMENT",
        item_name="Cement",
        input_mode="PACKAGE_COUNT",
        normalized_base_unit="bag",
    )

    req = CanonicalUserRequest(
        raw_message="danata sellers lage thiyena items monada",
        intent=Intent.LIVE_MARKETPLACE_QUERY,
        is_follow_up=False,
    )
    sm.process_request(req)

    assert sm.active_requirement_draft is None
    assert sm.last_item_candidate is None
    assert sm.current_state == AssistantState.LIVE_QUERY


def test_invariant_marketplace_provenance():
    """Invariant 7: No current marketplace price may be returned without live-tool provenance."""
    val_gate = ValidationGate()
    # Claiming price with empty tool results MUST fail validation gate
    valid = val_gate.validate_marketplace_provenance(
        intent=Intent.LIVE_MARKETPLACE_QUERY,
        tool_results=[],
        response_text="Current price is LKR 2,500.",
    )
    assert valid is False


def test_invariant_unit_conversion():
    """Invariant 8: Unit conversion between sqft and m2 is deterministic."""
    m2 = QuantityNormalizer.sqft_to_sqm(100.0)
    assert m2 == pytest.approx(9.2903, rel=1e-3)

    sqft = QuantityNormalizer.sqm_to_sqft(10.0)
    assert sqft == pytest.approx(107.639, rel=1e-3)


def test_invariant_noisy_k_suffix():
    """Invariant 9: '10k bags' is normalized to 10,000 bags."""
    qty = QuantityNormalizer.parse_quantity_structure("10k bags")
    assert qty is not None
    assert qty.package_count == 10000
    assert qty.value == 10000.0


def test_invariant_invalid_location_tokens_rejected():
    """Invariant 10: Invalid location tokens like 'site' or 'kohomada' are rejected as delivery cities."""
    assert ValidationGate.validate_location("site") is None
    assert ValidationGate.validate_location("site eka") is None
    assert ValidationGate.validate_location("kohomada") is None
    assert ValidationGate.validate_location("Colombo") == "Colombo"
    assert ValidationGate.validate_location("Kandy") == "Kandy"


def test_invariant_custom_item_candidate_accepted():
    """Invariant 11: Open-vocabulary custom items (Water Pump, Generator) are accepted with null catalog ID."""
    resolver = ItemResolver()
    res1 = resolver.resolve("Water Pump")
    assert res1 is not None
    assert res1.is_custom_item is True
    assert res1.catalog_item_id is None
    assert res1.display_name == "Water Pump"

    res2 = resolver.resolve("Generator")
    assert res2 is not None
    assert res2.is_custom_item is True
    assert res2.display_name == "Generator"


def test_invariant_multi_draft_support():
    """Invariant 12: Additive requirement requests ('also need a water pump') create a new draft without destroying previous draft."""
    sm = ConversationStateMachine()

    # Draft 1: Generator
    req1 = CanonicalUserRequest(
        raw_message="I want a generator",
        intent=Intent.CREATE_REQUIREMENT_DRAFT,
        item_candidate="Generator",
        resolved_item="Generator",
        is_custom_item=True,
    )
    sm.process_request(req1)
    draft1 = RequirementDraft(template_id="CUSTOM_ITEM", category_id="GENERAL", item_name="Generator", display_name="Generator", is_custom_item=True, input_mode="BASE_QUANTITY", entered_quantity=2.0, normalized_base_unit="piece", ready_for_review=True)
    sm.set_active_draft(draft1)
    assert len(sm.drafts) == 1

    # Additive Draft 2: Water Pump
    req2 = CanonicalUserRequest(
        raw_message="also need a water pump",
        intent=Intent.CREATE_REQUIREMENT_DRAFT,
        item_candidate="Water Pump",
        resolved_item="Water Pump",
        is_custom_item=True,
    )
    sm.process_request(req2)
    draft2 = RequirementDraft(template_id="CUSTOM_ITEM", category_id="GENERAL", item_name="Water Pump", display_name="Water Pump", is_custom_item=True, input_mode="BASE_QUANTITY", entered_quantity=1.0, normalized_base_unit="piece", ready_for_review=True)
    sm.set_active_draft(draft2)

    assert len(sm.drafts) == 2
    assert sm.drafts[0].item_name == "Generator"
    assert sm.drafts[1].item_name == "Water Pump"


def test_invariant_4l_cans_3k_parsing():
    """Invariant 13: '4L cans 3k' is normalized to 3,000 cans of 4L."""
    qty = QuantityNormalizer.parse_quantity_structure("4L cans 3k")
    assert qty is not None
    assert qty.package_count == 3000
    assert qty.package_size == 4.0
    assert qty.value == 12000.0

