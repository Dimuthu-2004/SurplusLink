# Material Inventory and Listings - Agent and Test Evidence

- Owned component: material categories, seller listings, verification, inventory rules and listing search.
- Owned agent: `MaterialMatchingAgent` in `ai-service/app/agents/material_matching.py`.
- Schemas: `MatchingRequest`, `Candidate`, and `MatchingResponse` in `ai-service/app/agents/matching_schemas.py`.
- Input: buyer identity, category, quantity/unit, budget and deadline.
- Output: ranked candidates with `maximumContribution` and `fullCoverage`, or structured `INVALID_INPUT`, `SEARCH_UNAVAILABLE`, or `NO_CANDIDATE` failure.
- Allowed read-only tools: `search_active_materials`, `get_material_detail`.
- Safe failure: invalid data, unavailable search, self-owned listing, inactive/expired listing, mismatch, or zero contribution cannot produce a selectable candidate.

```powershell
cd ai-service; py -3 -m pytest member_tests/member1_tests.py -v
powershell -ExecutionPolicy Bypass -File scripts/member_checks/run_member1.ps1
cd ai-service; py -3 -m examples.member1_material_matching_example
```

Related evidence: backend Materials controller/service tests; React manager materials tests; Flutter material listing and inventory tests.
