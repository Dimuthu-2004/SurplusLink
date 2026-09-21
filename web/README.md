# SurplusLink React authentication shell

This Vite/React TypeScript client contains the manager workspace and shared authentication infrastructure. Authentication uses the ASP.NET endpoints `POST /api/auth/login` and authenticated `GET /api/auth/me`.

## Runtime packages

- `axios` `1.20.0` for the reusable JSON and bearer-token client.
- `react-router-dom` `7.18.3` for public, protected, and role-aware routing.
- React context plus `useReducer` for the small authentication state machine; no additional state package is required.

## Local run

The default API URL is `http://localhost:5170`. Override it only with a public API origin:

```powershell
$env:VITE_API_BASE_URL = 'https://api.example.com'
npm run dev
```

Vite variables are shipped to the browser. Never put JWTs, passwords, connection strings, or other secrets in `VITE_*` variables.

## Verification

```powershell
npm test
npm run build
```

## Manager Buyer Requirements (M2-4)

See [Manager Buyer Requirements](../docs/buyer-requirements-react.md) for routes, exact files, shared API integration, and tests.

## Manager Dashboard

The manager home at `/app/manager` combines the existing authenticated GET summaries:

- `/api/materials/analytics/summary`: active listings and category totals across all statuses.
- `/api/requirements/analytics/summary?upcomingDays=7`: open requirements, the `PENDING_APPROVAL` status count, and upcoming deadlines. The count covers the full window; the table displays the API's preview of up to ten requirements, with local times.
- `/api/matches/analytics/summary`: average score, average distance in kilometers, and the backend's top rejection reasons. Null averages display “Not available”.
- `/api/transactions/analytics/summary`: pending approval, approved, rejected, and completed transaction counts.

Each section loads independently with its own refresh, error/retry, and empty state. No placeholder data or additional backend endpoints are used. Material listings are now at `/app/manager/materials`; existing detail routes remain available. All routes use the existing manager role guard.

`src/test/managerDashboard.test.tsx` exercises the routed component through the shared Axios client, including bearer authentication, exact endpoint contracts, counts versus previews, partial failure/retry, and empty/null results.
