import 'package:mobile/core/api_client.dart';
import 'package:mobile/materials/material_inventory_gateway.dart';
import 'package:mobile/materials/material_models.dart';

final class MaterialInventoryRepository implements MaterialInventoryGateway {
  MaterialInventoryRepository(this._apiClient);

  final ApiClient _apiClient;

  @override
  Future<MaterialListingPage> search(MaterialListingQuery query) async {
    final path = Uri(
      path: '/api/materials',
      queryParameters: query.toQueryParameters(),
    ).toString();
    final json = await _apiClient.getJson(path, authenticated: true);
    return MaterialListingPage.fromJson(json);
  }

  @override
  Future<MaterialListing> getById(String listingId) async {
    final json = await _apiClient.getJson(
      '/api/materials/$listingId',
      authenticated: true,
    );
    return MaterialListing.fromJson(json);
  }

  @override
  Future<MaterialListing> create(MaterialListingDraft draft) async {
    final json = await _apiClient.postJson(
      '/api/materials',
      draft.toJson(),
      authenticated: true,
    );
    return MaterialListing.fromJson(json);
  }

  @override
  Future<MaterialListing> update(String listingId, MaterialListingDraft draft) async {
    final json = await _apiClient.putJson(
      '/api/materials/$listingId',
      draft.toJson(),
      authenticated: true,
    );
    return MaterialListing.fromJson(json);
  }

  @override
  Future<void> delete(String listingId) =>
      _apiClient.delete('/api/materials/$listingId', authenticated: true);

  @override
  Future<MaterialListing> publish(String listingId) async {
    final json = await _apiClient.patchJson(
      '/api/materials/$listingId/publish',
      null,
      authenticated: true,
    );
    return MaterialListing.fromJson(json);
  }

  @override
  Future<List<MaterialListingHistoryEntry>> history(String listingId) async {
    final json = await _apiClient.getListJson(
      '/api/materials/$listingId/history',
      authenticated: true,
    );
    return json.map(MaterialListingHistoryEntry.fromJson).toList();
  }
}