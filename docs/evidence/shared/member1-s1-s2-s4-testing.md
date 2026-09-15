# Member 1 Shared Testing Report

## Environment

- Testing date/time: 2026-09-15T12:41:59.4800175+05:30
- .NET SDK: 8.0.423
- PostgreSQL: server 18.4; `psql` client 18.4
- EF Core/Npgsql project version: 8.0.10
- EF CLI: 10.0.12 (global; no repository tool manifest; version differs from project EF Core)
- Actual API URL used: `http://localhost:5170` (health and Swagger both returned HTTP 200).
- Database identifier used: `Host=localhost;Port=5432;Database=surpluslink;Username=postgres;Password=***`
- Swagger: `http://localhost:5170/swagger` and `/swagger/v1/swagger.json`; live Swagger JSON returned HTTP 200.

Discovery confirmed the expected root scaffold: `backend/SurplusLink.Api`, `backend/SurplusLink.Tests`, `ai-service`, `docs`, `.github`, `README.md`, `.gitignore`, and `.env.example`. The API uses `ConnectionStrings:SurplusLink`, bound to `DatabaseOptions`, through environment configuration or user secrets. The design-time factory reads `ConnectionStrings__SurplusLink` (or a local fallback). JWT configuration keys are `Jwt:Issuer`, `Jwt:Audience`, `Jwt:Secret`, and `Jwt:ExpirationMinutes`; the committed non-secret issuer/audience are `SurplusLink.Api` / `SurplusLink.Clients` with 60-minute expiry. Launch profiles configure HTTP `http://localhost:5170`; HTTPS configures `https://localhost:7197` plus HTTP `http://localhost:5170`. Auth contract: `RegisterRequest { Email, Password, Role }`, `LoginRequest { Email, Password }`, `AuthResponse { Token, User }`; public roles are `SELLER` and `BUYER`.

## Revalidation

Revalidated at the end of this review on 2026-09-15 (local development environment): live `/health` and `/swagger/v1/swagger.json` each returned HTTP 200; `dotnet ef migrations list` returned both known migrations when the existing user-secret connection was passed process-locally; `dotnet ef database update` returned “database is already up to date”; PostgreSQL listed the nine current tables and both migration-history records; `dotnet test` passed 14/14 again. No configuration values, credentials, JWTs, or hashes were printed.

## S1 Results

| Test | Command/Method | Expected | Actual | PASS/FAIL/BLOCKED |
|---|---|---|---|---|
| Shared scaffold | Root inventory | Expected paths exist | All found | PASS |
| Toolchain | `git`, `dotnet`, `python`, `psql` version commands | Tools available | Git 2.45.1; .NET 8.0.423; Python 3.14.3; psql 18.4 | PASS |
| Project files | File checks | API/test `.csproj` files exist | Both found | PASS |
| AI scaffold | File inventory | AI scaffold exists | `ai-service/app/main.py`, package init, requirements found | PASS |
| API restore | `dotnet restore ./backend/SurplusLink.Api/SurplusLink.Api.csproj` | Succeeds | Up to date; success | PASS |
| Test restore | `dotnet restore ./backend/SurplusLink.Tests/SurplusLink.Tests.csproj` | Succeeds | Up to date; success | PASS |
| API build | `dotnet build ...Api.csproj --no-restore` | Succeeds | 0 warnings, 0 errors | PASS |
| Test build | `dotnet build ...Tests.csproj --no-restore` | Succeeds | 0 warnings, 0 errors | PASS |
| Python compilation | `python -m compileall ./ai-service` | Succeeds | Completed without errors | PASS |

## S2 Results

The valid local `Jwt:Secret` was supplied through user secrets after the initial run. The already-running API was verified at `http://localhost:5170`: `/health` and `/swagger/v1/swagger.json` both returned HTTP 200. Generated test emails/passwords/tokens were not printed. A PowerShell-to-curl quoting issue initially produced invalid JSON; using JSON via standard input corrected the test client. This was not an API defect.

| Test | Command/Method | Expected | Actual | PASS/FAIL/BLOCKED |
|---|---|---|---|---|
| AUTH-01 Valid SELLER registration | Live `POST /api/auth/register` | 201 and persisted user | HTTP 201; `token` and SELLER user returned | PASS |
| AUTH-02 Valid BUYER registration | Live `POST /api/auth/register` | 201 and persisted user | HTTP 201; `token` and BUYER user returned | PASS |
| AUTH-03 Public MANAGER registration | Live `POST /api/auth/register` | Rejected; no user | HTTP 400; DB assertion found 0 public MANAGER rows | PASS |
| AUTH-04 Empty registration body | Live `POST /api/auth/register` | Validation rejection | HTTP 400 | PASS |
| AUTH-05 Invalid email/field | Live invalid email and short password | Rejected | HTTP 400 | PASS |
| AUTH-06 Duplicate email | Two live registrations | Conflict; one account | Second request HTTP 409 | PASS |
| AUTH-07 Password storage | Two live registrations + PostgreSQL assertion | Hash differs from plaintext | HTTP 201 each; DB returned 2 rows and `hash-not-plaintext=true` | PASS |
| AUTH-08 Valid login | Live `POST /api/auth/login` | Token and user returned | HTTP 200; `token` and user returned | PASS |
| AUTH-09 Wrong password | Live `POST /api/auth/login` | Rejected; no token | HTTP 401 | PASS |
| AUTH-10 Me without token | Live `GET /api/auth/me` | Protected data withheld | HTTP 401 | PASS |
| AUTH-11 Me with JWT | Live `GET /api/auth/me` | User returned | HTTP 200; authenticated SELLER returned | PASS |
| AUTH-12 JWT role claim | Decode generated JWT locally | Role claim exists | JWT `ClaimTypes.Role` value was SELLER | PASS |
| AUTH-13 Seeded MANAGER | DB role-count query + seed-source inspection | Seed status reported | SELLER, BUYER and MANAGER rows exist; Development seed supports all roles | PASS |
## S4 Results

### A. CURRENT SHARED SCHEMA — tested now

| Test | Command/Method | Expected | Actual | PASS/FAIL/BLOCKED |
|---|---|---|---|---|
| EF CLI | `dotnet ef --version` | Available | 10.0.12; package mismatch noted | PASS |
| EF/Npgsql packages | `dotnet list package` | Relevant packages present | EF Core Design/Runtime and Npgsql all 8.0.10 | PASS |
| Migrations | `dotnet ef migrations list` | Migrations visible | `20260911095641_AddAuthentication`, `20260912042326_AddMarketplaceIntegrity` | PASS |
| Migration application | `dotnet ef database update` with configured connection | Applies/current | Succeeded; database already up to date | PASS |
| Migration history | PostgreSQL query | History visible | Both migrations recorded at product version 8.0.10 | PASS |
| Tables | `psql \dt` | Current tables visible | 9 current tables including `Users` and history | PASS |
| Auth table | information_schema + pg_indexes | User columns/indexes identified | `Users`: Id, citext Email, PasswordHash, Role, UTC timestamps; unique Email index | PASS |
| Constraints | pg_constraint metadata | PK/FK/check constraints visible | PKs, FKs, and numeric checks present | PASS |
| Indexes | pg_indexes metadata | Current indexes visible | 27 indexes, including required unique/query indexes | PASS |
| Timestamps | information_schema metadata | Applicable timestamp fields present | All 8 auditable current tables have UTC fields/defaults | PASS |
| Migration seed data | PostgreSQL count | Seed data exists | 6 category rows | PASS |
| Development user seed state | PostgreSQL role count | Existing user seed state reported | SELLER, BUYER, MANAGER rows present; passwords/hashes not displayed | PASS |
| Fresh isolated schema creation | New empty DB migration test | Fresh creation demonstrated | Not run: scope used only configured DB; no database name invented/created | BLOCKED |

### B. FUTURE S4 ITEMS — must be rechecked after M1–M4 integration

Do not regard absent, renamed, or later-owned business entities as a failure now. After each member integrates schema changes, recheck migrations, relationships/delete behavior, PK/FK/check/unique constraints, indexes, timestamps, and seed data for those owned entities.

## Automated Test Results

Command: `dotnet test ./backend/SurplusLink.Tests/SurplusLink.Tests.csproj --no-restore --logger "console;verbosity=normal"`

- Total: 14
- Passed: 14
- Failed: 0
- Duration: 2.5798 seconds

The executed tests cover role DTO validation, model constraints/indexes/timestamps/seeds, reservation DI, health, CORS, Swagger JWT definition, validation/404 problem details, and safe exception responses. TestServer logged an HTTPS-redirection warning with no impact on results.

## Problems Found

1. Global `dotnet-ef` 10.0.12 differs from project EF Core/Npgsql 8.0.10. It worked for listing/update here, but should be aligned before migration authoring.
2. The design-time context factory reads `ConnectionStrings__SurplusLink` or its fallback, not user secrets. A bare migration-list call could not authenticate to determine applied/pending state; applied history was verified in PostgreSQL after passing the configured connection process-locally.
3. Security review: tracked configuration has placeholders only and `.gitignore` excludes `.env`/`*.secret`. Development-only test account passwords exist in seed source; they are not reproduced here and must remain non-production only.
4. The original missing-`Jwt:Secret` local configuration blocker was resolved by the user through local user secrets; it is not a repository code defect.

## Small Fixes Made

None. The JWT startup failure is missing local secret configuration, not a safe source-code change without a user-supplied secret.

## Remaining Blockers

- Align `dotnet-ef` with EF Core 8.0.10 before migration authoring.
- A fresh isolated database migration test was intentionally not run because no additional database was created.

## Evidence to Screenshot

- API and test build output (0 warnings/0 errors).
- `dotnet test` summary (14 passed, 0 failed).
- `dotnet ef database update` plus the two-row migration-history query.
- `psql \dt`, `Users` metadata/indexes, and constraints/indexes output.
- Live API health/Swagger response at `http://localhost:5170`, with no secret values visible.
- After local JWT configuration: `Now listening on`, Swagger, registration/login, authenticated `/api/auth/me`, and a non-plaintext hash check.


