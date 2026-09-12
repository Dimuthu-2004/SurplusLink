# Database integrity and reservation concurrency

The `AddMarketplaceIntegrity` migration extends the authentication schema with normalized categories, listings,
material requests, matches, workflows, reservations, and audit logs. Database check constraints enforce positive
quantities, prices, and budgets; match score and reserved quantity ranges are also enforced in PostgreSQL.

## Create and apply the migration

Supply local secrets through environment configuration before running EF tooling:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ConnectionStrings__SurplusLink = 'Host=localhost;Port=5432;Database=surpluslink;Username=postgres;Password=your-local-password'
$env:Jwt__Secret = 'replace-with-a-local-secret-at-least-thirty-two-characters'

dotnet ef migrations add AddMarketplaceIntegrity `
  --project backend/SurplusLink.Api `
  --startup-project backend/SurplusLink.Api `
  --output-dir Data/Migrations

dotnet ef migrations script AddAuthentication AddMarketplaceIntegrity `
  --project backend/SurplusLink.Api `
  --startup-project backend/SurplusLink.Api `
  --output artifacts/add-marketplace-integrity.sql

dotnet ef database update AddMarketplaceIntegrity `
  --project backend/SurplusLink.Api `
  --startup-project backend/SurplusLink.Api
```

The migration is already checked in. Do not run `migrations add` again unless the model changes; use the script and
database-update commands to inspect or apply it.

## Roll back

Generate and inspect a reverse script before changing a shared database:

```powershell
dotnet ef migrations script AddMarketplaceIntegrity AddAuthentication `
  --project backend/SurplusLink.Api `
  --startup-project backend/SurplusLink.Api `
  --output artifacts/rollback-marketplace-integrity.sql

dotnet ef database update AddAuthentication `
  --project backend/SurplusLink.Api `
  --startup-project backend/SurplusLink.Api
```

Only remove the migration file when it has never been shared or applied:

```powershell
dotnet ef migrations remove `
  --project backend/SurplusLink.Api `
  --startup-project backend/SurplusLink.Api
```

Rolling back deletes marketplace data. Back up shared databases and coordinate the application rollback first.

## Viva notes

- Categories are separate rows referenced by foreign keys, avoiding repeated category text and update anomalies.
- Unique database indexes, unlike application-only checks, prevent duplicate email, category, and match records under
  concurrent requests. PostgreSQL `citext` makes email and category uniqueness case-insensitive.
- Check constraints protect every database write path, including scripts and future services that bypass API validation.
- Separate listing and request indexes support status-only, category-only, seller-only, and deadline-only queries. The
  match index starts with request ID and sorts score descending because ranking is performed within one request.
- UTC creation and update timestamps are stamped by the DbContext; database defaults also protect direct inserts.
- `Listing.Version` maps to PostgreSQL `xmin`. EF includes the original version in updates and reports a concurrency
  conflict when another reservation changed the listing first.
- Reservation creation, aggregate reserved-quantity update, and its audit record share one transaction. A failed check
  or concurrency update rolls back the entire operation, preventing oversubscription and partial history.
- Seed data contains deterministic reference categories only. Development users remain in the development seed and no
  production credentials are stored in migrations.
- Audit logs use `(EntityType, EntityId, CreatedAtUtc)` because the main access pattern is the chronological history of
  one entity. The generic entity reference cannot be represented by one conventional foreign key.
