# Architecture diagram source

```mermaid
flowchart LR
  W[React TypeScript] --> API[ASP.NET Core Web API]
  M[Flutter] --> API
  API --> EF[EF Core]
  EF --> DB[(PostgreSQL)]
  API --> AI[FastAPI + LangGraph\ninternal only]
```
