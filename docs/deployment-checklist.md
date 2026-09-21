# SurplusLink deployment checklist

This is a platform-neutral checklist for the existing SurplusLink stack. It
does not replace ASP.NET Core, PostgreSQL, React/Vite, Flutter, or FastAPI.
Adapt the commands to the chosen host, service manager, registry, or app store,
but keep the component boundaries and startup order unchanged.

## 1. Release inputs

- [ ] Record the Git commit, release/version number, target environment, and
      rollback commit or artifact.
- [ ] Confirm a disposable/staging PostgreSQL database and a database backup or
      snapshot before applying migrations.
- [ ] Store server secrets in the deployment secret manager. Never place them
      in `VITE_*`, Flutter `--dart-define`, source control, or screenshots.
- [ ] Confirm the public API HTTPS origin, the React origin, and the mobile
      network path. A physical Android device cannot use `localhost` or
      `10.0.2.2` to reach a remote API.
- [ ] Confirm the API, PostgreSQL, and AI service versions are compatible with
      the selected commit.

## 2. Component checklist

### PostgreSQL

- **Build/provision command:** PostgreSQL is a service, not a repository build.
  Provision the supported PostgreSQL instance and create the configured
  database/user through the target platform's managed-service or administration
  procedure. The repository CI uses PostgreSQL 18.
- **Environment names:** the API consumes
  `ConnectionStrings__SurplusLink` (for example,
  `Host=<host>;Port=5432;Database=<db>;Username=<user>;Password=<secret>`).
- **Health check:** `pg_isready -h <host> -p 5432 -U <user> -d <database>`;
  then verify an authenticated connection with `psql`.
- **CORS/network risks:** do not expose PostgreSQL publicly. Allow ingress only
  from the API network identity, enforce TLS where required, and restrict the
  database user to the required application privileges.
- **Migration/startup order:** take a backup/snapshot, verify connectivity,
  then start the API. The API calls `Database.MigrateAsync()` during startup;
  do not let multiple independently deployed API versions race migrations.
- **Rollback/fallback:** restore the database snapshot only when the migration
  is proven incompatible and no newer writes must be preserved. Prefer rolling
  the API artifact forward/back to a compatible version; schema rollback is a
  deliberate database operation, not an automatic app rollback.
- **Evaluator evidence:** PostgreSQL version, database name redacted as needed,
  `pg_isready` output, migration history, backup/snapshot identifier, and a
  screenshot/log showing the API connected successfully.

### ASP.NET Core API

- **Build command:** `dotnet restore SurplusLink.sln` followed by
  `dotnet publish backend/SurplusLink.Api/SurplusLink.Api.csproj -c Release
  --no-restore -o <publish-directory>`.
- **Environment names:**
  `ConnectionStrings__SurplusLink`, `Jwt__Issuer`, `Jwt__Audience`,
  `Jwt__Secret`, `Jwt__ExpirationMinutes`, `Cors__AllowedOrigins__0` and any
  additional `Cors__AllowedOrigins__<index>` values,
  `AI_SERVICE_BASE_URL`, `AI_SERVICE_SHARED_TOKEN`,
  `AgentWorkflow__Enabled`, `AgentWorkflow__PollSeconds`,
  `AgentWorkflow__TimeoutSeconds`, `AgentWorkflow__HttpTimeoutSeconds`,
  `AgentWorkflow__MaxRetries`, `AgentWorkflow__MaxCandidates`, plus the
  `Routing__*` values documented in `docs/routing-provider.md` when live
  routing is enabled.
- **Health check:** `GET https://<api-origin>/health` must return HTTP 200 and
  `Healthy`. Use `/swagger` only when the target environment intentionally
  exposes development Swagger; it is enabled by the current app only in
  Development/Testing environments.
- **CORS/network risks:** `Cors__AllowedOrigins__*` must contain exact browser
  origins, not API paths, wildcards, or trailing route paths. CORS is a browser
  policy, not authentication. Require HTTPS for public traffic, protect JWT
  secrets, and do not expose internal AI routes through the public ingress.
- **Migration/startup order:** API startup validates configuration, applies
  EF migrations, runs development seed behavior where applicable, then serves
  `/health`. Start it after PostgreSQL and before clients. Enable the workflow
  worker only after FastAPI, its shared token, routing configuration, and
  timeout budgets are ready.
- **Rollback/fallback:** keep the previous published API artifact available;
  route traffic back to it if health or smoke tests fail. If a migration has
  already changed the schema, use a compatible previous API or a reviewed
  database recovery plan rather than blindly starting an old binary.
- **Evaluator evidence:** publish output, commit/version, sanitized environment
  key list, migration log, `/health` response, API logs with secrets redacted,
  and the post-deploy client smoke-test results.

### FastAPI + LangGraph AI service

- **Build command:** from `ai-service`,
  `python -m pip install -r requirements.txt`; start with
  `python -m uvicorn app.main:app --host 0.0.0.0 --port <internal-port>`.
  The offline verification remains `python -m unittest discover -s tests -v`.
- **Environment names:** `AI_SERVICE_SHARED_TOKEN`,
  `WORKFLOW_STAGE_TIMEOUT_SECONDS`, `WORKFLOW_TIMEOUT_SECONDS`,
  `WORKFLOW_TOOL_TIMEOUT_SECONDS`, `WORKFLOW_MAX_RETRIES`, and
  `WORKFLOW_TRANSACTION_THRESHOLD`. Keep the shared token identical to the
  API value and at least 32 characters.
- **Health check:** `GET http://<internal-ai-host>:<port>/internal/health` must
  return `{"status":"ok"}`. The endpoint is internal; authenticate workflow
  execution with `X-Internal-Token`.
- **CORS/network risks:** keep the service on a private network. It has no
  public-client CORS role and its workflow endpoint must not be published via
  the public API ingress. Permit ingress only from the API and restrict egress
  according to the selected routing/model dependencies.
- **Migration/startup order:** no database migration is owned by FastAPI. Start
  it after its environment is valid and before setting API
  `AgentWorkflow__Enabled=true`; then configure the API's
  `AI_SERVICE_BASE_URL` and shared token.
- **Rollback/fallback:** set API `AgentWorkflow__Enabled=false` and keep the
  public read/auth API available if the AI service is unhealthy. Roll back to a
  previous AI artifact only after checking workflow contract compatibility.
  The offline graph/demo is a presentation fallback, not a substitute for a
  failed production routing call.
- **Evaluator evidence:** dependency installation/build logs, unit-test result,
  internal health response, sanitized configuration names, API-to-AI request
  correlation/log evidence, and a workflow terminal status.

### React Vite web client

- **Build command:** from `web`, `npm ci` followed by `npm run test` and
  `npm run build`. Serve the generated `web/dist` as static assets through the
  selected web host.
- **Environment names:** `VITE_API_BASE_URL` only. It is compiled into browser
  assets and must contain only the public API origin. Never put JWTs,
  passwords, database strings, or internal tokens in `VITE_*` variables.
- **Health check:** load the deployed web origin, confirm the asset response is
  successful, log in through `POST /api/auth/login`, and confirm the browser
  can call `GET /api/auth/me` and a protected read endpoint.
- **CORS/network risks:** configure the API's exact web origin in
  `Cors__AllowedOrigins__*`; ensure HTTPS mixed-content rules are satisfied;
  verify the built client is not still pointing at `localhost`.
- **Migration/startup order:** deploy the API and verify `/health` before
  publishing the static client. A client build does not migrate the database.
- **Rollback/fallback:** retain the previous static asset bundle and restore it
  if authentication or API calls fail. Keep the API contract compatible while
  rolling back a client bundle.
- **Evaluator evidence:** `npm ci`, `npm test`, `npm run build` outputs, commit,
  deployed asset URL, browser network evidence with secrets redacted, and the
  end-to-end smoke-test result.

### Flutter Android client

- **Build command:** from `mobile`, run `flutter pub get`, `flutter analyze`,
  `flutter test`, then `flutter build apk --release
  --dart-define=API_BASE_URL=https://<api-origin>` (or the chosen signed app
  bundle command for the target distribution channel).
- **Environment names:** `API_BASE_URL` is supplied with `--dart-define` and
  must be an HTTPS public API origin for release. Never put JWTs, passwords, or
  private service credentials in Dart defines.
- **Health check:** install the release artifact on a test device/emulator,
  sign in, and confirm authenticated API calls. For an Android emulator using a
  local HTTP API only, the development address is
  `http://10.0.2.2:5170`; this is not a deployment address. A physical device
  needs a reachable HTTPS host and trusted certificate.
- **CORS/network risks:** native Flutter requests are not governed by browser
  CORS, but Android network security and TLS certificate trust still apply.
  Debug cleartext support is not a release deployment strategy. Confirm DNS,
  firewall, certificate chain, and API reachability from the device network.
- **Migration/startup order:** deploy and health-check the API first, then build
  the app with the final API origin. Do not ship a client pointing at a local
  address.
- **Rollback/fallback:** retain the previous APK/AAB and previous API origin
  compatibility. Use the prior mobile artifact if the new API contract or TLS
  path fails; do not fall back to direct database or AI-service access.
- **Evaluator evidence:** `flutter analyze`, `flutter test`, release build
  output, artifact version, `API_BASE_URL` value (without secrets), device/OS,
  screenshots of login and a protected screen, and captured API response status.

## 3. Deployment order

1. Freeze the release commit and record rollback artifacts.
2. Provision PostgreSQL, networking, backup/snapshot, and credentials.
3. Verify PostgreSQL readiness and authenticated connectivity.
4. Publish/start the API. Let startup apply migrations, then verify `/health`.
5. Start FastAPI privately and verify `/internal/health`.
6. If workflows are required, configure the API AI URL/token, routing values,
   and `AgentWorkflow__Enabled=true`; verify one controlled workflow.
7. Build and publish React with the deployed API origin.
8. Build/sign Flutter Android with the deployed HTTPS API origin.
9. Run the post-deploy end-to-end test below from both a browser and a device or
   emulator where available.
10. Monitor logs, API health, database connections, workflow states, and client
    errors for the agreed observation window.

## 4. Post-deploy end-to-end test

Use a dedicated evaluator account and non-production data. Record timestamps,
commit, API URL, client versions, device/browser, and HTTP statuses.

### API and authentication

```powershell
Invoke-WebRequest https://<api-origin>/health -UseBasicParsing
```

Expected: HTTP 200 and `Healthy`. From React or Flutter, sign in with
`POST /api/auth/login`, store the returned token through the existing client
mechanism, and request `GET /api/auth/me` with `Authorization: Bearer <token>`.
Expected: HTTP 200 and the correct user/role.

### React path

1. Open `https://<web-origin>` and confirm the deployed bundle loads without
   console errors.
2. Sign in as the evaluator account.
3. Confirm the protected manager/buyer route appropriate to the account loads.
4. Confirm one read request such as `GET /api/materials` or
   `GET /api/requirements/my` returns data or a valid empty page.
5. For a buyer test only, create a disposable draft requirement, submit it,
   verify it becomes `OPEN`, then cancel it if the environment's test policy
   permits. Do not use production business data.
6. Capture browser URL, screenshots, Network-tab status codes, API health, and
   any console/API errors.

### Flutter path

1. Install the release APK/AAB-derived test build on the evaluator device.
2. Confirm the app was built with the deployed HTTPS `API_BASE_URL`.
3. Sign in as the evaluator account and confirm the protected home screen.
4. Navigate to a screen that performs an authenticated API read and confirm it
   renders data or a valid empty state.
5. Repeat the same disposable buyer action only if it is part of the approved
   test data plan; otherwise keep the client test read-only.
6. Capture device/OS, app version, screenshots, API response statuses, and
   logs with tokens removed.

### Pass/fail evidence

- [ ] PostgreSQL readiness and migration evidence captured.
- [ ] API `/health` returned HTTP 200.
- [ ] FastAPI `/internal/health` returned `{"status":"ok"}` when enabled.
- [ ] React bundle loaded and authenticated API read succeeded.
- [ ] Flutter release build loaded and authenticated API read succeeded.
- [ ] No client attempted direct PostgreSQL or AI-service access.
- [ ] CORS, TLS, DNS, firewall, and certificate results recorded.
- [ ] Rollback artifact and decision owner recorded.
- [ ] Any skipped workflow/write test is explicitly marked `not measured`.

## 5. Rollback decision

If the API health check, migration, authentication, or client smoke test fails,
stop promotion. Keep PostgreSQL intact, route back to the previous API artifact
or static client bundle, and disable `AgentWorkflow__Enabled` if the AI service
is the failing dependency. Preserve logs and the database migration state for
the evaluator. Do not claim a successful deployment from a health check alone;
the React/Flutter authenticated read is the minimum end-to-end proof.