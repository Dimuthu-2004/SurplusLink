import 'dart:async';

import 'package:mobile/auth/auth_gateway.dart';
import 'package:mobile/auth/auth_models.dart';
import 'package:mobile/core/token_storage.dart';

const sellerUser = AppUser(
  id: '00000000-0000-0000-0000-000000000001',
  email: 'seller@example.com',
  role: AppRole.seller,
);

const buyerUser = AppUser(
  id: '00000000-0000-0000-0000-000000000002',
  email: 'buyer@example.com',
  role: AppRole.buyer,
);

final class FakeAuthGateway implements AuthGateway {
  AppUser? restoredUser;
  Object? restoreError;
  Object? loginError;
  Completer<AuthSession>? loginCompleter;
  AppUser loginUser = sellerUser;
  AppRole? registeredRole;
  UserProfile? registeredProfile;
  bool logoutCalled = false;

  @override
  Future<AppUser?> restoreSession() async {
    if (restoreError != null) {
      throw restoreError!;
    }
    return restoredUser;
  }

  @override
  Future<AuthSession> login({
    required String email,
    required String password,
  }) async {
    if (loginCompleter != null) {
      return loginCompleter!.future;
    }
    if (loginError != null) {
      throw loginError!;
    }
    return AuthSession(token: 'test-token', user: loginUser);
  }

  @override
  Future<AuthSession> register({
    required String email,
    required String password,
    required AppRole role,
    UserProfile? profile,
  }) async {
    registeredRole = role;
    registeredProfile = profile;
    final user = role == AppRole.seller ? sellerUser : buyerUser;
    return AuthSession(token: 'test-token', user: user);
  }

  @override
  Future<AppUser> updateProfile(UserProfile profile) async => AppUser(
    id: loginUser.id,
    email: loginUser.email,
    role: loginUser.role,
    fullName: profile.fullName,
    phoneNumber: profile.phoneNumber,
    businessName: profile.businessName,
    address: profile.address,
  );

  @override
  Future<void> logout() async {
    logoutCalled = true;
  }
}

final class MemoryTokenStorage implements TokenStorage {
  String? token;

  @override
  Future<void> deleteToken() async {
    token = null;
  }

  @override
  Future<String?> readToken() async => token;

  @override
  Future<void> writeToken(String token) async {
    this.token = token;
  }
}
