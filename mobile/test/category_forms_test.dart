import 'dart:async';
import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mobile/categories/category_dropdown.dart';
import 'package:mobile/categories/category_filter.dart';
import 'package:mobile/categories/material_category.dart';
import 'package:mobile/core/api_client.dart';
import 'package:mobile/core/api_exception.dart';
import 'package:mobile/materials/material_inventory_gateway.dart';
import 'package:mobile/materials/material_inventory_repository.dart';
import 'package:mobile/materials/material_models.dart';
import 'package:mobile/requirements/requirement_repository.dart';
import 'package:mobile/screens/add_material_screen.dart';
import 'package:mobile/screens/edit_material_screen.dart';
import 'package:mobile/screens/requirement_form_screen.dart';

import 'support/fakes.dart';
import 'support/requirement_fakes.dart';

const categoryOptions = [
  MaterialCategory('c1', 'Cement'),
  MaterialCategory('c2', 'Steel'),
];

void main() {
  test('seller and buyer repositories read the real category route with shared bearer auth', () async {
    for (final buyer in [false, true]) {
      final api = ApiClient(
        baseUri: Uri.parse('https://api.surpluslink.test'),
        tokenStorage: MemoryTokenStorage()
          ..token = buyer ? 'buyer-token' : 'seller-token',
        httpClient: MockClient((request) async {
          expect(request.method, 'GET');
          expect(request.url.path, '/api/material-categories');
          expect(
            request.headers['authorization'],
            buyer ? 'Bearer buyer-token' : 'Bearer seller-token',
          );
          return http.Response(
            jsonEncode([
              {
                'id': 'arbitrary-server-id',
                'name': 'Returned category',
                'createdAtUtc': '2026-01-01T00:00:00Z',
                'updatedAtUtc': '2026-01-02T00:00:00Z',
              },
            ]),
            200,
          );
        }),
      );
      final result = buyer
          ? await RequirementRepository(api).categories()
          : await MaterialInventoryRepository(api).categories();
      expect(result.single.id, 'arbitrary-server-id');
      expect(result.single.name, 'Returned category');
    }
  });

  for (final buyer in [false, true]) {
    final label = buyer ? 'Requirement' : 'Material';
    testWidgets('$label category loading then names and required validation', (
      tester,
    ) async {
      final pending = Completer<List<MaterialCategory>>();
      final material = FakeCategoryMaterials()..pending = pending;
      final requirement = FakeRequirements()..pendingCategories = pending;
      await pumpForm(tester, buyer, material, requirement, settle: false);
      expect(find.byType(CircularProgressIndicator), findsOneWidget);
      expect(find.byType(CategoryDropdown), findsNothing);
      pending.complete(categoryOptions);
      await tester.pumpAndSettle();
      await tester.tap(
        find.byKey(Key(buyer ? 'requirement-save' : 'save-material')),
      );
      await tester.pumpAndSettle();
      expect(find.text('Choose a category.'), findsOneWidget);
      expect(material.saved, isNull);
      expect(requirement.saved, isNull);
      await tester.tap(find.byType(CategoryDropdown));
      await tester.pumpAndSettle();
      expect(find.text('Cement').last, findsOneWidget);
      expect(find.text('Steel').last, findsOneWidget);
      expect(find.text('c1'), findsNothing);
      expect(find.text('Category ID'), findsNothing);
      await tester.tap(find.text('Steel').last);
      await tester.pumpAndSettle();
    });

    testWidgets('$label selecting a name submits its ID', (tester) async {
      final material = FakeCategoryMaterials();
      final requirement = FakeRequirements()
        ..categoryItems = categoryOptions
        ..saveError = const ApiException('Test save intercepted.');
      await pumpForm(tester, buyer, material, requirement);
      await tester.tap(find.byType(CategoryDropdown));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Steel').last);
      await tester.pumpAndSettle();
      final fields = buyer
          ? {
              'requirement-quantity': '10',
              'requirement-unit': 'kg',
              'requirement-budget': '100',
              'requirement-latitude': '6',
              'requirement-longitude': '79',
            }
          : {
              'material-title': 'Surplus steel',
              'material-description': 'Reusable beams',
              'material-quantity': '10',
              'material-unit-price': '100',
            };
      for (final field in fields.entries) {
        await tester.enterText(find.byKey(Key(field.key)), field.value);
      }
      await tester.tap(
        find.byKey(Key(buyer ? 'requirement-save' : 'save-material')),
      );
      await tester.pumpAndSettle();
      expect(
        buyer
            ? requirement.saved!.toJson()['categoryId']
            : material.saved!.toJson()['categoryId'],
        'c2',
      );
    });

    testWidgets(
      '$label edit preselects the current category and preserves its ID on save',
      (tester) async {
        final material = FakeCategoryMaterials();
        final requirement = FakeRequirements()
          ..categoryItems = categoryOptions
          ..saveError = const ApiException('Test save intercepted.');
        await pumpForm(tester, buyer, material, requirement, editing: true);
        final dropdown = tester.widget<DropdownButtonFormField<String>>(
          find.byType(DropdownButtonFormField<String>).first,
        );
        expect(dropdown.initialValue, 'c1');
        expect(find.text('Cement'), findsOneWidget);
        await tester.tap(
          find.byKey(Key(buyer ? 'requirement-save' : 'save-material')),
        );
        await tester.pumpAndSettle();
        expect(
          buyer ? requirement.saved!.categoryId : material.saved!.categoryId,
          'c1',
        );
        if (!buyer) expect(material.updatedId, 'listing-1');
      },
    );

    testWidgets('$label unavailable edit category requires a new selection', (
      tester,
    ) async {
      final material = FakeCategoryMaterials()..items = [categoryOptions.last];
      final requirement = FakeRequirements()
        ..categoryItems = [categoryOptions.last];
      await pumpForm(tester, buyer, material, requirement, editing: true);
      await tester.tap(
        find.byKey(Key(buyer ? 'requirement-save' : 'save-material')),
      );
      await tester.pumpAndSettle();
      expect(find.text('Choose a category.'), findsOneWidget);
      expect(material.saved, isNull);
      expect(requirement.saved, isNull);
      expect(tester.takeException(), isNull);
    });

    testWidgets('$label category failure and empty state support retry', (
      tester,
    ) async {
      final material = FakeCategoryMaterials()
        ..error = const ApiException('Cannot load categories.');
      final requirement = FakeRequirements()
        ..error = const ApiException('Cannot load categories.');
      await pumpForm(tester, buyer, material, requirement);
      expect(find.text('Cannot load categories.'), findsOneWidget);
      expect(find.byType(CategoryDropdown), findsNothing);
      expect(
        find.byKey(Key(buyer ? 'requirement-save' : 'save-material')),
        findsNothing,
      );
      material
        ..error = null
        ..items = [];
      requirement
        ..error = null
        ..categoryItems = [];
      await tester.tap(find.text('Retry'));
      await tester.pumpAndSettle();
      expect(
        find.textContaining('No categories are available'),
        findsOneWidget,
      );
      material.items = categoryOptions;
      requirement.categoryItems = categoryOptions;
      await tester.tap(find.text('Retry'));
      await tester.pumpAndSettle();
      expect(find.byType(CategoryDropdown), findsOneWidget);
    });
  }

  testWidgets(
    'material category filter selects an ID and clears to all categories',
    (tester) async {
      String? selected;
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: CategoryFilter(
              loadCategories: () async => categoryOptions,
              onChanged: (value) => selected = value,
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();
      await tester.tap(find.byType(CategoryDropdown));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Steel').last);
      await tester.pumpAndSettle();
      expect(selected, 'c2');
      await tester.tap(find.byType(CategoryDropdown));
      await tester.pumpAndSettle();
      await tester.tap(find.text('All categories').last);
      await tester.pumpAndSettle();
      expect(selected, isNull);
    },
  );
}

Future<void> pumpForm(
  WidgetTester tester,
  bool buyer,
  FakeCategoryMaterials material,
  FakeRequirements requirement, {
  bool editing = false,
  bool settle = true,
}) async {
  tester.view.physicalSize = const Size(1100, 2400);
  tester.view.devicePixelRatio = 1;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);
  await tester.pumpWidget(
    MaterialApp(
      home: buyer
          ? RequirementFormScreen(
              gateway: requirement,
              requirementId: editing ? 'r1' : null,
            )
          : editing
          ? EditMaterialScreen(gateway: material, listingId: 'listing-1')
          : AddMaterialScreen(gateway: material),
    ),
  );
  if (settle) await tester.pumpAndSettle();
}

class FakeCategoryMaterials implements MaterialInventoryGateway {
  List<MaterialCategory> items = categoryOptions;
  Completer<List<MaterialCategory>>? pending;
  Object? error;
  MaterialListingDraft? saved;
  String? updatedId;

  @override
  Future<List<MaterialCategory>> categories() async {
    if (pending != null) return pending!.future;
    if (error != null) throw error!;
    return items;
  }

  @override
  Future<MaterialListing> getById(String id) async => MaterialListing(
    id: id,
    sellerId: 'seller-1',
    categoryId: 'c1',
    categoryName: 'Cement',
    title: 'Surplus cement',
    description: 'Unused bags',
    quantity: 10,
    reservedQuantity: 0,
    unit: 'kg',
    condition: 'GOOD',
    unitPrice: 25,
    latitude: null,
    longitude: null,
    availableUntil: DateTime.now().add(const Duration(days: 7)),
    status: 'DRAFT',
    createdAtUtc: DateTime.utc(2026),
    updatedAtUtc: DateTime.utc(2026),
    photos: [],
  );

  @override
  Future<MaterialListing> create(MaterialListingDraft draft) async {
    saved = draft;
    throw const ApiException('Test save intercepted.');
  }

  @override
  Future<MaterialListing> update(String id, MaterialListingDraft draft) {
    updatedId = id;
    return create(draft);
  }

  @override
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);
}
