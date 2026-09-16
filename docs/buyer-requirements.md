# Buyer Requirements (Member 2 / M2-1 and M2-2)

## Start locally

Use the existing shared repository at `C:\Users\Dimuthu_K\SurplusLink\SurplusLink`, branch `feature/m2-2-matching-status-logic`.

From that repository root:

```powershell
dotnet run --project backend/SurplusLink.Api --launch-profile http
```

Open http://localhost:5170/swagger. Existing Development startup applies pending migrations and reads your saved database/JWT configuration. This change was tested in a disposable database; the normal development database was not migrated during implementation.

For an explicit EF update, supply the same connection string to the design-time factory. It reads an environment variable, not user secrets automatically. If your connection is already saved in this project's user secrets:

```powershell
$settingsPath = Join-Path $env:APPDATA 'Microsoft/UserSecrets/c445f959-799c-440e-91c2-aeddb53a0fb6/secrets.json'
$localSettings = Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json
$env:ConnectionStrings__SurplusLink = $localSettings.'ConnectionStrings:SurplusLink'
dotnet ef database update --project backend/SurplusLink.Api
dotnet ef migrations list --project backend/SurplusLink.Api
Remove-Item Env:ConnectionStrings__SurplusLink
```

Do not commit connection strings, tokens, or user secrets.

## API contract

All routes use the existing JWT authentication. Buyer identity comes from the token.
Create/update accept categoryId, requiredQuantity, unit, maximumBudget, deadline, latitude, longitude, and optional notes (maximum 2,000 characters). Notes are trimmed; null or omission clears them on PUT.
Unknown properties, including buyerId and status, return 400.

| Route | Access | Success |
| --- | --- | --- |
| POST /api/requirements | BUYER | 201, DRAFT and Location header |
| GET /api/requirements/{id} | Owning BUYER or MANAGER | 200 |
| PUT /api/requirements/{id} | Owning BUYER, DRAFT only | 200 |
| DELETE /api/requirements/{id} | Owning BUYER, DRAFT only | 204 |
| GET /api/requirements/my | BUYER | 200, own rows only |
| GET /api/requirements | MANAGER | 200, all buyers for monitoring |
| GET /api/requirements/{id}/history | Owning BUYER or MANAGER | 200, paginated audit history |
| GET /api/requirements/analytics/summary | MANAGER | 200, aggregate dashboard data |
| POST /api/requirements/{id}/submit | Owning BUYER, DRAFT only | 200, OPEN |
| POST /api/requirements/{id}/start-matching | Owning BUYER, OPEN only | Currently 503; see M4-2 dependency |
| POST /api/requirements/{id}/cancel | Owning BUYER, DRAFT or OPEN | 200, CANCELLED |

Both lists accept page (default 1, maximum 1,000,000) and pageSize (default 20, maximum 100).
Responses contain items, total, page, pageSize. Total counts all filtered rows before pagination. Default ordering is newest creation first, then ID. Full query options are below.
Dates are UTC. Responses expose createdAt and updatedAt using the shared audit timestamp fields.

Rules:
- Quantity: positive, at most 3 decimal places, maximum 999999999999999.999.
- Budget: positive, at most 2 decimal places, maximum 9999999999999999.99.
- Unit: nonblank, maximum 32 characters, trimmed.
- Category must exist.
- Deadline must be future on create, update, submit, and start-matching. Supply an ISO 8601 timestamp with Z or an offset.
- New requests require both coordinates: latitude -90..90, longitude -180..180, at most 6 decimal places.
- 400 = invalid input/deadline; 401 = unauthenticated; 403 = role/ownership denied; 404 = missing ID; 409 = invalid state or linked draft deletion.
- Editing/deletion is restricted to DRAFT. Cancellation after matching begins requires workflow coordination and is intentionally unavailable here.
- All nine states are persisted: DRAFT, OPEN, MATCHING, MATCH_FOUND, PENDING_APPROVAL, APPROVED, REJECTED, COMPLETED, CANCELLED. Later workflow transitions are not buyer-controlled CRUD.
- Mutations write to the existing AuditLogs infrastructure. Row locks serialize lifecycle changes; PostgreSQL xmin also provides a concurrency token.

## Search, filters, and sorting (M2-2)

Both GET /api/requirements (manager) and GET /api/requirements/my (buyer) accept:

| Parameter | Meaning |
| --- | --- |
| search | Case-insensitive substring in category name OR notes; trimmed; maximum 200 characters. Percent, underscore, and backslash are literal characters. |
| status | One named lifecycle status, case-insensitive; numeric/unknown values return 400. |
| categoryId | Category GUID; a nonexistent category produces an empty list. |
| deadlineFrom / deadlineTo | Inclusive bounds, individually optional; ISO 8601 timestamps with Z or offset. Reversed bounds return 400. |
| sort | deadline, budget, or createdAt; default createdAt. |
| sortDir | asc or desc; default desc. |
| page / pageSize | 1-based page and maximum 100 items; defaults 1 and 20. |

All supplied filters are combined with AND. Buyer scope is enforced before searching/counting.
Sort values are case-insensitive; ties always use ID ascending for stable pagination.
An empty result or a page past the end returns items: [] and the correct total.

Example:

```text
/api/requirements/my?search=cement&status=OPEN&sort=deadline&sortDir=asc&page=1&pageSize=10
/api/requirements?deadlineFrom=2026-10-01T00:00:00Z&deadlineTo=2026-10-31T23:59:59Z&sort=budget&sortDir=desc
```

## History (M2-2)

GET /api/requirements/{id}/history accepts page/pageSize with the same bounds/defaults.
It returns items, total, page, pageSize, ordered oldest first, then audit ID.
Each item has id, actorUserId, action, fromStatus, toStatus, createdAt.

Existing CREATED, UPDATED, DELETED, SUBMITTED, MATCHING_STARTED, and CANCELLED action names
are preserved. A successful transition also records STATUS_CHANGED with the old/new status.
Status transitions use the existing AuditLogs.Action column as
`STATUS_CHANGED:<old>:<new>`; the history response exposes its parts as separate fields.
No shared audit schema change or duplicate history table is needed.

The DbContext records status changes for tracked BuyerRequest updates through synchronous or
asynchronous SaveChanges, in the same transaction as the state change. Repeated unchanged saves
add no events. A buyer action supplies its actor; background changes without an explicit action
have actorUserId: null. Bulk SQL/ExecuteUpdate bypasses SaveChanges, so future workflow code
must use tracked changes or explicitly write its audit within the same transaction.
Failed/rejected starts and rolled-back transitions do not create success history. The deferred
M4-2 starter continues to return 503 and leave OPEN unchanged.

Owners and managers may read history; other buyers/sellers cannot. Missing/deleted requirements
return 404, even if retained audit rows exist. Legacy audit actions remain visible, but historical
status pairs before M2-2 are not invented or backfilled.

## Manager analytics (M2-2)

GET /api/requirements/analytics/summary?upcomingDays=7 is manager-only.
upcomingDays accepts 1..365 and defaults to 7. The response includes:

- total and countsByStatus (all nine statuses, including zero counts).
- countsByCategory with category ID/name, including categories with zero requirements.
- openCount: exactly status OPEN, including overdue OPEN rows.
- asOf and upcomingUntil: the UTC window used for the summary.
- upcomingDeadlineCount and upcomingDeadlines: all qualifying rows counted, with the first
  10 returned by earliest deadline then ID. Includes OPEN, MATCHING, MATCH_FOUND,
  PENDING_APPROVAL, APPROVED; excludes expired deadlines, drafts, rejected and terminal rows.
- averageMaximumBudget across all existing requirements; null when none exist.
- averageQuantityByUnit with unit, averageRequiredQuantity and count, keeping different unit
  strings separate (for example, kg and unit). No unit conversion or mixed-unit average is used.

Counts, averages and the deadline preview share a repeatable-read database snapshot. Analytics
covers all requirements and does not inherit list filters. This endpoint does not modify state,
expire requirements automatically, start workflows or reserve stock.

## Migration and compatibility

M2-1 migration: `backend/SurplusLink.Api/Data/Migrations/20260916095201_AddBuyerRequirements.cs`.

M2-2 migration: `backend/SurplusLink.Api/Data/Migrations/20260916102925_AddBuyerRequirementNotes.cs`.
It only adds non-null Notes (maximum 2,000 characters) to MaterialRequests, backfilling existing
rows with an empty string. Downgrading this migration removes the notes; request IDs and
workflow/audit tables are unchanged.

BuyerRequest replaces the old MaterialRequest CLR class and uses one DbSet, BuyerRequests.
It maps onto the existing **MaterialRequests** table. RequiredQuantity maps to Quantity,
MaximumBudget to Budget, Deadline to DeadlineUtc. CreatedAtUtc/UpdatedAtUtc remain the shared timestamp columns.
The legacy Title column is retained; new requests receive an internal category-based title.

The migration adds Unit and nullable Latitude/Longitude, plus checks for a nonblank unit,
paired valid coordinates, and the nine statuses. Existing positive quantity/budget checks
and restrictive buyer/category foreign keys remain. It adds (BuyerId, CreatedAtUtc) and
(Status, DeadlineUtc) indexes, retaining the status/category/deadline indexes.
The buyer/creation compound index replaces the redundant BuyerId-only index.
The generated xmin operation uses PostgreSQL's existing system column.

Existing request IDs, match links, reservation links, and quantities/budgets are preserved.
Legacy rows receive Unit = "unit" and null coordinates because their original units/locations
were not stored. These values must be reviewed before future matching consumes legacy rows.
Status conversion: MATCHED -> MATCH_FOUND, FULFILLED -> COMPLETED, EXPIRED -> CANCELLED.
OPEN and CANCELLED retain their meaning.

No workflow tables are added, dropped, or changed. The old match-based Workflows scaffold is
not the canonical AgentWorkflow schema. Existing migration files are untouched; the current
snapshot changes only for BuyerRequest. Other members should merge this migration and regenerate
their own new migration/snapshot if they independently changed the same EF snapshot.

Rollback removes the new unit/location fields and maps new statuses to older equivalents.
It loses the finer lifecycle distinctions (including the previous EXPIRED distinction); back up data before using a downgrade.

## Swagger manual tests

1. Start the API and open Swagger. Use existing authentication to obtain tokens for Buyer A,
   Buyer B, a seller, and a manager. Click **Authorize** and enter the token; switch tokens for role tests.
2. As Buyer A, POST /api/requirements with the JSON below. Replace deadline with a future date.
   Expect 201, status DRAFT, your buyerId, timestamps, and an id. Copy the id.
3. GET that id: 200. GET /my: it contains the id. PUT the same body with quantity 12: 200.
   The createdAt value stays the same to database timestamp precision; updatedAt advances.
4. Create a second draft and DELETE it: 204. GET it again: 404.
5. On the first draft, start-matching: 409. Submit: 200 and OPEN.
   Submit again, edit, or delete the OPEN request: each returns 409.
6. Start-matching on the OPEN request twice: each returns 503 with
   "Matching is not available yet. Your requirement remains open."
   GET it again: OPEN. No reservation, stock, or workflow records should change.
7. Cancel that OPEN request: 200 and CANCELLED. Submit/start/cancel again: 409.
   Create a separate draft and cancel it to check DRAFT -> CANCELLED.
8. As Buyer B, GET/PUT/DELETE/submit/start/cancel Buyer A's existing request: 403.
   GET /my must contain only Buyer B's rows.
9. As manager, GET /api/requirements and GET a buyer's id: 200.
   Create/update/delete/submit/start/cancel: 403. As seller: requirement routes return 403.
   With no token: 401.
10. Repeat creation with zero/negative quantity or budget; too many decimal places; blank unit;
    missing/unknown category; past or missing deadline; missing/out-of-range coordinates:
    each returns 400. Adding buyerId or status also returns 400.
11. GET /my?page=0 or ?pageSize=101: 400. Use a nonexistent GUID for each item action: 404.
12. To verify deadline expiry, create a draft with a deadline a few seconds ahead, wait past it,
    then submit: 400 and DRAFT. Separately submit before expiry, wait, then start: 400 and OPEN.

Example creation/update body:

```json
{
  "categoryId": "00000000-0000-0000-0000-000000000101",
  "requiredQuantity": 10,
  "unit": "kg",
  "maximumBudget": 25000,
  "deadline": "2026-12-31T12:00:00Z",
  "latitude": 6.9271,
  "longitude": 79.8612
}
```

### Additional M2-2 Swagger checks

1. Create several drafts with different categories, budgets, deadlines and notes. As a buyer,
   search for part of a category name and then part of notes; mixed case must match.
2. Combine status, categoryId and deadline bounds. Verify total is the full filtered count;
   use pageSize=1 and page=2 to confirm each page contains a different row. Run all six
   sort/sortDir combinations; tied values retain a stable order.
3. Switch to another buyer: /my never exposes the first buyer's records, including totals.
   Switch to manager: the same filters on /api/requirements search every buyer's rows.
4. Try invalid status, sort, sortDir, reversed deadlines, page=0, pageSize=101: expect 400.
5. GET a new draft's /history: CREATED. Update, submit, then cancel; history should include
   UPDATED, SUBMITTED, CANCELLED and DRAFT -> OPEN -> CANCELLED status events. A second
   submit/cancel returns 409 and adds no events. Start on OPEN returns 503 until M4-2 is wired,
   and adds no MATCHING_STARTED or status change.
6. Read history as another buyer/seller: 403; as manager: 200. Test history pagination.
7. As manager, GET /analytics/summary and compare counts to the list. Drafts/cancelled rows
   are not upcoming deadlines. Try upcomingDays=1 and 30 to check the window. As buyer/seller:
   403; with upcomingDays=0: 400.

## M4-2 dependency and M2-7 handoff

Production registration currently uses DeferredRequirementWorkflowStarter. It deliberately returns
503 before changing the OPEN requirement; no placeholder workflow ID is returned.

M4-2 must replace that registration with its canonical implementation of
IRequirementWorkflowStarter.StartAsync(requirementId, buyerId, cancellationToken):
- Create or reuse the real AgentWorkflow, using requirementId as the idempotency key.
- Use the same scoped SurplusLinkDbContext and the current transaction; do not start a nested
  transaction or commit independently. Persist the workflow before returning its nonempty ID.
- Add any canonical persistence/link/idempotency constraints in the M4-owned migration.
- Do not reserve material during workflow start.
- Leave AgentStep, AgentToolCall, Approval and orchestration under M4 ownership.
- Coordinate any later workflow cancellation/state transitions with the requirement lifecycle.

On success the existing service sets MATCHING and returns { requirement, workflowId }.
Concurrent starts are serialized. Once MATCHING, repeated starts return 409 before invoking the
starter, preventing accidental duplicates. Failed starts roll back the transaction.
M2-7 must test this with real canonical persistence, including a failed-save/retry and concurrent
calls, confirming a single stable persisted workflow ID and unchanged stock.
The automated success-path test uses a test double only; it does not prove canonical persistence.

## Automated verification

`backend/SurplusLink.Tests/BuyerRequirementsIntegrationTests.cs` uses a disposable PostgreSQL database
with a random surpluslink_m2_test_ prefix. It upgrades the existing M1 migration with linked legacy
rows, exercises HTTP endpoints using real JWT validation, checks SQL constraints and concurrent
transitions, and removes its database afterward.

Set SURPLUSLINK_TEST_CONNECTION to PostgreSQL credentials that can create/drop a test database,
then run `dotnet test SurplusLink.sln`.
`RequirementQueriesAndHistoryTests.cs` additionally covers query combinations, literal search,
sort directions, pagination, notes validation, history authorization, concurrent starts,
tracked status changes, transaction rollback, analytics and empty summaries. For this local user-secrets setup:

```powershell
$settingsPath = Join-Path $env:APPDATA 'Microsoft/UserSecrets/c445f959-799c-440e-91c2-aeddb53a0fb6/secrets.json'
$localSettings = Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json
$env:SURPLUSLINK_TEST_CONNECTION = $localSettings.'ConnectionStrings:SurplusLink'
dotnet test SurplusLink.sln
Remove-Item Env:SURPLUSLINK_TEST_CONNECTION
```

Without that variable, the PostgreSQL integration tests explicitly skip; existing tests still run.

## Exact changed files

New:
- backend/SurplusLink.Api/Models/BuyerRequest.cs
- backend/SurplusLink.Api/Requirements/RequirementContracts.cs
- backend/SurplusLink.Api/Requirements/RequirementService.cs
- backend/SurplusLink.Api/Requirements/RequirementsController.cs
- backend/SurplusLink.Api/Workflows/IRequirementWorkflowStarter.cs
- backend/SurplusLink.Api/Data/Migrations/20260916095201_AddBuyerRequirements.cs
- backend/SurplusLink.Api/Data/Migrations/20260916095201_AddBuyerRequirements.Designer.cs
- backend/SurplusLink.Tests/BuyerRequirementsIntegrationTests.cs
- docs/buyer-requirements.md

Updated:
- backend/SurplusLink.Api/Data/MarketplaceModelConfiguration.cs
- backend/SurplusLink.Api/Data/SurplusLinkDbContext.cs
- backend/SurplusLink.Api/Data/Migrations/SurplusLinkDbContextModelSnapshot.cs
- backend/SurplusLink.Api/Models/MarketplaceStatuses.cs
- backend/SurplusLink.Api/Models/MaterialMatch.cs
- backend/SurplusLink.Api/Models/Reservation.cs
- backend/SurplusLink.Api/Materials/MaterialInventoryService.cs
- backend/SurplusLink.Api/Reservations/ReservationService.cs
- backend/SurplusLink.Api/Program.cs
- backend/SurplusLink.Tests/SchemaIntegrityTests.cs

Replaced:
- backend/SurplusLink.Api/Models/MaterialRequest.cs -> Models/BuyerRequest.cs


### Additional M2-2 files

New:
- backend/SurplusLink.Api/Requirements/RequirementQueryContracts.cs
- backend/SurplusLink.Api/Requirements/RequirementQueryBuilder.cs
- backend/SurplusLink.Api/Data/Migrations/20260916102925_AddBuyerRequirementNotes.cs
- backend/SurplusLink.Api/Data/Migrations/20260916102925_AddBuyerRequirementNotes.Designer.cs
- backend/SurplusLink.Tests/RequirementQueriesAndHistoryTests.cs

Updated:
- backend/SurplusLink.Api/Models/BuyerRequest.cs
- backend/SurplusLink.Api/Data/MarketplaceModelConfiguration.cs
- backend/SurplusLink.Api/Data/SurplusLinkDbContext.cs
- backend/SurplusLink.Api/Data/Migrations/SurplusLinkDbContextModelSnapshot.cs
- backend/SurplusLink.Api/Requirements/RequirementContracts.cs
- backend/SurplusLink.Api/Requirements/RequirementService.cs
- backend/SurplusLink.Api/Requirements/RequirementsController.cs
- backend/SurplusLink.Tests/BuyerRequirementsIntegrationTests.cs
- docs/buyer-requirements.md
