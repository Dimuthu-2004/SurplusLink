import 'package:mobile/categories/material_category.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/materials/material_inventory_gateway.dart';
import 'package:mobile/materials/material_models.dart';
import 'package:mobile/screens/my_materials_screen.dart';

import 'support/fakes.dart';
import 'support/requirement_fakes.dart';

import 'package:mobile/app.dart';
import 'package:mobile/auth/auth_controller.dart';
import 'package:mobile/auth/auth_models.dart';
import 'package:go_router/go_router.dart';

void main() {
  for (final roles in [
    [AppRole.seller],
    [AppRole.buyer],
    [AppRole.seller, AppRole.buyer],
    [AppRole.buyer, AppRole.seller],
    [AppRole.manager],
  ]) {
    testWidgets('dashboard and protected navigation for $roles', (
      tester,
    ) async {
      final auth = AuthController(
        FakeAuthGateway()
          ..restoredUser = AppUser(
            id: sellerUser.id,
            email: 'marketplace@test.local',
            roles: roles,
          ),
      );
      final materials = _FakeMaterialsGateway();
      await tester.pumpWidget(
        SurplusLinkApp(
          authController: auth,
          materialGateway: materials,
          requirementGateway: FakeRequirements(),
        ),
      );
      await tester.pumpAndSettle();
      expect(
        find.text('My Materials'),
        roles.contains(AppRole.seller) ? findsOneWidget : findsNothing,
      );
      expect(
        find.text('Add Material'),
        roles.contains(AppRole.seller) ? findsOneWidget : findsNothing,
      );
      expect(
        find.text('My Requirements'),
        roles.contains(AppRole.buyer) ? findsOneWidget : findsNothing,
      );
      expect(
        find.text('Create Requirement'),
        roles.contains(AppRole.buyer) ? findsOneWidget : findsNothing,
      );
      expect(
        find.text('Manager home'),
        roles.contains(AppRole.manager) ? findsOneWidget : findsNothing,
      );
      if (roles.contains(AppRole.seller) && roles.contains(AppRole.buyer)) {
        expect(find.text('Marketplace Home'), findsOneWidget);
        expect(find.text('SELL'), findsOneWidget);
        expect(find.text('BUY'), findsOneWidget);
      }
      final router = GoRouter.of(
        tester.element(find.byKey(const Key('logout-button'))),
      );
      for (final entry in [
        ('/materials', AppRole.seller),
        ('/materials/new', AppRole.seller),
        ('/materials/material-1', AppRole.seller),
        ('/materials/material-1/edit', AppRole.seller),
        ('/requirements', AppRole.buyer),
        ('/requirements/new', AppRole.buyer),
      ]) {
        router.go(entry.$1);
        await tester.pumpAndSettle();
        expect(
          router.routeInformationProvider.value.uri.path,
          roles.contains(entry.$2) ? entry.$1 : '/home',
        );
        router.go('/home');
        await tester.pumpAndSettle();
      }
      if (roles.contains(AppRole.seller)) {
        expect(materials.lastQuery!.mineOnly, isTrue);
      }
      await tester.tap(find.byKey(const Key('logout-button')));
      await tester.pumpAndSettle();
      router.go('/requirements/new');
      await tester.pumpAndSettle();
      expect(router.routeInformationProvider.value.uri.path, '/login');
    });
  }

  testWidgets('My Materials shows an empty state and add action', (
    tester,
  ) async {
    await tester.pumpWidget(
      MaterialApp(
        home: MyMaterialsScreen(
          gateway: _FakeMaterialsGateway(),
          user: sellerUser,
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('No materials match these filters.'), findsOneWidget);
    expect(find.byKey(const Key('add-material-button')), findsOneWidget);
  });

  testWidgets('dual-role seller returns from details to refreshed inventory', (
    tester,
  ) async {
    final gateway = FakeAuthGateway()
      ..restoredUser = AppUser(
        id: sellerUser.id,
        email: sellerUser.email,
        roles: const [AppRole.buyer, AppRole.seller],
      );
    final auth = AuthController(gateway);
    final materials = _FakeMaterialsGateway()..includeListing = true;
    await tester.pumpWidget(
      SurplusLinkApp(
        authController: auth,
        materialGateway: materials,
        requirementGateway: FakeRequirements(),
      ),
    );
    await tester.pumpAndSettle();
    await tester.ensureVisible(find.byKey(const Key('open-my-materials')));
    await tester.tap(find.byKey(const Key('open-my-materials')));
    await tester.pumpAndSettle();
    await tester.ensureVisible(
      find.byKey(const Key('material-card-material-1')),
    );
    await tester.tap(find.byKey(const Key('material-card-material-1')));
    await tester.pumpAndSettle();
    expect(find.byKey(const Key('edit-material')), findsOneWidget);
    expect(find.byKey(const Key('publish-material')), findsOneWidget);
    final searches = materials.searchCount;
    await tester.tap(find.byType(BackButton));
    await tester.pumpAndSettle();
    expect(materials.searchCount, searches + 1);
    expect(materials.lastQuery!.mineOnly, isTrue);
    await tester.tap(find.byType(BackButton));
    await tester.pumpAndSettle();
    expect(find.text('Marketplace Home'), findsOneWidget);
    await tester.ensureVisible(find.byKey(const Key('open-my-requirements')));
    await tester.tap(find.byKey(const Key('open-my-requirements')));
    await tester.pumpAndSettle();
    expect(find.text('My Requirements'), findsOneWidget);
    expect(auth.user, same(gateway.restoredUser));
    expect(gateway.logoutCalled, isFalse);
  });
}

final class _FakeMaterialsGateway implements MaterialInventoryGateway {
  MaterialListingQuery? lastQuery;
  bool includeListing = false;
  int searchCount = 0;
  @override
  Future<String> uploadPhoto(List<int> bytes) => throw UnimplementedError();
  @override
  String photoUrl(String path) => path;

  @override
  Future<List<MaterialCategory>> categories() async => [
    const MaterialCategory('c1', 'Cement'),
  ];

  @override
  Future<MaterialListing> create(MaterialListingDraft draft) =>
      throw UnimplementedError();

  @override
  Future<void> delete(String listingId) => throw UnimplementedError();

  @override
  Future<MaterialListing> getById(String listingId) => Future.value(
    MaterialListing(
      id: listingId,
      sellerId: sellerUser.id,
      categoryId: 'c1',
      categoryName: 'Cement',
      title: 'Surplus cement',
      description: 'Sealed bags',
      quantity: 10,
      reservedQuantity: 0,
      unit: 'bags',
      condition: 'GOOD',
      unitPrice: 100,
      latitude: null,
      longitude: null,
      availableUntil: DateTime(2030),
      status: 'DRAFT',
      createdAtUtc: DateTime(2026),
      updatedAtUtc: DateTime(2026),
      photos: const [],
    ),
  );

  @override
  Future<List<MaterialListingHistoryEntry>> history(String listingId) =>
      Future.value(const []);

  @override
  Future<MaterialListing> publish(String listingId) =>
      throw UnimplementedError();

  @override
  Future<MaterialListingPage> search(MaterialListingQuery query) async {
    searchCount++;
    lastQuery = query;
    return MaterialListingPage(
      items: includeListing ? [await getById('material-1')] : const [],
      totalCount: includeListing ? 1 : 0,
      totalPages: includeListing ? 1 : 0,
      page: 1,
      pageSize: query.pageSize,
    );
  }

  @override
  Future<MaterialListing> update(
    String listingId,
    MaterialListingDraft draft,
  ) => throw UnimplementedError();
}
