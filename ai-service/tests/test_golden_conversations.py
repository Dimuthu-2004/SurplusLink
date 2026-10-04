import pytest
from app.assistant.canonical_request import CanonicalUserRequest
from app.assistant.item_resolver import ItemResolver
from app.assistant.semantic_schemas import Intent, QuantitySlots, RequirementDraft
from app.assistant.state_machine import AssistantState, ConversationStateMachine
from app.assistant.unit_normalizer import QuantityNormalizer
from app.assistant.validation_gate import ValidationGate


def test_scenario_1_tiles_dimensions_and_area():
    """
    Scenario 1: 'I need 600x600 mm tiles for 12m2'
    -> dimensions 600x600mm, area 12sqm, no unit rejection
    """
    msg = "I need 600x600 mm tiles for 12m2"
    norm = QuantityNormalizer.parse_quantity_structure(msg)
    assert norm is not None
    assert norm.dimensions is not None
    assert norm.dimensions.width == 600
    assert norm.dimensions.length == 600
    assert norm.dimensions.unit == "mm"
    assert norm.coverage_area_m2 == pytest.approx(12.0)


def test_scenario_2_cement_bags_and_package_size_merging():
    """
    Scenario 2: 'I need 10 bags of cement in Malabe' then '50kg'
    -> 10 bags, 50kg/bag, 500kg total, Malabe retained
    """
    sm = ConversationStateMachine()

    req1 = CanonicalUserRequest(
        raw_message="I need 10 bags of cement in Malabe",
        intent=Intent.CREATE_REQUIREMENT_DRAFT,
        item_candidate="Cement",
        location="Malabe",
        package_count=10,
        package_unit="bag",
    )
    sm.process_request(req1)
    assert sm.known_fields["package_count"] == 10
    assert sm.known_fields["location_text"] == "Malabe"

    req2 = CanonicalUserRequest(
        raw_message="50kg",
        intent=Intent.CONTINUE_REQUIREMENT_DRAFT,
        is_follow_up=True,
        package_size=50.0,
        package_unit="kg",
    )
    sm.process_request(req2)
    assert sm.known_fields["package_count"] == 10
    assert sm.known_fields["package_size"] == 50.0
    assert sm.known_fields["location_text"] == "Malabe"


def test_scenario_3_live_marketplace_price_provenance():
    """
    Scenario 3: 'What is the current price of 10L paint?'
    -> live marketplace query / price info intent, requiring live tool provenance
    """
    req = CanonicalUserRequest(
        raw_message="What is the current price of 10L paint?",
        intent=Intent.PRICE_INFORMATION,
        is_question=True,
        item_candidate="Paint",
        package_size=10.0,
        package_unit="L",
    )
    assert req.intent in {Intent.PRICE_INFORMATION, Intent.LIVE_MARKETPLACE_QUERY}
    # Prove that empty tool results fail validation gate for price claims
    val = ValidationGate.validate_marketplace_provenance(req.intent, [], "LKR 5,000")
    assert val is False


def test_scenario_4_estimation_continuation_without_fallback():
    """
    Scenario 4: 'How much paint do I need for a 10sqm wall?' then 'interior emulsion, 2 coats'
    -> same estimation context continues
    """
    sm = ConversationStateMachine()

    req1 = CanonicalUserRequest(
        raw_message="How much paint do I need for a 10sqm wall?",
        intent=Intent.MATERIAL_ESTIMATION,
        item_candidate="Paint",
        coverage_area=10.0,
    )
    sm.process_request(req1)
    assert sm.current_state in {AssistantState.ESTIMATION_COLLECTING, AssistantState.ESTIMATION_COLLECTING_INPUTS}
    assert sm.known_fields["coverage_area"] == 10.0

    req2 = CanonicalUserRequest(
        raw_message="interior emulsion, 2 coats",
        intent=Intent.MATERIAL_ESTIMATION,
        is_follow_up=True,
        preferences={"paint_type": "Emulsion", "coats": "2"},
    )
    sm.process_request(req2)
    assert sm.current_state in {AssistantState.ESTIMATION_COLLECTING, AssistantState.ESTIMATION_COLLECTING_INPUTS}
    assert sm.known_fields["coverage_area"] == 10.0
    assert sm.known_fields["preferences"]["paint_type"] == "Emulsion"


def test_scenario_5_open_vocabulary_water_pump():
    """
    Scenario 5: 'I want a water pump'
    -> Water Pump resolved as valid custom item candidate, not rejected
    """
    resolver = ItemResolver()
    resolved = resolver.resolve("water pump")
    assert resolved is not None
    assert resolved.is_custom_item is True
    assert resolved.display_name == "Water Pump"


def test_scenario_6_multi_draft_generator_and_water_pump():
    """
    Scenario 6: 'I want a generator' then '2' then 'to my current location' then 'also need a water pump'
    -> Generator draft preserved (Draft A)
    -> NEW Water Pump draft created (Draft B)
    """
    sm = ConversationStateMachine()

    # Turn 1: Generator
    req1 = CanonicalUserRequest(
        raw_message="I want a generator",
        intent=Intent.CREATE_REQUIREMENT_DRAFT,
        item_candidate="Generator",
        resolved_item="Generator",
        is_custom_item=True,
    )
    sm.process_request(req1)
    draft1 = RequirementDraft(id="d1", template_id="CUSTOM_ITEM", category_id="GENERAL", item_name="Generator", display_name="Generator", is_custom_item=True, input_mode="BASE_QUANTITY", normalized_base_unit="piece")
    sm.set_active_draft(draft1)

    # Turn 2: Quantity = 2
    req2 = CanonicalUserRequest(raw_message="2", intent=Intent.CONTINUE_REQUIREMENT_DRAFT, is_follow_up=True, quantity=QuantitySlots(value=2.0))
    sm.process_request(req2)
    draft1.entered_quantity = 2.0
    draft1.normalized_quantity = 2.0
    sm.set_active_draft(draft1)

    # Turn 3: Location = Colombo
    req3 = CanonicalUserRequest(raw_message="Colombo", intent=Intent.CONTINUE_REQUIREMENT_DRAFT, is_follow_up=True, location="Colombo")
    sm.process_request(req3)
    draft1.location_text = "Colombo"
    draft1.ready_for_review = True
    sm.set_active_draft(draft1)

    assert len(sm.drafts) == 1
    assert sm.drafts[0].item_name == "Generator"
    assert sm.drafts[0].ready_for_review is True

    # Turn 4: Additive request "also need a water pump"
    req4 = CanonicalUserRequest(
        raw_message="also need a water pump",
        intent=Intent.CREATE_REQUIREMENT_DRAFT,
        item_candidate="Water Pump",
        resolved_item="Water Pump",
        is_custom_item=True,
    )
    sm.process_request(req4)
    draft2 = RequirementDraft(id="d2", template_id="CUSTOM_ITEM", category_id="GENERAL", item_name="Water Pump", display_name="Water Pump", is_custom_item=True, input_mode="BASE_QUANTITY", normalized_base_unit="piece")
    sm.set_active_draft(draft2)

    assert len(sm.drafts) == 2
    assert sm.drafts[0].item_name == "Generator"
    assert sm.drafts[1].item_name == "Water Pump"
    assert sm.active_draft_id == "d2"


def test_scenario_7_broad_marketplace_query():
    """
    Scenario 7: 'What items are currently available from sellers?'
    -> broad live query, no stale item inherited
    """
    sm = ConversationStateMachine()
    sm.last_item_candidate = "Paint"

    req = CanonicalUserRequest(
        raw_message="What items are currently available from sellers?",
        intent=Intent.LIVE_MARKETPLACE_QUERY,
        is_follow_up=False,
    )
    st = sm.process_request(req)
    assert st == AssistantState.LIVE_QUERY
    assert sm.last_item_candidate is None


def test_scenario_8_marble_in_gampaha():
    """
    Scenario 8: 'Is marble available in Gampaha?'
    -> Marble + Gampaha recognized for live listing search
    """
    resolver = ItemResolver()
    resolved = resolver.resolve("marble")
    assert resolved is not None
    assert resolved.display_name == "Marble"

    loc = ValidationGate.validate_location("Gampaha")
    assert loc == "Gampaha"

