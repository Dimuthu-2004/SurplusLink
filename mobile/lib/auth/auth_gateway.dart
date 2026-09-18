import 'package:mobile/auth/auth_models.dart';

abstract interface class AuthGateway {
  Future<AppUser?> restoreSession();

  Future<AuthSession> login({required String email, required String password});

  Future<AuthSession> register({
    required String email,
    required String password,
    required List<AppRole> roles,
    UserProfile? profile,
  });

  Future<AppUser> updateProfile(UserProfile profile);

  Future<void> logout();
}
