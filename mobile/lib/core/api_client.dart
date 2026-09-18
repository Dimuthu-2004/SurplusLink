import 'dart:async';
import 'dart:convert';

import 'package:http/http.dart' as http;
import 'package:mobile/core/api_exception.dart';
import 'package:mobile/core/token_storage.dart';

final class ApiClient {
  ApiClient({
    required this.baseUri,
    required this.httpClient,
    required this.tokenStorage,
    this.timeout = const Duration(seconds: 15),
  });

  final Uri baseUri;
  final http.Client httpClient;
  final TokenStorage tokenStorage;
  final Duration timeout;

  Future<Map<String, dynamic>> getJson(
    String path, {
    bool authenticated = false,
  }) => _expectMap(
    _send(method: 'GET', path: path, authenticated: authenticated),
  );

  Future<List<Map<String, dynamic>>> getListJson(
    String path, {
    bool authenticated = false,
  }) async {
    final json = await _send(
      method: 'GET',
      path: path,
      authenticated: authenticated,
    );
    if (json is! List) {
      throw const FormatException('Expected a JSON array.');
    }
    return json.map((item) {
      if (item is! Map<String, dynamic>) {
        throw const FormatException('Expected a JSON object in the array.');
      }
      return item;
    }).toList();
  }

  Future<Map<String, dynamic>> postJson(
    String path,
    Map<String, dynamic> body, {
    bool authenticated = false,
  }) => _expectMap(
    _send(method: 'POST', path: path, body: body, authenticated: authenticated),
  );

  Future<Map<String, dynamic>> putJson(
    String path,
    Map<String, dynamic> body, {
    bool authenticated = false,
  }) => _expectMap(
    _send(method: 'PUT', path: path, body: body, authenticated: authenticated),
  );

  Future<Map<String, dynamic>> patchJson(
    String path,
    Map<String, dynamic>? body, {
    bool authenticated = false,
  }) => _expectMap(
    _send(
      method: 'PATCH',
      path: path,
      body: body,
      authenticated: authenticated,
    ),
  );

  Future<Map<String, dynamic>> uploadPhoto(List<int> bytes) async {
    final token = await tokenStorage.readToken();
    if (token == null || token.isEmpty) {
      throw const ApiException('Authentication is required.', statusCode: 401);
    }
    final request =
        http.MultipartRequest('POST', baseUri.resolve('/api/material-photos'))
          ..headers['Authorization'] = 'Bearer $token'
          ..files.add(
            http.MultipartFile.fromBytes('file', bytes, filename: 'photo'),
          );
    try {
      final response = await http.Response.fromStream(
        await httpClient.send(request).timeout(const Duration(seconds: 60)),
      );
      final body = _decodeBody(response.body);
      if (response.statusCode < 200 || response.statusCode >= 300) {
        throw _exceptionFrom(
          response.statusCode,
          body is Map<String, dynamic> ? body : {},
        );
      }
      if (body is! Map<String, dynamic>) throw const FormatException();
      return body;
    } on TimeoutException {
      throw const ApiException('Photo upload timed out. Please retry.');
    } on http.ClientException {
      throw const ApiException(
        'Unable to upload photo. Check your connection.',
      );
    } on FormatException {
      throw const ApiException('Invalid photo upload response.');
    }
  }

  Future<void> delete(String path, {bool authenticated = false}) async {
    await _send(method: 'DELETE', path: path, authenticated: authenticated);
  }

  Future<Map<String, dynamic>> _expectMap(Future<Object?> response) async {
    final json = await response;
    if (json is! Map<String, dynamic>) {
      throw const FormatException('Expected a JSON object.');
    }
    return json;
  }

  Future<Object?> _send({
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
      final token = await tokenStorage.readToken();
      if (token == null || token.isEmpty) {
        throw const ApiException(
          'Authentication is required.',
          statusCode: 401,
        );
      }
      headers['Authorization'] = 'Bearer $token';
    }

    try {
      final uri = baseUri.resolve(path);
      final response = switch (method) {
        'GET' => await httpClient.get(uri, headers: headers).timeout(timeout),
        'POST' =>
          await httpClient
              .post(uri, headers: headers, body: jsonEncode(body))
              .timeout(timeout),
        'PUT' =>
          await httpClient
              .put(uri, headers: headers, body: jsonEncode(body))
              .timeout(timeout),
        'PATCH' =>
          await httpClient
              .patch(
                uri,
                headers: headers,
                body: body == null ? null : jsonEncode(body),
              )
              .timeout(timeout),
        'DELETE' =>
          await httpClient.delete(uri, headers: headers).timeout(timeout),
        _ => throw ArgumentError.value(method, 'method', 'Unsupported method'),
      };

      final json = _decodeBody(response.body);
      if (response.statusCode < 200 || response.statusCode >= 300) {
        throw _exceptionFrom(
          response.statusCode,
          json is Map<String, dynamic> ? json : <String, dynamic>{},
        );
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

  static Object? _decodeBody(String body) =>
      body.trim().isEmpty ? null : jsonDecode(body);

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
        json['detail'] as String? ??
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
