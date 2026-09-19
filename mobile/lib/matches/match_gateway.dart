import 'match_models.dart';

abstract interface class MatchGateway {
  Future<MatchPage<RecommendedMatch>> list(
    String requirementId,
    MatchQuery query,
  );
  Future<RecommendedMatch> get(String requirementId, String matchId);
  Future<MatchPage<MatchHistoryEntry>> history(String matchId, {int page = 1});
}
