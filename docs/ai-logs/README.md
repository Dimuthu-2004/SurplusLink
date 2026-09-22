# AI assistance and workflow evidence

The application executes four controlled agents through internal FastAPI/LangGraph. Persisted AgentWorkflow, AgentStep and AgentToolCall records contain structured outputs, checks, timings, retry counts and sanitized errors. They do not store hidden model reasoning. The current graph is deterministic and does not require an external LLM key.

Codex assisted the 22 September 2026 audit continuation with repository/plan inspection, targeted implementation repairs, regression tests and documentation. The supplied earlier transcript describes previous Codex assistance. This is an assistance disclosure, not a claim about any member's personal authorship, review or viva preparation. Each member must review and explain the changes within their assigned component.

Record real, sanitized workflow exports/screenshots here for submission. Do not commit credentials, JWTs, personal/customer data or hidden reasoning. The live golden test exercises the real Python graph with explicitly simulated routing; it does not establish a live routing-provider or device walkthrough. Current measured test counts and remaining evidence are in [the audit report](../evidence/shared/pre-s12-audit-2026-09-22.md).
