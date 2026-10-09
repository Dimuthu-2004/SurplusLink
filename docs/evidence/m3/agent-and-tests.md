# Matching and Logistics - Agent and Test Evidence

- Owned component: match candidates, ranking information, route distance/duration, delivery feasibility and transport cost.
- Owned agent: `LogisticsAgent` in `ai-service/app/agents/logistics.py`.
- Schemas: `LogisticsRequest`, tool contracts, `CandidateLogistics`, and `LogisticsResponse` in `ai-service/app/agents/logistics_schemas.py`.
- Input: requirement ID, candidate listing IDs, buyer location and deadline.
- Output: per-candidate route and cost result, delivery feasibility, warnings and structured status.
- Allowed tools: `get_listing_location`, `get_route_estimate`, `calculate_transport_estimate`.
- Safe failure: missing coordinates, malformed provider data, timeout, rate-limit and unavailable routing return warnings without invented distances or costs; retries are bounded.

```powershell
cd ai-service; py -3 -m pytest member_tests/member3_tests.py -v
powershell -ExecutionPolicy Bypass -File scripts/member_checks/run_member3.ps1
cd ai-service; py -3 -m examples.member3_logistics_example
```

Related evidence: backend matching/routing tests; React matching integration tests; Flutter `recommended_matches_test.dart` and `match_ux_test.dart`.
