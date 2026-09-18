import 'package:mobile/auth/auth_gateway.dart';
import 'package:mobile/auth/auth_models.dart';
import 'package:mobile/core/api_client.dart';
import 'package:mobile/core/api_exception.dart';
import 'package:mobile/core/token_storage.dart';

final class AuthRepository implements AuthGateway {
  AuthRepository({required this._apiClient, required this._tokenStorage});

  final ApiClient _apiClient;
  final TokenStorage _tokenStorage;

  @override
  Future<AppUser?> restoreSession() async {
    final token = await _tokenStorage.readToken();
    if (token == null || token.isEmpty) {
      return null;
    }

    try {
      final json = await _apiClient.getJson(
        '/api/auth/me',
        authenticated: true,
      );
      return AppUser.fromJson(json);
    } on ApiException catch (exception) {
      if (exception.statusCode == 401 || exception.statusCode == 404) {
        await _tokenStorage.deleteToken();
        return null;
      }
      rethrow;
    }
  }

  @override
  Future<AuthSession> login({
    required String email,
    required String password,
  }) async {
    final json = await _apiClient.postJson('/api/auth/login', {
      'email': email.trim(),
      'password': password,
    });
    return _persistSession(json);
  }

  @override
  Future<AuthSession> register({
    required String email,
    required String password,
    required List<AppRole> roles,
    UserProfile? profile,
  }) async {
    if (roles.isEmpty ||
        roles.toSet().length != roles.length ||
        roles.any((role) => role != AppRole.seller && role != AppRole.buyer)) {
      throw ArgumentError.value(
        roles,
        'roles',
        'Select selling, buying, or both.',
      );
    }
    final json = await _apiClient.postJson('/api/auth/register', {
      'email': email.trim(),
      'password': password,
      'roles': roles.map((role) => role.apiValue).toList(),
      ...?profile?.toJson(),
    });
    return _persistSession(json);
  }

  @override
  Future<AppUser> updateProfile(UserProfile profile) async => AppUser.fromJson(
    await _apiClient.putJson(
      '/api/auth/me',
      profile.toJson(),
      authenticated: true,
    ),
  );

  @override
  Future<void> logout() => _tokenStorage.deleteToken();

  Future<AuthSession> _persistSession(Map<String, dynamic> json) async {
    final session = AuthSession.fromJson(json);
    await _tokenStorage.writeToken(session.token);
    return session;
  }
}
