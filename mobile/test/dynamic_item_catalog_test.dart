import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/categories/material_category.dart';
import 'package:mobile/materials/construction_item_template_models.dart';
import 'package:mobile/materials/material_inventory_gateway.dart';
import 'package:mobile/materials/material_models.dart';
import 'package:mobile/screens/material_listing_form_screen.dart';

final mockTemplates = [
  const ConstructionItemTemplate(
    id: 'paint-id',
    name: 'Paint',
    categoryId: 'c-finishes',
    categoryName: 'Finishes & Paints',
    itemClass: 'MATERIAL',
    quantityMode: 'PACKAGE',
    baseUnit: 'L',
    packageType: 'can',
    allowedUnits: ['L'],
    allowedPackageSizes: [1, 4, 10, 20],
    attributeSchema: '[{"id":"colour","label":"Colour","type":"string","required":true},{"id":"finish","label":"Finish","type":"select","options":["Gloss","Matte","Satin"]}]',
  ),
  const ConstructionItemTemplate(
    id: 'generator-id',
    name: 'Generator',
    categoryId: 'c-power',
    categoryName: 'Power & Heavy Machinery',
    itemClass: 'EQUIPMENT',
    quantityMode: 'PIECE',
    baseUnit: 'unit',
    allowedUnits: ['unit'],
    attributeSchema: '[{"id":"capacity_kva","label":"Capacity","type":"number","required":true,"unit":"kVA"},{"id":"fuel_type","label":"Fuel Type","type":"select","options":["Diesel","Petrol"]}]',
  ),
  const ConstructionItemTemplate(
    id: 'tiles-id',
    name: 'Floor Tiles',
    categoryId: 'c-finishes',
    categoryName: 'Finishes & Paints',
    itemClass: 'MATERIAL',
    quantityMode: 'PACKAGE',
    baseUnit: 'sqm',
    packageType: 'box',
    allowedUnits: ['sqm'],
    attributeSchema: '[{"id":"coveragePerBoxSqm","label":"Coverage per box","type":"number","packageSizeSource":"CALCULATED"},{"id":"dimensionsMm","label":"Dimensions","type":"string","required":true},{"id":"finish","label":"Finish","type":"select","options":["Gloss","Matt","Polished"]}]',
  ),
];

final mockCategories = [
  const MaterialCategory('c-finishes', 'Finishes & Paints'),
  const MaterialCategory('c-power', 'Power & Heavy Machinery'),
];

class _MockMaterialsGateway implements MaterialInventoryGateway {
  MaterialListingDraft? savedDraft;

  @override
  Future<List<MaterialCategory>> categories() async => mockCategories;

  @override
  Future<List<ConstructionItemTemplate>> itemTemplates({
    String? search,
    String? categoryId,
    String? itemClass,
  }) async => mockTemplates;

  @override
  Future<List<String>> categoryUnits(String categoryId) async => ['L', 'unit', 'kg'];

  @override
  Future<MaterialListing> create(MaterialListingDraft draft) async {
    savedDraft = draft;
    return MaterialListing(
      id: 'new-listing-1',
      sellerId: 'seller-1',
      categoryId: draft.categoryId,
      categoryName: 'Test',
      title: draft.title,
      description: draft.description,
      quantity: draft.quantity,
      reservedQuantity: 0,
      unit: draft.unit,
      condition: draft.condition,
      unitPrice: draft.unitPrice,
      latitude: draft.latitude,
      longitude: draft.longitude,
      availableUntil: draft.availableUntil,
      status: 'PENDING_VERIFICATION',
      createdAtUtc: DateTime.now(),
      updatedAtUtc: DateTime.now(),
      photos: const [],
      constructionItemTemplateId: draft.constructionItemTemplateId,
      specificationsJson: draft.specificationsJson,
      isCustomPendingReview: draft.isCustomPendingReview,
    );
  }

  @override
  Future<MaterialListing> getById(String listingId) => throw UnimplementedError();

  @override
  Future<MaterialListing> update(String listingId, MaterialListingDraft draft) async {
    savedDraft = draft;
    return create(draft);
  }

  @override
  Future<void> delete(String listingId) async {}

  @override
  Future<List<MaterialListingHistoryEntry>> history(String listingId) async => [];

  @override
  String photoUrl(String path) => path;

  @override
  Future<MaterialListing> publish(String listingId) => throw UnimplementedError();

  @override
  Future<MaterialListingPage> search(MaterialListingQuery query) => throw UnimplementedError();

  @override
  Future<String> uploadPhoto(List<int> bytes) async => 'https://photos.example.com/photo.jpg';
}

void main() {
  testWidgets('Paint listing calculates package quantity seamlessly without technical jargon', (tester) async {
    tester.view.physicalSize = const Size(1080, 2400);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(() => tester.view.resetPhysicalSize());

    final gateway = _MockMaterialsGateway();

    await tester.pumpWidget(
      MaterialApp(
        home: MaterialListingFormScreen(gateway: gateway),
      ),
    );
    await tester.pumpAndSettle();

    // Verify absence of internal technical quantity jargon
    expect(find.text('Sellable quantity mode'), findsNothing);
    expect(find.text('Base-equivalent quantity (calculated)'), findsNothing);

    // Open template picker
    await tester.tap(find.byKey(const Key('choose-construction-item-button')));
    await tester.pumpAndSettle();

    expect(find.text('What are you listing?'), findsWidgets);
    expect(find.text('Paint'), findsOneWidget);
    expect(find.text('Generator'), findsOneWidget);

    // Select Paint
    await tester.tap(find.byKey(const Key('template-tile-paint-id')));
    await tester.pumpAndSettle();

    // Verify Paint specifications rendered
    expect(find.text('Paint Specifications'), findsOneWidget);
    expect(find.text('Colour *'), findsOneWidget);

    // Fill specifications
    await tester.enterText(find.byKey(const Key('spec-field-colour')), 'Snow White');

    // Fill package size and count: 5 cans of 4 L
    await tester.enterText(find.byKey(const Key('material-package-size')), '4');
    await tester.enterText(find.byKey(const Key('material-package-count')), '5');
    await tester.enterText(find.byKey(const Key('material-unit-price')), '4500');

    // Check summary calculation: 5 * 4 = 20 L
    expect(find.textContaining('20.0 L (5 cans × 4.0 L)'), findsOneWidget);

    // Fill description
    await tester.enterText(find.byKey(const Key('material-description')), 'Top tier weather protection paint.');

    // Save
    await tester.tap(find.byKey(const Key('save-material')));
    await tester.pumpAndSettle();

    expect(gateway.savedDraft, isNotNull);
    final draft = gateway.savedDraft!;
    expect(draft.title, 'Paint');
    expect(draft.quantity, 20.0);
    expect(draft.quantityMode, 'PACKAGE');
    expect(draft.packageSize, 4.0);
    expect(draft.packageCount, 5);
    expect(draft.packageType, 'CAN');
    expect(draft.unitPrice, 4500.0); // physical package price
    expect(draft.constructionItemTemplateId, 'paint-id');
    expect(draft.isCustomPendingReview, isFalse);
    expect(jsonDecode(draft.specificationsJson!)['colour'], 'Snow White');
  });

  testWidgets('Generator listing renders piece mode without package controls', (tester) async {
    tester.view.physicalSize = const Size(1080, 2400);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(() => tester.view.resetPhysicalSize());

    final gateway = _MockMaterialsGateway();

    await tester.pumpWidget(
      MaterialApp(
        home: MaterialListingFormScreen(gateway: gateway),
      ),
    );
    await tester.pumpAndSettle();

    // Open template picker
    await tester.tap(find.byKey(const Key('choose-construction-item-button')));
    await tester.pumpAndSettle();

    // Select Generator
    await tester.tap(find.byKey(const Key('template-tile-generator-id')));
    await tester.pumpAndSettle();

    // Verify generator specifications rendered
    expect(find.text('Generator Specifications'), findsOneWidget);
    expect(find.text('Capacity * (kVA)'), findsOneWidget);

    // Verify package controls are NOT present for PIECE/EQUIPMENT
    expect(find.byKey(const Key('material-package-size')), findsNothing);
    expect(find.byKey(const Key('material-package-count')), findsNothing);

    // Fill capacity
    await tester.enterText(find.byKey(const Key('spec-field-capacity_kva')), '15');

    // Enter unit count and price
    await tester.enterText(find.byKey(const Key('material-quantity')), '1');
    await tester.enterText(find.byKey(const Key('material-unit-price')), '350000');
    await tester.enterText(find.byKey(const Key('material-description')), 'Heavy duty generator.');

    // Save
    await tester.tap(find.byKey(const Key('save-material')));
    await tester.pumpAndSettle();

    expect(gateway.savedDraft, isNotNull);
    final draft = gateway.savedDraft!;
    expect(draft.title, 'Generator');
    expect(draft.quantity, 1.0);
    expect(draft.quantityMode, 'PIECE');
    expect(draft.packageSize, 1.0);
    expect(draft.packageCount, 1);
    expect(draft.packageType, isNull);
    expect(draft.unitPrice, 350000.0);
    expect(draft.constructionItemTemplateId, 'generator-id');
    expect(jsonDecode(draft.specificationsJson!)['capacity_kva'], 15.0);
  });

  testWidgets('Fallback custom item allows listing unlisted items with review flag', (tester) async {
    tester.view.physicalSize = const Size(1080, 2400);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(() => tester.view.resetPhysicalSize());

    final gateway = _MockMaterialsGateway();

    await tester.pumpWidget(
      MaterialApp(
        home: MaterialListingFormScreen(gateway: gateway),
      ),
    );
    await tester.pumpAndSettle();

    // Open template picker and tap custom fallback
    await tester.tap(find.byKey(const Key('choose-construction-item-button')));
    await tester.pumpAndSettle();

    await tester.tap(find.byKey(const Key('custom-item-fallback-button')));
    await tester.pumpAndSettle();

    expect(find.text('Custom Construction Item'), findsOneWidget);
    expect(find.text('Flagged for manager review upon submission.'), findsOneWidget);
    expect(find.byKey(const Key('custom-sale-type-toggle')), findsOneWidget);

    // Fill custom title and description
    await tester.enterText(find.byKey(const Key('material-title')), 'Hydraulic Breaker Attachment');
    await tester.enterText(find.byKey(const Key('material-description')), 'Custom heavy attachment for excavator.');

    // Fill quantity and price as piece
    await tester.enterText(find.byKey(const Key('material-quantity')), '2');
    await tester.enterText(find.byKey(const Key('material-unit-price')), '220000');

    // Save
    await tester.tap(find.byKey(const Key('save-material')));
    await tester.pumpAndSettle();

    expect(gateway.savedDraft, isNotNull);
    final draft = gateway.savedDraft!;
    expect(draft.title, 'Hydraulic Breaker Attachment');
    expect(draft.isCustomPendingReview, isTrue);
    expect(draft.constructionItemTemplateId, isNull);
    expect(draft.quantity, 2.0);
    expect(draft.quantityMode, 'PIECE');
  });

  testWidgets('Tiles listing calculates coverage from dimensions and pieces per box', (tester) async {
    tester.view.physicalSize = const Size(1080, 2400);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(() => tester.view.resetPhysicalSize());

    final gateway = _MockMaterialsGateway();

    await tester.pumpWidget(
      MaterialApp(
        home: MaterialListingFormScreen(gateway: gateway),
      ),
    );
    await tester.pumpAndSettle();

    // Open template picker
    await tester.tap(find.byKey(const Key('choose-construction-item-button')));
    await tester.pumpAndSettle();

    // Select Floor Tiles
    await tester.tap(find.byKey(const Key('template-tile-tiles-id')));
    await tester.pumpAndSettle();

    // Verify Category dropdown is NOT shown for catalog items
    expect(find.byKey(const Key('material-category')), findsNothing);

    // Verify Tile Dimensions & Packaging rendered
    expect(find.text('Tile Dimensions & Packaging'), findsOneWidget);
    expect(find.byKey(const Key('tile-width-mm')), findsOneWidget);
    expect(find.byKey(const Key('tile-height-mm')), findsOneWidget);
    expect(find.byKey(const Key('tile-pieces-per-box')), findsOneWidget);

    // Default 600mm x 600mm x 4 pieces = 1.44 sqm per box
    expect(find.textContaining('1.44 m²'), findsWidgets);

    // Fill 10 boxes at 12000 LKR per box
    await tester.enterText(find.byKey(const Key('material-package-count')), '10');
    await tester.enterText(find.byKey(const Key('material-unit-price')), '12000');
    await tester.enterText(find.byKey(const Key('material-description')), 'High gloss porcelain floor tiles.');

    // Save
    await tester.tap(find.byKey(const Key('save-material')));
    await tester.pumpAndSettle();

    expect(gateway.savedDraft, isNotNull);
    final draft = gateway.savedDraft!;
    expect(draft.title, 'Floor Tiles');
    expect(draft.categoryId, 'c-finishes');
    expect(draft.quantity, 14.4);
    expect(draft.packageCount, 10);
    expect(draft.packageSize, 1.44);
    expect(draft.packageType, 'BOX');
    expect(draft.quantityMode, 'PACKAGE');
    expect(draft.isCustomPendingReview, isFalse);
    final specs = jsonDecode(draft.specificationsJson!);
    expect(specs['widthMm'], 600.0);
    expect(specs['heightMm'], 600.0);
    expect(specs['piecesPerBox'], 4);
    expect(specs['coveragePerBoxSqm'], 1.44);
  });
}
