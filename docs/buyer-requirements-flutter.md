# Buyer Requirements Flutter UI (M2-3)

## Run

This work is on feature/m2-3-flutter-buyer-ui in the existing shared repository.
The branch includes the completed M2-1/M2-2 backend commits.

Start the API in one PowerShell terminal:

~~~powershell
Set-Location C:\Users\Dimuthu_K\SurplusLink\SurplusLink\backend\SurplusLink.Api
dotnet run --launch-profile http
~~~

In another terminal:

~~~powershell
Set-Location C:\Users\Dimuthu_K\SurplusLink\SurplusLink\mobile
flutter devices
~~~

If no Android device appears, start an emulator from Android Studio Device Manager.
Run the app with the device ID shown by flutter devices:

~~~powershell
flutter run -d emulator-5554
~~~

Replace emulator-5554 if your listed device ID differs. The shared API client defaults
to http://10.0.2.2:5170 for the Android emulator. For a physical phone, use a reachable
development API address via --dart-define=API_BASE_URL=http://YOUR_HOST:PORT.
For Edge, use --dart-define=API_BASE_URL=http://localhost:5170 and the existing API CORS configuration.
Keep JWTs and credentials in the existing authentication flow.

Log in as BUYER and choose **My Requirements**.

## Screens and behavior

| Route | Screen |
| --- | --- |
| /requirements | My Requirements, search/filter/sort, totals and paging |
| /requirements/new | Create Requirement |
| /requirements/:id/edit | Edit Requirement (DRAFT only) |
| /requirements/:id | Details, confirmed Submit/Start Matching/Cancel/Delete actions |
| /requirements/:id/status | Workflow / Recommendation Status |
| /requirements/:id/history | Paginated audit history |

All these routes, including direct links, are protected by shared authentication and BUYER role.
SELLER/MANAGER users return to their home screen. The backend still enforces record ownership.
The router preserves an initial protected deep link while the session is restored.

- Category names come from GET /api/material-categories; IDs are not typed manually.
- Create/update use the M2 request DTO, including optional notes, with no buyerId/status overposting.
- A date picker shows deadlines in local time. Choosing a date sets local 23:59:59;
  requests send UTC. The initial deadline defaults to seven days ahead.
- Quantity: positive, up to three decimal places and 15 integer digits.
  Budget: positive, up to two decimal places and 16 integer digits.
- Unit: nonblank, at most 32 characters. Notes: at most 2,000 characters.
- Coordinates: both required, latitude -90..90 and longitude -180..180,
  up to six decimal places. GPS is rounded to six places before filling the fields.
- GPS capture only runs after a tap. Permission denial, permanently blocked permission,
  disabled location services and capture failure leave manual entry available.
  Device capture has a 20-second timeout.
- Forms retain entered values after validation/network errors and disable duplicate saves.
- Search matches category/notes. Filters: status, category and inclusive deadline date range.
  Sort: created date, deadline or budget, ascending/descending. Page sizes: 10/20/50.
  Apply search and filters resets to page 1; reset clears all filters. Older asynchronous
  list responses cannot overwrite a newer search.
- Drafts can be edited/deleted/submitted. OPEN can start matching. DRAFT/OPEN can cancel.
  An expired deadline disables submit/start. State transitions require confirmation.
- Details refresh after edits and when returning from status; a 409/503 action failure
  refreshes the last confirmed requirement state.
- Loading, empty and error/retry states are supplied for each data screen. API validation
  details are shown to the user. A 401 uses shared logout and returns to login; 403 stays
  an access error and does not clear the session.
- History uses the paged API (20 per page), showing old -> new statuses, local timestamps,
  and account/system actions.

## Workflow and recommendation dependency

No new workflow or recommendation endpoints/persistence are created in this UI.
The current production backend still returns 503 for start-matching until M4-2 is wired.
The UI displays the API's message and keeps the requirement OPEN.

If the API returns a successful start, the UI uses its actual requirement status and returned
workflow ID. It does not generate an ID or simulate matches. That ID is carried into the status
screen for this navigation session. A fresh direct status link only reads GET /api/requirements/:id;
there is currently no canonical workflow/recommendation read endpoint to recover extra details.

The status screen covers all nine requirement states, offers manual refresh, and explicitly
states that recommendation details are not yet available. M4 integration must supply the
canonical workflow/recommendation read contract before this UI can show recommendations,
approval details or a persistent workflow detail link. Starting matching never calls reservation APIs.

## Automated tests

From mobile:

~~~powershell
flutter analyze --no-pub
flutter test --no-pub
flutter build apk --debug --no-pub
~~~

New test coverage:

- requirement_repository_test.dart: all query parameters/UTC dates, shared bearer token,
  category/CRUD/action URLs, writable DTO fields, actual returned workflow ID, paged history,
  ProblemDetails/validation errors, 401 shared logout and 403 handling.
- buyer_requirements_widget_test.dart: BUYER home and all protected deep links; list loading,
  empty/error/retry/search/sort; invalid forms, date picker, GPS rounding, save deduplication;
  GPS denial/manual fallback; retained values on save errors; draft edit and OPEN edit block;
  action confirmation, submit, deferred start, cancel; successful start/status refresh using
  a test double; history transition/pagination/access-error/empty states; small-screen
  details with 1.8x text and expired-deadline action guards.
- support/requirement_fakes.dart: injectable test data, delayed requests and location source.
  These fakes are not production workflow implementations.

Widget/repository tests do not prove real GPS permission dialogs or live API connectivity.
Android, iOS/macOS platform permissions are configured; native iOS/macOS execution requires
those platforms and is not validated on this Windows workspace.

## Manual tests

Use Buyer A, Buyer B, and a seller account; a manager account is useful for role checks.
Keep the existing API/Swagger available for checking saved values and history.

1. **Navigation:** sign in as Buyer A. Open My Requirements. Sign out; revisiting any
   requirement link must take you to login. As seller/manager, Buyer navigation is hidden
   and direct requirement links return to that role's home.
2. **Empty/retry:** use an account with no requirements: the empty message and Create remain
   available. Stop the API and refresh: an error/Retry appears. Restart it and retry.
3. **Create validation:** tap Create, then Save draft without values. Required-field errors
   appear with no request saved. Try quantity 0/-1/0.0001, budget 0/0.001, latitude 91,
   longitude -181 and coordinates with seven decimals. Each must be rejected locally.
4. **Valid draft:** select Cement, quantity 10.125, unit kg, budget 25000.50, optional notes,
   a future deadline, latitude 6.9271 and longitude 79.8612. Save: Details shows DRAFT.
   Check Swagger GET by ID to verify values and UTC deadline.
5. **Date picker:** choose today/a future day; confirm the displayed local deadline ends
   at 23:59. Check that API storage represents the same instant in UTC.
6. **GPS:** enable location services and tap Use my GPS location. Allow access; both fields
   fill with at most six decimals. On an emulator, set a location using its Extended Controls.
   Repeat with services off, denied permission and permanently denied permission. Each case
   must explain the failure and permit manual coordinates; no location prompt appears before a tap.
7. **Save failure:** enter valid form values, stop the API, then save. The error should leave
   every field intact. Restart the API and save once. Rapid repeated taps must not send duplicate saves.
8. **Edit:** from a DRAFT, change quantity, notes and location and save. Details refreshes.
   Directly opening edit for OPEN or later states shows the draft-only message without a save form.
9. **Submit:** tap Submit. Go back in the dialog: no change. Confirm: OPEN; edit/delete vanish,
   and Start matching appears. Check History for SUBMITTED and DRAFT -> OPEN.
10. **Start matching now:** confirm Start matching on OPEN. With the current deferred backend,
    show "Matching is not available yet. Your requirement remains open." Keep OPEN and allow
    retry. No MATCHING_STARTED/status-change success entry or reservation should appear.
11. **After M4-2 integration:** a successful start navigates to Matching Status using the actual
    API workflow ID. Refresh reads the latest requirement status. Repeated starts must follow
    the backend's state guard, without extra workflows or material reservations.
12. **Cancel/delete:** cancel a separate DRAFT and an OPEN requirement; confirm CANCELLED and
    removal of management actions. Delete a separate draft and confirm return to the list.
    Declining either dialog must leave the requirement intact.
13. **Search/filter/sort:** create drafts with different notes, categories, budgets and deadlines.
    Search for part of notes and a category name; try mixed case. Combine status/category/date
    bounds. Test all three sort fields in both directions. With more than ten rows, go to the
    next page and verify totals; changing filters/reset returns to page 1.
14. **Ownership:** as Buyer B, My Requirements never includes A's rows. Directly opening A's
    details/edit/history/status must display an access error; no A content should be returned.
15. **History:** view a draft's history, then update/submit/cancel and refresh. It should show
    operations and status pairs chronologically. Test more than 20 events with paging.
    Failed or declined actions must not add success history.
16. **Status:** open Workflow / recommendation status for each available lifecycle state.
    It must show the API state, with no invented recommendations/progress/workflow ID.
    On a fresh direct link, unavailable workflow details are stated clearly.
17. **Expired/session handling:** use an expired JWT to load requirements: shared logout returns
    to login. For a draft whose deadline has passed, Submit is disabled; edit its deadline.
    For expired OPEN, Start is disabled and cancellation remains possible.
18. **Layout:** test a small phone, large system text, keyboard open, long notes, and rotation.
    Forms/lists scroll and buttons remain reachable; no horizontal overflow.

## Exact files

New production files:

- mobile/lib/requirements/requirement_models.dart
- mobile/lib/requirements/requirement_gateway.dart
- mobile/lib/requirements/requirement_repository.dart
- mobile/lib/requirements/requirement_location.dart
- mobile/lib/requirements/requirement_widgets.dart
- mobile/lib/screens/my_requirements_screen.dart
- mobile/lib/screens/requirement_form_screen.dart (Create and Edit)
- mobile/lib/screens/requirement_details_screen.dart (Submit/Start/Cancel/Delete confirmations)
- mobile/lib/screens/requirement_status_screen.dart
- mobile/lib/screens/requirement_history_screen.dart

Updated integration/platform files:

- mobile/lib/app.dart
- mobile/lib/main.dart
- mobile/lib/routing/app_router.dart
- mobile/lib/screens/home_screen.dart
- mobile/lib/core/api_client.dart (read ProblemDetails.detail)
- mobile/ios/Runner/Info.plist
- mobile/macos/Runner/Info.plist
- mobile/macos/Runner/DebugProfile.entitlements
- mobile/macos/Runner/Release.entitlements

Tests/documentation:

- mobile/test/requirement_repository_test.dart
- mobile/test/buyer_requirements_widget_test.dart
- mobile/test/support/requirement_fakes.dart
- docs/buyer-requirements-flutter.md
- mobile/README.md (link to this guide)

No backend endpoint/auth/workflow schema or dependency versions change for M2-3.
Android location permissions and the geolocator package were already present and are reused.
Pre-existing generated desktop plugin files are not part of the feature edits.
