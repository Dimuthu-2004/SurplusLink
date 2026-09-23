import 'package:mobile/core/api_client.dart';
import 'package:mobile/core/api_exception.dart';

import 'match_gateway.dart';
import 'match_models.dart';

class MatchRepository implements MatchGateway {
  MatchRepository(this._api, {this.onSessionExpired});
  final ApiClient _api;
  final Future<void> Function()? onSessionExpired;
  Future<T> _guard<T>(Future<T> Function() action) async {
    try {
      return await action();
    } on ApiException catch (error) {
      if (error.statusCode == 401) await onSessionExpired?.call();
      rethrow;
    }
  }

  @override
  Future<MatchPage<RecommendedMatch>> list(
    String requirementId,
    MatchQuery query,
  ) => _guard(
    () async => MatchPage.fromJson(
      await _api.getJson(
        Uri(
          path:
              '/api/matches/requirement/${Uri.encodeComponent(requirementId)}',
          queryParameters: query.toParameters(),
        ).toString(),
        authenticated: true,
      ),
      RecommendedMatch.fromJson,
    ),
  );

  @override
  Future<RecommendedMatch> get(String requirementId, String matchId) async {
    // The shared API has no GET /matches/{id}; resolve deep links through its scoped list.
    var page = 1;
    while (true) {
      final result = await list(
        requirementId,
        MatchQuery(
          page: page,
          pageSize: 100,
          sortBy: 'createdAt',
          sortDir: 'asc',
        ),
      );
      for (final match in result.items) {
        if (match.id == matchId && match.requirementId == requirementId) {
          return match;
        }
      }
      if (result.page != page) {
        throw const FormatException('Unexpected match page.');
      }
      if (page >= result.totalPages || result.items.isEmpty) break;
      page++;
    }
    throw const ApiException(
      'This match is no longer available for this requirement.',
      statusCode: 404,
    );
  }

  @override
  Future<void> select(String requirementId, String matchId) => _guard(
    () => _api.postJson('/api/requirements/$requirementId/select-match', {
      'matchId': matchId,
    }, authenticated: true),
  );

  @override
  Future<void> cancelPendingApproval(String requirementId) => _guard(
    () => _api.postJson('/api/requirements/$requirementId/cancel-pending-approval', {}, authenticated: true),
  );

  @override
  Future<MatchPage<MatchHistoryEntry>> history(
    String matchId, {
    int page = 1,
  }) => _guard(
    () async => MatchPage.fromJson(
      await _api.getJson(
        '/api/matches/${Uri.encodeComponent(matchId)}/history?page=$page&pageSize=20',
        authenticated: true,
      ),
      MatchHistoryEntry.fromJson,
    ),
  );
}
