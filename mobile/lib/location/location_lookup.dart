import 'package:mobile/core/api_client.dart';

typedef AddressLookup = Future<String?> Function(
  double latitude,
  double longitude,
);

typedef AddressSearch = Future<List<AddressResult>> Function(String query);

class AddressResult {
  const AddressResult(this.latitude, this.longitude, this.displayName);
  final double latitude, longitude;
  final String displayName;
}

class ApiLocationLookup {
  const ApiLocationLookup(this._apiClient);

  final ApiClient _apiClient;

  Future<List<AddressResult>> search(String query) async {
    final path = Uri(
      path: '/api/locations/search',
      queryParameters: {'query': query.trim()},
    ).toString();
    return (await _apiClient.getListJson(path))
        .map(
          (row) => AddressResult(
            (row['latitude'] as num).toDouble(),
            (row['longitude'] as num).toDouble(),
            row['displayName'] as String,
          ),
        )
        .toList();
  }

  Future<String?> lookup(double latitude, double longitude) async {
    final query = Uri(
      queryParameters: {
        'latitude': latitude.toStringAsFixed(6),
        'longitude': longitude.toStringAsFixed(6),
      },
    ).query;
    final response = await _apiClient.getJson('/api/locations/reverse?$query');
    return response['displayName'] as String?;
  }
}

Future<String?> unavailableAddress(double latitude, double longitude) async =>
    null;
