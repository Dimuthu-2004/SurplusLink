# Member 1 — Flutter Material Inventory Screens

## Scope

This mobile feature consumes the existing authenticated Material Listing API only. It does not add authentication, JWT storage, routes outside the shared router, or backend endpoints.

## Screens and routes

| Screen | Route | Access |
|---|---|---|
| My Materials | `/materials` | SELLER |
| Add Material | `/materials/new` | SELLER |
| Edit Material | `/materials/{id}/edit` | owning SELLER; the API remains the authority |
| Material Details | `/materials/{id}` | authenticated API access rules apply |

`GET /api/materials` drives search/filter/sort/pagination. `GET /api/materials/{id}/history` drives the history panel. Existing POST, PUT, PATCH publish, and DELETE endpoints drive seller actions.

## Photo and category constraints

- The existing API accepts a hosted `photoUrl`, but has no upload endpoint. The image picker therefore previews locally selected images only; add a hosted image URL to persist a photo in the current API contract.
- Existing `GET /api/material-categories` is manager-only. The seller form accepts the actual required `CategoryId` rather than making an unauthorized category-list call. A manager must create/supply the category ID.

## Manual test steps

1. Install the Flutter SDK, run `flutter pub get` in `mobile`, and start the existing ASP.NET API.
2. Run with `flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5170` for Android emulator, or a reachable API URL for another target.
3. Register or sign in as SELLER; on Seller home, select **My Materials**.
4. Verify loading, empty, retry/error, search, status/category filters, minimum/maximum price filters, price/date sorts, and **Load more** with more than one page of listings.
5. Select **Add material**. Check required fields, invalid category ID, zero quantity/price, past available date, photo URL validation, optional GPS permission/coordinates, and local image preview.
6. Submit a valid DRAFT listing using a manager-created category ID and a hosted image URL. Confirm details show the returned status and photos.
7. Edit the DRAFT listing, save, and confirm updated values plus `LISTING_UPDATED` in history. Publish it and confirm `PENDING_VERIFICATION` plus `LISTING_SUBMITTED_FOR_VERIFICATION`.
8. Confirm an ACTIVE/REJECTED listing cannot be edited unless the API permits it; confirm delete error handling if reservations/matches prevent deletion.
9. Sign in as BUYER and confirm seller management routes are denied and API visibility remains restricted to ACTIVE listings.

## Widget/API tests

- `test/material_inventory_repository_test.dart` checks authenticated search query parameters, create payload fields (including no client `ReservedQuantity`/status), and history parsing.
- `test/my_materials_screen_test.dart` checks the seller empty state and add action.
- Executed `dart analyze`: no issues found.
- Executed `flutter test`: 15 passed, 0 failed, including the Material Inventory API-contract and My Materials widget tests.
- `flutter pub get` resolved the image-picker and geolocator plugins and updated generated desktop registrants plus `pubspec.lock`.