# ADR 0002: React authentication state

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
