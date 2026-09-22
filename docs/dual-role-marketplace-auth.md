# Dual-role marketplace authentication

One account can hold SELLER, BUYER, or both. MANAGER remains seeded/admin-created and is never accepted through public registration. Matching, logistics and the canonical approval workflow now enforce this identity model.

## API and client contract

`POST /api/auth/register` retains the existing account/profile fields and replaces `role` with `roles: ["SELLER"]`, `["BUYER"]`, or `["SELLER", "BUYER"]`. Missing, empty, null, duplicate, unknown, numeric-string, and manager-containing selections return 400. Login, registration, GET `/api/auth/me`, and PUT `/api/auth/me` return `user.roles` (or `roles` on the direct user response). Each assignment creates one `ClaimTypes.Role`; no combined claim is used.

Flutter asks for account details first, then Sell surplus materials / Buy / request materials / Both. Dual-role users get one Marketplace Home with Add Material, My Materials, Create Requirement, and My Requirements. Existing listing history and requirement workflow/status screens remain accessible through their feature groups. My Materials sends `mineOnly=true`, filtering before pagination. React retains its manager focus; normal dual-role accounts cannot enter manager routes.

Deploy API and clients together for this contract change. Existing single-role accounts are not automatically granted another capability. Role-management/account-upgrade UI is outside this task. Existing JWTs retain their original claims until expiry; sign in again after an administrator changes assignments.

## Storage, migration, and development users

`Users` has many `UserRoleAssignments`, keyed by `(UserId, Role)` with a Users FK and valid-role constraint. Exactly one migration was created: `20260918145314_AddUserRoleAssignments`. Its generated SQL was reviewed before application: create relation, copy every old role, then drop `Users.Role`, all in one transaction. No ownership FK or user ID is changed. Downgrade refuses any account with zero or multiple roles rather than discarding information.

The local migration was applied with the existing connection from API user secrets, without changing its value or tracked appsettings files. Before/after hashes confirmed preservation of all **5 existing users and their roles**. Local listing/request counts were both zero; PostgreSQL integration fixtures independently verify preservation with populated listings, requests, matches, and reservations. At that milestone all seven migrations were applied. The resumed audit applied the subsequent duration migration and verified all eleven current migrations and no pending model changes.

Development startup seeds seller@test.local, buyer@test.local, dual@test.local, and manager@test.local using the existing development-only credential pattern in `DevelopmentSeed.cs`. Existing accounts are skipped, preserving their passwords, identities, and assigned capabilities. No secrets or connection values are recorded here.

## Matching contract

The planner derives `normalizedCriteria.buyerUserId` from stored `buyerRequest.buyerId`. Matching requires that nonempty UUID and trusted `MaterialListingRecord.seller_id` values. It checks both search records and refreshed details and reports `SELF_MATCH_NOT_ALLOWED` exclusions. Other eligible sellers still match. No identity is included in normal candidate output or sent to a model provider. The existing reservation service repeats the rule with a structured error code. M3 generation and workflow execution enforce `Listing.SellerId != BuyerRequest.BuyerId` using the same deterministic policy. See the current pre-S12 audit for integrated verification.

ADR 0002 is amended; the identity amendment remains in ADR 0002; ADR 0003 now records the implemented workflow and reservation boundary. Earlier milestone evidence documents historical single-role behavior and is superseded by this contract.

## Verification (2026-09-18)

| Check | Result |
| --- | --- |
| Sell / Buy / Both registration | PASS |
| Manager, mixed-manager, duplicate, missing and unknown registration roles rejected | PASS |
| Separate JWT role claims and current-user roles collection | PASS |
| Seller-only, buyer-only, and dual-role navigation | PASS |
| M1/M2 ownership and manager isolation | PASS |
| Self-match blocked and other sellers allowed | PASS |
| Migration applied; existing users preserved | PASS |
| Duplicate assignment rejected; lossy downgrade refused | PASS |
| `dotnet build` (repository root) | PASS; no warnings/errors |
| `dotnet test` with `SURPLUSLINK_TEST_CONNECTION` | PASS; 81 tests, none skipped |
| `dotnet ef migrations list --project backend/SurplusLink.Api` | PASS; seven applied |
| `dotnet ef database update --project backend/SurplusLink.Api` | PASS |
| `dotnet ef migrations has-pending-model-changes --project backend/SurplusLink.Api` | PASS; none |
| `flutter pub get` (mobile) | PASS |
| `flutter analyze` (mobile) | PASS; no issues |
| `flutter test` (mobile) | PASS; 54 tests |
| `npm run build` / `npm test` (web) | PASS; 25 tests |
| `.venv/Scripts/python.exe -m unittest discover -s tests -v` (ai-service) | PASS; 15 tests |

Backend tests use real PostgreSQL and disposable databases via the existing fixture. Flutter tests are host widget/repository tests; no device or emulator smoke test is claimed. The final route adjustment was rechecked with the auth and dashboard navigation suites.

## Changed files

Each path is relative to the repository root.

| File | Change |
| --- | --- |
| [ai-service/app/agents/matching_schemas.py](../ai-service/app/agents/matching_schemas.py) | Requires trusted buyer identity and defines structured self-match exclusions. |
| [ai-service/app/agents/material_matching.py](../ai-service/app/agents/material_matching.py) | Rejects self-owned search results and refreshed details before ranking. |
| [ai-service/app/agents/planner_schemas.py](../ai-service/app/agents/planner_schemas.py) | Adds buyer identity to normalized criteria and the fixed matching inputs. |
| [ai-service/app/agents/requirement_planner.py](../ai-service/app/agents/requirement_planner.py) | Derives matching identity from the stored request owner. |
| [ai-service/app/materials/read_boundary.py](../ai-service/app/materials/read_boundary.py) | Carries buyer and seller UUIDs through the trusted read contract. |
| [ai-service/contracts/requirement_planner.schema.json](../ai-service/contracts/requirement_planner.schema.json) | Updates the exported response schema for the buyer identity field. |
| [ai-service/tests/test_material_matching_agent.py](../ai-service/tests/test_material_matching_agent.py) | Tests self-match exclusion, valid alternatives, missing identity, and refreshed ownership. |
| [ai-service/tests/test_requirement_planner_agent.py](../ai-service/tests/test_requirement_planner_agent.py) | Checks that normalized identity comes from the stored buyer. |
| [backend/SurplusLink.Api/Auth/AuthDtos.cs](../backend/SurplusLink.Api/Auth/AuthDtos.cs) | Validates public role collections and returns roles arrays. |
| [backend/SurplusLink.Api/Auth/AuthService.cs](../backend/SurplusLink.Api/Auth/AuthService.cs) | Persists assignments, loads them at login, and emits separate role claims. |
| [backend/SurplusLink.Api/Controllers/AuthController.cs](../backend/SurplusLink.Api/Controllers/AuthController.cs) | Loads assignments for current-user and profile responses. |
| [backend/SurplusLink.Api/Data/DevelopmentSeed.cs](../backend/SurplusLink.Api/Data/DevelopmentSeed.cs) | Seeds normalized roles and adds a development-only dual-role account. |
| [backend/SurplusLink.Api/Data/MarketplaceModelConfiguration.cs](../backend/SurplusLink.Api/Data/MarketplaceModelConfiguration.cs) | Maps assignments with a composite key, foreign key, and valid-role constraint. |
| [backend/SurplusLink.Api/Data/Migrations/20260918145314_AddUserRoleAssignments.Designer.cs](../backend/SurplusLink.Api/Data/Migrations/20260918145314_AddUserRoleAssignments.Designer.cs) | Records the generated EF target model for the single new migration. |
| [backend/SurplusLink.Api/Data/Migrations/20260918145314_AddUserRoleAssignments.cs](../backend/SurplusLink.Api/Data/Migrations/20260918145314_AddUserRoleAssignments.cs) | Copies old roles transactionally and refuses a lossy downgrade. |
| [backend/SurplusLink.Api/Data/Migrations/SurplusLinkDbContextModelSnapshot.cs](../backend/SurplusLink.Api/Data/Migrations/SurplusLinkDbContextModelSnapshot.cs) | Records normalized role storage in the EF model snapshot. |
| [backend/SurplusLink.Api/Materials/MaterialDtos.cs](../backend/SurplusLink.Api/Materials/MaterialDtos.cs) | Adds the optional owner-only listing search filter. |
| [backend/SurplusLink.Api/Materials/MaterialInventoryService.cs](../backend/SurplusLink.Api/Materials/MaterialInventoryService.cs) | Uses membership for read permissions and filters own inventory before pagination. |
| [backend/SurplusLink.Api/Materials/MaterialsControllerBase.cs](../backend/SurplusLink.Api/Materials/MaterialsControllerBase.cs) | Builds material actors from all assigned role claims. |
| [backend/SurplusLink.Api/Models/MarketplaceMatchPolicy.cs](../backend/SurplusLink.Api/Models/MarketplaceMatchPolicy.cs) | Defines the reusable deterministic self-match rejection rule. |
| [backend/SurplusLink.Api/Models/User.cs](../backend/SurplusLink.Api/Models/User.cs) | Replaces the persisted single role with a collection of role assignments. |
| [backend/SurplusLink.Api/Reservations/ReservationExceptions.cs](../backend/SurplusLink.Api/Reservations/ReservationExceptions.cs) | Adds a structured reservation rejection code. |
| [backend/SurplusLink.Api/Reservations/ReservationService.cs](../backend/SurplusLink.Api/Reservations/ReservationService.cs) | Rejects reservations between a requirement and its owner's listing. |
| [backend/SurplusLink.Tests/AuthValidationTests.cs](../backend/SurplusLink.Tests/AuthValidationTests.cs) | Adapts public-manager validation to the roles collection. |
| [backend/SurplusLink.Tests/BuyerRequirementsIntegrationTests.cs](../backend/SurplusLink.Tests/BuyerRequirementsIntegrationTests.cs) | Allows test JWTs to contain multiple separate role claims. |
| [backend/SurplusLink.Tests/DualRoleAuthTests.cs](../backend/SurplusLink.Tests/DualRoleAuthTests.cs) | Covers registration, login, claims, current user, ownership, manager isolation, and self-trade. |
| [backend/SurplusLink.Tests/ProfileAndPhotoTests.cs](../backend/SurplusLink.Tests/ProfileAndPhotoTests.cs) | Uses the new public registration contract in profile validation. |
| [backend/SurplusLink.Tests/ProfilePersistenceTests.cs](../backend/SurplusLink.Tests/ProfilePersistenceTests.cs) | Verifies profile persistence with normalized role assignments. |
| [backend/SurplusLink.Tests/UserRoleMigrationTests.cs](../backend/SurplusLink.Tests/UserRoleMigrationTests.cs) | Tests migration round trips, duplicate rejection, and safe rollback refusal. |
| [docs/adr/0002-react-auth-state.md](../docs/adr/0002-react-auth-state.md) | Extends the existing ADR with identity, security, migration, UX, and matching decisions. |
| [docs/dual-role-marketplace-auth.md](../docs/dual-role-marketplace-auth.md) | Records the new contracts, migration evidence, verification, and complete change inventory. |
| [docs/requirement-planner-agent.md](../docs/requirement-planner-agent.md) | Documents trusted identity propagation and deterministic self-match exclusions. |
| [mobile/lib/auth/auth_controller.dart](../mobile/lib/auth/auth_controller.dart) | Passes selected marketplace role collections through existing auth state. |
| [mobile/lib/auth/auth_gateway.dart](../mobile/lib/auth/auth_gateway.dart) | Changes registration to accept a role collection. |
| [mobile/lib/auth/auth_models.dart](../mobile/lib/auth/auth_models.dart) | Parses role arrays and exposes role membership checks. |
| [mobile/lib/auth/auth_repository.dart](../mobile/lib/auth/auth_repository.dart) | Validates and sends public role collections through the existing auth API. |
| [mobile/lib/materials/material_models.dart](../mobile/lib/materials/material_models.dart) | Serializes the optional own-inventory filter. |
| [mobile/lib/routing/app_router.dart](../mobile/lib/routing/app_router.dart) | Checks marketplace capabilities for protected navigation. |
| [mobile/lib/screens/home_screen.dart](../mobile/lib/screens/home_screen.dart) | Shows seller, buyer, or combined marketplace actions in one dashboard. |
| [mobile/lib/screens/material_details_screen.dart](../mobile/lib/screens/material_details_screen.dart) | Uses role membership plus ownership for listing actions and history. |
| [mobile/lib/screens/my_materials_screen.dart](../mobile/lib/screens/my_materials_screen.dart) | Supports dual-role sellers while requesting only their own listings. |
| [mobile/lib/screens/register_screen.dart](../mobile/lib/screens/register_screen.dart) | Adds account-details onboarding followed by Sell, Buy, or Both selection. |
| [mobile/test/auth_navigation_test.dart](../mobile/test/auth_navigation_test.dart) | Tests all onboarding choices, validation, login, and logout. |
| [mobile/test/auth_repository_test.dart](../mobile/test/auth_repository_test.dart) | Tests role-array payloads and rejects invalid public selections. |
| [mobile/test/buyer_requirements_widget_test.dart](../mobile/test/buyer_requirements_widget_test.dart) | Updates the manager fixture to the roles collection. |
| [mobile/test/my_materials_screen_test.dart](../mobile/test/my_materials_screen_test.dart) | Tests all marketplace dashboards, protected routes, own inventory, and logout. |
| [mobile/test/profile_media_location_test.dart](../mobile/test/profile_media_location_test.dart) | Updates profile auth fixtures to the roles collection. |
| [mobile/test/support/fakes.dart](../mobile/test/support/fakes.dart) | Provides collection-aware auth fixtures and registration behavior. |
| [web/src/auth/authTypes.ts](../web/src/auth/authTypes.ts) | Parses and validates role collections in shared auth responses. |
| [web/src/components/AppLayout.tsx](../web/src/components/AppLayout.tsx) | Displays assigned roles and shows manager navigation only to managers. |
| [web/src/routing/RouteGuards.tsx](../web/src/routing/RouteGuards.tsx) | Authorizes routes by role membership. |
| [web/src/routing/roleRoutes.ts](../web/src/routing/roleRoutes.ts) | Chooses a deterministic home from the assigned roles. |
| [web/src/test/authNavigation.test.tsx](../web/src/test/authNavigation.test.tsx) | Tests dual-role marketplace access and manager-route exclusion. |
| [web/src/test/managerRequirements.test.tsx](../web/src/test/managerRequirements.test.tsx) | Updates manager-guard fixtures to role-array responses. |
