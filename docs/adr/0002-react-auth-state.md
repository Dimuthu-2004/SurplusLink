# ADR 0002: React authentication state

## Multi-role marketplace identity amendment (2026-09-18)

A marketplace user can sell surplus and buy materials with one account. Separate accounts would duplicate identity and split ownership/history; a `BOTH` role would break the existing independent SELLER/BUYER authorization rules. Both alternatives are rejected.

One `User` now has normalized `UserRoleAssignment` rows keyed by `(UserId, Role)`. The existing SELLER, BUYER, and MANAGER enum values remain. Public registration accepts exactly SELLER, BUYER, or both, and rejects empty, duplicate, unknown, or manager-containing selections. MANAGER remains development-seeded/admin-created; marketplace registration never grants it. ASP.NET role attributes and resource ownership checks remain authoritative. Tokens contain one standard role claim per assignment; auth responses expose a `roles` array.

Flutter onboarding asks for account details, then "How will you use SurplusLink?" with Sell / Buy / Both choices. A dual-role account sees one Marketplace Home with both sets of actions. React retains its context/reducer and manager focus; its guards check role membership. Existing single-role accounts retain their capabilities after migration. No role-management endpoint is added.

The migration copies existing roles before dropping `Users.Role`, preserving user IDs and ownership FKs. Downgrade is refused if any user cannot be represented by exactly one old role, preventing silent capability loss. Deploy clients and API together because the contract changes from `role` to `roles`. Existing signed single-role tokens remain valid until expiry; sign in again after an administrator changes assignments to obtain updated claims.

Self-match is a deterministic exclusion: `Listing.SellerId == BuyerRequest.BuyerId` produces `SELF_MATCH_NOT_ALLOWED`. The planner derives `buyerUserId` from the stored request, and matching checks both search records and refreshed details before ranking. Identity stays in the trusted deterministic boundary and is absent from candidate output; no LLM/provider receives it. Reservations independently block self-trade with the same reason. Future M3 candidate generation must filter `SellerId != BuyerId` and reuse this policy; no M3 implementation is included.

This amendment leaves the ADR count at two. The original React state decision follows.

- **Status:** Accepted
- **Context:** The React shell needs one small cross-cutting state domain: the current user, startup restoration, login progress, and authentication errors. It also needs navigation updates when that state changes.
- **Decision:** Use a React context backed by `useReducer`. Keep HTTP behavior in an Axios client, JWT access in a token-storage adapter, and access control in React Router guards.

## Rationale

`useReducer` makes authentication transitions explicit and deterministic: bootstrapping, anonymous, request started, authenticated, request failed, and logout. Context distributes that state to the small set of route and layout consumers without prop drilling. This is enough for one low-frequency global domain and avoids the bundle size, conventions, and duplicate server-state cache introduced by Redux, Zustand, or a query library.

The reducer is not used for business data. If later features introduce substantial server-state caching, pagination, invalidation, or optimistic updates, a focused server-state library can be added without replacing authentication state.

## Security and routing consequences

- The existing API returns bearer JWTs rather than setting an HttpOnly cookie. The shell therefore stores the token in `sessionStorage`, limiting persistence to the current browser tab. This is less persistent than `localStorage`, but JavaScript can still read it if an XSS vulnerability exists.
- The preferred future browser design is a Secure, HttpOnly, SameSite cookie issued by the API. That requires a backend contract change and is outside this shell.
- Route and role guards improve navigation and prevent accidental UI access. They are not an authorization boundary; ASP.NET authorization must continue to enforce every protected operation.
- The Axios request interceptor adds the bearer token. A `401` clears client authentication globally. API errors are normalized from both `{ message }` responses and ASP.NET validation problem details.

## Viva summary

Context answers “where is the session available?” and the reducer answers “which transitions are legal?”. Axios owns transport concerns, while React Router owns navigation policy. These responsibilities are separated so each can be tested independently and replaced without rewriting business features.
