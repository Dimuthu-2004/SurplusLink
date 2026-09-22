# RUNTIME WORKFLOW + MY OFFERS REPAIR REPORT

Date: 2026-09-22. Verification distinguishes automated integration/widget evidence from a manual device/browser walkthrough.

## BUG 1 — MATCHING STUCK

Root cause: `WorkflowExecutionOptions.Enabled` and `.env.example` defaulted to false, while the canonical starter still committed RUNNING/QUEUED and changed the requirement to MATCHING. The worker exited without processing. The golden test bypassed this failure by directly invoking `WorkflowQueueProcessor`. Flutter fetched the status only once, with a manual refresh button and no polling.

Local configuration inspection found DB/JWT user secrets, but no AI URL/token/enable override or routing environment settings. FastAPI was not listening on the default port before verification. This confirms a broken default launch path; it does not establish the configuration of a separate deployed app.

Changes:

- Workflow execution defaults to enabled. Existing startup validation requires a valid internal URL and token. Explicit disable returns 503 from Start Matching before queue/status mutation.
- `scripts/start-local.ps1` starts FastAPI and ASP.NET with the same session token, waits for health, and enables the worker. Normal DB/JWT configuration and real routing credentials remain required.
- Existing durable queue, validation, persistence and manager approval remain canonical. Failure reopens the requirement for explicit retry; no synthetic approval or direct database status repair was introduced.
- Flutter polls the existing owned requirement GET every four seconds while MATCHING, prevents overlapping loads, preserves status across transient errors, stops after leaving MATCHING, and cancels its timer on disposal. Manual refresh remains. FAILED workflow status explains how to retry.
- Golden integration now uses the actual hosted worker and real FastAPI graph, polls HTTP status, asserts exactly one workflow and manager pending-list visibility, and retains approve/reject/revise/reservation/participant checks.
- Added HTTP disabled-worker and hosted-worker failure/retry regressions.

Files: `backend/SurplusLink.Api/Workflows/{WorkflowExecution,PersistentRequirementWorkflowStarter,IRequirementWorkflowStarter}.cs`; `mobile/lib/screens/requirement_status_screen.dart`; `.env.example`; `README.md`; `scripts/start-local.ps1`; backend test fixtures, `PreS12GoldenWorkflowTests.cs`, `WorkflowExecutionTests.cs`; `mobile/test/runtime_repair_test.dart`.

| Check | Result / evidence |
| --- | --- |
| Start Matching request | PASS — HTTP returns MATCHING |
| Workflow execution starts | PASS — hosted worker integration and local API run |
| Planner | PASS — real FastAPI integration |
| Matching | PASS — real FastAPI integration |
| Logistics | PASS — graph integration with explicit test transport adapter |
| Validation | PASS — real FastAPI integration |
| Requirement leaves MATCHING | PASS — success, failure and revision paths |
| PENDING_APPROVAL reached | PASS — hosted-worker integration; real routing provider not verified |
| Flutter status refresh | PASS — widget regressions, including stop/disposal/error recovery |
| Recommended Matches visible | PASS — automated API/widget coverage; manual UI unverified |
| React Pending Approval visible | PASS — automated API/React coverage; manual UI unverified |

## BUG 2 — MY OFFERS ACCESS

Root cause of the reported literal “Access denied” remains **unreproduced** in this checkout. Before these edits, the Flutter source contained no such message, `/offers` had no role restriction, and the backend already allowed SELLER OR BUYER OR MANAGER. Normal-user queries were already overwritten with the authenticated identity, and details/history checked buyer-or-seller participation. Those controls were preserved.

Live logins for `seller@test.local`, `buyer@test.local`, and `dual@test.local` returned HTTP 200 for both offers and transactions, with zero records. Even the old empty `userId` query returned 200, so it is not claimed as the cause of a 403. No connected Android device was available to inspect the originally reported binary. A different/stale build or endpoint is possible but not proven.

Repairs made to confirmed UI/error-handling gaps:

- Marketplace screen explicitly accepts seller OR buyer capability, including dual role; other roles get a genuine access-denied state without making requests.
- Removed the redundant client `userId` filter. Server ownership remains authoritative.
- Empty unfiltered success shows “No offers yet.” Errors do not also render an empty state.
- 401 prompts sign-in, 403 shows access denied, other API failures and connection errors have distinct messages. Plain-text/HTML error responses preserve their HTTP status instead of losing it during JSON parsing.
- Existing buyer/seller participation labels and read-only details/history remain; dual-role mixed participation is tested. No manager actions were added.

Files: `mobile/lib/screens/my_offers_screen.dart`; `mobile/lib/offers/{offer_repository,offer_models}.dart`; `mobile/lib/core/api_client.dart`; `mobile/test/runtime_repair_test.dart`; `backend/SurplusLink.Tests/TransactionQueryIntegrationTests.cs`.

| Check | Result / evidence |
| --- | --- |
| SELLER /offers access | PASS — live API and Flutter route test |
| BUYER /offers access | PASS — live API and Flutter route test |
| SELLER+BUYER /offers access | PASS — live API and Flutter route test |
| Empty state instead of Access Denied | PASS — widget tests |
| Participant-only filtering | PASS — PostgreSQL integration, including forged userId |
| Unrelated-user privacy | PASS — detail/history 403, scoped empty list |
| Manager approval isolation | PASS — participant approve/reject/revise/complete denied |
| Offer Details | PASS — API and Flutter widget tests |
| Transaction History | PASS — API and Flutter widget tests |

## REGRESSION

| Check | Final result |
| --- | --- |
| Backend build | PASS — zero warnings/errors |
| Backend tests | PASS — 138 passed, 0 failed, 0 skipped |
| React build | PASS — `npm run build` |
| React tests | PASS — 33 passed |
| Flutter analyze | PASS — no issues |
| Flutter tests | PASS — 98 passed |
| Python tests | PASS — 42 passed |
| Local launcher | PASS — FastAPI health, API health, worker-start log and HTTP retry |
| Diff/script checks | PASS — no whitespace errors; PowerShell parser reported no errors |

Backend tests used disposable PostgreSQL databases and a real FastAPI process; the successful integration workflow uses the existing deterministic test transport adapter. This proves graph/queue/persistence/approval integration, not production maps-provider connectivity. Original 135 backend and 78 Flutter tests were retained, with 3 backend and 20 Flutter regressions added. All 33 React and 42 Python tests were retained.

Local evidence: `.runtime/build.log`, `.runtime/backend-final.log`, `.runtime/test-results/runtime-repair.trx`, `.runtime/flutter-analyze.log`, `.runtime/flutter-final.log`, `.runtime/launcher.log`, and `.runtime/manual-api.json`. Runtime files are ignored by Git and may include local operational details; they are not committed configuration.

The first local launcher invocation was blocked by Windows script policy. It was then verified with explicitly approved process-only `RemoteSigned`; no machine policy was changed. Earlier rebuild attempts encountered running-process file locks; the final build and tests above ran after releasing them.

## MANUAL E2E

| Requested step | Result |
| --- | --- |
| Seller My Offers | API PASS; visible Flutter screen unverified |
| Buyer Start Matching | API PASS: create, submit, start; OPEN → MATCHING |
| PENDING_APPROVAL | Manual flow blocked by missing routing configuration |
| Manager Approval | Manual flow blocked; automated integration passes |
| Buyer final transaction | Manual flow blocked; automated integration passes |
| Seller final transaction | Manual flow blocked; automated integration passes |

The local API run returned OPEN / REVISION_REQUESTED after the graph found no valid recommendation, confirming that this unsuccessful attempt did not remain MATCHING. No database statuses were manually changed.

The verified launcher uses port 8001 for its internal AI service and port 5170 for the public API in this session. A second Start Matching through that API created a different workflow ID and again transitioned MATCHING → OPEN / REVISION_REQUESTED. The API and AI launcher were left running for local follow-up. The separate port-8000 integration-test service was stopped after testing.

Browser control reported no available browser and rejected creation of an in-app browser. Windows computer-use initialization succeeded, but app discovery failed with “Computer Use native pipe is unavailable ... os error 2.” Flutter listed Windows and Edge only; no Android target was connected. Therefore no manual visible Flutter/React walkthrough is claimed.

Overall acceptance remains **FAIL / incomplete manual verification** until real routing settings are supplied, the reported Flutter build/endpoint is identified, and the requested visible walkthrough is completed. The missing routing configuration must not be replaced by invented distances or fake approvals.
