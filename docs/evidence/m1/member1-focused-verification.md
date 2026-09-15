# Member 1 Focused Verification Set

## Scope and prerequisite

Run this set against the integrated Member 1 source revision. The current origin/main includes the Material API, React manager pages, Flutter seller screens, and Matching Agent.

This local feature/unit-intergration-tests branch is behind that revision. Do not force-merge it while the generated Flutter files are dirty. No password, JWT, API URL, or database identifier is included in this plan: obtain those from the configured local environment.

## Exact automated test files

| ID | Test file | Test case | Expected result |
|---|---|---|---|
| M1-BE-01 | backend/SurplusLink.Tests/MaterialListingWorkflowTests.cs (add) | Seller creates a valid listing | POST /api/materials returns 201 Created, response status is DRAFT, seller ID is the authenticated seller, listing and LISTING_CREATED audit record persist. |
| M1-BE-02 | backend/SurplusLink.Tests/MaterialListingWorkflowTests.cs (add) | Seller edits another seller's listing | PUT /api/materials/{id} returns 404 Not Found (the implemented ownership-safe response); the target listing is unchanged. |
| M1-BE-03 | backend/SurplusLink.Tests/MaterialListingWorkflowTests.cs (add) and backend/SurplusLink.Tests/MaterialInventorySchemaTests.cs | Quantity is zero or negative | API model validation returns 400; no listing persists. The schema test confirms the positive-quantity database check constraint remains configured. |
| M1-BE-04 | backend/SurplusLink.Tests/MaterialListingQueryBuilderTests.cs | Search, category/status/condition/price filters, sorting, and pagination | Matching title/description/category results are returned; filters narrow correctly; page 2 contains the expected ordered items and totals. |
| M1-BE-05 | backend/SurplusLink.Tests/MaterialListingWorkflowTests.cs (add) | Manager verification permission | Non-manager verification request returns 403; a manager can verify a PENDING_VERIFICATION listing, which becomes ACTIVE and receives LISTING_VERIFIED audit history. |
| M1-WEB-01 | web/src/test/managerMaterials.test.tsx | Material table sends search/sort/page query | The current test confirms the applied query and Next-page request. |
| M1-WEB-02 | web/src/test/managerMaterials.test.tsx (add) | Material table API failure and retry | The error is shown in an alert; Retry issues a new list request and a successful retry renders the table. |
| M1-MOB-01 | mobile/test/material_inventory_repository_test.dart | Material search/filter transport contract | Authenticated GET /api/materials includes search, status, sort, page, and pageSize query parameters. |
| M1-MOB-02 | mobile/test/material_listing_form_screen_test.dart (add) | Add Material validation | Empty required values and non-positive quantity prevent create; field validation messages are shown. |
| M1-MOB-03 | mobile/test/my_materials_screen_test.dart (extend) | Search/filter behavior and empty state | The selected search/filter values are passed to the gateway; an empty result shows No materials match these filters. |
| M1-AI-01 | ai-service/tests/test_material_matching_agent.py | Active normal candidates | Valid active, verified, non-expired candidates are returned in basic-fit-score order. |
| M1-AI-02 | ai-service/tests/test_material_matching_agent.py | Inactive/expired exclusion | DRAFT, unverified, and expired entries never reach detail lookup or the output. |
| M1-AI-03 | ai-service/tests/test_material_matching_agent.py | Insufficient quantity safe case | A too-small active candidate returns status no_candidate, an empty candidate list, and failure code NO_CANDIDATE. |

## Commands

Run from the repository root after the relevant source has been integrated.

~~~powershell
dotnet test .\backend\SurplusLink.Tests --filter "FullyQualifiedName~MaterialListing"
~~~

Expected: M1-BE-01 through M1-BE-05 pass. The workflow tests require the test project to use an actual configured PostgreSQL test database; if that service/configuration is unavailable, mark these tests BLOCKED rather than passing them.

~~~powershell
cd .\web
npm test -- src/test/managerMaterials.test.tsx --reporter=verbose
~~~

Expected: M1-WEB-01 and M1-WEB-02 pass. The table test must verify both query parameters and an API-error/retry state.

~~~powershell
cd ..\mobile
flutter test test/material_inventory_repository_test.dart test/material_listing_form_screen_test.dart test/my_materials_screen_test.dart
~~~

Expected: M1-MOB-01 through M1-MOB-03 pass. Do not require a device/emulator because repository and widget tests use fakes/mocked HTTP.

~~~powershell
cd ..\ai-service
python -m unittest discover -s tests -p "test_material_matching_agent.py" -v
~~~

Expected: all three Matching Agent tests pass. The tests use a fake approved read boundary and never call a database or mutation endpoint.

## Live smoke checks

Use the real API URL printed by the API startup log and a real seeded/test account. Do not put tokens or passwords in source files.

1. Sign in as Seller A and create a valid listing. Confirm it is DRAFT.
2. Sign in as Seller B and attempt to update Seller A's listing. Confirm 404 and no data change.
3. Submit zero/negative quantity. Confirm 400 and no listing row.
4. Sign in as Manager, open the React Material table, search/filter/sort/page, then verify a pending listing.
5. In Flutter, leave required Add Material fields blank and enter zero quantity; confirm form validation prevents submission. Then apply search/filter in My Materials.
6. Run the Matching Agent suite; confirm inactive, expired, unverified, and too-small records are never candidates.

## Recording results

For each case record command, date/time, actual status, and a screenshot or terminal output. A missing PostgreSQL service, missing test database configuration, or unmerged source component is a BLOCKED condition, not a pass.