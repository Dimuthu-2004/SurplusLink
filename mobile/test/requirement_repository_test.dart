import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mobile/core/api_client.dart';
import 'package:mobile/core/api_exception.dart';
import 'package:mobile/requirements/requirement_models.dart';
import 'package:mobile/requirements/requirement_repository.dart';

import 'support/fakes.dart';

final rowJson = {
  'id': 'r1',
  'buyerId': 'b1',
  'categoryId': 'c1',
  'requiredQuantity': 12.125,
  'unit': 'kg',
  'maximumBudget': 100.50,
  'deadline': '2030-01-02T18:29:59Z',
  'status': 'DRAFT',
  'createdAt': '2026-01-01T00:00:00Z',
  'updatedAt': '2026-01-02T00:00:00Z',
  'latitude': 6.123456,
  'longitude': 79.123456,
  'notes': 'Cement',
};

void main() {
  test('query preserves all filters dates sort and totals using shared bearer auth', () async {
    final repository = repo((request) async {
      expect(request.url.path, '/api/requirements/my');
      expect(request.headers['authorization'], 'Bearer buyer-token');
      expect(request.url.queryParameters, {
        'search': '100%_ cement',
        'status': 'OPEN',
        'categoryId': 'c1',
        'deadlineFrom': '2030-01-01T00:00:00.000Z',
        'deadlineTo': '2030-02-01T00:00:00.000Z',
        'sort': 'budget',
        'sortDir': 'asc',
        'page': '2',
        'pageSize': '10',
      });
      return http.Response(
        jsonEncode({
          'items': [rowJson],
          'total': 23,
          'page': 2,
          'pageSize': 10,
        }),
        200,
      );
    });
    final result = await repository.my(
      RequirementQuery(
        search: ' 100%_ cement ',
        status: 'OPEN',
        categoryId: 'c1',
        deadlineFrom: DateTime.utc(2030),
        deadlineTo: DateTime.utc(2030, 2),
        sort: 'budget',
        sortDir: 'asc',
        page: 2,
      ),
    );
    expect(result.total, 23);
    expect(result.totalPages, 3);
    expect(result.items.single.notes, 'Cement');
  });

  test('CRUD categories and lifecycle actions use the existing requirement contract', () async {
    final paths = <String>[];
    final repository = repo((request) async {
      expect(request.headers['authorization'], 'Bearer buyer-token');
      paths.add('${request.method} ${request.url.path}');
      if (request.url.path == '/api/material-categories') {
        return http.Response('[{"id":"c1","name":"Cement"}]', 200);
      }
      if (request.method == 'DELETE') return http.Response('', 204);
      if (request.url.path.endsWith('start-matching')) {
        return http.Response(
          jsonEncode({
            'requirement': {...rowJson, 'status': 'MATCHING'},
            'workflowId': 'real-id',
          }),
          200,
        );
      }
      if (request.method == 'PUT' || request.url.path == '/api/requirements') {
        final body = jsonDecode(request.body) as Map<String, dynamic>;
        expect(body.keys.toSet(), {
          'categoryId',
          'requiredQuantity',
          'unit',
          'maximumBudget',
          'deadline',
          'latitude',
          'longitude',
          'notes',
        });
        expect(body['deadline'], '2030-01-02T18:29:59.000Z');
        expect(body['notes'], 'Cement');
      }
      return http.Response(jsonEncode(rowJson), 200);
    });
    final draft = RequirementDraft(
      categoryId: 'c1',
      requiredQuantity: 12.125,
      unit: 'kg',
      maximumBudget: 100.50,
      deadline: DateTime.parse('2030-01-02T23:59:59+05:30'),
      latitude: 6.123456,
      longitude: 79.123456,
      notes: ' Cement ',
    );
    expect((await repository.categories()).single.name, 'Cement');
    await repository.create(draft);
    await repository.get('r1');
    await repository.update('r1', draft);
    await repository.submit('r1');
    final start = await repository.startMatching('r1');
    expect(start.workflowId, 'real-id');
    expect(start.requirement.status, 'MATCHING');
    await repository.cancel('r1');
    await repository.delete('r1');
    expect(paths, [
      'GET /api/material-categories',
      'POST /api/requirements',
      'GET /api/requirements/r1',
      'PUT /api/requirements/r1',
      'POST /api/requirements/r1/submit',
      'POST /api/requirements/r1/start-matching',
      'POST /api/requirements/r1/cancel',
      'DELETE /api/requirements/r1',
    ]);
  });

  test(
    'active units use the category API and preserve bearer authentication',
    () async {
      final repository = repo((request) async {
        expect(request.method, 'GET');
        expect(request.url.path, '/api/material-categories/c1/units');
        expect(request.headers['authorization'], 'Bearer buyer-token');
        return http.Response('["pcs", "m2"]', 200);
      });
      expect(await repository.activeUnits('c1'), ['m2', 'pcs']);
    },
  );
  test('history parses paged status transitions', () async {
    final repository = repo((request) async {
      expect(request.url.queryParameters, {'page': '2', 'pageSize': '20'});
      return http.Response(
        jsonEncode({
          'items': [
            {
              'id': 'a1',
              'action': 'STATUS_CHANGED',
              'actorUserId': null,
              'fromStatus': 'OPEN',
              'toStatus': 'MATCHING',
              'createdAt': '2026-01-01T00:00:00Z',
            },
          ],
          'total': 21,
          'page': 2,
          'pageSize': 20,
        }),
        200,
      );
    });
    final page = await repository.history('r1', page: 2);
    expect(page.items.single.toStatus, 'MATCHING');
    expect(page.total, 21);
  });

  test(
    'problem detail and field validation reach callers without a fake success',
    () async {
      final repository = repo(
        (_) async => http.Response(
          jsonEncode({
            'title': 'Service Unavailable',
            'detail':
                'Matching is not available yet. Your requirement remains open.',
          }),
          503,
        ),
      );
      await expectLater(
        repository.startMatching('r1'),
        throwsA(
          isA<ApiException>()
              .having((e) => e.statusCode, 'status', 503)
              .having((e) => e.message, 'message', contains('remains open')),
        ),
      );
      final invalid = repo(
        (_) async => http.Response(
          jsonEncode({
            'title': 'Invalid input',
            'errors': {
              'Unit': ['Enter a unit.'],
            },
          }),
          400,
        ),
      );
      await expectLater(
        invalid.get('r1'),
        throwsA(
          isA<ApiException>().having(
            (e) => e.validationErrors?['Unit'],
            'validation',
            ['Enter a unit.'],
          ),
        ),
      );
    },
  );

  test('401 invokes shared session expiry and 403 does not log out', () async {
    var expired = 0;
    final unauthorized = repo(
      (_) async => http.Response('', 401),
      onExpired: () async {
        expired++;
      },
    );
    await expectLater(unauthorized.get('r1'), throwsA(isA<ApiException>()));
    expect(expired, 1);
    final forbidden = repo(
      (_) async => http.Response('', 403),
      onExpired: () async {
        expired++;
      },
    );
    await expectLater(forbidden.get('r1'), throwsA(isA<ApiException>()));
    expect(expired, 1);
  });
  test('toJson rounds latitude and longitude to six decimal places', () {
    final draft = RequirementDraft(
      categoryId: 'c1',
      requiredQuantity: 10,
      unit: 'kg',
      maximumBudget: 100,
      deadline: DateTime.utc(2030),
      latitude: 6.123456789,   // 9 decimal places
      longitude: 79.987654321, // 9 decimal places
    );
    final json = draft.toJson();
    expect(json['latitude'], 6.123457);   // rounded to 6
    expect(json['longitude'], 79.987654); // rounded to 6
  });

  test('toJson preserves coordinates already within six decimal places', () {
    final draft = RequirementDraft(
      categoryId: 'c1',
      requiredQuantity: 10,
      unit: 'kg',
      maximumBudget: 100,
      deadline: DateTime.utc(2030),
      latitude: 6.9271,
      longitude: 79.8612,
    );
    final json = draft.toJson();
    expect(json['latitude'], 6.9271);
    expect(json['longitude'], 79.8612);
  });
}

RequirementRepository repo(
  Future<http.Response> Function(http.Request) handler, {
  Future<void> Function()? onExpired,
}) => RequirementRepository(
  ApiClient(
    baseUri: Uri.parse('http://localhost:5170'),
    httpClient: MockClient(handler),
    tokenStorage: MemoryTokenStorage()..token = 'buyer-token',
  ),
  onSessionExpired: onExpired,
);
