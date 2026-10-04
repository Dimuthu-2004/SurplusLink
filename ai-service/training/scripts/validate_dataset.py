#!/usr/bin/env python3
"""
Authoritative SurplusLink Dataset Validator & Quality Inspector.
Validates datasets against Pydantic schema, checks class balance, language distribution,
train/test leakage, and outputs reports/dataset_quality_report.json.
"""

import json
import os
import sys
from collections import Counter
from typing import Any, Dict, List

# Add parent directory to sys.path to import app modules
sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..")))

from app.assistant.semantic_schemas import Intent, SemanticRoutingResult


def validate_jsonl(filepath: str) -> Dict[str, Any]:
    if not os.path.exists(filepath):
        return {"error": f"File not found: {filepath}"}

    total = 0
    schema_valid = 0
    intents = []
    languages = []
    messages = set()

    with open(filepath, "r", encoding="utf-8") as f:
        for line_no, line in enumerate(f, 1):
            if not line.strip():
                continue
            total += 1
            try:
                row = json.loads(line)
                messages.add(row["meta"]["user_message"].strip().lower())
                target_json = row["meta"]["raw_target"]

                # Pydantic schema validation
                parsed = SemanticRoutingResult.model_validate(target_json)
                schema_valid += 1

                for intent_obj in parsed.intents:
                    intents.append(intent_obj.intent.value)
                    languages.append(intent_obj.response_language)

            except Exception as ex:
                print(f"Validation failure in {filepath} line {line_no}: {str(ex)}")

    return {
        "file": os.path.basename(filepath),
        "total_examples": total,
        "schema_valid_examples": schema_valid,
        "schema_valid_rate": schema_valid / total if total > 0 else 0.0,
        "intent_distribution": dict(Counter(intents)),
        "language_distribution": dict(Counter(languages)),
        "unique_messages": len(messages),
        "messages_set": messages,
    }


def main():
    base_dir = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
    dataset_dir = os.path.join(base_dir, "datasets")
    reports_dir = os.path.join(base_dir, "reports")
    os.makedirs(reports_dir, exist_ok=True)

    print("Running SurplusLink Dataset Quality Inspector...")

    train_report = validate_jsonl(os.path.join(dataset_dir, "train.jsonl"))
    val_report = validate_jsonl(os.path.join(dataset_dir, "val.jsonl"))
    test_report = validate_jsonl(os.path.join(dataset_dir, "test.jsonl"))
    challenge_report = validate_jsonl(os.path.join(dataset_dir, "challenge.jsonl"))

    # Leakage check: overlap between train and test messages
    train_msgs = train_report.get("messages_set", set())
    test_msgs = test_report.get("messages_set", set())
    overlap = train_msgs.intersection(test_msgs)

    # Clean up set objects before JSON dumping
    train_report.pop("messages_set", None)
    val_report.pop("messages_set", None)
    test_report.pop("messages_set", None)
    challenge_report.pop("messages_set", None)

    quality_summary = {
        "train": train_report,
        "val": val_report,
        "test": test_report,
        "challenge": challenge_report,
        "data_leakage": {
            "overlap_count": len(overlap),
            "leakage_rate": len(overlap) / len(test_msgs) if test_msgs else 0.0,
            "has_leakage": len(overlap) > 0,
        },
        "all_datasets_passed_schema": (
            train_report.get("schema_valid_rate", 0) >= 0.95
            and val_report.get("schema_valid_rate", 0) >= 0.95
            and test_report.get("schema_valid_rate", 0) >= 0.95
            and challenge_report.get("schema_valid_rate", 0) >= 0.95
        ),
    }

    report_path = os.path.join(reports_dir, "dataset_quality_report.json")
    with open(report_path, "w", encoding="utf-8") as f:
        json.dump(quality_summary, f, indent=2, ensure_ascii=False)

    print("\nDataset Quality Report Summary:")
    print(f"• Train Schema Validity: {train_report.get('schema_valid_rate', 0):.1%}")
    print(f"• Test Schema Validity: {test_report.get('schema_valid_rate', 0):.1%}")
    print(f"• Challenge Schema Validity: {challenge_report.get('schema_valid_rate', 0):.1%}")
    print(f"• Data Leakage Overlap: {len(overlap)} messages (Leakage Rate: {len(overlap)/len(test_msgs) if test_msgs else 0:.1%})")
    print(f"• Saved Quality Report to: {report_path}")

    if not quality_summary["all_datasets_passed_schema"]:
        print("\n[ERROR] Schema validity threshold (>= 95%) was not met!")
        sys.exit(1)


if __name__ == "__main__":
    main()
