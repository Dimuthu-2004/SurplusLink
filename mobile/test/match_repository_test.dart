import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mobile/core/api_client.dart';
import 'package:mobile/core/api_exception.dart';
import 'package:mobile/matches/match_models.dart';
import 'package:mobile/matches/match_repository.dart';

import 'support/fakes.dart';

final matchJson = {
  'id': 'm1',
  'requirementId': 'r1',
  'listingId': 'l1',
  'score': .8,
  'status': 'ROUTED',
  'distance': 12.5,
  'estimatedTransportCost': 1000,
  'createdAt': '2026-09-19T00:00:00Z',
  'rejectionReason': null,
};
Map<String, dynamic> pageJson(
  List<Object> items, {
  int page = 1,
  int totalPages = 1,
}) => {
  'items': items,
  'total': 2,
  'page': page,
  'pageSize': 100,
  'totalPages': totalPages,
};

void main() {
  test('list sends all filters with shared bearer auth and GET only', () async {
    final repository = repo((request) async {
      expect(request.method, 'GET');
      expect(request.headers['authorization'], 'Bearer buyer-token');
      expect(request.url.path, '/api/matches/requirement/r1');
      expect(request.url.queryParameters, {
        'valid': 'true',
        'rejected': 'false',
        'status': 'ROUTED',
        'sortBy': 'distance',
        'sortDir': 'asc',
        'page': '2',
        'pageSize': '10',
      });
      return http.Response(
        jsonEncode(pageJson([matchJson], page: 2, totalPages: 2)),
        200,
      );
    });
    final result = await repository.list(
      'r1',
      const MatchQuery(
        valid: true,
        rejected: false,
        status: 'ROUTED',
        sortBy: 'distance',
        sortDir: 'asc',
        page: 2,
        pageSize: 10,
      ),
    );
    expect(result.total, 2);
    expect(result.items.single.distance, 12.5);
    expect(result.items.single.estimatedTransportCost, 1000);
  });
  test(
    'details use the single-match endpoint and history uses its documented GET endpoint',
    () async {
      final paths = <String>[];
      final repository = repo((request) async {
        expect(request.method, 'GET');
        paths.add(request.url.path);
        if (request.url.path.endsWith('/history')) {
          final page = int.parse(request.url.queryParameters['page']!);
          return http.Response(
            jsonEncode(
              pageJson([
                {
                  'id': 'h1',
                  'action': 'ROUTE',
                  'outcome': 'SUCCEEDED',
                  'createdAt': '2026-09-19T00:00:00Z',
                },
              ], page: page),
            ),
            200,
          );
        }
        expect(request.url.path, '/api/matches/m1');
        expect(request.url.query, isEmpty);
        return http.Response(
          jsonEncode(matchJson),
          200,
        );
      });
      expect((await repository.get('r1', 'm1')).id, 'm1');
      expect(
        (await repository.history('m1')).items.single.outcome,
        'SUCCEEDED',
      );
      expect(paths, [
        '/api/matches/m1',
        '/api/matches/m1/history',
      ]);
    },
  );
  test('details do not parse unrelated rows from the requirement match list',
      () async {
    final repository = repo((request) async {
      expect(request.url.path, '/api/matches/m1');
      return http.Response(jsonEncode(matchJson), 200);
    });

    expect((await repository.get('r1', 'm1')).id, 'm1');
  });
  test(
    'missing matches and malformed successful payloads are errors',
    () async {
      final repository = repo(
        (_) async => http.Response(jsonEncode(pageJson([])), 200),
      );
      await expectLater(
        repository.get('r1', 'missing'),
        throwsA(isA<ApiException>().having((e) => e.statusCode, 'status', 404)),
      );
      final invalid = repo((_) async => http.Response('{"items":null}', 200));
      await expectLater(
        invalid.list('r1', const MatchQuery()),
        throwsFormatException,
      );
      final row = RecommendedMatch.fromJson({
        ...matchJson,
        'distance': null,
        'estimatedTransportCost': null,
      });
      expect(row.distance, isNull);
      final routeFailed = RecommendedMatch.fromJson({
        ...matchJson,
        'status': 'ROUTE_FAILED',
        'valid': false,
        'rejected': false,
        'rejectionReason': 'ROUTE_UNAVAILABLE',
      });
      expect(routeFailed.isSelectable, isFalse);
    },
  );
  test('401 invokes shared logout while 403 does not', () async {
    var logouts = 0;
    var status = 401;
    final repository = repo(
      (_) async => http.Response('{"detail":"Denied"}', status),
      expired: () async {
        logouts++;
      },
    );
    await expectLater(
      repository.list('r1', const MatchQuery()),
      throwsA(isA<ApiException>()),
    );
    status = 403;
    await expectLater(repository.history('m1'), throwsA(isA<ApiException>()));
    expect(logouts, 1);
  });
}

MatchRepository repo(
  Future<http.Response> Function(http.Request) respond, {
  Future<void> Function()? expired,
}) => MatchRepository(
  ApiClient(
    baseUri: Uri.parse('https://api.test'),
    httpClient: MockClient(respond),
    tokenStorage: MemoryTokenStorage()..token = 'buyer-token',
  ),
  onSessionExpired: expired,
);
