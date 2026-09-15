import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mobile/core/api_client.dart';
import 'package:mobile/materials/material_inventory_repository.dart';
import 'package:mobile/materials/material_models.dart';

import 'support/fakes.dart';

void main() {
  test('search uses the authenticated paged materials contract', () async {
    final storage = MemoryTokenStorage()..token = 'seller-jwt';
    final repository = _repository(storage, (request) async {
      expect(request.method, 'GET');
      expect(request.url.path, '/api/materials');
      expect(request.url.queryParameters, {
        'search': 'steel',
        'status': 'ACTIVE',
        'sortBy': 'unitPrice',
        'sortDir': 'asc',
        'page': '2',
        'pageSize': '10',
      });
      expect(request.headers['authorization'], 'Bearer seller-jwt');
      return http.Response(_pageJson(), 200);
    });

    final page = await repository.search(const MaterialListingQuery(
      search: 'steel',
      status: 'ACTIVE',
      sortBy: 'unitPrice',
      sortDir: 'asc',
      page: 2,
      pageSize: 10,
    ));

    expect(page.totalCount, 1);
    expect(page.totalPages, 1);
    expect(page.items.single.title, 'Steel beam');
  });

  test('create sends only the existing material API request fields', () async {
    final storage = MemoryTokenStorage()..token = 'seller-jwt';
    final repository = _repository(storage, (request) async {
      expect(request.method, 'POST');
      expect(request.url.path, '/api/materials');
      final body = jsonDecode(request.body) as Map<String, dynamic>;
      expect(body['categoryId'], '00000000-0000-0000-0000-000000000101');
      expect(body.containsKey('reservedQuantity'), isFalse);
      expect(body.containsKey('status'), isFalse);
      expect(body['photos'], [
        {'photoUrl': 'https://images.example.test/steel.jpg', 'sortOrder': 0},
      ]);
      return http.Response(_listingJson(), 201);
    });

    final listing = await repository.create(MaterialListingDraft(
      categoryId: '00000000-0000-0000-0000-000000000101',
      title: 'Steel beam',
      description: 'Reclaimed structural steel.',
      quantity: 10,
      unit: 'kg',
      condition: 'GOOD',
      unitPrice: 25,
      availableUntil: DateTime.utc(2030, 1, 1),
      photoUrls: const ['https://images.example.test/steel.jpg'],
    ));

    expect(listing.status, 'DRAFT');
  });

  test('history uses the shared listing history endpoint', () async {
    final storage = MemoryTokenStorage()..token = 'seller-jwt';
    final repository = _repository(storage, (request) async {
      expect(request.method, 'GET');
      expect(request.url.path, '/api/materials/listing-1/history');
      return http.Response(jsonEncode([
        {
          'id': 'audit-1',
          'actorUserId': 'seller-1',
          'action': 'LISTING_CREATED',
          'createdAtUtc': '2030-01-01T00:00:00Z',
        },
      ]), 200);
    });

    final history = await repository.history('listing-1');

    expect(history.single.action, 'LISTING_CREATED');
  });
}

MaterialInventoryRepository _repository(
  MemoryTokenStorage storage,
  Future<http.Response> Function(http.Request) handler,
) => MaterialInventoryRepository(
  ApiClient(
    baseUri: Uri.parse('https://api.surpluslink.test'),
    httpClient: MockClient(handler),
    tokenStorage: storage,
  ),
);

String _pageJson() => jsonEncode({
  'items': [jsonDecode(_listingJson())],
  'totalCount': 1,
  'totalPages': 1,
  'page': 2,
  'pageSize': 10,
});

String _listingJson() => jsonEncode({
  'id': 'listing-1',
  'sellerId': 'seller-1',
  'categoryId': '00000000-0000-0000-0000-000000000101',
  'categoryName': 'Steel',
  'title': 'Steel beam',
  'description': 'Reclaimed structural steel.',
  'quantity': 10.0,
  'reservedQuantity': 0.0,
  'unit': 'kg',
  'condition': 'GOOD',
  'unitPrice': 25.0,
  'latitude': null,
  'longitude': null,
  'availableUntil': '2030-01-01T00:00:00Z',
  'status': 'DRAFT',
  'createdAtUtc': '2029-12-01T00:00:00Z',
  'updatedAtUtc': '2029-12-01T00:00:00Z',
  'photos': [],
});