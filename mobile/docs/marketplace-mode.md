# Buyer / Seller marketplace mode

Implemented client-side mode selection for accounts with both BUYER and SELLER roles. No backend, API contract, JWT, database role, matching, reservation, approval, handover, or completion implementation was changed.

## State and persistence

`MarketplaceModeController`, owned by `AuthController.marketplace`, stores the active presentation mode in memory. `MarketplaceModeScope` exposes it to shared navigation. The production composition in `main.dart` injects `SecureMarketplaceModeStorage`, which uses the existing flutter_secure_storage dependency and the per-user key `marketplace_mode:<userId>`. The stored value is only `buyer` or `seller`; no authorization data is copied or modified.

Buyer-only and seller-only accounts resolve automatically to their available mode and have no switcher. Dual-role accounts default to Buyer Mode when no valid preference exists. Session restoration reads the preference before displaying the authenticated experience. Logout clears the in-memory mode and the current user's preference; preference writes and logout cleanup are serialized. Storage failures do not invalidate authentication, and restore/save failures have a UI message.

## UI and screen reuse

The existing HomeScreen and HomeDashboardController render one set of role-specific cards, actions, and activity. There is no duplicate dashboard. The header has compact Buyer Mode / Seller Mode buttons using the existing orange accent and light surface, selected accessibility semantics, and a 220 ms fade/slide transition. Header content scales down as needed at phone width.

Existing requirement, match, material, offer, transaction detail, and history screens remain in use, with their original gateways and business actions. The shared RoleNavigation shows one role's destinations. Mode changes replace the entire navigation stack with `/home`, including imperative transaction/detail pages; routes belonging to the other mode redirect to Home. The selected dashboard controller is recreated and refreshed, and a disposed controller ignores late results.

MyOffersScreen uses a read-only MarketplaceOfferView for dual-role lists. It filters by the authenticated user's buyerId or sellerId participation. Because existing APIs do not provide a participation filter, it reads the result pages before applying local pagination, preserving API filters and sort order. This can require additional list requests for dual-role accounts with long histories. Single-role accounts use the original gateway directly and retain their existing requests. Detail and mutation methods continue through the original gateway.

The production ApiClient shares a MutationActivity tracker with the mode controller. POST, PUT, PATCH, DELETE, and photo uploads keep switching disabled until the request finishes, including errors. Tracking does not change or cancel requests. The real authenticated user and both roles remain intact throughout.

## Verification

- **PASS** `flutter analyze`: no issues found.
- **PASS** `flutter test`: all **173** tests passed.
- **PASS** `git diff --check`.
- **PASS** supplemental backend policy tests: 8 passed, including self-match rejection.
- **BLOCKED / unverified** supplemental PostgreSQL integration coverage: 8 skipped because `SURPLUSLINK_TEST_CONNECTION` was unset. Database-backed reservation, handover, completion, and role-integrity scenarios were not exercised in this run.

New and updated Flutter coverage includes single-role visibility and existing flows, dual-mode visibility, real roles/token preservation, preference restoration and invalid fallback, user isolation, logout cleanup/write ordering, storage failures, pending mutation success/failure, phone-width layout, complete navigation reset, filtered offers and transactions across pages, unchanged mutation delegation, self-match API rejection propagation, and late dashboard response disposal. Existing buyer/seller matching, inventory, handover, receipt, and completion widget/repository tests also passed.

Commands ran from `mobile` for Flutter, and repository root for the supplemental backend check:

```powershell
flutter analyze
flutter test

dotnet test backend/SurplusLink.Tests/SurplusLink.Tests.csproj -c Release --filter "FullyQualifiedName~DualRoleAuthTests.Shared_self_match_policy|FullyQualifiedName~PartialMultiMatchSelectionTests|FullyQualifiedName~Reservation|FullyQualifiedName~Transaction" --verbosity minimal
```

## Files changed by this task

All paths below are relative to `mobile/`.

| File | Change |
| --- | --- |
| `lib/marketplace/marketplace_mode.dart` | New enum and safe role-based mode resolution |
| `lib/marketplace/marketplace_mode_controller.dart` | New mode state, lifecycle, persistence coordination, and inherited scope |
| `lib/marketplace/marketplace_mode_storage.dart` | New per-user secure preference storage and injectable memory storage |
| `lib/marketplace/marketplace_offer_view.dart` | New read-only participation filtering and pagination |
| `lib/widgets/marketplace_mode_switcher.dart` | New responsive header switcher |
| `lib/core/mutation_activity.dart` | New pending-mutation tracking |
| `lib/main.dart` | Production storage and mutation tracker wiring |
| `lib/app.dart` | Shared mode scope and navigation-stack reset |
| `lib/auth/auth_controller.dart` | Bind mode to session lifecycle and clear on logout |
| `lib/core/api_client.dart` | Observe mutation lifetime without changing request semantics |
| `lib/home/home_dashboard_controller.dart` | Select existing role reads and ignore disposed responses |
| `lib/routing/app_router.dart` | Mode-aware presentation redirects and offers configuration |
| `lib/screens/home_screen.dart` | Reuse dashboard sections for selected mode, transition, and responsive header |
| `lib/screens/my_offers_screen.dart` | Use selected participation view for list reads |
| `lib/widgets/role_navigation.dart` | Render only selected mode destinations |
| `test/marketplace_mode_test.dart` | New mode, persistence, mutation, navigation, API, and layout regressions |
| `test/home_screen_test.dart` | Exclusive mode content and single-role switcher absence |
| `test/auth_navigation_test.dart` | Dual registration now lands in Buyer Mode |
| `test/my_materials_screen_test.dart` | Explicit mode selection in dual-role inventory/navigation flows |
| `test/recommended_matches_test.dart` | Updated dual-role home assertion |
| `test/runtime_repair_test.dart` | Default dual-role offers show buyer participation only |
| `docs/marketplace-mode.md` | Implementation and verification report |

Pre-existing generated desktop plugin changes and the unrelated web change were preserved.
