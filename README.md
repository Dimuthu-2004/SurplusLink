# SurplusLink

SE3090 group monorepo for an AI-powered construction-material surplus marketplace. This repository is intentionally a scaffold: it contains no marketplace, authentication, payment, recommendation, or workflow features.

## Architecture boundary

```text
React TypeScript web ─┐
                      ├──> ASP.NET Core Web API ───> EF Core ───> PostgreSQL
Flutter mobile ───────┘             │
                                    └──> FastAPI + LangGraph (internal only)
```

The Web API is the only public backend. Web and mobile must call only that API: neither may connect to PostgreSQL nor call the AI service. The API owns the database connection and is the only caller of the internal AI service.

## Layout

- `backend/SurplusLink.Api` — public ASP.NET Core API; EF Core and Npgsql boundary.
- `backend/SurplusLink.Tests` — API test project.
- `web` — React + TypeScript client.
- `mobile` — Flutter client.
- `ai-service` — internal Python FastAPI + LangGraph service.
- `docs/adr`, `docs/diagrams`, `docs/evidence/m1..m4`, `docs/ai-logs` — architecture, milestone evidence, and AI trace records.

## Windows PowerShell: bootstrap from an empty folder

```powershell
mkdir SurplusLink; Set-Location SurplusLink; git init
dotnet new sln -n SurplusLink
mkdir backend, docs, .github
dotnet new webapi -n SurplusLink.Api -o backend/SurplusLink.Api --framework net8.0 --use-controllers --no-openapi
dotnet new xunit -n SurplusLink.Tests -o backend/SurplusLink.Tests --framework net8.0
dotnet sln SurplusLink.sln add backend/SurplusLink.Api/SurplusLink.Api.csproj backend/SurplusLink.Tests/SurplusLink.Tests.csproj
dotnet add backend/SurplusLink.Tests/SurplusLink.Tests.csproj reference backend/SurplusLink.Api/SurplusLink.Api.csproj
npm create vite@latest web -- --template react-ts
flutter create mobile --org com.surpluslink
mkdir ai-service, ai-service/app, docs/adr, docs/diagrams, docs/evidence/m1, docs/evidence/m2, docs/evidence/m3, docs/evidence/m4, docs/ai-logs, .github/workflows
```

Then add the repository configuration files in this scaffold and install client dependencies:

```powershell
npm --prefix web install
python -m pip install -r ai-service/requirements.txt
```

## Run commands

Run each in a separate PowerShell window from the repository root:

```powershell
# Public API
dotnet run --project backend/SurplusLink.Api

# Web client
npm --prefix web run dev

# Mobile client (with a device/emulator selected)
Push-Location mobile; flutter run; Pop-Location

# Internal AI service
python -m uvicorn app.main:app --app-dir ai-service --reload
```

## Local configuration

`.env.example` documents the API-owned development configuration. Supply those values as API environment variables (for example, `ConnectionStrings__SurplusLink`); never expose them in web or mobile builds or commit real credentials. The API fails fast when database, JWT, or configured CORS settings are missing or invalid. The JWT signing secret must be supplied through secure environment configuration and is never stored in this repository.

In development, Swagger UI is available at `/swagger` and describes JWT bearer authentication. The unauthenticated `/health` endpoint is a lightweight liveness check.

Database integrity rules, migration and rollback commands, and viva notes are documented in [`docs/database-integrity.md`](docs/database-integrity.md).


