# Member 1 — Material Inventory & Listings

## Implementation

The existing shared `Categories` and `Listings` tables were extended in place rather than renamed or duplicated, because they are already referenced by `MaterialRequests`, `Matches`, and `Reservations`. In this component they are the material-category and material-listing aggregate.

- `Listing` now has Description, Unit, Condition, Latitude, Longitude, AvailableUntil, and a ListingPhoto collection.
- `ListingPhoto` has an FK to Listings, UTC audit fields, a non-negative sort-order check, and a unique `(ListingId, SortOrder)` index.
- Listing constraints include positive quantity/price, reserved quantity range, and valid paired latitude/longitude ranges.
- The listing lifecycle is `DRAFT` -> `PENDING_VERIFICATION` -> `ACTIVE` or `REJECTED`.
- Seller request DTOs omit SellerId, Status, and ReservedQuantity. A client-supplied unknown `reservedQuantity` JSON property was ignored; the persisted/returned value remained `0.000`.
- Listing updates replace photos transactionally. The implementation persists the xmin-concurrency-tracked listing first, detaches it, then inserts replacement photos so no stale parent update is sent in the second save.

## Files

- `backend/SurplusLink.Api/Models/Listing.cs`
- `backend/SurplusLink.Api/Models/ListingPhoto.cs`
- `backend/SurplusLink.Api/Models/MarketplaceStatuses.cs`
- `backend/SurplusLink.Api/Data/SurplusLinkDbContext.cs`
- `backend/SurplusLink.Api/Data/MarketplaceModelConfiguration.cs`
- `backend/SurplusLink.Api/Data/Migrations/20260915075217_AddMaterialInventoryListings.cs`
- `backend/SurplusLink.Api/Data/Migrations/20260915075217_AddMaterialInventoryListings.Designer.cs`
- `backend/SurplusLink.Api/Data/Migrations/SurplusLinkDbContextModelSnapshot.cs`
- `backend/SurplusLink.Api/Materials/MaterialDtos.cs`
- `backend/SurplusLink.Api/Materials/MaterialOperationException.cs`
- `backend/SurplusLink.Api/Materials/MaterialsControllerBase.cs`
- `backend/SurplusLink.Api/Materials/IMaterialInventoryService.cs`
- `backend/SurplusLink.Api/Materials/MaterialInventoryService.cs`
- `backend/SurplusLink.Api/Materials/MaterialListingsController.cs`
- `backend/SurplusLink.Api/Materials/MaterialCategoriesController.cs`
- `backend/SurplusLink.Api/Program.cs`
- `backend/SurplusLink.Api/Reservations/ReservationService.cs`
- `backend/SurplusLink.Tests/MaterialInventorySchemaTests.cs`

## Endpoints

| Endpoint | Access | Behavior |
|---|---|---|
| `POST /api/materials` | SELLER | Creates a DRAFT listing. |
| `GET /api/materials/{id}` | SELLER/BUYER/MANAGER | Seller sees own; buyer sees only unexpired ACTIVE; manager sees all. |
| `PUT /api/materials/{id}` | owning SELLER | Replaces editable DRAFT/REJECTED details and photos. |
| `DELETE /api/materials/{id}` | owning SELLER | Deletes unless reservations or matches exist. |
| `GET /api/materials/my` | SELLER | Returns own listings. |
| `PATCH /api/materials/{id}/publish` | owning SELLER | Moves DRAFT/REJECTED to PENDING_VERIFICATION. |
| `PATCH /api/materials/{id}/verify` | MANAGER | `{ "approved": true }` activates; `false` rejects. |
| `GET/POST/PUT/DELETE /api/material-categories` | MANAGER | Material category CRUD; deletion is blocked while in use. |

## Swagger Verification

Final Release verification used `http://localhost:5171/swagger`.

| Check | Actual |
|---|---|
| Swagger JSON | HTTP 200 |
| Seller/buyer public registration; manager seeded login | 201 / 201 / 200 |
| Manager category read | 200 |
| Seller listing create, photo update, publish, manager verify | 201 / 200 / 200 / 200 |
| Lifecycle | DRAFT -> PENDING_VERIFICATION -> ACTIVE |
| Buyer read before/after activation | 404 / 200 |
| Seller `GET /api/materials/my` | 200 |
| Manager rejection then seller delete | 201 / 200 / 200 / 204 |
| Manager temporary category CRUD | 201 / 200 / 204 |
| Buyer create / seller verify role guards | 403 / 403 |
| ReservedQuantity client-write attempt | response remained `0.000` |
| Duplicate photo sort-order validation | 400 |

## Validation

- Applied migration: `20260915075217_AddMaterialInventoryListings`.
- PostgreSQL confirmed all six added Listings columns, both new Listing indexes, the ListingPhotos unique index, and the ListingPhotos FK.
- `dotnet build` Release: 0 warnings, 0 errors.
- `dotnet test` Release: 15 passed, 0 failed.
