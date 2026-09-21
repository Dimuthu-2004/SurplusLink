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