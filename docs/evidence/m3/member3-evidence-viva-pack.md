# Member 3 Evidence and Viva Pack

## Purpose and presentation order

This pack is a recording guide and viva revision sheet for Member 3's matching,
logistics, routing, client, analytics, and agent-workflow contributions. Do not
submit placeholder entries as evidence: replace each with a dated screenshot,
terminal capture, or pull-request link from the integrated branch. Never show a
JWT, password, connection string, routing key, or real customer data.

Suggested demonstration order: sign in as buyer, show a paged matching query,
open its database/audit result, show routing success and safe routing failure,
show React and Flutter results, run tests, then walk through the agent workflow
and manager approval gate.

## Screenshot and evidence checklist

Store captures under `docs/evidence/m3/screenshots/` using the filenames below.
Each caption must include date/time, branch/commit, actor role, input, expected
result, and actual result.

| ID | Filename placeholder | Capture | What proves it |
|---|---|---|---|
| M3-API-01 | `01-matches-filter-sort-page.png` | `GET /api/matches/requirement/{id}` with filters, sort, and page | Query controls, stable pagination, filtered total. |
| M3-API-02 | `02-match-history.png` | Match history response | Persisted generate/rank/route/reject audit events. |
| M3-API-03 | `03-match-analytics.png` | Manager analytics summary response | Aggregate score, routing success/failure, rejection metrics. |
| M3-DB-01 | `04-match-and-history-rows.png` | Read-only SQL/DB viewer for `Matches` and history/audit rows | Match status, score, route fields, timestamps, and audit persistence agree with the API. |
| M3-DB-02 | `05-indexes-and-constraints.png` | DB viewer/query-plan screen | Match indexes, foreign keys, self-match/quantity/status constraints as applicable. |
| M3-ROUTE-01 | `06-routing-success.png` | API/UI route success | Provider distance, duration, and transport estimate reach the match. |
| M3-ROUTE-02 | `07-routing-safe-failure.png` | Timeout/429/unavailable case | Error code only; no invented distance/cost and no provider secret/body. |
| M3-REACT-01 | `08-react-match-comparison.png` | Manager React match comparison/list | Filters, sorting, distance/cost, status, and empty/loading/error state. |
| M3-FLUTTER-01 | `09-flutter-recommended-matches.png` | Buyer Flutter recommended matches/details | Same trusted API data and clear route/availability result. |
| M3-AGENT-01 | `10-agent-workflow-detail.png` | Workflow details API/UI | Planner, Matching, Logistics, Validation steps; traces and `PENDING_APPROVAL`. |
| M3-AGENT-02 | `11-manager-approval.png` | Manager approval then reservation row | Human gate before reservation and terminal status/audit. |
| M3-PERF-01 | `12-query-plan-or-timings.png` | Query plan or repeated timing capture | Index use / bounded page size / response timing evidence. |
| M3-TEST-01 | `13-dotnet-tests.png` | Focused .NET test output | Matching, routing, workflow, authorization, and concurrency tests. |
| M3-TEST-02 | `14-react-flutter-agent-tests.png` | Relevant Web, Flutter, and AI test output | Client and agent contracts, without live credentials. |
| M3-PR-01 | `15-prs.png` | GitHub PR list/detail | Member 3 commits, review, CI, and merge evidence. |

### Safe capture commands

Use configured local accounts and replace placeholders; do not paste tokens into
shell history or screenshots.

```powershell
dotnet test .\backend\SurplusLink.Tests --filter "FullyQualifiedName~Match|FullyQualifiedName~Routing|FullyQualifiedName~Workflow"
cd .\web; npm test -- --runInBand
cd ..\mobile; flutter test test/recommended_matches_test.dart test/match_repository_test.dart
cd ..\ai-service; python -m unittest discover -s tests -p "test_*.py" -v
```

For PostgreSQL-backed tests, set `SURPLUSLINK_TEST_CONNECTION` only in the local
environment. If it is unavailable, record **BLOCKED: PostgreSQL test connection
not configured** rather than a pass.

## Contribution outline — fill before submission

| Area | Member 3 contribution placeholder | Evidence link / commit / PR |
|---|---|---|
| Matching API and query builder | `[Describe endpoints, filters, policy checks, and ownership.]` | `[PR/link]` |
| Match persistence and audit history | `[Describe model/migration/audit work.]` | `[PR/link]` |
| Routing adapter and transport estimate | `[Describe interface, OpenRouteService adapter, configuration, tests.]` | `[PR/link]` |
| React manager matching UI | `[Describe pages, API client, loading/error/empty states.]` | `[PR/link]` |
| Flutter buyer match UI | `[Describe repository, screens, and tests.]` | `[PR/link]` |
| Agent workflow contribution | `[Describe deterministic matching/logistics inputs and workflow trace.]` | `[PR/link]` |
| Tests, review, and defect fixes | `[List test files, reviews, bugs found/fixed.]` | `[PR/link]` |

## Short technical explanations

### Deterministic ranking vs an LLM

Deterministic ranking applies the same published score formula to the same
trusted listing and requirement fields every time. It uses fixed eligibility
rules—different owner, active/verified, enough quantity, matching unit/category,
budget, and deadline—then stable tie-breaking by ID. It is reproducible,
testable, and cannot be persuaded by a prompt to ignore a hard rule.

An LLM is useful for interpreting optional free text or explaining a result, but
it must not select a database mutation, bypass eligibility, or approve a trade.
In SurplusLink, topology, tools, validation, and approval are code-owned; notes
and objectives are untrusted data.

### Indexes

An index is a lookup structure that lets PostgreSQL find/filter/sort candidate
rows without scanning every row. Useful indexes follow real access paths, such
as match requirement plus score, listing status plus availability date, and
workflow status for the worker queue. Indexes improve reads but add write and
storage cost, so demonstrate them with a query plan or measurable bounded-page
timing rather than claiming every index always helps.

### Route adapter, HttpClient, timeout, and rate limit

The application calls routing through `IRoutingProvider`/
`ITransportEstimateService`, not directly from a mobile/web client. The adapter
owns provider-specific HTTP request/response mapping. DI-managed `HttpClient`
supports reuse, controlled buffer limits, redacted logs, disabled redirects, and
server-only credentials. A timeout returns a controlled routing error; it does
not fabricate a straight-line route. A 429 is rate-limited, honours bounded
`Retry-After` information when supplied, and is not automatically retried in a
way that amplifies quota use.

### Safe failure

Safe failure means the system fails closed: unknown route/cost, malformed tool
response, timeout, missing configuration, expired listing, or invalid workflow
cannot become a cheap/valid recommendation. Store a stable code such as
`ROUTING_TIMEOUT` or `WORKFLOW_EXECUTION_FAILED`, avoid secrets/provider bodies,
keep stock unreserved, and return the requirement to a state where the user can
retry or revise deliberately.

### Query controls

Search/filter/sort/page controls are validated allow-lists, not arbitrary SQL.
Filters combine with AND; `page` and `pageSize` have safe bounds; sort has an
allowed field and direction; IDs break ties so page boundaries do not drift.
Compute the filtered total before applying `Skip`/`Take`, and expose enough
metadata for React/Flutter to render a correct pager.

### Analytics and performance metrics

Analytics answer operational questions: total matches, average score/distance,
route success/failure rate, and leading rejection reasons. A rate is `success /
all attempts`; when there are no attempts it should be null, not a misleading
0%. For performance, record endpoint latency (p50/p95 if available), query-plan
index use, returned row count, route latency/failure rate, workflow duration,
tool duration/retry count, and client loading/error/retry behavior. Report the
environment and sample size with any number.

## Twenty viva questions with concise answers

1. **Why is matching deterministic?** Fixed rules and score inputs make it reproducible and testable.
2. **Why not let an LLM choose the listing?** It could be inconsistent or prompt-injected; code enforces eligibility and ranking.
3. **How is self-dealing prevented?** Stored buyer and seller IDs must differ; matching, workflow validation, reservation, and DB constraints recheck it.
4. **What makes a listing eligible?** Active/verified, unexpired, category/unit-compatible, enough available quantity, different owner, and within budget/deadline.
5. **Why use stable tie-breaking?** Equal scores must not reorder across requests and break pagination or audit reproducibility.
6. **What does the route adapter isolate?** Provider URL, credentials, payload format, and provider response mapping stay out of business logic and clients.
7. **Why use DI-managed HttpClient?** It centralises configuration and avoids unsafe per-request clients/socket exhaustion.
8. **What happens on routing timeout?** A safe error code is returned; no distance/cost is invented and the candidate cannot pass validation.
9. **How is a 429 handled?** It becomes a rate-limit failure with bounded retry-after information, without an aggressive automatic retry.
10. **Why is route cost server-side?** Pricing/provider credentials and trusted calculations must not be client-controlled.
11. **What are the query controls?** Validated filters, allow-listed sort fields/direction, bounded pagination, total count, and stable tie-breaks.
12. **Why are indexes not free?** They speed selected reads but cost space and make writes more expensive.
13. **How would you prove an index is used?** Capture `EXPLAIN (ANALYZE, BUFFERS)` for a representative bounded query.
14. **What does safe failure mean for a workflow?** It fails closed, stores a non-sensitive code, makes no reservation, and permits deliberate correction/retry.
15. **What is the manager gate?** A valid workflow stops at `PENDING_APPROVAL`; only a JWT `MANAGER` can decide.
16. **When is stock reserved?** Only inside the ASP.NET approval transaction after current stock/status/ownership checks.
17. **Why recheck at approval?** Snapshot information can become stale while the manager is reviewing it.
18. **What do tool traces record?** Tool name, safe input/output, status, timing, retry count, and safe error code.
19. **How do analytics avoid false claims?** Null is used for unavailable averages/rates and every metric states its denominator.
20. **How would you investigate a slow match list?** Reproduce validated query, inspect SQL/query plan and indexes, check page size/row count, then measure API and client timings.

## Five live debug/modification tasks

1. **Add a deterministic tie-break.** Create equal-score candidates, show unstable ordering risk, add/verify ID secondary sort, and run the matching test.
2. **Diagnose a routing timeout.** Use a fake delayed provider, show `ROUTING_TIMEOUT`, confirm no cost is persisted, then adjust only the configured timeout within bounds.
3. **Add a safe query filter.** Add one allow-listed match status filter to request DTO, query builder, API test, React/Flutter request builder, and documentation; reject unknown values with 400.
4. **Prove the approval gate.** Attempt an approval as `BUYER`+`SELLER`, receive 403, then approve as `MANAGER` and show one reservation/audit event; repeat approval to show idempotency.
5. **Explain an analytics discrepancy.** Seed a successful route, failed route, and rejected match; calculate expected success rate/rejection counts, compare with API summary, and fix only the denominator/query if it differs.

## Final viva checklist

- [ ] All placeholders replaced with dated evidence and PR links.
- [ ] Screenshots redact secrets, tokens, email addresses, and connection strings.
- [ ] PostgreSQL-gated tests marked pass only when actually run.
- [ ] Explain one normal path and one safe-failure path without reading code.
- [ ] Demonstrate manager approval/reservation and a rejected non-manager attempt.
- [ ] Be able to name the relevant endpoint, table/model, test, and PR for each claim.
