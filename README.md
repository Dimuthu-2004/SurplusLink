# SurplusLink

SurplusLink is the SE3090 construction-material surplus marketplace. The existing application includes inventory, buyer requirements, matching/logistics, offers, manager approval, reservations, transaction completion, audit history and four controlled agents.

## Architecture

```text
React manager web -----+
                      +--> ASP.NET Core API --> EF Core / PostgreSQL
Flutter marketplace --+          |
                                 +--> internal FastAPI / LangGraph
                                 +--> routing / reverse-geocoding providers
```

Only ASP.NET owns authentication, authorization, validation, persistence and reservation. Clients never call PostgreSQL or Python directly. The four-agent sequence is Planner -> Matching -> Logistics -> Validation -> PENDING_APPROVAL. Inventory is reserved only after a manager decision.

## Roles and ownership

Public registration accepts SELLER, BUYER, or separate SELLER and BUYER assignments on one account. Flutter presents Sell / Buy / Both. BOTH is not an authorization role. MANAGER is provisioned separately. JWTs carry separate role claims; ownership checks still apply to every marketplace resource. Self-matching and self-trading are blocked deterministically.

## Repository

| Directory | Responsibility |
| --- | --- |
| `backend/SurplusLink.Api` | Shared API, database, routing and workflow worker |
| `backend/SurplusLink.Tests` | Unit, API, PostgreSQL, concurrency and live graph tests |
| `web` | React manager dashboard, inventory, requirements, comparison and approvals |
| `mobile` | Flutter seller/buyer marketplace and participant outcomes |
| `ai-service/app` | Internal FastAPI and four LangGraph agents |
| `docs` | Architecture, ADRs, setup, testing, evidence and deployment |
| `perf` | k6 scenario and measured-output location |

## Continue an existing checkout

Use .NET SDK **8.0.423** (`global.json`). This audit used Node **24.14.0**, Flutter **3.47.3**, Dart **3.13.3**, and Python **3.14**; CI currently targets Node 22, Python 3.12 and PostgreSQL 18. Do not replace working local configuration simply to match an audit environment.

```powershell
dotnet restore SurplusLink.sln
npm --prefix web ci
Push-Location mobile
flutter pub get
Pop-Location
python -m venv ai-service/.venv
ai-service/.venv/Scripts/python.exe -m pip install -r ai-service/requirements-test.txt
```

Configure the API using existing user secrets or the environment names in `.env.example`. Do not commit actual credentials. The current EF design-time factory defaults to Development and reads API user secrets; `ConnectionStrings__SurplusLink` can override the connection in the command process. Build the current source before using `--no-build`, which otherwise may run a stale migration assembly.

```powershell
dotnet ef migrations list --project backend/SurplusLink.Api
dotnet ef database update --project backend/SurplusLink.Api
```

Apply **all** committed migrations, including `20260922053900_AddMatchDuration`. API startup also migrates its configured database outside the Testing environment. Do not generate duplicate migrations from the historical documentation examples.

## Run locally

Run each service in its own terminal, using the same internal shared token in API and AI environments. The canonical Python entry point is `app.main:app`, not the root `main.py`.

```powershell
# Internal AI: set AI_SERVICE_SHARED_TOKEN through your local secret mechanism first.
ai-service/.venv/Scripts/python.exe -m uvicorn app.main:app --app-dir ai-service --host 127.0.0.1 --port 8000

# Public API: also configure normal DB/JWT/CORS settings and real routing credentials.
$env:AI_SERVICE_BASE_URL = 'http://127.0.0.1:8000'
$env:AgentWorkflow__Enabled = 'true'
dotnet run --project backend/SurplusLink.Api

# React
npm --prefix web run dev

# Flutter Android emulator
Push-Location mobile
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5170
Pop-Location
```

The worker is enabled by default and startup validates its internal URL/token. Explicitly disabling it makes Start Matching return 503 without changing the requirement. On Windows, `powershell -NoProfile -ExecutionPolicy RemoteSigned -File scripts/start-local.ps1` starts AI and API together with one generated session token, so they cannot silently use different credentials. The execution policy applies only to that process. Database/JWT user secrets and `Routing__*` environment settings are still required. `.env.example` documents names; ASP.NET does not automatically load a `.env` file.

The durable worker processes queued requests, including requests left queued by an earlier stopped process. Flutter polls matching status every four seconds until it changes. Execution failure reopens the requirement for an explicit retry. Missing routing configuration fails safely rather than inventing a route. Use the explicitly labelled offline demo for an offline presentation:

```powershell
Push-Location ai-service
.venv/Scripts/python.exe -m app.workflows.demo
Pop-Location
```

Development API: `http://localhost:5170`; `/health` is liveness, and `/swagger` is enabled in Development/Testing. A physical phone requires the computer's reachable network address. Configure CORS for the actual React origin.

## Verification

```powershell
dotnet build SurplusLink.sln --configuration Release
dotnet test SurplusLink.sln --configuration Release
npm --prefix web run build
npm --prefix web test -- --maxWorkers=1
Push-Location mobile
flutter analyze
flutter test
Pop-Location
Push-Location ai-service
.venv/Scripts/python.exe -m unittest discover -s tests -v
Pop-Location
```

Set `SURPLUSLINK_TEST_CONNECTION` to a local PostgreSQL account allowed to create disposable databases. Tests generate their own random database names and migrate/drop only those databases. For live cross-language tests, additionally set `SURPLUSLINK_AI_TEST_URL` and `AI_SERVICE_SHARED_TOKEN` and run FastAPI. Without these settings those tests skip; a green default run is not proof of full integration. React has no lint script.

## Current readiness and evidence

The project is **local only**, as confirmed on 22 September 2026. Automated API-to-AI integration uses an explicit test routing adapter; it is not evidence of a real maps-provider or Flutter-device/React-browser walkthrough. See the [resumed pre-S12 audit](docs/evidence/shared/pre-s12-audit-2026-09-22.md) for current checks, changes and remaining submission tasks. Earlier evidence packs describe their milestone dates and are not final verification claims.

- [Architecture](docs/diagrams/architecture.md) and [entity relationships](docs/diagrams/entity-relationships.md)
- [Dual-role identity](docs/dual-role-marketplace-auth.md)
- [Agent orchestration and limits](docs/validation-orchestration.md)
- [Matching API](docs/matching-logistics.md) and [routing setup](docs/routing-provider.md)
- [Backend CI](docs/backend-ci.md), [performance](docs/performance-test-se3090.md), [deployment](docs/deployment-checklist.md)
- [AI assistance disclosure](docs/ai-logs/README.md)
