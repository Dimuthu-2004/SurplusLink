# Logistics Agent

`app.agents.logistics.LogisticsAgent` is a deterministic, read-only LangGraph
workflow: validate input, evaluate candidates, validate output. It uses no LLM,
maps SDK, direct database access, reservation or approval operation.

```python
agent = LogisticsAgent(approved_backend_tools, max_retries=1)
response = agent.assess({
    "requirementId": "00000000-0000-0000-0000-000000000001",
    "candidateListingIds": ["00000000-0000-0000-0000-000000000002"],
    "buyerLocation": {"latitude": 6.9, "longitude": 79.8},
    "deadline": "2030-01-01T12:00:00Z",
})
```

The trusted caller must authorize the requirement and candidates, and supply the
stored requirement's location and deadline. IDs must be nonempty UUIDs. Input
allows 1–100 unique candidates, no extra control fields, a timezone-aware future
deadline, and finite coordinates within geographic bounds. Missing buyer
coordinates produce a warning for every candidate without tool calls. Zero is a
valid coordinate. Invalid coordinates produce INVALID_INPUT rather than being
silently repaired. Input errors do not echo supplied text.

## Allowed tool boundary

Inject a `LogisticsTools` implementation backed by ASP.NET Core. The agent passes
frozen Pydantic request objects and validates response mappings with extra fields
forbidden. The only tool methods called are:

| Tool | Validated request | Response |
|---|---|---|
| get_listing_location | requirementId, listingId | listingId, latitude, longitude |
| get_route_estimate | requirementId, listingId, sellerLatitude, sellerLongitude, buyerLatitude, buyerLongitude | success, distanceKm, durationMinutes, errorCode, retryAfterSeconds |
| calculate_transport_estimate | requirementId, listingId, distanceKm, durationMinutes | estimatedTransportCost, errorCode |

Optional fields can be omitted or null. Location output must identify the
requested listing; missing coordinates stop that candidate. Successful route
responses must include nonnegative finite distance and duration with no error.
Failed route responses must contain an allowed error code and no measurements.
Pricing must contain either a nonnegative finite cost or an allowed error code,
never both. Numbers or decimal strings are accepted; booleans, NaN, infinity,
negative measurements and values over 1e18 are rejected.

The route shape matches the ASP.NET routing adapter's result. The backend tool
implementation should unwrap the transport service's result to the documented
pricing contract. This change adds the agent and mockable boundary, not HTTP
endpoints or credentials. Production wiring must use finite HTTP timeouts and
must not add hidden retries. Provider keys remain in ASP.NET Core.

## Results and failure behavior

Each candidate preserves input order and returns listingId, distanceKm,
durationMinutes, estimatedTransportCost, deliveryFeasible, reason and warnings.
The envelope includes requirementId and status: ok, partial, failed, or
invalid_input. Decimal fields serialize as JSON strings to preserve precision;
`assess_json` emits JSON only.

`deliveryFeasible` means route duration fits between the current clock and the
deadline, assuming immediate departure. It is not a guarantee of inventory,
loading time, carrier capacity, traffic changes, approval or reservation. The
clock is sampled after tools finish, including retry time. Arrival exactly at
the deadline is feasible. A measured route beyond the deadline returns false
with DEADLINE_EXCEEDED; unknown route data returns null, never false or zero as a
substitute for unavailable information. Feasibility and confirmed route metrics
are preserved when only pricing fails; cost remains null.

Warnings contain a fixed code, tool, attempts and optional retryAfterSeconds.
Missing coordinates, timeouts, rate limits, malformed output, unavailable
providers and missing pricing are structured failures, never invented estimates.
Raw provider messages and exception text are not returned. One candidate's
failure does not discard other candidates' confirmed results.

Retry policy is server-owned: `max_retries` defaults to 1 and accepts 0–3;
each candidate/tool gets at most `1 + max_retries` attempts. Only timeout,
connection/OS errors, ROUTING_TIMEOUT, ROUTING_UNAVAILABLE and
TRANSPORT_UNAVAILABLE are retried. Invalid output and configuration errors are
not retried. Rate limits are returned immediately with Retry-After metadata,
leaving delayed rescheduling to the orchestrator. There is no agent sleep or
unbounded retry loop. At most 100 × 3 × 4 tool calls can occur per invocation.

## Tests

From `ai-service`, install the existing requirements in a virtual environment,
then run `.venv/Scripts/python -m unittest discover -s tests -v` on Windows.
Tests use only fake tools and a fixed clock. They cover normal results, missing
coordinates, provider timeout/failure/rate limit, retry bounds and recovery,
invalid tool output, wrong listing identity, deadline boundaries, injection
fields, partial results, repeated calls and final response validation.
