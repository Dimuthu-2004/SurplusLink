import 'package:mobile/core/api_client.dart';
import 'package:mobile/core/api_exception.dart';
import 'package:mobile/categories/category_repository.dart';

import 'requirement_gateway.dart';
import 'requirement_models.dart';

class RequirementRepository implements RequirementGateway {
  RequirementRepository(this._api, {this.onSessionExpired});
  final ApiClient _api;
  final Future<void> Function()? onSessionExpired;

  Future<T> _guard<T>(Future<T> Function() request) async {
    try {
      return await request();
    } on ApiException catch (error) {
      if (error.statusCode == 401) await onSessionExpired?.call();
      rethrow;
    }
  }

  @override
  Future<List<RequirementCategory>> categories() =>
      _guard(() => CategoryRepository(_api).categories());
  @override
  Future<RequirementPage<BuyerRequirement>> my(RequirementQuery query) =>
      _guard(() async {
        final path = Uri(
          path: '/api/requirements/my',
          queryParameters: query.toQueryParameters(),
        ).toString();
        return RequirementPage.fromJson(
          await _api.getJson(path, authenticated: true),
          BuyerRequirement.fromJson,
        );
      });
  @override
  Future<BuyerRequirement> get(String id) => _guard(
    () async => BuyerRequirement.fromJson(
      await _api.getJson('/api/requirements/$id', authenticated: true),
    ),
  );
  @override
  Future<BuyerRequirement> create(RequirementDraft draft) => _guard(
    () async => BuyerRequirement.fromJson(
      await _api.postJson(
        '/api/requirements',
        draft.toJson(),
        authenticated: true,
      ),
    ),
  );
  @override
  Future<BuyerRequirement> update(String id, RequirementDraft draft) => _guard(
    () async => BuyerRequirement.fromJson(
      await _api.putJson(
        '/api/requirements/$id',
        draft.toJson(),
        authenticated: true,
      ),
    ),
  );
  @override
  Future<void> delete(String id) =>
      _guard(() => _api.delete('/api/requirements/$id', authenticated: true));
  Future<BuyerRequirement> _action(String id, String action) => _guard(
    () async => BuyerRequirement.fromJson(
      await _api.postJson(
        '/api/requirements/$id/$action',
        {},
        authenticated: true,
      ),
    ),
  );
  @override
  Future<BuyerRequirement> submit(String id) => _action(id, 'submit');
  @override
  Future<BuyerRequirement> cancel(String id) => _action(id, 'cancel');
  @override
  Future<RequirementStartResult> startMatching(String id) => _guard(() async {
    final json = await _api.postJson(
      '/api/requirements/$id/start-matching',
      {},
      authenticated: true,
    );
    return RequirementStartResult(
      BuyerRequirement.fromJson(json['requirement'] as Map<String, dynamic>),
      json['workflowId'] as String,
    );
  });
  @override
  Future<RequirementPage<RequirementHistoryEntry>> history(
    String id, {
    int page = 1,
  }) => _guard(
    () async => RequirementPage.fromJson(
      await _api.getJson(
        '/api/requirements/$id/history?page=$page&pageSize=20',
        authenticated: true,
      ),
      RequirementHistoryEntry.fromJson,
    ),
  );
}
