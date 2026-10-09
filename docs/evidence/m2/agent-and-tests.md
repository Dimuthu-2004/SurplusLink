# Buyer Requirements - Agent and Test Evidence

- Owned component: buyer requirement lifecycle, submission, matching start, status/history and query operations.
- Owned agent: `RequirementPlannerAgent` in `ai-service/app/agents/requirement_planner.py`.
- Schemas: `PlannerRequest`, `NormalizedCriteria`, `PlannerSuccess`, and `PlannerFailure` in `ai-service/app/agents/planner_schemas.py`.
- Input: stored `BuyerRequest` plus optional untrusted objective text.
- Output: normalized criteria and the immutable sequence Matching, Logistics, Validation, ManagerApproval; invalid inputs return structured issues.
- Tool permissions: none. The planner cannot search, reserve, approve, or use network tools.
- Safe failure: missing/invalid required fields fail closed; objective and notes cannot change topology or bypass manager approval.

```powershell
cd ai-service; py -3 -m pytest member_tests/member2_tests.py -v
powershell -ExecutionPolicy Bypass -File scripts/member_checks/run_member2.ps1
cd ai-service; py -3 -m examples.member2_requirement_planner_example
```

Related evidence: backend requirement integration tests; React requirement pages/tests; Flutter buyer-requirement widget tests.
