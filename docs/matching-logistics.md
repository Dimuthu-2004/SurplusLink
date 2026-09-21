# Matching and logistics queries

All routes use the shared JWT authentication and role claims. A BUYER (including
SELLER+BUYER accounts) may read only matches for their own requirement. MANAGER
may read all requirements and the aggregate summary. SELLER-only accounts cannot
read these endpoints. Missing resources return 404; another buyer's resource
returns 403.

## Endpoints

- `GET /api/matches/requirement/{id}` accepts optional `valid`, `rejected`, and
  `status` filters. Booleans accept `true`/`false`; status accepts GENERATED,
  RANKED, ROUTED, ROUTE_FAILED, or REJECTED, case-insensitively.
- `sortBy` accepts `score` (default), `distance`, `estimatedTransportCost`, or
  `createdAt`. `sortDir` accepts `asc` or `desc` (default). Unknown route values
  sort last in both directions; ID ascending breaks ties for stable pagination.
- `page` defaults to 1 (maximum 1,000,000), and `pageSize` defaults to 20 (maximum
  100). The response contains `items`, filtered `total`, `totalPages`, `page`, and
  `pageSize`. Totals are computed before pagination. Invalid inputs return 400.
- `GET /api/matches/{id}/history` uses the same pagination parameters. Entries
  contain ID, actor ID (null for background work), action, route outcome, and UTC
  creation time. Ordering is chronological with ID as a tie-breaker.
- `GET /api/matches/analytics/summary` is MANAGER-only. It returns `total`,
  `averageScore`, `averageDistance`, up to ten `topRejectionReasons` (count
  descending, reason ascending), `routeSuccessCount`, `routeFailureCount`,
  `routeSuccessRate`, and `routeFailureRate`.

`valid` means not rejected; it is not a promise of a completed route or an
approved reservation. Filters are combined with AND, so contradictory filters
return an empty page. Distance is kilometers and estimated transport cost is LKR.
Averages include all stored matches; unknown distances are excluded from the
distance average. Empty averages are null. Rejection counts use current rejected
matches. Route rates are fractions from 0 to 1 over all logged attempts, including
retries and attempts on subsequently rejected matches. With no attempts, rates
are null, not a fabricated 0% success rate.

## Action persistence and workflow integration

This branch previously had only the basic match entity; no matching/logistics
action endpoints or production workflow integration existed. `MatchService`
provides trusted integration methods, registered in dependency injection:

- `GenerateAsync`: records a candidate and GENERATE. Deterministically invalid
  candidates are stored as REJECTED with a reason and REJECT event, including
  SELF_MATCH_NOT_ALLOWED. Rejects expired or non-open/non-matching requirements
  and duplicate candidate generation. It does not search inventory itself.
- `RankAsync`: stores a caller-computed score from 0 to 1 and records RANK.
- `RecordRouteAsync`: stores a computed route result and records ROUTE_SUCCEEDED
  or ROUTE_FAILED. History exposes action ROUTE and outcome SUCCEEDED/FAILED.
  Failed attempts clear stale route measurements; successful attempts require
  nonnegative distance and cost. Ranking preserves an existing route outcome.
- `RejectAsync`: stores the supplied reason and records REJECT. Rejected matches
  cannot be reranked or rerouted.

The workflow must supply trusted calculations and actor identity; these methods
are not exposed as buyer-facing write endpoints. They do not implement a route
provider, pricing algorithm, or reservation/workflow transition. Each state
change and audit event is saved atomically. Optimistic concurrency prevents a
stale writer from overwriting a rejection or appending a misleading success log.

Self-matching is a hard backend rule based on stored ownership:
`Listing.SellerId != BuyerRequest.BuyerId`. `GenerateAsync` persists a self-match
as `REJECTED` with `SELF_MATCH_NOT_ALLOWED`; it is absent from `valid=true`
results and cannot be revived by ranking or routing. The workflow excludes own
listings before limiting candidates and rechecks ownership before publishing a
recommendation. An agent/LLM cannot override this rule by supplying a high score
or a successful validation result. Category, quantity, budget, unit, active and
expiry checks continue to apply to other sellers' listings. The existing
MaterialMatch model, endpoint contracts and ranking rules are unchanged.

Apply the `AddMatchLogistics` EF migration before using the endpoints. Existing
matches retain their scores and receive GENERATED status with unknown route
measurements; historical events and route outcomes are not invented.

## Verification

`dotnet test backend/SurplusLink.Tests` runs query validation, filters, all sort
directions and PostgreSQL SQL translation without requiring a live database.
Set `SURPLUSLINK_TEST_CONNECTION` to a PostgreSQL account permitted to create
temporary test databases to also run HTTP, migration, history, analytics,
self-match, and concurrency tests. The integration fixture creates and removes
only its own uniquely named test databases.
