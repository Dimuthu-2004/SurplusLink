# Member 1 Evidence and Viva Pack

This pack is a presentation checklist and speaking aid for Member 1. It does
not include a personal reflection. Replace every square-bracket placeholder
with the actual date, screenshot filename, commit, pull request, or measured
result before submission.

## 1. Evidence rules

- Capture the whole application window where possible, including the URL,
  endpoint, test name, or terminal command that identifies the evidence.
- Do not show passwords, JWTs, connection strings, private keys, or personal
  data. Mask tokens and account identifiers.
- Use the same seeded/test data across screenshots where possible. Record the
  role used: `BUYER`, `SELLER`, `MANAGER`, or dual-role account.
- A screenshot proves what was displayed at one point in time. Pair important
  screenshots with a test result, API response, commit, or PR link.
- Record the exact date and branch/commit for each item:
  - Evidence date: `[YYYY-MM-DD]`
  - Branch/commit: `[branch and short SHA]`
  - Environment: `[local/staging, API URL without secrets, database name masked]`

## 2. Screenshots to capture

Use the following filenames so the evidence folder is easy to audit.

### A. Swagger/API

1. `01-swagger-index.png`: Swagger UI showing the API title and the
   `/api/requirements`, `/api/materials`, `/api/material-categories`,
   `/api/matches`, and auth route groups.
2. `02-swagger-requirement-create.png`: `POST /api/requirements` with a valid
   request body and the `201 Created` response. Mask the bearer token.
3. `03-swagger-ownership.png`: the same requirement requested as another buyer
   (expected `403`) and anonymously (expected `401`).
4. `04-swagger-search-page.png`: a material or match list request showing
   search/filter/sort/page/pageSize query parameters and a response containing
   `items`, `total`, `page`, and `totalPages`.
5. `05-swagger-workflow.png`: submit/start-matching/cancel sequence showing
   status transitions and a conflict response for an invalid repeated action.
6. `06-swagger-category.png`: manager category create/update/delete, including
   a rejected non-manager request.

Suggested source anchors: `RequirementsController`, `MaterialListingsController`,
`MatchesController`, and the Swagger test in
`backend/SurplusLink.Tests/BuyerRequirementsIntegrationTests.cs`.

### B. Database and integrity

1. `07-db-erd-or-tables.png`: database client showing the relevant tables and
   relationships: users/roles, categories, listings/material requests,
   buyer requests, matches, reservations, workflows, agent steps/tool calls,
   and approvals.
2. `08-db-owner-foreign-keys.png`: table definition or query output showing
   owner foreign keys, category foreign keys, and delete/update behaviour.
3. `09-db-check-constraints.png`: positive quantity, valid budget, valid unit,
   allowed status, and latitude/longitude checks.
4. `10-db-indexes.png`: indexes used for owner/status/category/deadline and
   list-query performance.
5. `11-db-audit-workflow.png`: before/after rows for a requirement or listing,
   its history/audit row, and (where applicable) match/workflow rows.
6. `12-db-migration.png`: migration applied successfully and no pending model
   changes.

Do not expose real credentials or an unmasked connection string. The tagged
integration test is useful corroboration: it checks direct-write constraint
rejection, legacy links, workflow entities, and migration consistency.

### C. React web application

1. `13-react-login-role.png`: signed-in manager view and visible role-appropriate
   navigation.
2. `14-react-material-table.png`: manager material table with search, category
   or status filter, sort direction, page number, and page-size result.
3. `15-react-material-verify.png`: pending listing verification and the updated
   status/audit indication.
4. `16-react-categories-crud.png`: category manager page showing create, edit,
   and delete (or the documented validation/error state).
5. `17-react-analytics.png`: dashboard/match/inventory/requirement analytics
   with loading, populated, and empty/error state if available.
6. `18-react-matches.png`: match comparison/detail page with score, distance,
   route outcome, rejection reason, or history.
7. `19-react-error-retry.png`: an API failure shown as an alert/status message,
   followed by a successful Retry result.

Relevant source examples include `ManagerCategoriesPage`,
`ManagerListingsPage`, `MatchAnalyticsWidget`, `AnalyticsChart`, and the
manager API modules under `web/src/features/`.

### D. Flutter mobile application

1. `20-flutter-home-role.png`: mobile home screen after sign-in.
2. `21-flutter-add-material.png`: add-material form with category, quantity,
   unit, condition, price, expiry, location, and image controls.
3. `22-flutter-image-picker.png`: image picker flow and selected preview; use
   a non-sensitive test image.
4. `23-flutter-gps-location.png`: location permission/result or the explicit
   manual-location fallback and validation.
5. `24-flutter-my-materials.png`: My Materials list with search/filter and an
   empty-result state.
6. `25-flutter-requirement-match.png`: requirement or recommended-match screen
   showing match details and a usable loading/error state.
7. `26-flutter-category-validation.png`: category loading failure/retry or
   form validation for missing required data and non-positive quantity.

Relevant dependencies are `image_picker` and `geolocator`; relevant screens
include `material_listing_form_screen.dart`, `my_materials_screen.dart`,
`requirement_form_screen.dart`, and `recommended_matches_screen.dart`.

### E. Matching Agent and workflow

1. `27-agent-run-input-output.png`: sanitized Matching Agent input/output for
   an eligible candidate, including score/status and no secret values.
2. `28-agent-exclusions.png`: test output or logs showing draft, unverified,
   expired, insufficient-quantity, and self-owned candidates excluded.
3. `29-agent-approval-workflow.png`: manager approval/revision/failure state
   in the workflow UI or API.
4. `30-agent-tool-audit.png`: persisted workflow, step, tool-call, and
   approval records (IDs may be shortened).
5. `31-agent-tests.png`: focused Python test run showing the material matching
   and requirement planner tests passing.

Explain that the agent proposes/calculates within a controlled workflow; it
does not override backend ownership, validation, or authorization rules.

### F. Tests, commits, and pull requests

1. `32-backend-tests.png`: focused `dotnet test` output, including
   `BuyerRequirementsIntegrationTests` and the relevant material/match/workflow
   tests.
2. `33-react-tests.png`: focused Vitest output for manager materials,
   categories, matches, analytics, and dashboard.
3. `34-flutter-tests.png`: focused Flutter repository/widget tests.
4. `35-ai-tests.png`: focused Python agent tests.
5. `36-build-green.png`: backend build plus web build, and mobile analyze/test
   output if required by the submission rubric.
6. `37-commit-history.png`: `git log --oneline --decorate` with Member 1
   commits highlighted.
7. `38-pr-overview.png`: PR title, description, changed-file summary,
   reviewers, checks, and merge status.
8. `39-pr-review.png`: review comments addressed and final passing checks.

Record commands and actual outcomes in the evidence index below; never label a
blocked test as passed.

## 3. Evidence index to complete

| ID | Screenshot/file | Feature proved | Role/data used | Related test/commit/PR | Result |
|---|---|---|---|---|---|
| E-01 | `[filename]` | Swagger routes/auth | `[role]` | `[test/commit]` | `[pass/blocked]` |
| E-02 | `[filename]` | Database constraints/indexes | `[sanitised DB]` | `[test/migration]` | `[pass/blocked]` |
| E-03 | `[filename]` | React search/filter/page | `MANAGER` | `[test/commit]` | `[pass/blocked]` |
| E-04 | `[filename]` | Flutter listing/location/image | `SELLER` | `[test/commit]` | `[pass/blocked]` |
| E-05 | `[filename]` | Matching Agent exclusions | `[test fixture]` | `[Python test]` | `[pass/blocked]` |
| E-06 | `[filename]` | Tests and PR traceability | `[branch/SHA]` | `[PR URL]` | `[pass/blocked]` |

## 4. Contribution outline placeholders

This is an evidence outline, not a reflection. Fill it with verifiable facts.

### Member and scope

- Member: `[name/student number]`
- Assigned sprint/story IDs: `[IDs]`
- Main responsibility: `[one-sentence feature scope]`
- In-scope layers: `[backend / database / React / Flutter / AI / tests]`
- Out-of-scope or shared work: `[brief factual boundary]`

### Implementation contributions

- Backend endpoints/classes: `[paths, controller/service names, route summary]`
- Database entities/migrations/indexes/constraints: `[names and migration]`
- React pages/components/API clients: `[paths and behaviour]`
- Flutter screens/repositories/widgets: `[paths and behaviour]`
- Agent/workflow schemas/tools/tests: `[paths and behaviour]`
- Error handling and authorization: `[roles, status codes, ownership rule]`

### Verification contributions

- Tests added/updated: `[test paths and test names]`
- Commands run: `[exact commands]`
- Results: `[passed count, blocked prerequisites, date]`
- Manual smoke checks: `[scenario and observed result]`
- Defects found and fixed: `[issue/PR/commit]`

### Collaboration and traceability

- Commits: `[SHA - message - date]`
- Pull requests: `[URL, title, review/check status]`
- Review feedback addressed: `[comment -> change -> verification]`
- Files/screenshots linked to contribution: `[E-IDs]`

## 5. Twenty likely viva questions

1. **What problem does SurplusLink solve?**  
   It connects surplus material listings with buyer requirements, while
   controlling ownership, validation, matching, workflow, and logistics data.
2. **Walk through creating a buyer requirement.**  
   The authenticated buyer submits a DTO to the controller; the service
   validates and normalises it, persists the entity, and returns a
   `201 Created` response with a location.
3. **Why are draft, open, matching, and terminal statuses separate?**  
   They model a state machine so only valid transitions are possible and
   terminal/workflow records cannot be changed through ordinary CRUD.
4. **How is ownership enforced?**  
   JWT identity is compared with the stored owner in the service/query. A
   different owner cannot read or mutate the resource; manager read access is
   explicitly role-gated.
5. **Why can an anonymous request get 401 while another user gets 403?**  
   `401` means authentication is missing/invalid; `403` means the caller is
   authenticated but lacks permission for that resource/action.
6. **What is an entity?**  
   An entity is a domain object with identity and lifecycle, such as a
   `BuyerRequest`, `MaterialListing`, `Match`, or `Approval`, mapped to a table.
7. **What is a relationship?**  
   It is a link between entities, usually represented by a foreign key, such
   as a listing belonging to a seller or a match linking a requirement to a
   listing.
8. **What is an index and why use one?**  
   An index is an ordered lookup structure that speeds common filters/joins,
   such as owner, status, category, or deadline queries, at extra write/storage
   cost.
9. **What is a constraint?**  
   A database rule that rejects invalid data, such as positive quantity,
   allowed status, valid coordinates, or an existing owner/category foreign key.
10. **Explain DTO, service, and controller.**  
    A DTO is the API input/output shape; the controller handles HTTP/auth
    concerns; the service owns validation, business rules, transactions, and
    persistence orchestration.
11. **Why not put all business logic in the controller?**  
    A thin controller is easier to test and keeps HTTP concerns separate from
    rules reused by other endpoints, workflows, and background operations.
12. **How do search, filter, sort, and pagination work together?**  
    Build one query, apply search and filters, apply deterministic ordering,
    calculate totals, then apply page/page-size. The response returns items and
    pagination metadata.
13. **Why must sorting be deterministic?**  
    A stable tie-breaker (for example ID) prevents records moving between pages
    when equal scores or dates are present.
14. **How do you avoid exposing another user's rows?**  
    Apply the owner predicate in the database query/service, not just in the
    UI, and test both a permitted owner and a different owner.
15. **How does the Flutter image picker fit the flow?**  
    The user selects a file, the UI validates it, and the repository/gateway
    sends it using the supported API contract; the UI shows preview/error state.
16. **How does GPS work and what happens if permission is denied?**  
    Request location permission through the platform plugin, validate the
    returned range, and provide a clear error or manual fallback instead of
    silently inventing coordinates.
17. **What is category CRUD?**  
    Managers can create, read, update, and delete material categories; clients
    use them for forms and filters, while authorization prevents ordinary users
    from administering the taxonomy.
18. **What do the analytics endpoints measure?**  
    They aggregate stored requirements, listings, matches, route outcomes, and
    rejection reasons; empty averages are represented honestly rather than
    fabricated.
19. **What is the Matching Agent allowed to do?**  
    It can validate/read eligible candidates and produce a ranked proposal or
    workflow step. Backend rules still reject self-matches, invalid ownership,
    expired/inactive/unverified records, and insufficient quantity.
20. **How did you prove the implementation works?**  
    By combining unit/integration/UI/agent tests, Swagger/API smoke checks,
    database evidence, role-matrix checks, screenshots, commits, and reviewed
    PRs, with blocked prerequisites recorded honestly.

## 6. Five evaluator-style live changes/debug tasks

### Task 1: Add a new listing filter

**Prompt:** Add a `condition` filter to the manager material table and preserve
the selected value when moving to the next page.

**Expected approach:** Extend the request/query DTO and query builder, pass the
value through the React API client and UI, update/reset pagination when filters
change, add a backend query test and a React request test.

**Demo proof:** Show the network request, filtered rows, page metadata, and
tests. Mention SQL translation and deterministic ordering.

### Task 2: Fix an ownership leak

**Prompt:** A buyer can see another buyer's requirement by changing the ID in
the URL. Diagnose and fix it.

**Expected approach:** Reproduce with two authenticated clients, inspect the
service authorization predicate, return the repository-standard `403`/`404`
response, and add a regression test for read, update, delete, and actions.

**Demo proof:** Show before/after HTTP results and the test that prevents
regression. Do not rely on hiding the row in React.

### Task 3: Repair a pagination bug

**Prompt:** Page 2 repeats an item from page 1 when two matches have equal
scores.

**Expected approach:** Add a stable secondary ordering (such as ID), apply
ordering before `Skip`/`Take`, verify totals are calculated before pagination,
and test both ascending and descending directions.

**Demo proof:** Show the exact two-page response and the query test.

### Task 4: Handle image/GPS failure safely

**Prompt:** Image selection or GPS permission fails on Flutter. The form must
remain usable and must not submit fake coordinates.

**Expected approach:** Preserve the form state, display a retry/error message,
allow the documented manual fallback if supported, validate required fields,
and ensure the gateway does not send invalid/null data contrary to the API
contract.

**Demo proof:** Run the widget test or emulator flow for denial/failure,
successful retry, and blocked invalid submission.

### Task 5: Make an agent candidate safe

**Prompt:** The Matching Agent ranks a candidate owned by the same buyer as
the requirement.

**Expected approach:** Add/repair the exclusion in the trusted backend/workflow
boundary as well as the agent-side candidate handling; persist an auditable
reject reason such as `SELF_MATCH_NOT_ALLOWED`; add a Python test and a
backend integration test.

**Demo proof:** Show the candidate absent from valid recommendations, the
rejection/history record, and both test suites. Explain why a high model score
cannot override a backend invariant.

## 7. Short concept cheat sheet

| Term | Simple explanation | SurplusLink example |
|---|---|---|
| Entity | A uniquely identifiable business object stored and changed over time. | `BuyerRequest`, `MaterialListing`, `Match`, `Approval`. |
| Relationship | A link between entities, normally represented by a foreign key. | A match links one requirement to one listing. |
| Index | A lookup structure that makes frequent searches/joins faster. | Owner/status/category indexes support list screens. |
| Constraint | A rule the database refuses to violate. | Quantity must be positive; category and owner must exist. |
| DTO | A deliberately shaped request or response contract. | `SaveRequirementRequest` carries form input. |
| Service | The layer that applies business rules and coordinates persistence. | `RequirementService` validates transitions and ownership. |
| Controller | The HTTP boundary that maps requests to service calls and responses. | `RequirementsController` exposes `/api/requirements`. |
| Ownership auth | Permission based on the authenticated user's stored ownership. | A buyer can manage only their own requirement. |
| Search/filter/sort/page | Query controls applied before returning a bounded result set. | Material search plus status/category filter and page metadata. |
| GPS | Device location used as validated latitude/longitude. | Requirement/listing location supports distance matching. |
| Image picker | A mobile file-selection capability with preview/upload handling. | Seller attaches material photos. |
| Category CRUD | Manager administration of the material taxonomy. | Add/edit/delete material categories used by forms. |
| Analytics | Aggregates that explain system activity and outcomes. | Match score/distance, route success, rejection reasons, inventory totals. |
| Matching Agent | A controlled read/rank/validation workflow that proposes matches. | Eligible listings are ranked, audited, and routed for workflow/approval. |

## 8. Final submission checklist

- [ ] All `[placeholders]` replaced with factual values.
- [ ] Screenshots are numbered, readable, and secrets are masked.
- [ ] Each claimed feature has at least one implementation reference and one
      verification reference.
- [ ] Test output includes command, date, commit, and pass/blocked status.
- [ ] PR/commit links identify the Member 1 contribution.
- [ ] No personal reflection has been added to this pack.
