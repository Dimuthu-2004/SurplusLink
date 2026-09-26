import 'package:mobile/auth/auth_models.dart';

abstract interface class AuthGateway {
  Future<AppUser?> restoreSession();

  Future<AuthSession> login({required String email, required String password});

  Future<void> register({
    required String email,
    required String password,
    required List<AppRole> roles,
    UserProfile? profile,
  });
  Future<void> verifyEmail({required String email, required String code});
  Future<void> resendVerification({required String email});
  Future<void> forgotPassword({required String email});
  Future<void> resetPassword({required String email, required String code, required String newPassword});

  Future<AppUser> updateProfile(UserProfile profile);

  Future<void> logout();
}
