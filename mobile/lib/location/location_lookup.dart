import 'package:mobile/core/api_client.dart';

typedef AddressLookup = Future<String?> Function(
  double latitude,
  double longitude,
);

class ApiLocationLookup {
  const ApiLocationLookup(this._apiClient);

  final ApiClient _apiClient;

  Future<String?> lookup(double latitude, double longitude) async {
    final query = Uri(queryParameters: {
      'latitude': latitude.toStringAsFixed(6),
      'longitude': longitude.toStringAsFixed(6),
    }).query;
    final response = await _apiClient.getJson('/api/locations/reverse?$query');
    return response['displayName'] as String?;
  }
}

Future<String?> unavailableAddress(double latitude, double longitude) async => null;