"""Run the held-out Groq router evaluation and emit a reproducible JSON report."""
from __future__ import annotations

import json
from pathlib import Path

from dotenv import load_dotenv

from app.assistant.llm_provider import GroqLLMProvider
from app.assistant.semantic_router import SemanticRouter
from app.assistant.semantic_schemas import ConversationState, Intent
from tests.evaluation_corpus import CORPUS, NEGATIVE_REQUIREMENT_CASES


def main() -> int:
    load_dotenv(Path(__file__).resolve().parents[1] / ".env.local")
    provider = GroqLLMProvider()
    if not provider.is_available():
        print("Groq is not configured; evaluation was not run.")
        return 2
    router = SemanticRouter(provider)
    correct = total = clarifications = false_drafts = failures = 0
    per_intent = {}
    for expected, utterances in CORPUS.items():
        intent_correct = 0
        for utterance in utterances:
            result = router.classify(utterance, ConversationState())
            total += 1
            if result is None:
                failures += 1
                continue
            predicted = {item.intent.value for item in result.intents}
            intent_correct += expected in predicted
            clarifications += any(item.needs_clarification for item in result.intents)
        correct += intent_correct
        per_intent[expected] = {"correct": intent_correct, "total": len(utterances), "accuracy": intent_correct / len(utterances)}
    for utterance in NEGATIVE_REQUIREMENT_CASES:
        result = router.classify(utterance, ConversationState())
        if result and any(item.intent == Intent.CREATE_REQUIREMENT_DRAFT for item in result.intents):
            false_drafts += 1
    report = {
        "utterances": total,
        "intent_accuracy": correct / total if total else 0,
        "clarification_rate": clarifications / total if total else 0,
        "false_requirement_creation_count": false_drafts,
        "negative_requirement_cases": len(NEGATIVE_REQUIREMENT_CASES),
        "provider_failures": failures,
        "per_intent": per_intent,
    }
    output = Path(__file__).resolve().parents[1] / "docs" / "ai-semantic-evaluation.json"
    output.write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(json.dumps(report, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
