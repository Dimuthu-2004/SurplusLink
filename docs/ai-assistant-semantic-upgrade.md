# SurplusLink semantic assistant upgrade

## Architecture

The active `/internal/chat` runtime now uses `SurplusLinkSemanticAssistantEngine`. Every message is classified by Groq into the strict Pydantic `SemanticRoutingResult` schema. The schema supports up to three intents, confidence, question/purchase/follow-up flags, typed slots, clarification, response-language style, and a retrieval-only query rewrite. Low-confidence and ambiguous classifications clarify; they do not create a buyer draft.

Conversation state stores `activeRequirementDraft`, `awaitingField`, `lastResolvedIntent`, `lastReferencedItem`, and `lastToolContext`. Knowledge or live-data turns do not discard an active draft.

The LLM only extracts user concepts. `CatalogDraftValidator` resolves the item against catalog results and deterministically validates units, package sizes/arithmetic, preference field keys/options, and missing required fields. No item-specific preference branches exist.

RAG combines BM25 with local `sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2` embeddings through FastEmbed/ONNX. Metadata filtering is available before retrieval; reciprocal-rank fusion and heading-aware reranking combine channels. The index is rebuilt by `python app/rag/build_index.py`. Answers are generated only from retrieved chunks and include citations; provider failure returns the highest-ranked verified chunk.

Live listings, requirements, offers, transactions, and matches come only from allow-listed ASP.NET tools. Mixed intents can call a live tool and RAG in the same turn.

The normal Flutter requirement form now receives a typed `AiRequirementPrefill`, resolves the catalog template, creates dynamic preference controls, and only then applies base/package/piece/continuous quantities and generic `preferences[fieldKey]` values.

## Verification snapshot (2026-10-02)

- AI service: 70 tests passed (including multi-turn structured draft continuation).
- ASP.NET API: build passed, 0 warnings and 0 errors.
- Flutter: analyze passed with no issues.
- Focused Flutter assistant models: 3 tests passed.
- ASP.NET test project: 215 passed, 68 environment-dependent tests skipped, 0 failed.
- Hybrid index: 23 documents, 98 chunks, 98 multilingual embedding vectors.
- Held-out corpus: 200 utterances (25 for each of 8 important intents) plus 8 negative/ambiguity cases.
- Live intent accuracy and false-creation metrics: not measured because the configured Groq credential returned `expired_api_key`. The reproducible runner is `ai-service/evaluate_semantic_router.py`; it does not emit a report when the provider fails.
- Physical-device E2E: not run in this environment; no device was attached.

The older `assistant_engine.py` and `requirement_draft_agent.py` remain as inactive compatibility modules for the pre-existing test suite. They are not imported by the production FastAPI entrypoint. They should be deleted after downstream imports migrate; their heuristic behavior is not part of the active runtime.
