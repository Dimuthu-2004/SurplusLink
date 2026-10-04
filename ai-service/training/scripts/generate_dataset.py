#!/usr/bin/env python3
"""
Authoritative SurplusLink AI Assistant Dataset Generation Pipeline.
Generates 2,500+ balanced, structured training examples and 320+ challenge examples
strictly formatted as valid JSON matching Pydantic SemanticRoutingResult / CanonicalUserRequest.
"""

import json
import os
import random
from typing import Any, Dict, List

ROUTER_SYSTEM_PROMPT = """You are the semantic router for SurplusLink, a Sri Lankan construction reuse marketplace.
Classify meaning, never literal keywords. Return ONLY valid JSON matching this schema:

Output JSON Format:
{
  "intents": [
    {
      "intent": "CREATE_REQUIREMENT_DRAFT" | "CONTINUE_REQUIREMENT_DRAFT" | "MATERIAL_ESTIMATION" | "PRICE_INFORMATION" | "LIVE_MARKETPLACE_QUERY" | "LIVE_DATA_QUERY" | "TRANSACTION_QUERY" | "CONSTRUCTION_KNOWLEDGE" | "PLATFORM_HELP" | "UNIT_CONVERSION" | "MATCH_EXPLANATION" | "CLARIFICATION" | "SECURITY_REFUSAL" | "UNKNOWN",
      "confidence": float (0.0 to 1.0),
      "is_question": boolean,
      "is_purchase_request": boolean,
      "is_follow_up": boolean,
      "referenced_item": string or null,
      "extracted_slots": {
        "item": string or null,
        "quantity": {
          "value": float or null,
          "unit": string or null,
          "approximate": boolean,
          "package_count": integer or null,
          "package_size": float or null,
          "package_unit": string or null
        } or null,
        "preferences": {},
        "location_text": string or null,
        "notes": string or null,
        "live_resource": string or null,
        "referenced_id": string or null
      },
      "needs_clarification": boolean,
      "clarification_question": string or null,
      "retrieval_query": string or null,
      "response_language": "en" | "si" | "ta" | "si-Latn"
    }
  ]
}"""

# Real SurplusLink materials, Sri Lankan cities, units, colours
MATERIALS = ["Cement", "Paint", "Tiles", "Steel Rebar", "Concrete Blocks", "PVC Pipes", "Sand", "Bricks", "Timber", "Sealant", "Marble"]
CITIES = ["Colombo", "Kandy", "Malabe", "Gampaha", "Nugegoda", "Maharagama", "Moratuwa", "Homagama", "Kurunegala", "Jaffna", "Galle", "Negombo"]
COLOURS = ["White", "Off-White", "Yellow", "Grey", "Blue", "Green", "Terracotta"]
PAINT_TYPES = ["Emulsion", "Weather-shield", "Enamel", "Primer", "Undercoat", "Epoxy"]

SEED_TEMPLATES = [
    # 1. CREATE_REQUIREMENT_DRAFT
    {
        "intent": "CREATE_REQUIREMENT_DRAFT",
        "phrasings": [
            ("mata {colour} {item} {qty}{unit} one {city} walata", "si-Latn", False, True),
            ("i need {qty} {unit} of {item} in {city}", "en", False, True),
            ("heta site ekata {item} tikak hoyaganna puluwanda near {city}", "si-Latn", True, True),
            ("{city} walin {item} {qty}{unit} ganna thiyenawada", "si-Latn", True, True),
            ("{city} க்கு {qty} {unit} {item} வாங்க வேண்டும்", "ta", False, True),
            ("source {qty} {unit} {item} sacks for job in {city}", "en", False, True),
            ("mata {item} {qty} {unit} wage one", "si-Latn", False, True),
            ("{item} {qty} {unit} draft ekak danna {city} walata", "si-Latn", False, True),
            ("looking for {qty} {item} near {city}", "en", False, True),
            ("{city} langin {item} {qty}{unit} aran denta", "si-Latn", False, True),
        ],
    },
    # 2. MATERIAL_ESTIMATION
    {
        "intent": "MATERIAL_ESTIMATION",
        "phrasings": [
            ("{area}m2 area ekakata {item} kochchara ooneda", "si-Latn", True, False),
            ("work out the {item} needed for a {area} square metre wall", "en", True, False),
            ("room eka 12ft x 10ft height 9ft. {item} kochchara yaida?", "si-Latn", True, False),
            ("වර්ග මීටර් {area}කට {item} ප්‍රමාණය ගණන් කරන්න", "si", True, False),
            ("{area} சதுர மீட்டருக்கு எவ்வளவு {item} தேவை", "ta", True, False),
            ("{area}m2 wall ekakata {item} coats dekak gahanna litres keeyak yai da", "si-Latn", True, False),
            ("calculate required {item} for {area} sqm floor", "en", True, False),
            ("concrete cube ekaka {item} bags keeyak yaida", "si-Latn", True, False),
            ("plastering {area}m2 area ekakata {item} keeyada", "si-Latn", True, False),
            ("{dim} tiles {area}m2 ekakata box keeyak ooneda", "si-Latn", True, False),
        ],
    },
    # 3. PRICE_INFORMATION
    {
        "intent": "PRICE_INFORMATION",
        "phrasings": [
            ("{item} {qty}{unit} price keeyada normally?", "si-Latn", True, False),
            ("what is the going rate for {item} in Sri Lanka", "en", True, False),
            ("{item} sack ekaka ganan kohomada dan", "si-Latn", True, False),
            ("{item} කියුබ් එකක සාමාන්‍ය මිල දැනගන්න පුළුවන්ද", "si", True, False),
            ("ஒரு பெட்டி {item} விலை என்ன", "ta", True, False),
            ("{item} per unit average market price keeyada", "si-Latn", True, False),
            ("roughly what budget should I expect for {qty}{unit} {item}", "en", True, False),
            ("{item} price single bag rate keeyada", "si-Latn", True, False),
            ("what is the market price of {item} this week", "en", True, False),
            ("{item} ganan kohomada machan", "si-Latn", True, False),
        ],
    },
    # 4. LIVE_MARKETPLACE_QUERY
    {
        "intent": "LIVE_MARKETPLACE_QUERY",
        "phrasings": [
            ("site eke sellers lage {item} price kohomada", "si-Latn", True, False),
            ("show active {item} listings near {city}", "en", True, False),
            ("dan sellers lage thiyena items monada", "si-Latn", True, False),
            ("are there any sellers offering {item} in {city}", "en", True, False),
            ("{city} sellers laga {item} thiyenawada", "si-Latn", True, False),
            ("browse current marketplace listings for {item}", "en", True, False),
            ("available {item} sellers near {city}", "en", True, False),
            ("market eke thiyena lowest price {item}", "si-Latn", True, False),
            ("{city} walin {item} ganna active listings monada", "si-Latn", True, False),
            ("check active marketplace inventory for {item}", "en", True, False),
        ],
    },
    # 5. CONSTRUCTION_KNOWLEDGE
    {
        "intent": "CONSTRUCTION_KNOWLEDGE",
        "phrasings": [
            ("how to store {item} bags safely at site", "en", True, False),
            ("{item} tika safe widiyata tiyaganne kohomada", "si-Latn", True, False),
            ("{item} break wenne nathi widiyata transport karanne kohomada", "si-Latn", True, False),
            ("{item} මලකඩෙන් බේරාගන්නේ කොහොමද", "si", True, False),
            ("மீதமுள்ள {item} பாதுகாப்பாக வைப்பது எப்படி", "ta", True, False),
            ("is dampness harmful when {item} is kept for a while", "en", True, False),
            ("{item} apply karanna kalin primer gahannama ooneda", "si-Latn", True, False),
            ("what is the recommended curing time for C25 concrete", "en", True, False),
            ("{item} eliye thibbama monawada wenne", "si-Latn", True, False),
            ("best practices for handling {item} on jobsite", "en", True, False),
        ],
    },
    # 6. PLATFORM_HELP
    {
        "intent": "PLATFORM_HELP",
        "phrasings": [
            ("walk me through posting unused building supplies", "en", True, False),
            ("seller kenek widiyata listing ekak danne kohomada", "si-Latn", True, False),
            ("ගැලපීමක් ලැබුණාට පස්සේ මොකද වෙන්නේ", "si", True, False),
            ("SurplusLink இல் வாங்குபவர் கோரிக்கை செய்வது எப்படி", "ta", True, False),
            ("where do I confirm that the handover happened", "en", True, False),
            ("how does buyer and seller matching work on SurplusLink", "en", True, False),
            ("what are the payment approval rules for buyers", "en", True, False),
            ("how to cancel a requirement draft on mobile app", "en", True, False),
            ("what happens when a seller accepts my offer", "en", True, False),
            ("how to leave feedback for seller after transaction", "en", True, False),
        ],
    },
    # 7. LIVE_DATA_QUERY & TRANSACTION_QUERY & MATCH_EXPLANATION
    {
        "intent": "LIVE_DATA_QUERY",
        "phrasings": [
            ("show the current state of the offers on my account", "en", True, False),
            ("mage active listings monawada balanna", "si-Latn", True, False),
            ("මගේ ගනුදෙනු වල අලුත්ම තත්ත්වය පෙන්වන්න", "si", True, False),
            ("எனது திறந்த கோரிக்கைகளை காட்டு", "ta", True, False),
            ("did any of my marketplace offers get accepted", "en", True, False),
            ("show my active requirement drafts", "en", True, False),
            ("mage past transactions list eka pennanna", "si-Latn", True, False),
            ("check my pending matches", "en", True, False),
            ("my current listings overview", "en", True, False),
            ("explain why match score was 95% for recommendation #1", "en", True, False),
        ],
    },
    # 8. UNIT_CONVERSION
    {
        "intent": "UNIT_CONVERSION",
        "phrasings": [
            ("convert {qty} sqft into square meters", "en", True, False),
            ("500kg කියන්නේ cement bags කීයක්ද", "si", True, False),
            ("100 sqft m2 kiyada", "si-Latn", True, False),
            ("convert {qty} litres to gallons", "en", True, False),
            ("how many 50kg bags in 1 metric ton", "en", True, False),
            ("12m2 feet walata harawanna", "si-Latn", True, False),
            ("{qty} sqm equal to how many sqft", "en", True, False),
            ("1 cube of sand in cubic meters", "en", True, False),
            ("50kg bags 10k in total tonnes", "en", True, False),
            ("600x600 tile size in inches", "en", True, False),
        ],
    },
    # 9. CLARIFICATION & AMBIGUOUS
    {
        "intent": "CLARIFICATION",
        "phrasings": [
            ("paint price", "en", False, False),
            ("10k", "en", False, False),
            ("site eka", "si-Latn", False, False),
            ("50kg ne?", "si-Latn", True, False),
            ("tile kohomada", "si-Latn", True, False),
            ("paint", "en", False, False),
            ("cement 5", "en", False, False),
            ("normal eka", "si-Latn", False, False),
            ("ow eka hari", "si-Latn", False, False),
            ("na bn requirement ekak daanna one", "si-Latn", False, False),
        ],
    },
]


def generate_single_example(intent_cfg: Dict[str, Any], idx: int) -> Dict[str, Any]:
    intent = intent_cfg["intent"]
    phrasings = intent_cfg["phrasings"]
    tpl, lang, is_q, is_p = random.choice(phrasings)

    item = random.choice(MATERIALS)
    city = random.choice(CITIES)
    colour = random.choice(COLOURS)
    paint_type = random.choice(PAINT_TYPES)
    qty = random.choice([5, 10, 20, 50, 100, 500, 1000])
    area = random.choice([10, 12, 15, 20, 25, 50, 100])
    unit = random.choice(["L", "kg", "bags", "m2", "boxes", "cubes", "pcs"])
    dim = random.choice(["600x600", "300x300", "300x600", "2x2 ft"])

    user_msg = tpl.format(
        item=item,
        city=city,
        colour=colour,
        paint_type=paint_type,
        qty=qty,
        area=area,
        unit=unit,
        dim=dim,
    )

    # Optional noisy Sri Lankan typos (15% ratio)
    if random.random() < 0.15:
        user_msg = user_msg.replace("paint", "paitn").replace("cement", "cemnt").replace("sealant", "sealent").replace("one", "oone")

    slots: Dict[str, Any] = {
        "item": item if intent not in ["PLATFORM_HELP", "UNIT_CONVERSION", "CLARIFICATION"] else None,
        "quantity": {"value": float(qty), "unit": unit} if is_p or "qty" in tpl else None,
        "preferences": {"colour": colour} if "colour" in tpl else {},
        "location_text": city if is_p or "{city}" in tpl else None,
    }

    if intent in ["LIVE_DATA_QUERY", "TRANSACTION_QUERY"]:
        slots["live_resource"] = "offers" if "offers" in user_msg else "listings"

    res_json = {
        "intents": [
            {
                "intent": intent,
                "confidence": round(random.uniform(0.92, 0.99), 2),
                "is_question": is_q,
                "is_purchase_request": is_p,
                "is_follow_up": False,
                "referenced_item": item if intent not in ["PLATFORM_HELP", "CLARIFICATION"] else None,
                "extracted_slots": slots,
                "needs_clarification": intent == "CLARIFICATION" or (intent == "MATERIAL_ESTIMATION" and "type" not in user_msg.lower()),
                "clarification_question": "To calculate required paint, please specify paint type and surface area." if intent == "MATERIAL_ESTIMATION" else None,
                "retrieval_query": user_msg if intent in ["CONSTRUCTION_KNOWLEDGE", "PLATFORM_HELP"] else None,
                "response_language": lang,
            }
        ]
    }

    # Format into OpenAI / Groq chat completion JSONL format
    chat_example = {
        "id": f"sl-semantic-{idx:05d}",
        "messages": [
            {"role": "system", "content": ROUTER_SYSTEM_PROMPT},
            {"role": "user", "content": json.dumps({"conversation": {}, "message": user_msg}, ensure_ascii=False)},
            {"role": "assistant", "content": json.dumps(res_json, ensure_ascii=False)},
        ],
        "meta": {
            "intent": intent,
            "language": lang,
            "user_message": user_msg,
            "raw_target": res_json,
        },
    }
    return chat_example


def generate_multi_turn_and_adversarial_examples(start_idx: int) -> List[Dict[str, Any]]:
    examples = []
    idx = start_idx

    # Multi-turn Follow-Up Example
    t1_msg = "cement bags 10k one malabe"
    t1_target = {
        "intents": [
            {
                "intent": "CREATE_REQUIREMENT_DRAFT",
                "confidence": 0.98,
                "is_question": False,
                "is_purchase_request": True,
                "is_follow_up": False,
                "referenced_item": "Cement",
                "extracted_slots": {
                    "item": "Cement",
                    "quantity": {"value": 10000.0, "unit": "bag", "package_count": 10000},
                    "location_text": "Malabe",
                },
                "needs_clarification": True,
                "response_language": "si-Latn",
            }
        ]
    }
    examples.append({
        "id": f"sl-semantic-{idx:05d}",
        "messages": [
            {"role": "system", "content": ROUTER_SYSTEM_PROMPT},
            {"role": "user", "content": json.dumps({"conversation": {}, "message": t1_msg}, ensure_ascii=False)},
            {"role": "assistant", "content": json.dumps(t1_target, ensure_ascii=False)},
        ],
        "meta": {"intent": "CREATE_REQUIREMENT_DRAFT", "language": "si-Latn", "user_message": t1_msg, "raw_target": t1_target},
    })
    idx += 1

    t2_msg = "50kg"
    t2_target = {
        "intents": [
            {
                "intent": "CONTINUE_REQUIREMENT_DRAFT",
                "confidence": 0.97,
                "is_question": False,
                "is_purchase_request": True,
                "is_follow_up": True,
                "referenced_item": "Cement",
                "extracted_slots": {
                    "item": "Cement",
                    "quantity": {"value": 50.0, "unit": "kg", "package_size": 50.0, "package_unit": "kg"},
                    "location_text": "Malabe",
                },
                "needs_clarification": False,
                "response_language": "si-Latn",
            }
        ]
    }
    examples.append({
        "id": f"sl-semantic-{idx:05d}",
        "messages": [
            {"role": "system", "content": ROUTER_SYSTEM_PROMPT},
            {
                "role": "user",
                "content": json.dumps(
                    {
                        "conversation": {
                            "activeRequirementDraft": {"itemName": "Cement", "locationText": "Malabe", "packageCount": 10000},
                            "lastReferencedItem": "Cement",
                        },
                        "message": t2_msg,
                    },
                    ensure_ascii=False,
                ),
            },
            {"role": "assistant", "content": json.dumps(t2_target, ensure_ascii=False)},
        ],
        "meta": {"intent": "CONTINUE_REQUIREMENT_DRAFT", "language": "si-Latn", "user_message": t2_msg, "raw_target": t2_target},
    })
    idx += 1

    # Topic Switch Adversarial Example
    ts_msg = "dan sellers lage thiyena items monada?"
    ts_target = {
        "intents": [
            {
                "intent": "LIVE_MARKETPLACE_QUERY",
                "confidence": 0.96,
                "is_question": True,
                "is_purchase_request": False,
                "is_follow_up": False,
                "referenced_item": None,
                "extracted_slots": {"item": None},
                "needs_clarification": False,
                "response_language": "si-Latn",
            }
        ]
    }
    examples.append({
        "id": f"sl-semantic-{idx:05d}",
        "messages": [
            {"role": "system", "content": ROUTER_SYSTEM_PROMPT},
            {
                "role": "user",
                "content": json.dumps(
                    {
                        "conversation": {
                            "activeRequirementDraft": {"itemName": "Cement", "locationText": "Malabe"},
                            "lastReferencedItem": "Cement",
                        },
                        "message": ts_msg,
                    },
                    ensure_ascii=False,
                ),
            },
            {"role": "assistant", "content": json.dumps(ts_target, ensure_ascii=False)},
        ],
        "meta": {"intent": "LIVE_MARKETPLACE_QUERY", "language": "si-Latn", "user_message": ts_msg, "raw_target": ts_target},
    })
    idx += 1

    # Correction Example
    corr_msg = "na na mata ganna newei price eka danaganna one"
    corr_target = {
        "intents": [
            {
                "intent": "PRICE_INFORMATION",
                "confidence": 0.95,
                "is_question": True,
                "is_purchase_request": False,
                "is_follow_up": True,
                "referenced_item": "Paint",
                "extracted_slots": {"item": "Paint"},
                "needs_clarification": False,
                "response_language": "si-Latn",
            }
        ]
    }
    examples.append({
        "id": f"sl-semantic-{idx:05d}",
        "messages": [
            {"role": "system", "content": ROUTER_SYSTEM_PROMPT},
            {
                "role": "user",
                "content": json.dumps(
                    {
                        "conversation": {"lastReferencedItem": "Paint"},
                        "message": corr_msg,
                    },
                    ensure_ascii=False,
                ),
            },
            {"role": "assistant", "content": json.dumps(corr_target, ensure_ascii=False)},
        ],
        "meta": {"intent": "PRICE_INFORMATION", "language": "si-Latn", "user_message": corr_msg, "raw_target": corr_target},
    })

    return examples


def main():
    target_dir = os.path.join(os.path.dirname(__file__), "..", "datasets")
    os.makedirs(target_dir, exist_ok=True)

    print("Generating SurplusLink fine-tuning dataset...")
    random.seed(42)

    total_target = 2500
    all_examples = []

    # Generate balanced main dataset with unique user messages to eliminate data leakage
    idx = 1
    seen_messages = set()
    all_examples = []

    while len(all_examples) < total_target:
        for cfg in SEED_TEMPLATES:
            ex = generate_single_example(cfg, idx)
            msg_key = ex["meta"]["user_message"].strip().lower()
            if msg_key not in seen_messages:
                seen_messages.add(msg_key)
                all_examples.append(ex)
                idx += 1
            if len(all_examples) >= total_target:
                break

    # Append multi-turn & adversarial examples
    special_examples = generate_multi_turn_and_adversarial_examples(idx)
    for ex in special_examples:
        msg_key = ex["meta"]["user_message"].strip().lower()
        if msg_key not in seen_messages:
            seen_messages.add(msg_key)
            all_examples.append(ex)

    # Shuffle deterministically
    random.shuffle(all_examples)

    # Split into 80% train, 10% val, 10% test
    n_total = len(all_examples)
    n_train = int(n_total * 0.80)
    n_val = int(n_total * 0.10)

    train_data = all_examples[:n_train]
    val_data = all_examples[n_train : n_train + n_val]
    test_data = all_examples[n_train + n_val :]

    # Save JSONL datasets
    def save_jsonl(filename: str, data: List[Dict[str, Any]]):
        path = os.path.join(target_dir, filename)
        with open(path, "w", encoding="utf-8") as f:
            for item in data:
                f.write(json.dumps(item, ensure_ascii=False) + "\n")
        print(f"Saved {len(data)} items to {path}")

    save_jsonl("train.jsonl", train_data)
    save_jsonl("val.jsonl", val_data)
    save_jsonl("test.jsonl", test_data)

    # Generate 320 item Challenge Set with ZERO overlap against train/val/test
    challenge_data = []
    c_idx = 1
    while len(challenge_data) < 320:
        cfg = random.choice(SEED_TEMPLATES)
        ex = generate_single_example(cfg, 10000 + c_idx)
        msg_key = ex["meta"]["user_message"].strip().lower()
        c_idx += 1
        if msg_key not in seen_messages:
            seen_messages.add(msg_key)
            challenge_data.append(ex)
    save_jsonl("challenge.jsonl", challenge_data)

    print(f"\nDataset generation completed! Total: {n_total} main examples + {len(challenge_data)} challenge examples.")


if __name__ == "__main__":
    main()
