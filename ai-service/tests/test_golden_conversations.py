import pytest
from app.assistant.canonical_request import CanonicalUserRequest
from app.assistant.semantic_schemas import Intent, QuantitySlots, RequirementDraft
from app.assistant.state_machine import AssistantState, ConversationStateMachine
from app.assistant.unit_normalizer import QuantityNormalizer


def test_golden_cement_requirement_multiturn():
    """
    Golden Multi-turn Test 1:
    Turn 1: "cement bags 10k one malabe"
    State Machine merges: Cement, 10,000 bags, Malabe. Awaiting package size.
    Turn 2: "50kg"
    State Machine merges: package_size=50kg, total=500,000kg.
    """
    sm = ConversationStateMachine()

    # Turn 1
    t1_qty = QuantityNormalizer.parse_quantity_structure("10k bags")
    req1 = CanonicalUserRequest(
        raw_message="cement bags 10k one malabe",
        intent=Intent.CREATE_REQUIREMENT_DRAFT,
        item_candidate="Cement",
        location="Malabe",
        quantity=QuantitySlots(value=t1_qty.value, unit=t1_qty.unit, package_count=t1_qty.package_count),
        package_count=10000,
    )
    st1 = sm.process_request(req1)
    assert st1 == AssistantState.REQUIREMENT_COLLECTING_INPUTS
    assert sm.known_fields["item"] == "Cement"
    assert sm.known_fields["package_count"] == 10000
    assert sm.known_fields["location_text"] == "Malabe"

    # Turn 2
    t2_qty = QuantityNormalizer.parse_quantity_structure("50kg")
    req2 = CanonicalUserRequest(
        raw_message="50kg",
        intent=Intent.CONTINUE_REQUIREMENT_DRAFT,
        is_follow_up=True,
        package_size=50.0,
        package_unit="kg",
        quantity=QuantitySlots(value=50.0, unit="kg"),
    )
    st2 = sm.process_request(req2)
    assert st2 == AssistantState.REQUIREMENT_COLLECTING_INPUTS

    # Merged assertions
    assert sm.known_fields["item"] == "Cement"
    assert sm.known_fields["package_count"] == 10000
    assert sm.known_fields["location_text"] == "Malabe"
    assert sm.known_fields["package_size"] == 50.0
    assert sm.known_fields["package_unit"] == "kg"


def test_golden_paint_estimation_multiturn():
    """
    Golden Multi-turn Test 2:
    Turn 1: "10m2 wall ekakata paint kochchara yaida" -> collects area=10m2
    Turn 2: "interior emulsion" -> merges paint_type="emulsion"
    """
    sm = ConversationStateMachine()

    # Turn 1
    t1_area = QuantityNormalizer.parse_area("10m2 wall ekakata paint kochchara yaida")
    req1 = CanonicalUserRequest(
        raw_message="10m2 wall ekakata paint kochchara yaida",
        intent=Intent.MATERIAL_ESTIMATION,
        item_candidate="Paint",
        coverage_area=t1_area[0] if t1_area else 10.0,
    )
    st1 = sm.process_request(req1)
    assert st1 == AssistantState.ESTIMATION_COLLECTING_INPUTS
    assert sm.known_fields["item"] == "Paint"
    assert sm.known_fields["coverage_area"] == 10.0

    # Turn 2
    req2 = CanonicalUserRequest(
        raw_message="interior emulsion",
        intent=Intent.MATERIAL_ESTIMATION,
        is_follow_up=True,
        preferences={"paint_type": "Emulsion"},
    )
    st2 = sm.process_request(req2)
    assert st2 == AssistantState.ESTIMATION_COLLECTING_INPUTS

    # Merged assertions
    assert sm.known_fields["item"] == "Paint"
    assert sm.known_fields["coverage_area"] == 10.0
    assert sm.known_fields["preferences"]["paint_type"] == "Emulsion"


def test_golden_topic_switch_reset():
    """
    Golden Multi-turn Test 3:
    Turn 1: Cement requirement draft active
    Turn 2: User asks "danata sellers lage thiyena items monada"
    State Machine resets requirement draft context for the broad marketplace query.
    """
    sm = ConversationStateMachine()
    sm.active_requirement_draft = RequirementDraft(
        template_id="CEMENT",
        category_id="CAT_CEMENT",
        item_name="Cement",
        input_mode="PACKAGE_COUNT",
        normalized_base_unit="bag",
    )
    sm.current_state = AssistantState.REQUIREMENT_COLLECTING_INPUTS
    sm.last_item_candidate = "Cement"

    req2 = CanonicalUserRequest(
        raw_message="danata sellers lage thiyena items monada",
        intent=Intent.LIVE_MARKETPLACE_QUERY,
        is_follow_up=False,
    )
    st2 = sm.process_request(req2)

    assert st2 == AssistantState.LIVE_QUERY
    assert sm.active_requirement_draft is None
    assert sm.last_item_candidate is None
