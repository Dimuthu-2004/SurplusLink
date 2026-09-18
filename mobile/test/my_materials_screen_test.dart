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
      expect(find.text('Manager home'), findsNothing);
      final router = GoRouter.of(
        tester.element(find.byKey(const Key('logout-button'))),
      );
      for (final entry in [
        ('/materials', AppRole.seller),
        ('/materials/new', AppRole.seller),
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
}

final class _FakeMaterialsGateway implements MaterialInventoryGateway {
  MaterialListingQuery? lastQuery;
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
  Future<MaterialListing> getById(String listingId) =>
      throw UnimplementedError();

  @override
  Future<List<MaterialListingHistoryEntry>> history(String listingId) =>
      throw UnimplementedError();

  @override
  Future<MaterialListing> publish(String listingId) =>
      throw UnimplementedError();

  @override
  Future<MaterialListingPage> search(MaterialListingQuery query) async {
    lastQuery = query;
    return MaterialListingPage(
      items: const [],
      totalCount: 0,
      totalPages: 0,
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
