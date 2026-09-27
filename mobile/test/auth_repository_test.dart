import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mobile/auth/auth_models.dart';
import 'package:mobile/auth/auth_repository.dart';
import 'package:mobile/core/api_client.dart';
import 'package:mobile/core/api_exception.dart';

import 'support/fakes.dart';

void main() {
  test(
    'login uses the ASP.NET contract and securely persists the JWT',
    () async {
      final storage = MemoryTokenStorage();
      final repository = _repository(storage, (request) async {
        expect(request.method, 'POST');
        expect(request.url.path, '/api/auth/login');
        expect(jsonDecode(request.body), {
          'email': 'seller@example.com',
          'password': 'Password123!',
        });
        return http.Response(_authJson('SELLER'), 200);
      });

      final session = await repository.login(
        email: ' seller@example.com ',
        password: 'Password123!',
      );

      expect(session.user.roles, [AppRole.seller]);
      expect(storage.token, 'signed-jwt');
    },
  );

  test('session restoration sends the JWT as a bearer token', () async {
    final storage = MemoryTokenStorage()..token = 'stored-jwt';
    final repository = _repository(storage, (request) async {
      expect(request.method, 'GET');
      expect(request.url.path, '/api/auth/me');
      expect(request.headers['authorization'], 'Bearer stored-jwt');
      return http.Response(_userJson('BUYER'), 200);
    });

    final user = await repository.restoreSession();

    expect(user?.roles, [AppRole.buyer]);
  });

  test('registration sends only the selected public role', () async {
    final storage = MemoryTokenStorage();
    final repository = _repository(storage, (request) async {
      expect(request.method, 'POST');
      expect(request.url.path, '/api/auth/register');
      expect(jsonDecode(request.body), {
        'email': 'buyer@example.com',
        'password': 'Password123!',
        'roles': ['BUYER'],
      });
      return http.Response(
        jsonEncode({
          'email': 'buyer@example.com',
          'emailVerificationRequired': true,
        }),
        201,
      );
    });

    await repository.register(
      email: 'buyer@example.com',
      password: 'Password123!',
      roles: [AppRole.buyer],
    );

    expect(storage.token, isNull);
  });

  test(
    'dual registration sends two roles and invalid capabilities never call API',
    () async {
      final storage = MemoryTokenStorage();
      var calls = 0;
      final repository = _repository(storage, (request) async {
        calls++;
        expect(jsonDecode(request.body)['roles'], ['SELLER', 'BUYER']);
        return http.Response(
          jsonEncode({
            'email': 'dual@test.local',
            'emailVerificationRequired': true,
          }),
          201,
        );
      });
      await repository.register(
        email: 'dual@test.local',
        password: 'Password123!',
        roles: [AppRole.seller, AppRole.buyer],
      );
      expect(storage.token, isNull);
      for (final roles in <List<AppRole>>[
        [],
        [AppRole.manager],
        [AppRole.seller, AppRole.manager],
        [AppRole.buyer, AppRole.buyer],
      ]) {
        await expectLater(
          repository.register(
            email: 'x@test.local',
            password: 'Password123!',
            roles: roles,
          ),
          throwsArgumentError,
        );
      }
      expect(calls, 1);
    },
  );

  test('expired session is cleared after an unauthorized response', () async {
    final storage = MemoryTokenStorage()..token = 'expired-jwt';
    final repository = _repository(
      storage,
      (request) async =>
          http.Response(jsonEncode({'message': 'Invalid token.'}), 401),
    );

    final user = await repository.restoreSession();

    expect(user, isNull);
    expect(storage.token, isNull);
  });

  test('API message is exposed without leaking transport details', () async {
    final repository = _repository(
      MemoryTokenStorage(),
      (request) async => http.Response(
        jsonEncode({'message': 'Invalid email or password.'}),
        401,
      ),
    );

    await expectLater(
      repository.login(email: 'buyer@example.com', password: 'wrong-pass'),
      throwsA(
        isA<ApiException>()
            .having((error) => error.statusCode, 'statusCode', 401)
            .having(
              (error) => error.message,
              'message',
              'Invalid email or password.',
            ),
      ),
    );
  });

  test('login preserves the structured unverified-account code', () async {
    final repository = _repository(
      MemoryTokenStorage(),
      (request) async => http.Response(
        jsonEncode({
          'code': 'EMAIL_NOT_VERIFIED',
          'message': 'Your email has not been verified.',
        }),
        403,
      ),
    );

    await expectLater(
      repository.login(email: 'buyer@example.com', password: 'Password123!'),
      throwsA(
        isA<ApiException>()
            .having((error) => error.statusCode, 'statusCode', 403)
            .having((error) => error.code, 'code', 'EMAIL_NOT_VERIFIED'),
      ),
    );
  });

  test('verification and resend use the existing email endpoints', () async {
    final requests = <http.Request>[];
    final repository = _repository(MemoryTokenStorage(), (request) async {
      requests.add(request);
      return http.Response(jsonEncode({'message': 'ok'}), 200);
    });

    await repository.verifyEmail(email: ' user@example.com ', code: '012345');
    await repository.resendVerification(email: ' user@example.com ');

    expect(requests[0].url.path, '/api/auth/email-verification/verify');
    expect(jsonDecode(requests[0].body), {
      'email': 'user@example.com',
      'code': '012345',
    });
    expect(requests[1].url.path, '/api/auth/email-verification/resend');
    expect(jsonDecode(requests[1].body), {'email': 'user@example.com'});
  });
}

AuthRepository _repository(
  MemoryTokenStorage storage,
  Future<http.Response> Function(http.Request) handler,
) {
  final client = ApiClient(
    baseUri: Uri.parse('https://api.surpluslink.test'),
    httpClient: MockClient(handler),
    tokenStorage: storage,
  );
  return AuthRepository(apiClient: client, tokenStorage: storage);
}

String _authJson(String role) =>
    jsonEncode({'token': 'signed-jwt', 'user': jsonDecode(_userJson(role))});

String _userJson(String role) => jsonEncode({
  'id': '00000000-0000-0000-0000-000000000001',
  'email': 'user@example.com',
  'roles': [role],
});
