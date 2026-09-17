# Manager Buyer Requirements React pages (M2-4)

## Run

Use branch feature/m2-4-react-manager-ui in the existing shared repository. It includes
the completed M2-1/M2-2 backend and M2-3 work.

API terminal:

~~~powershell
Set-Location C:\Users\Dimuthu_K\SurplusLink\SurplusLink\backend\SurplusLink.Api
dotnet run --launch-profile http
~~~

Web terminal:

~~~powershell
Set-Location C:\Users\Dimuthu_K\SurplusLink\SurplusLink\web
npm run dev
~~~

Open the local URL printed by Vite, sign in as MANAGER, and choose **Buyer Requirements**
in the existing sidebar. Shared VITE_API_BASE_URL configuration, JWT storage, session
restoration, error normalization and 401 logout handling are reused.

## Routes

All routes sit inside the existing ProtectedRoute and RoleRoute role="MANAGER":

| Route | Purpose |
| --- | --- |
| /app/manager/requirements | Request analytics and requirements table |
| /app/manager/requirements/:requirementId | Full requirement details |
| /app/manager/requirements/:requirementId/history | Paginated audit history |
| /app/manager/requirements/:requirementId/workflow | Latest requirement workflow status |

Guests go to login; buyers/sellers return to their role home. Manager pages are read-only.
The backend continues to enforce manager access on the API.

## Table and details

- Search category names or notes; maximum 200 characters.
- Filter by status, category and an inclusive deadline date range.
- Local start/end-of-day values are converted to UTC for the existing deadlineFrom/deadlineTo
  query contract. Reversed ranges produce an inline error without sending a request.
- Sort deadline, maximum budget or created date, ascending/descending.
- Page sizes 10, 20, 50, 100. Total is the API's filtered total before pagination.
  Applying/resetting filters returns to page 1; previous/next retain applied filters.
- Old asynchronous responses are ignored after a newer request or route change.
- Category dropdown failure has its own retry; requests can still be viewed by category ID.
- Details show buyer/request/category IDs, quantity and unit, budget, local deadline,
  notes, coordinates and creation/update timestamps.
- Notes are rendered as escaped text, preserving line breaks. No currency is invented.
- Details link to history and workflow status. No manager mutation controls are added.
- History shows old/new status pairs, timestamps and account/system actors, oldest first,
  with 20 events per page and the backend total.
- Loading, empty and error/retry states are supplied. Missing/forbidden requirements display
  specific messages. Shared API errors also read ASP.NET ProblemDetails.detail.

## Analytics widget

RequestAnalyticsWidget calls the existing manager summary endpoint independently from
the table. Its counts are for all requirements, not the current table filter.

It displays total, exact OPEN count, upcoming deadline count and average maximum budget.
An expandable breakdown shows counts by status/category, average quantity grouped by unit,
and up to ten upcoming deadlines linking to requirement details. A 1/7/30/90-day selector
controls the upcoming window. Empty data and null averages are handled explicitly.
Refresh analytics does not reset table filters.

## Workflow dependency

The workflow link reads GET /api/requirements/:id and presents its real saved lifecycle
state and last-updated time, with manual refresh and a history link.

There is no canonical workflow/recommendation read endpoint in the current shared API.
The page clearly says detailed workflow information/recommendations are unavailable.
It does not invent a workflow ID, recommendation, progress percentage or persistence schema.
Once M4 supplies that read contract, the page can link to real workflow details.

## Tests

From web:

~~~powershell
npm test
npm run build
~~~

src/test/managerRequirements.test.tsx covers:

- Actual shared Axios bearer interception, GET routes, filters, sorting, paging and history/summary parameters.
- ProblemDetails.detail error preservation.
- Component search/filter/date/sort/page/reset behavior and filtered totals.
- Reversed date range validation and table error/empty recovery independent of analytics.
- Ignoring stale asynchronous list responses.
- Read-only details, workflow navigation, actual saved status and history pagination.
- Empty analytics and changing its deadline window.
- Protection of list/detail/history/workflow routes for guests, BUYER and SELLER accounts.

## Manual checks

1. Sign in as manager and open Buyer Requirements; verify sidebar selection, analytics,
   category names, statuses, quantity/budget/date columns and API totals.
2. Search a category name and then text from notes. Combine a status, category and date range.
   Try all three sort fields in both directions, change page size and navigate multiple pages.
   Apply a new filter while on page 2: it returns to page 1. Reset clears all filters.
3. Enter a reversed date range: inline error, no table request. Search a nonexistent term:
   empty message, correct zero total. Test local dates around midnight/time-zone boundaries.
4. Stop the API and refresh the page: table/analytics errors offer independent retries.
   Restart the API and retry. Category fetch failure should not hide readable requirement IDs.
5. View a requirement. Verify IDs, notes, quantity, budget, coordinates and local timestamps
   against Swagger. No submit/edit/delete/cancel/start buttons should be available.
6. Open View history. Verify operation names and old -> new status transitions. Test more than
   20 events with paging; a request without events shows an empty message.
7. Open View workflow status. Verify the saved state and refresh after a backend state change.
   No made-up workflow/recommendation information should appear.
8. Expand analytics. Compare category/status counts to Swagger. Change the deadline window;
   check upcoming request links, zero data, null budget average and quantities grouped by unit.
9. Sign out and directly visit each manager requirements route: login appears. Sign in as
   BUYER/SELLER and try the routes: role home appears and requirement API calls are not made.
   An expired session follows the shared 401 logout flow.
10. Open a nonexistent requirement ID: not-found message. Check a 403 response and retry.
    Test browser narrow width, keyboard-only navigation, horizontal table scrolling, long
    notes/IDs and large text. Screen-reader labels identify filters, results and pagination.

## Exact files

New:
- web/src/features/requirements/managerRequirementsApi.ts
- web/src/features/requirements/requirementUi.tsx
- web/src/features/requirements/RequestAnalyticsWidget.tsx
- web/src/pages/manager/ManagerRequirementsPage.tsx
- web/src/pages/manager/ManagerRequirementDetailsPage.tsx (details and workflow-status view)
- web/src/pages/manager/ManagerRequirementHistoryPage.tsx
- web/src/test/managerRequirements.test.tsx
- docs/buyer-requirements-react.md

Updated:
- web/src/app/App.tsx
- web/src/components/AppLayout.tsx
- web/src/api/apiClient.ts
- web/src/styles.css
- web/README.md

No backend migrations, authentication duplicates, workflow tables or package updates are needed.
