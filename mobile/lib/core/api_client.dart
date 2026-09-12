import 'dart:async';
import 'dart:convert';

import 'package:http/http.dart' as http;
import 'package:mobile/core/api_exception.dart';
import 'package:mobile/core/token_storage.dart';

final class ApiClient {
  ApiClient({
    required this._baseUri,
    required this._httpClient,
    required this._tokenStorage,
    this.timeout = const Duration(seconds: 15),
  });

  final Uri _baseUri;
  final http.Client _httpClient;
  final TokenStorage _tokenStorage;
  final Duration timeout;

  Future<Map<String, dynamic>> getJson(
    String path, {
    bool authenticated = false,
  }) async {
    return _send(method: 'GET', path: path, authenticated: authenticated);
  }

  Future<Map<String, dynamic>> postJson(
    String path,
    Map<String, dynamic> body, {
    bool authenticated = false,
  }) async {
    return _send(
      method: 'POST',
      path: path,
      body: body,
      authenticated: authenticated,
    );
  }

  Future<Map<String, dynamic>> _send({
    required String method,
    required String path,
    required bool authenticated,
    Map<String, dynamic>? body,
  }) async {
    final headers = <String, String>{'Accept': 'application/json'};
    if (body != null) {
      headers['Content-Type'] = 'application/json';
    }
    if (authenticated) {
      final token = await _tokenStorage.readToken();
      if (token == null || token.isEmpty) {
        throw const ApiException(
          'Authentication is required.',
          statusCode: 401,
        );
      }
      headers['Authorization'] = 'Bearer $token';
    }

    try {
      final uri = _baseUri.resolve(path);
      final response = switch (method) {
        'GET' => await _httpClient.get(uri, headers: headers).timeout(timeout),
        'POST' =>
          await _httpClient
              .post(uri, headers: headers, body: jsonEncode(body))
              .timeout(timeout),
        _ => throw ArgumentError.value(method, 'method', 'Unsupported method'),
      };

      final json = _decodeBody(response.body);
      if (response.statusCode < 200 || response.statusCode >= 300) {
        throw _exceptionFrom(response.statusCode, json);
      }
      return json;
    } on TimeoutException {
      throw const ApiException('The server took too long to respond.');
    } on http.ClientException {
      throw const ApiException('Unable to connect to the SurplusLink API.');
    } on FormatException {
      throw const ApiException('The server returned an invalid response.');
    }
  }

  static Map<String, dynamic> _decodeBody(String body) {
    if (body.trim().isEmpty) {
      return <String, dynamic>{};
    }
    final decoded = jsonDecode(body);
    if (decoded is! Map<String, dynamic>) {
      throw const FormatException('Expected a JSON object.');
    }
    return decoded;
  }

  static ApiException _exceptionFrom(
    int statusCode,
    Map<String, dynamic> json,
  ) {
    final validationErrors = <String, List<String>>{};
    final rawErrors = json['errors'];
    if (rawErrors is Map<String, dynamic>) {
      for (final entry in rawErrors.entries) {
        final value = entry.value;
        if (value is List) {
          validationErrors[entry.key] = value.map((item) => '$item').toList();
        }
      }
    }

    final message =
        json['message'] as String? ??
        json['title'] as String? ??
        (validationErrors.isNotEmpty
            ? validationErrors.values.expand((items) => items).join(' ')
            : 'Request failed with status $statusCode.');

    return ApiException(
      message,
      statusCode: statusCode,
      validationErrors: validationErrors.isEmpty ? null : validationErrors,
    );
  }
}
