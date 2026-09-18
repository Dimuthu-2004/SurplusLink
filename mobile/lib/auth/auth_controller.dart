import 'package:flutter/foundation.dart';
import 'package:mobile/auth/auth_gateway.dart';
import 'package:mobile/auth/auth_models.dart';
import 'package:mobile/core/api_exception.dart';

enum AuthStatus { initializing, unauthenticated, authenticated }

final class AuthController extends ChangeNotifier {
  AuthController(this._gateway);

  final AuthGateway _gateway;
  AuthStatus _status = AuthStatus.initializing;
  AppUser? _user;
  bool _isBusy = false;
  String? _errorMessage;

  AuthStatus get status => _status;
  AppUser? get user => _user;
  bool get isBusy => _isBusy;
  String? get errorMessage => _errorMessage;
  bool get isAuthenticated => _status == AuthStatus.authenticated;

  Future<void> initialize() async {
    _status = AuthStatus.initializing;
    notifyListeners();
    try {
      _user = await _gateway.restoreSession();
      _status = _user == null
          ? AuthStatus.unauthenticated
          : AuthStatus.authenticated;
    } on ApiException catch (exception) {
      _user = null;
      _status = AuthStatus.unauthenticated;
      _errorMessage = exception.message;
    } on FormatException {
      _user = null;
      _status = AuthStatus.unauthenticated;
      _errorMessage = 'The server returned an invalid authentication response.';
    } on Object {
      _user = null;
      _status = AuthStatus.unauthenticated;
      _errorMessage = 'Unable to restore your session. Please sign in again.';
    } finally {
      notifyListeners();
    }
  }

  Future<bool> login({required String email, required String password}) =>
      _authenticate(() => _gateway.login(email: email, password: password));

  Future<bool> register({
    required String email,
    required String password,
    required List<AppRole> roles,
    UserProfile? profile,
  }) => _authenticate(
    () => _gateway.register(
      email: email,
      password: password,
      roles: roles,
      profile: profile,
    ),
  );

  Future<bool> updateProfile(UserProfile profile) async {
    if (_isBusy) return false;
    _errorMessage = null;
    _setBusy(true);
    try {
      _user = await _gateway.updateProfile(profile);
      return true;
    } on ApiException catch (error) {
      _errorMessage = error.message;
      return false;
    } on Object {
      _errorMessage = 'Unable to save your profile. Please retry.';
      return false;
    } finally {
      _setBusy(false);
    }
  }

  Future<void> logout() async {
    if (_isBusy) {
      return;
    }
    _setBusy(true);
    try {
      await _gateway.logout();
    } finally {
      _user = null;
      _status = AuthStatus.unauthenticated;
      _errorMessage = null;
      _setBusy(false);
    }
  }

  void clearError() {
    if (_errorMessage == null) {
      return;
    }
    _errorMessage = null;
    notifyListeners();
  }

  Future<bool> _authenticate(Future<AuthSession> Function() request) async {
    if (_isBusy) {
      return false;
    }
    _errorMessage = null;
    _setBusy(true);
    try {
      final session = await request();
      _user = session.user;
      _status = AuthStatus.authenticated;
      return true;
    } on ApiException catch (exception) {
      _errorMessage = exception.message;
      return false;
    } on FormatException {
      _errorMessage = 'The server returned an invalid authentication response.';
      return false;
    } on Object {
      _errorMessage = 'Authentication failed. Please try again.';
      return false;
    } finally {
      _setBusy(false);
    }
  }

  void _setBusy(bool value) {
    _isBusy = value;
    notifyListeners();
  }
}
