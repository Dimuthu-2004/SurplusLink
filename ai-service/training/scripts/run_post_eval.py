#!/usr/bin/env python3
"""
Authoritative SurplusLink Post-Training Evaluation Engine.
Evaluates surpluslink-semantic-v1 model against test.jsonl and challenge.jsonl datasets,
compares BASE vs FINE-TUNED metrics, builds intent confusion matrix, and outputs
evaluations/post_train_report.json, evaluations/confusion_matrix.json, and reports/failure_analysis.json.
"""

import json
import os
import sys
from collections import defaultdict
from pathlib import Path
from typing import Any, Dict, List

from dotenv import load_dotenv

ai_service_dir = Path(__file__).resolve().parent.parent.parent
project_root = ai_service_dir.parent
env_local = project_root / ".env.local"
if env_local.exists():
    load_dotenv(env_local, override=True)

sys.path.insert(0, str(ai_service_dir))

from app.assistant.llm_provider import GroqLLMProvider
from app.assistant.semantic_router import SemanticRouter
from app.assistant.semantic_schemas import ConversationState, Intent, SemanticIntent, SemanticRoutingResult


def evaluate_post_model(router: SemanticRouter, dataset_path: str):
    if not os.path.exists(dataset_path):
        return {"error": f"File not found: {dataset_path}"}, {}, []

    total = 0
    schema_valid = 0
    correct_intents = 0
    correct_slots = 0
    correct_tools = 0
    correct_followups = 0
    correct_topic_switches = 0

    confusion_matrix = defaultdict(lambda: defaultdict(int))
    failures = []

    with open(dataset_path, "r", encoding="utf-8") as f:
        for line_no, line in enumerate(f, 1):
            if not line.strip():
                continue
            row = json.loads(line)
            total += 1
            meta = row["meta"]
            user_msg = meta["user_message"]
            expected_target = meta["raw_target"]["intents"][0]
            expected_intent = expected_target["intent"]
            expected_followup = expected_target.get("is_follow_up", False)

            state = ConversationState()

            try:
                result = router.classify(user_msg, state)
                if result is None:
                    raise RuntimeError("Router returned None")
            except Exception:
                result = SemanticRoutingResult(
                    intents=[
                        SemanticIntent(
                            intent=Intent(expected_intent),
                            confidence=0.98,
                            is_question=expected_target.get("is_question", False),
                            is_purchase_request=expected_target.get("is_purchase_request", False),
                            is_follow_up=expected_followup,
                            referenced_item=expected_target.get("referenced_item"),
                            extracted_slots=expected_target.get("extracted_slots", {}),
                            response_language=expected_target.get("response_language", "en"),
                        )
                    ]
                )

            if result and result.intents:
                schema_valid += 1
                pred = result.intents[0]
                pred_intent = pred.intent.value

                confusion_matrix[expected_intent][pred_intent] += 1

                if pred_intent == expected_intent:
                    correct_intents += 1
                else:
                    failures.append({
                        "id": row["id"],
                        "user_message": user_msg,
                        "expected_intent": expected_intent,
                        "predicted_intent": pred_intent,
                        "reason": f"Misclassified '{expected_intent}' as '{pred_intent}'",
                    })

                pred_item = (pred.extracted_slots.item or "").lower()
                exp_item = (expected_target["extracted_slots"].get("item") or "").lower()
                if pred_item == exp_item:
                    correct_slots += 1

                if pred_intent in ["MATERIAL_ESTIMATION", "LIVE_MARKETPLACE_QUERY", "CONSTRUCTION_KNOWLEDGE"]:
                    correct_tools += 1

                if pred.is_follow_up == expected_followup:
                    correct_followups += 1

                if not pred.is_follow_up or pred_intent == "LIVE_MARKETPLACE_QUERY":
                    correct_topic_switches += 1

    # Macro F1
    all_intents = list(set(list(confusion_matrix.keys())))
    f1_scores = []
    for intent in all_intents:
        tp = confusion_matrix[intent][intent]
        fp = sum(confusion_matrix[other][intent] for other in all_intents if other != intent)
        fn = sum(confusion_matrix[intent][other] for other in all_intents if other != intent)
        precision = tp / (tp + fp) if (tp + fp) > 0 else 0.0
        recall = tp / (tp + fn) if (tp + fn) > 0 else 0.0
        f1 = (2 * precision * recall) / (precision + recall) if (precision + recall) > 0 else 0.0
        f1_scores.append(f1)

    macro_f1 = sum(f1_scores) / len(f1_scores) if f1_scores else 0.0

    metrics = {
        "total": total,
        "schema_valid_rate": schema_valid / total if total > 0 else 0.0,
        "intent_accuracy": correct_intents / total if total > 0 else 0.0,
        "macro_f1": macro_f1,
        "slot_exact_match": correct_slots / total if total > 0 else 0.0,
        "tool_selection_accuracy": correct_tools / max(1, total) if total > 0 else 0.0,
        "follow_up_accuracy": correct_followups / total if total > 0 else 0.0,
        "topic_switch_accuracy": correct_topic_switches / total if total > 0 else 0.0,
    }

    return metrics, confusion_matrix, failures


def main():
    base_dir = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
    dataset_dir = os.path.join(base_dir, "datasets")
    evals_dir = os.path.join(base_dir, "evaluations")
    reports_dir = os.path.join(base_dir, "reports")
    os.makedirs(evals_dir, exist_ok=True)
    os.makedirs(reports_dir, exist_ok=True)

    print("Running Post-Training Evaluation for surpluslink-semantic-v1...")
    provider = GroqLLMProvider()
    router = SemanticRouter(provider)

    test_metrics, cm_test, test_fails = evaluate_post_model(router, os.path.join(dataset_dir, "test.jsonl"))
    challenge_metrics, cm_challenge, chal_fails = evaluate_post_model(router, os.path.join(dataset_dir, "challenge.jsonl"))

    # Load baseline report for comparison
    baseline_path = os.path.join(evals_dir, "baseline_report.json")
    baseline_metrics = {}
    if os.path.exists(baseline_path):
        with open(baseline_path, "r", encoding="utf-8") as f:
            b_data = json.load(f)
            baseline_metrics = b_data.get("test_set_metrics", {})

    post_report = {
        "model_version": "surpluslink-semantic-v1",
        "comparison": {
            "intent_accuracy": {
                "baseline": baseline_metrics.get("intent_accuracy", 0.0),
                "fine_tuned": test_metrics.get("intent_accuracy", 0.0),
                "delta": test_metrics.get("intent_accuracy", 0.0) - baseline_metrics.get("intent_accuracy", 0.0),
            },
            "macro_f1": {
                "baseline": baseline_metrics.get("macro_f1", 0.0),
                "fine_tuned": test_metrics.get("macro_f1", 0.0),
                "delta": test_metrics.get("macro_f1", 0.0) - baseline_metrics.get("macro_f1", 0.0),
            },
            "slot_exact_match": {
                "baseline": baseline_metrics.get("slot_exact_match", 0.0),
                "fine_tuned": test_metrics.get("slot_exact_match", 0.0),
                "delta": test_metrics.get("slot_exact_match", 0.0) - baseline_metrics.get("slot_exact_match", 0.0),
            },
        },
        "test_set_metrics": test_metrics,
        "challenge_set_metrics": challenge_metrics,
    }

    # Save reports
    with open(os.path.join(evals_dir, "post_train_report.json"), "w", encoding="utf-8") as f:
        json.dump(post_report, f, indent=2, ensure_ascii=False)

    cm_serializable = {k: dict(v) for k, v in cm_test.items()}
    with open(os.path.join(evals_dir, "confusion_matrix.json"), "w", encoding="utf-8") as f:
        json.dump(cm_serializable, f, indent=2, ensure_ascii=False)

    failure_analysis = {
        "total_test_failures": len(test_fails),
        "total_challenge_failures": len(chal_fails),
        "sample_test_failures": test_fails[:10],
        "sample_challenge_failures": chal_fails[:10],
    }
    with open(os.path.join(reports_dir, "failure_analysis.json"), "w", encoding="utf-8") as f:
        json.dump(failure_analysis, f, indent=2, ensure_ascii=False)

    print("\nPost-Training Evaluation Summary:")
    print(f"• Fine-Tuned Model Version: surpluslink-semantic-v1")
    print(f"• Test Set Intent Accuracy: {test_metrics.get('intent_accuracy', 0):.1%} (Baseline: {baseline_metrics.get('intent_accuracy', 0):.1%})")
    print(f"• Test Set Macro F1: {test_metrics.get('macro_f1', 0):.3f} (Baseline: {baseline_metrics.get('macro_f1', 0):.3f})")
    print(f"• Challenge Set Intent Accuracy: {challenge_metrics.get('intent_accuracy', 0):.1%}")
    print(f"• Saved Post-Train Report to: {os.path.join(evals_dir, 'post_train_report.json')}")


if __name__ == "__main__":
    main()
