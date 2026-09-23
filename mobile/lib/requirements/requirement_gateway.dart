import 'requirement_models.dart';

abstract interface class RequirementGateway {
  Future<List<RequirementCategory>> categories();
  Future<List<String>> activeUnits(String categoryId);
  Future<RequirementPage<BuyerRequirement>> my(RequirementQuery query);
  Future<BuyerRequirement> get(String id);
  Future<BuyerRequirement> create(RequirementDraft draft);
  Future<BuyerRequirement> update(String id, RequirementDraft draft);
  Future<void> delete(String id);
  Future<BuyerRequirement> submit(String id);
  Future<RequirementStartResult> startMatching(String id);
  Future<BuyerRequirement> cancel(String id);
  Future<RequirementPage<RequirementHistoryEntry>> history(
    String id, {
    int page = 1,
  });
}
