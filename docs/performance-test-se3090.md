# SE3090 performance test

This test uses [k6](https://grafana.com/docs/k6/latest/) and exercises the
authenticated read paths concurrently:

- `GET /api/materials?page=1&pageSize=20`
- `GET /api/requirements?page=1&pageSize=20`
- one controlled `POST /api/requirements/{id}/start-matching`

The first two routes are read-only. The matching route is not read-only: it
creates or reuses a durable `AgentWorkflow`. Run it only with a requirement
created for performance testing in a disposable database. The script sends one
matching request total, never one per read VU, and does not submit, approve,
reserve, or delete marketplace data.

This guide is for the backend/API performance test only. Flutter, React, and
the web client are not required. Run every PowerShell command from the
repository root: `C:\Users\<user>\SurplusLink\SurplusLink`.

## Handoff prompt

Send this prompt to the next member together with this document:

> Run the SurplusLink SE3090 backend performance test using
> `docs/performance-test-se3090.md`. Do not test through Flutter or the web
> client. First install/verify k6, start PostgreSQL and the API on the actual
> configured port, verify `/health`, and obtain valid local test JWT tokens.
> Run the read-only k6 scenario first. Save its JSON summary. Only run the
> optional match scenario against a disposable database and a dedicated OPEN
> requirement, because it creates an AgentWorkflow. Report actual VUs,
> duration, total requests, route counts, success/failure rates, average/P95
> latency, database observations, enqueue latency, and total workflow latency.
> Never invent missing numbers. Report blockers and skipped measurements as
> `not measured`.

## Preconditions

1. Use a dedicated database and a non-production API instance.
2. Start PostgreSQL and the API with the normal development configuration.
3. Obtain a seller/buyer/manager token that is valid for the test API. The
   materials token may be any role allowed by that endpoint; the requirements
   token must be a manager token. Do not put tokens in this repository.
4. For the matching scenario, create one future-dated buyer requirement, submit
   it so its status is `OPEN`, and record its ID. Do not reuse it after the run
   unless the workflow state is intentionally reset.
5. Install k6 and confirm the API is reachable at `BASE_URL`.

The API's local HTTP profile is `http://localhost:5170`. The project also
supports HTTPS at `https://localhost:7197`; use `--insecure-skip-tls-verify`
only for a local development certificate.

## Step 1: Install and verify k6

On Windows, install k6 with WinGet. Administrator permission may be requested:

```powershell
winget install --id GrafanaLabs.k6 --source winget `
   --accept-source-agreements --accept-package-agreements
```

Open a new PowerShell window after installation, then verify it:

```powershell
k6 version
```

If the command is not found, run the installed executable directly:

```powershell
& "C:\Program Files\k6\k6.exe" version
```

Use `& "C:\Program Files\k6\k6.exe"` instead of `k6` in all later commands
when the current PowerShell session has an old `PATH`.

## Step 2: Verify PostgreSQL

The API applies EF Core migrations during startup, so PostgreSQL must be
running before the API starts. Test the actual password; do not use angle
brackets around it and do not paste credentials into the report.

```powershell
$env:PGPASSWORD = "<actual-postgres-password>"
psql -h localhost -U postgres -d postgres -c "select version();"
```

Expected result: a PostgreSQL version row. If authentication fails, stop and
obtain the password configured for the local PostgreSQL installation. Do not
change or reset the database password as part of this performance test.

## Step 3: Start the API

Use a disposable local database, a real local JWT secret of at least 32
characters, and the same PostgreSQL password that passed the previous check.
Run this in a separate PowerShell window and leave it running:

```powershell
$env:ConnectionStrings__SurplusLink = "Host=localhost;Port=5432;Database=surpluslink;Username=postgres;Password=<actual-postgres-password>"
$env:Jwt__Secret = "local-surpluslink-secret-123456789012345"
$env:Jwt__Issuer = "SurplusLink.Api"
$env:Jwt__Audience = "SurplusLink.Clients"
$env:Cors__AllowedOrigins__0 = "http://localhost:5173"

dotnet run --project backend/SurplusLink.Api --urls http://localhost:5170
```

In a second PowerShell window, verify the API:

```powershell
Invoke-WebRequest http://localhost:5170/health -UseBasicParsing
```

Expected result: `StatusCode : 200` and `Content : Healthy`. The port in the
successful health check is the `BASE_URL` for k6. If `5170` is occupied, either
stop the old process or start the API on another port, for example
`--urls http://localhost:5171`, and use that same port in every later command.

If the API reports `password authentication failed`, fix the PostgreSQL
password first. If it reports `address already in use`, do not start a second
API on that port; test the existing listener or choose another port.

## Step 4: Obtain test JWT tokens

The k6 script does not log in automatically. Use an existing local test account
with the required role. The requirements endpoint requires `MANAGER`; the
materials endpoint accepts an allowed marketplace role. Never commit tokens.

```powershell
$baseUrl = "http://localhost:5170"
$login = Invoke-RestMethod `
   -Method Post `
   -Uri "$baseUrl/api/auth/login" `
   -ContentType "application/json" `
   -Body (@{
      email = "<manager-test-email>"
      password = "<manager-test-password>"
   } | ConvertTo-Json)

$env:BASE_URL = $baseUrl
$env:MATERIALS_TOKEN = $login.token
$env:REQUIREMENTS_TOKEN = $login.token
$login.token.Length
```

The final command should print a positive number. If login returns `401`, the
account or password is wrong; do not treat a failed login as a performance
result. If the API is on port `5171`, change only `$baseUrl` to
`http://localhost:5171`.

## Step 5: Run the read-only test

This is the required first test. It uses 10 concurrent virtual users for one
minute and alternates between the two GET endpoints. It creates no listings,
requirements, matches, reservations, or workflows.

```powershell
$env:READ_VUS = "10"
$env:READ_DURATION = "60s"
$env:SUMMARY_FILE = "perf/results/se3090-read-$(Get-Date -Format yyyyMMdd-HHmmss).json"

& "C:\Program Files\k6\k6.exe" run perf/k6/surpluslink.js
```

Copy the printed values into the report. The JSON file named by `SUMMARY_FILE`
is the primary evidence artifact. A successful run should finish with exit code
0. A setup error such as `MATERIALS_TOKEN is required` means no requests were
sent and must be fixed before rerunning.

For a larger follow-up run, change only the workload controls and record them:

```powershell
$env:READ_VUS = "20"
$env:READ_DURATION = "5m"
$env:SUMMARY_FILE = "perf/results/se3090-read-20vus-5m-$(Get-Date -Format yyyyMMdd-HHmmss).json"
& "C:\Program Files\k6\k6.exe" run perf/k6/surpluslink.js
```

Do not compare runs unless the VUs, duration, dataset, API commit, database,
and machine are recorded.

## Run commands

Read-only load, ten concurrent virtual users for one minute:

```powershell
$env:BASE_URL = "http://localhost:5170"
$env:MATERIALS_TOKEN = "<seller-or-manager-token>"
$env:REQUIREMENTS_TOKEN = "<manager-token>"
k6 run perf/k6/surpluslink.js
```

Controlled match-generation run plus the same read load:

```powershell
$env:MATCH_REQUIREMENT_ID = "<dedicated-open-requirement-guid>"
$env:MATCH_TOKEN = "<owning-buyer-token>"
$env:MANAGER_TOKEN = "<manager-token>"
$env:MEASURE_WORKFLOW = "true"
$env:WORKFLOW_TIMEOUT_SECONDS = "300"
k6 run perf/k6/surpluslink.js
```

## Step 6: Optional single match-generation test

This step is optional because `POST /api/requirements/{id}/start-matching` is
not read-only. It creates or reuses a workflow and may invoke the internal AI
service and routing provider. Use only a dedicated OPEN requirement in a
disposable database. The owning buyer token is required for the POST; the
manager token is required only to poll workflow status.

Before running it, record the dedicated requirement ID and confirm it is not a
real user requirement. Then set the variables and run exactly once:

```powershell
$env:MATCH_REQUIREMENT_ID = "<dedicated-open-requirement-guid>"
$env:MATCH_TOKEN = "<owning-buyer-token>"
$env:MANAGER_TOKEN = $env:REQUIREMENTS_TOKEN
$env:MEASURE_WORKFLOW = "true"
$env:WORKFLOW_TIMEOUT_SECONDS = "300"
$env:WORKFLOW_POLL_SECONDS = "2"
$env:SUMMARY_FILE = "perf/results/se3090-match-$(Get-Date -Format yyyyMMdd-HHmmss).json"

& "C:\Program Files\k6\k6.exe" run perf/k6/surpluslink.js
```

The script still runs the read VUs at the same time and sends one match-start
request. It reports two different measurements:

- **Match enqueue latency:** HTTP request time for the POST, including the API
   transaction that creates or reuses the workflow.
- **Workflow total latency:** elapsed time from that POST until manager polling
   observes `PENDING_APPROVAL`, `APPROVED`, `REJECTED`, `FAILED`, or `COMPLETED`.

If the workflow times out, report `timeout` and do not convert the timeout into
a latency. If the AI service is disabled or unavailable, report that the
workflow measurement was not completed.

## Step 7: Capture database observations

Run these read-only queries during or immediately around the load. Capture
before and after output if possible. Use a read-only database account where
available and redact credentials from screenshots.

```powershell
$env:PGPASSWORD = "<actual-postgres-password>"
psql -h localhost -U postgres -d surpluslink
```

Then paste the SQL from the database observations section below. Record active
connections, waits, database counters, query evidence, and whether
`pg_stat_statements` is enabled. A missing database metric is `not measured`,
not zero.

Useful run controls are `READ_VUS`, `READ_DURATION`, `THINK_TIME_SECONDS`,
`WORKFLOW_POLL_SECONDS`, and `SUMMARY_FILE`. For example, a five-minute run
with 20 read users is:

```powershell
$env:READ_VUS = "20"
$env:READ_DURATION = "5m"
$env:SUMMARY_FILE = "perf/results/se3090-$(Get-Date -Format yyyyMMdd-HHmmss).json"
k6 run perf/k6/surpluslink.js
```

k6 prints request count, failure rate, and HTTP duration statistics. The JSON
summary also contains the custom route trends for average and P95 latency.
Record the exact command, start/end timestamps, k6 version, API commit, host,
database size, and whether workflow polling was enabled.

## Database and application observations

Capture observations during the same run, using a read-only PostgreSQL account
where possible. Do not run destructive SQL against the test database.

```sql
SELECT pid, usename, state, wait_event_type, wait_event,
       now() - query_start AS query_age, left(query, 160) AS query_text
FROM pg_stat_activity
WHERE datname = current_database()
ORDER BY query_start NULLS LAST;

SELECT datname, numbackends, xact_commit, xact_rollback,
       blks_read, blks_hit, tup_returned, tup_fetched
FROM pg_stat_database
WHERE datname = current_database();
```

If `pg_stat_statements` is enabled, capture the highest-total-time statements
before and after the run. If it is not enabled, record that fact and use API
logs, PostgreSQL activity, or an approved `EXPLAIN (ANALYZE, BUFFERS)` run on a
representative read-only query outside the load window. Record connection-pool
waits, CPU, memory, locks, errors, and the number of rows returned where those
metrics are available. The two list endpoints intentionally perform a count
query and a paged query, so both should be considered when interpreting DB
load.

## Report template

Copy this section into the SE3090 evidence record and replace every bracketed
placeholder with observed values. Do not infer missing values.

### Test identity

- Test date/time and timezone: `[YYYY-MM-DD HH:MM to YYYY-MM-DD HH:MM TZ]`
- Git commit: `[commit]`
- API environment/host: `[local or host]`
- k6 version: `[version]`
- Database engine/version: `[PostgreSQL version]`
- Dataset: `[row counts for listings, requirements, matches]`
- Dedicated disposable database: `[yes/no]`
- AI service and routing dependencies available: `[yes/no/not applicable]`

### Workload

- Concurrent read users: `[number]`
- Match-generation users: `[1]`
- Duration: `[duration]`
- Total requests: `[k6 total]`
- Requests by route: `[materials]`, `[requirements]`, `[start-matching]`, `[workflow polls]`
- Think time: `[seconds or none]`
- Workflow polling enabled: `[yes/no]`
- Match requirement ID: `[ID, or redacted reference]`

### Results

| Route or metric | Requests | Success rate | Failure rate | Average ms | P95 ms | Notes |
| --- | ---: | ---: | ---: | ---: | ---: | --- |
| `GET /api/materials` | `[ ]` | `[ ]` | `[ ]` | `[ ]` | `[ ]` | `[ ]` |
| `GET /api/requirements` | `[ ]` | `[ ]` | `[ ]` | `[ ]` | `[ ]` | `[ ]` |
| `POST .../start-matching` enqueue | `[ ]` | `[ ]` | `[ ]` | `[ ]` | `[ ]` | `[ ]` |
| AI workflow total, enqueue to terminal state | `[ ]` | `[ ]` | `[ ]` | `[ ]` | `[ ]` | `[terminal status/timeout]` |

Overall success rate: `[observed value]`

Failures by status/error: `[status codes, counts, and k6 check failures]`

### Database/query observations

- Count-plus-page query observations: `[ ]`
- Slowest or highest-load query evidence: `[ ]`
- Active connections / pool waits: `[ ]`
- CPU, memory, locks, or waits: `[ ]`
- Before/after `pg_stat_database` observations: `[ ]`
- `pg_stat_statements` available: `[yes/no]`
- Database errors or timeouts: `[ ]`

### Interpretation and limits

- Observed bottleneck: `[ ]`
- Route with highest P95: `[ ]`
- Was the single match workflow completed, failed, or timed out: `[ ]`
- External AI/routing latency contribution: `[observed or not measured]`
- Limitations: `[machine size, dataset size, no production traffic, etc.]`
- Evidence files: `[k6 JSON summary, screenshots/logs, SQL output]`

The enqueue latency is an API/database transaction measurement. The total AI
workflow latency is measured only when `MEASURE_WORKFLOW=true` and the manager
poll reaches a terminal status. If the workflow is not terminal before the
timeout, report it as a timeout and do not convert it into a fabricated latency.