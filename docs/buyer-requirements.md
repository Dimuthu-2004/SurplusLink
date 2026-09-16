# Buyer Requirements (Member 2 / M2-1)

## Start locally

Use the existing shared repository at `C:\Users\Dimuthu_K\SurplusLink\SurplusLink`, branch `feature/m2-1-backend-core-db`.

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
Create/update accept only categoryId, requiredQuantity, unit, maximumBudget, deadline, latitude, longitude.
Unknown properties, including buyerId and status, return 400.

| Route | Access | Success |
| --- | --- | --- |
| POST /api/requirements | BUYER | 201, DRAFT and Location header |
| GET /api/requirements/{id} | Owning BUYER or MANAGER | 200 |
| PUT /api/requirements/{id} | Owning BUYER, DRAFT only | 200 |
| DELETE /api/requirements/{id} | Owning BUYER, DRAFT only | 204 |
| GET /api/requirements/my | BUYER | 200, own rows only |
| GET /api/requirements | MANAGER | 200, all buyers for monitoring |
| POST /api/requirements/{id}/submit | Owning BUYER, DRAFT only | 200, OPEN |
| POST /api/requirements/{id}/start-matching | Owning BUYER, OPEN only | Currently 503; see M4-2 dependency |
| POST /api/requirements/{id}/cancel | Owning BUYER, DRAFT or OPEN | 200, CANCELLED |

Both lists accept page (default 1, maximum 1,000,000) and pageSize (default 20, maximum 100).
Responses contain items, total, page, pageSize. Ordering is newest creation first, then ID.
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

## Migration and compatibility

Migration: `backend/SurplusLink.Api/Data/Migrations/20260916095201_AddBuyerRequirements.cs`.

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
then run `dotnet test SurplusLink.sln`. For this local user-secrets setup:

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
