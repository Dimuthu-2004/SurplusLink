import 'match_models.dart';

abstract interface class MatchGateway {
  Future<MatchPage<RecommendedMatch>> list(
    String requirementId,
    MatchQuery query,
  );
  Future<RecommendedMatch> get(String requirementId, String matchId);
  Future<void> select(String requirementId, String matchId, {double? quantity});
  Future<void> selectMatches(
    String requirementId,
    List<MatchAllocation> allocations,
  );
  Future<void> cancelPendingApproval(String requirementId);
  Future<MatchPage<MatchHistoryEntry>> history(String matchId, {int page = 1});
}
