# Architecture diagram source

```mermaid
flowchart LR
  W[React TypeScript\nroute guards] --> API[ASP.NET Core Web API\nJWT authorization]
  M[Flutter\nSell / Buy / Both onboarding] --> API
  API --> EF[EF Core]
  EF --> DB[(PostgreSQL)]
  API --> AI[FastAPI + LangGraph\ninternal only]
  DB --> UR[UserRoleAssignment\n(UserId, Role)]
  API --> OC[Ownership checks\nBuyerId / SellerId]
  API --> DM[Deterministic self-match policy\nSELF_MATCH_NOT_ALLOWED]
  UR --> API
  OC --> API
  DM --> API
```

The marketplace identity decision uses one normalized user with separate
`SELLER` and `BUYER` role assignments and separate JWT role claims. `BOTH` is
an onboarding choice, not a third authorization role. `MANAGER` remains
separately provisioned and isolated from public marketplace registration.
Ownership and self-match checks remain trusted API/database policy rather than
UI or model instructions. See
[`docs/adr/0002-react-auth-state.md`](../adr/0002-react-auth-state.md).
