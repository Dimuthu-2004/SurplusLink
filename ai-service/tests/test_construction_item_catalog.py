from app.catalog.item_templates import (
    CONSTRUCTION_ITEM_TEMPLATES,
    get_catalog_tool_definitions,
    resolve_phrase_to_template,
)


def test_catalog_has_representative_construction_templates():
    assert len(CONSTRUCTION_ITEM_TEMPLATES) >= 15
    names = {t.name for t in CONSTRUCTION_ITEM_TEMPLATES}
    assert "Paint" in names
    assert "Generator" in names
    assert "Cement" in names
    assert "Reinforcement Steel" in names
    assert "Sand" in names
    assert "Tiles" in names
    assert "Air Compressor" in names
    assert "Angle Grinder" in names
    assert "PVC Pipes" in names
    assert "Roofing Sheets" in names
    assert "Scaffolding" in names
    assert "Custom Construction Item" in names


def test_maps_user_phrases_to_templates():
    # 1. 5kVA diesel generator -> Generator
    match_gen = resolve_phrase_to_template("5kVA diesel generator")
    assert match_gen is not None
    assert match_gen.name == "Generator"
    assert match_gen.item_class == "EQUIPMENT"
    assert match_gen.quantity_mode == "PIECE"

    # 2. paint 10L -> Paint
    match_paint = resolve_phrase_to_template("paint 10L")
    assert match_paint is not None
    assert match_paint.name == "Paint"
    assert match_paint.item_class == "MATERIAL"
    assert match_paint.quantity_mode == "PACKAGE"
    assert match_paint.base_unit == "L"

    # 3. 12mm steel rods -> Reinforcement Steel
    match_steel = resolve_phrase_to_template("12mm steel rods")
    assert match_steel is not None
    assert match_steel.name == "Reinforcement Steel"
    assert match_steel.item_class == "MATERIAL"
    assert match_steel.quantity_mode == "PIECE"

    # Additional phrases
    match_sand = resolve_phrase_to_template("fine river sand 10 m3")
    assert match_sand is not None
    assert match_sand.name == "Sand"
    assert match_sand.quantity_mode == "CONTINUOUS_BULK"

    match_comp = resolve_phrase_to_template("50L air compressor")
    assert match_comp is not None
    assert match_comp.name == "Air Compressor"
    assert match_comp.item_class == "EQUIPMENT"

    match_grind = resolve_phrase_to_template("makita angle grinder 100mm")
    assert match_grind is not None
    assert match_grind.name == "Angle Grinder"
    assert match_grind.item_class == "TOOL"


def test_structured_tool_definitions_contain_authoritative_units():
    tools = get_catalog_tool_definitions()
    assert len(tools) >= 15
    for tool in tools:
        assert "name" in tool
        assert "quantityMode" in tool
        assert "baseUnit" in tool
        assert "attributes" in tool
        # LLMs must not guess modes: quantityMode must be one of the authoritative options
        assert tool["quantityMode"] in ("PACKAGE", "PIECE", "CONTINUOUS_BULK", "LENGTH", "AREA", "VOLUME")
