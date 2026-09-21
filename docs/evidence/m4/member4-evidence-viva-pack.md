# Member 4 Evidence and Viva Pack

This pack covers Member 4's offer, reservation, transaction, manager approval,
workflow, Validation Agent, observability, and CI work. It intentionally has no
personal-reflection section. Replace every placeholder with dated, redacted,
real evidence from the integrated branch. Never capture secrets or personal data.

## Screenshot checklist

Save captures in `docs/evidence/m4/screenshots/`; each caption records date,
branch/commit, actor role, input, expected result, and actual result.

| ID | Filename placeholder | Capture and proof |
|---|---|---|
| M4-DB-01 | `01-offer-rows.png` | Offers rows: distinct buyer/seller identities, values, and status. |
| M4-DB-02 | `02-reservation-and-stock.png` | Reservation row and listing reserved quantity after approval only. |
| M4-DB-03 | `03-transaction-rows.png` | Pending/approved/rejected/completed transaction lifecycle. |
| M4-DB-04 | `04-constraints-indexes-version.png` | Positive/range/self-dealing constraints, indexes, and concurrency version. |
| M4-WF-01 | `05-pending-approval-workflow.png` | Planner, Matching, Logistics, Validation and `PENDING_APPROVAL`. |
| M4-WF-02 | `06-step-tool-call-traces.png` | Workflow/Step/ToolCall safe I/O, duration, retries, status, errors. |
| M4-WF-03 | `07-safe-workflow-failure.png` | Timeout/invalid result fails safely with no reservation. |
| M4-REACT-01 | `08-react-manager-approvals.png` | React approval detail, controls, history, loading/error state. |
| M4-FLUTTER-01 | `09-flutter-requirement-status.png` | Flutter buyer workflow/requirement status, without manager actions. |
| M4-AUTH-01 | `10-manager-role-gate.png` | BUYER/SELLER 403 compared with MANAGER decision success. |
| M4-TX-01 | `11-approval-reservation-audit.png` | Approval transaction, single reservation, and audit trail. |
| M4-TX-02 | `12-reject-revise-no-reservation.png` | Reject/revise persists decision but changes no stock. |
| M4-TEST-01 | `13-backend-tests.png` | M4 approval/reservation/concurrency/safe-failure tests. |
| M4-TEST-02 | `14-agent-tests.png` | Validation/workflow structured I/O, allowlist, retries/timeouts. |
| M4-CI-01 | `15-github-actions.png` | Action run, commit, jobs, result, and documented skipped prerequisite. |
| M4-PR-01 | `16-m4-prs.png` | PRs, commits, review, CI, and merge evidence. |

## Contribution outline — replace placeholders

| Area | Contribution placeholder | Evidence / PR |
|---|---|---|
| Offer and transaction lifecycle | `[Models, migration, constraints, APIs.]` | `[link]` |
| Reservation/concurrency | `[Atomic stock checks, idempotency, conflict behavior.]` | `[link]` |
| Workflow persistence | `[AgentWorkflow, Step, ToolCall, Approval state.]` | `[link]` |
| Manager approval and React | `[Role gate, approve/reject/revise, UI.]` | `[link]` |
| Validation Agent/orchestration | `[Allowlisted checks, schema, retry/timeout.]` | `[link]` |
| Flutter status | `[Repository, screen, loading/error handling.]` | `[link]` |
| Tests, CI, review | `[Tests, Actions, defect fixes.]` | `[link]` |

## Simple explanations

**DB transaction and concurrency.** Approval rechecks current ownership, stock,
listing/request status, and validation, then creates the reservation, updates
stock/status, and writes audits as one transaction. Any failure rolls the whole
operation back. Locks and PostgreSQL optimistic concurrency/version checks make
the stale competing request fail rather than oversell stock.

**Manager auth.** Authentication identifies the caller through a signed JWT.
Authorisation requires the `MANAGER` role at decision endpoints. A user with
both `BUYER` and `SELLER` claims still gets 403; business logic is not entered.

**Approve/reject/revise.** Only valid `PENDING_APPROVAL` can approve, and that
is the only path that reserves. Reject records a terminal decision with no stock
change. Revise records correction feedback and must require a fresh validated
run, never reuse a stale recommendation.

**Workflow state.** Durable state records queued/running work, fixed Planner →
Matching → Logistics → Validation stages, `PENDING_APPROVAL`, and terminal or
safe-failure states. It replaces an untraceable chat response with recoverable,
permissioned state.

**Observability.** Workflow, Step, ToolCall, Approval, and AuditLog records
answer what ran, when, how long, how often it retried, who decided, and what
safe error occurred. Store codes and structured safe data, never keys, URLs,
provider bodies, or stack traces.

**Retry and safe failure.** Limits bound attempts and time; only safe transient
reads retry. A timeout or bad tool result fails closed: no fabricated cost,
validity, match, or reservation; the user can deliberately revise/retry.

**Validation Agent.** It owns no database mutation or approval. It runs a fixed
tool allowlist for active/expiry, quantity, budget, complete match data, and
transaction threshold. All required deterministic checks pass before pending
approval; a manager cannot override a failed check.

**GitHub Actions.** CI supplies repeatable evidence for a commit/PR. Capture the
workflow name, SHA, jobs, test result, and any external integration prerequisite
that caused a documented skip; CI complements rather than replaces review.

## 20 viva questions

1. When is stock reserved? — Only after manager approval, in the ASP.NET transaction.
2. Why recheck stock? — A workflow snapshot can be stale during review.
3. How is overselling prevented? — Availability checks plus locking/version conflict handling.
4. What is idempotent approval? — Repeating an approved decision creates no second reservation.
5. Why use `PENDING_APPROVAL`? — It separates recommendation from human-authorised mutation.
6. Can BUYER+SELLER approve? — No; `MANAGER` is a separate required role.
7. What does reject do to stock? — Nothing; it records a decision/audit only.
8. What must revise prevent? — Approval from an old recommendation.
9. Why persist steps? — Durable progress, recovery, and audit context.
10. Why persist tool calls? — Safe I/O, timing, retry, and error evidence.
11. What can Validation Agent do? — Only fixed deterministic checks over trusted data.
12. Why cannot it reserve? — Validation and mutation are separate responsibilities.
13. What happens on tool timeout? — Safe failed trace; no approval or reservation.
14. Why cap retries? — To avoid hidden failure, load amplification, and duplicate work.
15. What is a safe error? — Stable code without sensitive exception/provider details.
16. How is self-dealing blocked? — Policy checks and database buyer/seller-different constraints.
17. Why audit? — Accountability: actor, action, entity, and time.
18. What is a threshold warning? — Manager-review signal, never automatic permission.
19. How test manager-only routes? — Dual normal roles expect 403; manager expects allowed result.
20. What does CI evidence show? — Exact SHA, jobs, pass/fail, and explicit prerequisites.

## Five live modification/debug tasks

1. Add/run a test proving BUYER+SELLER receives 403 for approve/reject/revise while MANAGER succeeds.
2. Seed near-exhausted stock; perform competing approvals and show one safe conflict, never oversell.
3. Add a deterministic validation check, schema/trace/test, then prove failure cannot reserve.
4. Use a delayed fake tool/workflow; show bounded timeout, safe error code, and zero reservations.
5. Add a safe approval-summary field (for example reservation ID), surface it in React, and test it.

## Capture commands and final checklist

```powershell
dotnet test .\backend\SurplusLink.Tests --filter "FullyQualifiedName~Workflow|FullyQualifiedName~Reservation|FullyQualifiedName~Transaction|FullyQualifiedName~AgentWorkflow"
cd .\ai-service; python -m unittest discover -s tests -p "test_validation_workflow.py" -v
cd ..\web; npm test -- --runInBand
cd ..\mobile; flutter test test/recommended_matches_test.dart test/requirement_repository_test.dart
```

Set `SURPLUSLINK_TEST_CONNECTION` locally for PostgreSQL tests. Without it,
record **BLOCKED**, not pass. Before submission confirm every placeholder is
replaced, screenshots are redacted, normal/reject/revise/safe-failure paths are
captured, stock change is shown only after approval, and every contribution has
a commit/PR/evidence link.
