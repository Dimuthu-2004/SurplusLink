# MATCHING + ROUTING + MANAGER UI REPAIR

## Verification-order bug

Root cause: GenerateAsync returned a duplicate conflict before rechecking eligibility;
bulk generation skipped existing pairs. Workflow snapshots filtered invalid stock
out entirely, which also hid rejected candidates from comparison.

Fix: explicit generation locks the requirement, reuses the unique pair, reruns
eligibility, and clears stale scores and route metrics. It audits REEVALUATE.
Verification audits STALE_LISTING_VERIFIED for non-final candidates without starting
background work. Active workflows, reservations and business outcomes protect
candidates against reset. Workflow retries take fresh snapshots, prioritize
eligible stock, preserve visible deterministic rejections and route eligible stock
only. Shared delivery-calendar-day policy remains authoritative.

Re-evaluation test: PASS. HTTP tests use distinct seller, buyer and manager identities
with Tiles quantity 500, unit price 800, requirement quantity 400 and budget 400000.
Both verification orders reuse the same candidate ID and expose persisted route
fields through the buyer and manager GET endpoint. Tests also cover concurrent
generation, no duplicate rows, insufficient quantity, self-match rejection, approval
recommendation protection, and reservation/final-outcome protection.

## Routing

Provider: existing OpenRouteService POST directions JSON adapter.
Endpoint: https://api.openrouteservice.org/v2/directions/driving-car/json
API key: Authorization header, server environment only.
Coordinates: [seller longitude, seller latitude], [buyer longitude, buyer latitude].
Units: metres to kilometres, seconds to minutes.
Cost: BaseFee + distanceKm * CostPerKm + durationMinutes * CostPerMinute;
rounded to two decimals, midpoint away from zero.
Timeout: linked cancellation deadline, default 10 seconds; allowed range (0,120].
DI: AddRoutingProvider reads Routing__ environment variables, registers the typed
HttpClient and transport estimator. It deliberately does not read appsettings or
ASP.NET user secrets for routing. Redirects are disabled and logged headers redacted.

Required: Routing__Endpoint, Routing__ApiKey, Routing__BaseFee, Routing__CostPerKm,
Routing__CostPerMinute. Optional: Routing__Provider and Routing__TimeoutSeconds.
The local launcher now imports only Routing__ settings from ignored .env.local
(or -RoutingEnvFile), preserves existing process values, and passes the environment
to ASP.NET. Missing/placeholder settings are warned about by name only.
The importer never evaluates values and never logs secrets.

Mocked provider tests: PASS, including coordinate order, distance/duration conversion,
pricing, timeout, 429, invalid response, unavailable provider and invalid coordinates.
Route persistence is covered by PostgreSQL matching/workflow tests.
Environment import test: PASS.
Real route smoke test: NOT RUN. No local routing credentials were available in the
process environment, ignored local file, or existing user-secrets keys inspected.
No fake production routing or hardcoded key was added.

## React manager comparison

Match comparison and Candidate Table: PASS. The adapter preserves actual match
status instead of collapsing non-rejected states into VALID. Rows show material,
category, required/available quantities, unit price, score, route distance/duration,
LKR transport cost and readable rejection reason. Failed routes remain visible.
Eligibility and match-status filters, score/distance/cost sorting, pagination,
requirement context, detail links, refresh, loading, empty, 403, 404, network error
and retry are covered. Changing sort/filter resets pagination. Missing requirement
context provides a Choose requirement link and makes no candidate request.

Manager authorization: PASS. Existing bearer-token API and role guards are retained;
backend tests check manager access, ownership isolation, buyer/seller restrictions
and dual roles. React tests additionally block marketplace roles on both comparison
routes. Direct match detail responses now include the same optional context as lists.

## Dashboard

Deadline source: RequirementService.SummaryAsync queries db.BuyerRequests and Deadline.
It does not join MaterialListing.AvailableUntil into the deadline results.
Requirement-only semantics: PASS.
Header renamed to Upcoming requirement deadlines: PASS.
Requirement, Status and Deadline columns are retained; no combined expiry table.

## Verification

- dotnet build backend/SurplusLink.Api/SurplusLink.Api.csproj: PASS, 0 warnings/errors.
- dotnet test backend/SurplusLink.Tests/SurplusLink.Tests.csproj: PASS, 142 passed,
  zero skipped, including real local FastAPI graph tests and disposable PostgreSQL databases.
- npm test: PASS, 41 tests across 10 files.
- npm run build: PASS.
- flutter analyze: PASS, no issues.
- flutter test: PASS, 111 tests.
- Python unittest discovery: PASS, 42 tests.
- scripts/test-routing-env.ps1: PASS.
- git diff --check: PASS.

The cross-flow acceptance was exercised through HTTP integration tests with mocked
routing. React rendered the actual API DTO shape in authenticated adapter tests;
Flutter widget/repository regressions passed. This is not a claim of a new manual
phone/browser walkthrough or a real OpenRouteService request. The temporary FastAPI
process was stopped after tests; fixtures removed only their generated databases.
Remaining blocker: real routing credentials are required for the provider smoke test.

## Files changed

- `.env.example`
- `README.md`
- `backend/SurplusLink.Api/Matching/MatchOperations.cs`
- `backend/SurplusLink.Api/Matching/MatchService.cs`
- `backend/SurplusLink.Api/Materials/MaterialInventoryService.cs`
- `backend/SurplusLink.Api/Workflows/WorkflowExecution.cs`
- `backend/SurplusLink.Tests/MatchIntegrationTests.cs`
- `docs/matching-logistics.md`
- `docs/routing-provider.md`
- `scripts/start-local.ps1`
- `web/src/app/App.tsx`
- `web/src/features/matches/MatchAnalyticsWidget.tsx`
- `web/src/features/matches/managerMatchesApi.ts`
- `web/src/pages/manager/ManagerDashboardPage.tsx`
- `web/src/pages/manager/ManagerMatchComparisonPage.tsx`
- `web/src/test/authNavigation.test.tsx`
- `web/src/test/managerDashboard.test.tsx`
- `web/src/test/managerMatches.test.tsx`
- `backend/SurplusLink.Tests/MatchingReevaluationTests.cs`
- `scripts/import-routing-env.ps1`
- `scripts/test-routing-env.ps1`
- `web/src/features/matches/matchFormatters.ts`
- `web/src/test/matchingIntegration.test.tsx`
- `docs/evidence/shared/matching-routing-manager-repair-2026-09-22.md` (this report)
