# SurplusLink React authentication shell

This Vite/React TypeScript client contains shared authentication infrastructure only. It uses the ASP.NET endpoints `POST /api/auth/login` and authenticated `GET /api/auth/me`.

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
