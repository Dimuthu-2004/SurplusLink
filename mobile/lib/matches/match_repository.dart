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
    final match = RecommendedMatch.fromJson(
      await _api.getJson(
        '/api/matches/${Uri.encodeComponent(matchId)}',
        authenticated: true,
      ),
    );
    if (match.requirementId != requirementId) {
      throw const ApiException(
        'This match is no longer available for this requirement.',
        statusCode: 404,
      );
    }
    return match;
  }

  @override
  Future<void> select(String requirementId, String matchId, {double? quantity}) => _guard(
    () => _api.postJson('/api/requirements/$requirementId/select-match', {
      'matchId': matchId,
      'quantity': ?quantity,
    }, authenticated: true),
  );

  @override
  Future<void> selectMatches(
    String requirementId,
    List<MatchAllocation> allocations,
  ) => _guard(
    () => _api.postJson('/api/requirements/$requirementId/select-matches', {
      'allocations': allocations.map((a) => a.toJson()).toList(),
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
