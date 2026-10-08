# Offers, Reservations, Transactions and Approval - Agent and Test Evidence

- Owned component: offers, reservations, transaction lifecycle, manager approval and workflow audit/history.
- Owned agent: `ValidationAgent` in `ai-service/app/agents/validation.py`; shared orchestration in `ai-service/app/workflows/orchestration.py`.
- Schemas: `ValidationInput`, `ValidationResult`, `CheckResult`, and `ToolTrace`.
- Input: candidate/listing state, quantity, budget and logistics evidence.
- Output: `valid`, `requiresApproval`, `recommendedMatchId`, violations and warnings.
- Allowed tools: active, expiry, availability, budget, match-completeness, and transaction-threshold checks defined by `ALLOWED_TOOLS`.
- Safe failure: deterministic violations and tool timeout/unavailability fail closed. The agent has no mutation, approval, or reservation capability; only the authorized backend manager path may reserve after approval.

```powershell
cd ai-service; py -3 -m pytest member_tests/member4_tests.py -v
powershell -ExecutionPolicy Bypass -File scripts/member_checks/run_member4.ps1
cd ai-service; py -3 -m examples.member4_validation_example
```

Related evidence: backend workflow and transaction integration tests; React manager approval/transaction tests; Flutter transaction and match detail tests.
