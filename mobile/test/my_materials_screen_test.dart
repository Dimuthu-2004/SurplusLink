import 'package:mobile/categories/material_category.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/materials/material_inventory_gateway.dart';
import 'package:mobile/materials/material_models.dart';
import 'package:mobile/screens/my_materials_screen.dart';

import 'support/fakes.dart';

void main() {
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
  Future<MaterialListingPage> search(MaterialListingQuery query) async =>
      MaterialListingPage(
        items: const [],
        totalCount: 0,
        totalPages: 0,
        page: 1,
        pageSize: query.pageSize,
      );

  @override
  Future<MaterialListing> update(
    String listingId,
    MaterialListingDraft draft,
  ) => throw UnimplementedError();
}
