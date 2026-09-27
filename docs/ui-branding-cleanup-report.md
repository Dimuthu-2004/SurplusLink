1. **Black logo background removal**

   Used the built-in imagegen background-extraction edit on the existing `web/src/assets/surpluslink-logo.png`. The prompt requested removal of only the black background/negative spaces, genuine PNG alpha, and preservation of the existing symbol, SurplusLink lettering, tagline, colors, and geometry. No alternative brand was designed. Alpha decoding tests verify transparent background pixels. All runtime references now use the shared brand components. Original source files remain for reference.

2. **Transparent logo assets**

   - `web/src/assets/branding/surpluslink-logo-transparent.png`
   - `mobile/assets/branding/surpluslink_logo_transparent.png`

   Flutter's symbol-only use clips the symbol from this same asset; it does not invent a separate mark.

3. **Uploaded Lottie startup animation**

   Copied `Architecture and Construction Animation.lottie` from the supplied local Downloads file, unchanged, to `mobile/assets/animations/architecture_and_construction.lottie`. Declared it in `pubspec.yaml` and added `lottie` 3.6.1. The documented `LottieComposition.decodeZip` API selects `animations/12345.json` and loads the embedded image. See https://pub.dev/documentation/lottie/latest/ . Startup uses a warm `#FFFDF9` background, centered responsive animation capped at 280px, a 40px transparent logo, reduced-motion support, and a 220ms fade cover. Initialization and auth routing run underneath; no minimum animation playback timer was added. Android/iOS native launch images were removed; Android 12+ has an empty splash icon to prevent an oversized static logo before Flutter starts.

4. **Manager approval navigation**

   - `/app/manager/listing-approvals`: Seller Listing Approvals; existing listings query locked to `PENDING_VERIFICATION`, using the existing review/verification action.
   - `/app/manager/requirement-approvals`: Buyer Requirement Approvals; existing grouped workflows API and decision actions.
   - `/app/manager/approvals` redirects to the requirement queue for old links.
   - Updated navigation, dashboard links, headings, and detail back links. No new approval backend was introduced.

5. **Buyer Requirement Approval table**

   Material / Requirement, Buyer, Requested, Selected, Remaining, Fulfillment, Sellers, Total Value, Status, Action. Quantities include units, Full/Partial have badges, and rows become labeled cards below 1100px without a wide scrolling table. Details include every seller allocation, names, material links, quantity, availability, price, allocation value, score, distance, transport, and status, plus available requirement notes/category/budget/deadline. References are secondary metadata.

6. **Buyer My Offers columns**

   Material / Requirement, Seller, Quantity, Unit, Total Value, Status, Created, Details. Seller business name is preferred over full name. Dual-role accounts retain their existing combined activity and use a meaningful Trading partner column.

7. **Seller My Offers columns**

   Material, Buyer, Allocated Quantity, Unit, Total Value, Status, Created, Details. Removed the technical Offer and Context primary columns.

8. **Human-readable names**

   Offer list and detail reads share one EF SQL projection through existing Buyer, Seller, Listing, and MaterialRequest navigation properties. React displays the projected names and never falls back to raw participant IDs. Grouped approval names already existed in the backend and are reused. No React name-resolution requests were added.

9. **Currency formatting**

   Added `web/src/utils/currency.ts` with `formatLkr` (for example `LKR 2,000.00`). Replaced USD formatters in manager listing list/details. Centralized amounts in offers/details, transactions, requirement lists/details, approval summaries/allocations, buyer listing cards/details, match formatting, and match analytics. Dashboard was audited and its approval links updated; it currently has no displayed monetary metric requiring conversion. Numeric stored values are unchanged.

10. **Forgot Password OTP root cause**

    Successful unauthenticated requests dispatched `CLEAR_ERROR`, whose reducer did not clear `pending`. This left recovery and verification controls loading after a completed API call; registration also lacked a completion dispatch. Added `REQUEST_FINISHED` without changing endpoints or SMTP. A shared text-based OTP input captures values synchronously, filters numeric digits, caps at six, supports paste/backspace/Enter, and disables only during requests. Reset validation controls submit availability and errors allow retry. Verification now resets the visual sign-up mode when returning to sign-in, without authenticating automatically.

11. **NIC labels**

    React registration and Flutter shared registration/profile fields now display `NIC`, including their NIC validation messages. Existing 9-digits-plus-V/X and 12-digit validation rules remain intact. Backend NIC validation was not changed.

12. **Backend DTO/API fields**

    Added optional `buyerName`, `sellerName`, `sellerBusinessName`, `materialName`, `requirementTitle`, and `unit` to `OfferResponse` and the React Offer type. Original fields and endpoint authorization remain compatible. No schema migration or transaction-write changes. SQL projection and serialization tests check public fields and absence of sensitive profile data.

13. **Files changed**

    - `backend/SurplusLink.Api/Transactions/TransactionContracts.cs`
    - `backend/SurplusLink.Api/Transactions/TransactionService.cs`
    - `backend/SurplusLink.Tests/OfferPresentationTests.cs`
    - `backend/SurplusLink.Tests/TransactionQueryIntegrationTests.cs`
    - `mobile/android/app/src/main/res/drawable-v21/launch_background.xml`
    - `mobile/android/app/src/main/res/drawable/launch_background.xml`
    - `mobile/android/app/src/main/res/drawable/splash_empty.xml`
    - `mobile/android/app/src/main/res/values-night-v31/styles.xml`
    - `mobile/android/app/src/main/res/values-v31/styles.xml`
    - `mobile/assets/animations/architecture_and_construction.lottie`
    - `mobile/assets/branding/surpluslink_logo_transparent.png`
    - `mobile/ios/Runner/Base.lproj/LaunchScreen.storyboard`
    - `mobile/lib/app.dart`
    - `mobile/lib/screens/splash_screen.dart`
    - `mobile/lib/widgets/profile_fields.dart`
    - `mobile/lib/widgets/startup_transition.dart`
    - `mobile/lib/widgets/surplus_link_logo.dart`
    - `mobile/pubspec.lock`
    - `mobile/pubspec.yaml`
    - `mobile/test/auth_navigation_test.dart`
    - `mobile/test/branding_startup_test.dart`
    - `mobile/test/my_materials_screen_test.dart`
    - `mobile/test/profile_media_location_test.dart`
    - `web/src/app/App.tsx`
    - `web/src/assets/branding/surpluslink-logo-transparent.png`
    - `web/src/auth/AuthContext.tsx`
    - `web/src/components/AppLayout.tsx`
    - `web/src/components/OtpInput.tsx`
    - `web/src/components/SurplusLinkLogo.tsx`
    - `web/src/features/matches/MatchAnalyticsWidget.tsx`
    - `web/src/features/matches/matchFormatters.ts`
    - `web/src/features/transactions/transactionsApi.ts`
    - `web/src/pages/LoginPage.tsx`
    - `web/src/pages/MyOffersPage.tsx`
    - `web/src/pages/OfferDetailsPage.tsx`
    - `web/src/pages/buyer/BuyerListingCard.tsx`
    - `web/src/pages/buyer/BuyerListingDetailsPage.tsx`
    - `web/src/pages/manager/ManagerApprovalsPage.tsx`
    - `web/src/pages/manager/ManagerDashboardPage.tsx`
    - `web/src/pages/manager/ManagerListingsPage.tsx`
    - `web/src/pages/manager/ManagerMaterialDetailsPage.tsx`
    - `web/src/pages/manager/ManagerRequirementDetailsPage.tsx`
    - `web/src/pages/manager/ManagerRequirementsPage.tsx`
    - `web/src/pages/manager/ManagerWorkflowDetailsPage.tsx`
    - `web/src/pages/manager/WorkflowSummary.tsx`
    - `web/src/pages/manager/WorkflowTransactions.tsx`
    - `web/src/pages/manager/managerLayout.css`
    - `web/src/test/authNavigation.test.tsx`
    - `web/src/test/buyerMarketplace.test.tsx`
    - `web/src/test/currency.test.ts`
    - `web/src/test/managerApprovals.test.tsx`
    - `web/src/test/managerDashboard.test.tsx`
    - `web/src/test/offerDetails.test.tsx`
    - `web/src/test/presentationCleanup.test.tsx`
    - `web/src/test/workflowRecommendation.test.tsx`
    - `web/src/utils/currency.ts`
    - `docs/ui-branding-cleanup-report.md`

    The pre-existing edit to `mobile/macos/Flutter/GeneratedPluginRegistrant.swift` was left untouched and is excluded above.

14. **Tests PASS/FAIL**

    - PASS: `npm test -- --run` (invoked via `npm.cmd` because PowerShell blocks npm.ps1): 121 tests, 18 files.
    - PASS: `npm run build` (via `npm.cmd`); Vite reports a bundle-size warning.
    - PASS: `flutter analyze`: no issues.
    - FAIL: `flutter test`: 190 passed, 1 failed. The failure is `match_details_polish_test.dart: narrow layout remains usable`, an existing 320px match-details bottom-navigation overflow. The failing test and screen are unchanged; this issue was outside the listed scope. New branding/startup tests and existing auth/registration/verification-routing tests pass.
    - PASS: `flutter build apk --debug`: `mobile/build/app/outputs/flutter-apk/app-debug.apk`.
    - PASS with PostgreSQL 18 enabled: `dotnet test SurplusLink.sln --configuration Release --no-restore`: 219 passed, 2 skipped, 0 failed. The two skipped tests require the live AI graph. This supersedes the initial local run with 63 database-dependent skips. The CI count failure was reproduced (expected 1 pending transaction, actual 2): the new DTO test left a pending transaction in a shared class database. Transaction tests now each own a disposable database through `IAsyncLifetime`; assertions and production behavior are unchanged.
    - PASS: runtime source audit found no remaining old logo references, incorrect USD currency formatter, or Sri Lankan NIC user-facing label in React/Flutter.
    - Live browser visual review was unavailable because no connected browser was available. PNG alpha, asset decoding, responsive widget bounds, and rendered React content were covered by automated checks.

AI matching, buyer selection, manager approval business logic,
inventory reservation, handover and completion flows were not changed.
