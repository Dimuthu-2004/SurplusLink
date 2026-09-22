# Validation Agent and shared workflow

The runnable graph is `Planner -> Matching -> Logistics -> Validation -> PENDING_APPROVAL`.
It composes the existing three agents; no LLM, external model key or generated plan is needed.
Notes and objective-like text are data. Neither they nor a future LLM preference can change the
graph edges, tool names, numeric checks, selected IDs or approval policy.

## Run the stable offline demo

From `ai-service`:

```powershell
python -m pip install -r requirements-test.txt
python -m app.workflows.demo
python -m unittest discover -s tests -v
```

The demo uses explicitly labelled in-memory fixtures with a deadline relative to today.
Direct runtime/test dependencies are pinned to the verified versions; Python tests also run in CI.
It runs the real four-agent graph, returns `PENDING_APPROVAL`, and prints all six check traces.
It does not connect to a database, reserve stock, create transactions or call a maps provider.
These fixtures are never a fallback for a failed production routing call.

## Start the integrated services

Choose a shared random token of at least 32 characters and set the same value in both service
terminals via your local secret mechanism. Do not put it in web/mobile configuration or source control.

FastAPI terminal, from `ai-service` (after setting `AI_SERVICE_SHARED_TOKEN`):

```powershell
python -m uvicorn app.main:app --host 127.0.0.1 --port 8000
```

ASP.NET terminal, from `backend/SurplusLink.Api`, with normal API database/JWT configuration
and the same `AI_SERVICE_SHARED_TOKEN`:

```powershell
$env:AI_SERVICE_BASE_URL = 'http://127.0.0.1:8000'
$env:AgentWorkflow__Enabled = 'true'
dotnet run
```

Live routing also requires the existing `Routing__*` environment configuration described in
[routing-provider.md](routing-provider.md). Missing route/pricing data cannot produce approval.
Use the offline demo when presenting without a configured maps service.

1. The buyer creates and submits a requirement, then calls
   `POST /api/requirements/{id}/start-matching` with their normal bearer token.
2. ASP.NET checks ownership/state, commits `AgentWorkflow(RUNNING, QUEUED)` and changes
   the requirement to `MATCHING`. The response contains the persisted workflow ID.
3. `WorkflowExecutionWorker` polls PostgreSQL. `FOR UPDATE SKIP LOCKED` gives a queued row
   to one worker. The worker reads a bounded listing shortlist and routing estimates through
   the existing API-owned transport service. Stored buyer identity and prices are authoritative.
4. `AgentWorkflowClient` sends `POST /internal/workflows/run` with `X-Internal-Token` and
   the stored requirement/listing snapshot. FastAPI fails closed when its token is missing.
   The endpoint is internal; do not publish it through the public API ingress.
5. The four graph nodes adapt the snapshot to the existing agent contracts. Matching ranks
   candidates; Logistics consumes actual route/price estimates; Validation checks
   candidates in ranked order until one passes all deterministic checks. If none passes, the result
   requests revision. The shortlist prioritizes feasible inventory before unit price and listing ID,
   and is capped at 10 by default.
6. ASP.NET validates the response envelope, IDs, stage order, all six successful tool results and
   route costs against its snapshot. It rereads stock, price, expiry and eligibility before saving
   the recommendation. It writes the result, steps, tool traces and audit atomically to PostgreSQL.
7. Managers inspect `GET /api/workflows/{id}` and the existing approval dashboard. Approval is
   still a separate manager action. A failed validation result cannot be overridden with Approve.

The worker uses the workflow table as a durable queue; there is no in-memory fire-and-forget
task. A worker crash rolls back its transaction and releases the claim for replay. The graph is
read-only, so replay does not reserve twice. The worker intentionally holds one transaction while
calling the bounded internal service; this is a small-demo design, not a high-throughput queue.
Old `RUNNING/MATCHING` scaffold rows are not automatically replayed; only `RUNNING/QUEUED`
rows created by this starter are processed. The worker is enabled by default and requires a valid
internal service URL/token at startup. Explicitly disabling execution rejects new starts with 503
without changing the requirement. Use `scripts/start-local.ps1` to start both local services with
one session token. Flutter polls matching requirements every four seconds until their state changes.

## Validation contract and tools

```json
{
  "valid": true,
  "requiresApproval": true,
  "recommendedMatchId": "00000000-0000-0000-0000-000000000005",
  "violations": [],
  "warnings": []
}
```

On invalid input/checks, `valid` and `requiresApproval` are false, `recommendedMatchId` is null,
and `violations` contains stable codes. Unknown measurements are not zero. All arithmetic uses
decimal quantities/costs; Python serializes Decimal values as strings and the ASP.NET client accepts them.

| Allowed validation tool | Deterministic rule |
| --- | --- |
| `check_listing_active` | Stored listing status is `ACTIVE`. |
| `check_listing_not_expired` | Listing expires after now and no earlier than the delivery deadline. |
| `check_available_quantity` | Available quantity covers requested quantity; equality is allowed. |
| `check_budget` | Quantity × unit price + transport cost is within budget; equality is allowed. |
| `check_match_data_complete` | Nonempty IDs, different buyer/seller, matching category/unit, known route/cost, feasible delivery, future deadline. |
| `check_transaction_threshold` | Total value is known; value at or above the configured threshold adds `TRANSACTION_THRESHOLD_REQUIRES_REVIEW`. |

Threshold escalation is a warning, not a budget violation. All valid transactions require manager
approval, even below the threshold. Validation exposes only these six methods. Its tools are pure
checks over the authenticated snapshot and have no inventory, reservation or transaction write API.
Planner/Matching/Logistics retain their own separate read-only tool boundaries.

Business-rule failures, missing candidates or incomplete logistics produce `REVISION_REQUESTED`.
Exhausted tool/transport errors and timeouts produce `FAILED`; neither can reach approval.
The response contract also supports `REJECTED` for a terminal policy result, but the current
automated graph uses revision for business failures so a buyer can make an explicit fresh attempt.

## Limits and failure behavior

| Service/environment setting | Default | Allowed range |
| --- | --- | --- |
| `WORKFLOW_STAGE_TIMEOUT_SECONDS` | 5 | >0 to 30 seconds |
| `WORKFLOW_TIMEOUT_SECONDS` | 25 | >0 to 120 seconds |
| `WORKFLOW_TOOL_TIMEOUT_SECONDS` | 1 | >0 to 10 seconds |
| `WORKFLOW_MAX_RETRIES` | 1 | 0–3 retries after first attempt |
| `WORKFLOW_TRANSACTION_THRESHOLD` | 100000 | Positive finite decimal, same currency as budget |
| `AgentWorkflow__PollSeconds` | 2 | 1–60 seconds |
| `AgentWorkflow__TimeoutSeconds` | 60 | 1–180 seconds, includes snapshot/routing and HTTP |
| `AgentWorkflow__HttpTimeoutSeconds` | 30 | 1–120 seconds |
| `AgentWorkflow__MaxRetries` | 1 | 0–3 retries |
| `AgentWorkflow__MaxCandidates` | 10 | 1–20 listings |

Keep the API HTTP timeout above the graph timeout and the API overall timeout above both routing
and HTTP budgets. The shortest active timeout wins. Stage timeouts and HTTP timeouts are terminal
for that attempt: a request already in flight is not immediately duplicated. Python retries transient
connection failures and async validation-tool timeouts within the configured budgets. Malformed tool
responses are never retried or accepted. ASP.NET retries transient HTTP 5xx/429/network failures,
but not authentication or schema failures. Every retry occurs inside the overall deadline.

The existing synchronous agents run on worker threads over bounded local snapshots. Timing out
stops awaiting them; Python cannot forcibly kill a thread. They perform no remote I/O or mutations
in this workflow, and no new attempt is started after a stage timeout. Keep that boundary if replacing
the snapshot adapters. An external adapter must implement its own cancellable I/O deadline.

On service exceptions, logs and stored errors contain stable error codes, never provider response
bodies, connection strings or raw exception messages. Internal token headers are redacted. No failed
result is promoted to approval using an invented route or fallback price.

## PostgreSQL mapping

No new tables or migration are required. The existing workflow tables are used:

| Result/state | PostgreSQL mapping |
| --- | --- |
| Start accepted | `AgentWorkflows.Status=RUNNING`, `CurrentStage=QUEUED`; `MaterialRequests.Status=MATCHING`. |
| Valid result | `AgentWorkflows.Status/CurrentStage=PENDING_APPROVAL`; selected ID in `MaterialMatchId`; `ValidationJson` contains the exact validation contract. |
| Evaluated candidates | Snapshot candidates persist with score, route distance/duration/cost and explicit rejection reasons; the selected match is ROUTED. |
| Buyer state after valid result | `MaterialRequests.Status=MATCH_FOUND`, matching the existing manager approval precondition; workflow owns the approval gate. |
| Invalid business result | `AgentWorkflows.Status=REVISION_REQUESTED`; no selected match; request returns to `OPEN` for explicit retry. |
| Execution failure | `AgentWorkflows.Status=FAILED`, sanitized `ErrorJson`, invalid `ValidationJson`; request returns to `OPEN`. |
| Stage observations | `AgentSteps` ordered PLANNER/MATCHING/LOGISTICS/VALIDATION, structured output, status, duration, retries, errors. |
| Validation checks | Six `AgentToolCalls` under the validation step, structured check output, retry count, timestamps and duration. |
| Full envelope | `AgentWorkflows.OutputJson`; workflow retry total includes HTTP, stage and tool retries. |
| Audit | `AuditLogs` action `WORKFLOW_<STATUS>`, system actor; existing `Approvals` remain manager-only. |
| Inventory/transactions | A valid recommendation creates a PENDING offer and PENDING_APPROVAL transaction with zero reserved quantity. Only manager approval reserves inventory and updates participant outcomes. |

For a fresh attempt after revision/failure, the starter creates a new workflow and preserves the old
audit history. It reuses an existing running/pending/approved/completed workflow instead of duplicating it.
The existing manager revision action records a revision step and note; the buyer reviews the note, edits the reopened OPEN requirement if needed, and explicitly starts a fresh attempt. The live golden test verifies that this attempt returns to approval and can complete without reserving twice.

## Verification

Python tests cover tool boundaries, exact numeric thresholds, safe missing data, prompt-like notes,
timeouts/retries, schema failures, internal authentication and the actual graph sequence.
ASP.NET tests cover strict response validation, retry/auth behavior and PostgreSQL persistence.

```powershell
dotnet test SurplusLink.sln --filter 'FullyQualifiedName~WorkflowExecutionTests|FullyQualifiedName~WorkflowPersistenceTests'
```

PostgreSQL tests use `SURPLUSLINK_TEST_CONNECTION` only to create a randomly named disposable
database. The configured application database is never migrated or cleared by those tests.
For the live cross-language test, additionally run FastAPI and set `SURPLUSLINK_AI_TEST_URL`
and the matching `AI_SERVICE_SHARED_TOKEN` in the test terminal. The test uses the real FastAPI
graph plus an explicit test-only transport fixture, then verifies persisted steps/checks and unchanged stock.

## Final audit follow-up (22 September 2026)

Approval rechecks the current listing, request, offered terms, route completeness, transport-inclusive budget and delivery timing while locking the request and listing inside the workflow transaction. The transaction-approval endpoint delegates to this same gate. Completion confirms reservations and updates the buyer request, workflow and participant history.

Apply `AddMatchDuration` before running these versions. See the current [audit report](evidence/shared/pre-s12-audit-2026-09-22.md); earlier test summaries in this document are historical. The real FastAPI golden regression uses test-only routing, not a live provider.
