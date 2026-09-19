# Flutter recommended matches

From Marketplace Home's Buy section, open My Requirements, select a requirement,
then choose Recommended Matches. Both BUYER-only and SELLER+BUYER accounts use
the existing AuthController, token storage, API client, and GoRouter. Access is
based on role membership in either order. Guests are redirected to login;
authenticated accounts without BUYER return home. Logout also protects open
match screens.

Routes:

- `/requirements/:id/matches`: paginated results with eligibility (all, not
  rejected, rejected), status, score/distance/transport cost/created date sorting,
  ascending/descending order, totals, refresh, and loading/empty/error/retry states.
  Changing a filter or sort resets the page. Returning from Details preserves
  list state. Late responses cannot overwrite a newer filter request.
- `/requirements/:id/matches/:matchId`: score, status, distance in km, estimated
  transport cost in LKR, rejection reason, creation time, listing reference, and
  paginated history. Missing route measurements display unavailable, not zero.
  History errors have an independent retry and do not hide the match.

The feature is read-only. MatchGateway exposes only list, get, and history;
MatchRepository sends GET requests only. There are no approve, reject, reserve,
publish, or workflow-start actions on either match screen. A recommendation is
not presented as an approved match or a reservation.

## API dependency

Deploy the match query/history implementation from `feature/m3-2-match-query-analytics`
and its migration before using this feature. This Flutter branch does not include
those backend changes. It consumes the shared contracts:

- `GET /api/matches/requirement/{id}` with `valid`, `rejected`, `status`, `sortBy`,
  `sortDir`, `page`, `pageSize`, and response `items`, `total`, `totalPages`, `page`,
  `pageSize`.
- `GET /api/matches/{id}/history` with page/pageSize and the same page envelope.

There is no single-match GET endpoint in that API. For refresh and direct links,
the repository searches the requirement-scoped list in pages of 100, sorted by
creation time and the API's ID tie-breaker, until it finds the match. This can
require several requests for large candidate sets; a future scoped detail
endpoint can replace this repository method without changing the screens.
Details does not depend on transient router extras, so deep links work after
session restoration. A missing match produces a 404 state; API ownership checks
remain authoritative. A 401 invokes the shared logout callback.

## Verification

Run `flutter test --no-pub` from `mobile`. Tests cover role combinations and direct
links, logout, requirement navigation, preservation of the same dual-role user,
GET-only bearer-authenticated requests, query parameters, later-page lookup,
loading/empty/error/retry states, pagination, filter resets, and history failure.
