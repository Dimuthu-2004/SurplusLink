import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/app.dart';
import 'package:mobile/auth/auth_controller.dart';
import 'package:mobile/auth/auth_models.dart';
import 'package:mobile/core/api_exception.dart';
import 'package:mobile/routing/app_router.dart';

import 'support/fakes.dart';

void main() {
  testWidgets('guest cannot open the protected home route', (tester) async {
    final gateway = FakeAuthGateway();

    await _pumpApp(tester, gateway, initialLocation: AppRoutes.home);

    expect(find.text('Welcome to SurplusLink'), findsOneWidget);
    expect(find.byKey(const Key('login-submit')), findsOneWidget);
    expect(find.byKey(const Key('role-home-title')), findsNothing);
  });

  testWidgets('restored buyer sees buyer-aware home', (tester) async {
    final gateway = FakeAuthGateway()..restoredUser = buyerUser;

    await _pumpApp(tester, gateway);

    expect(find.text('Buyer home'), findsOneWidget);
    expect(find.text(buyerUser.email), findsOneWidget);
  });

  testWidgets('successful login navigates to seller home', (tester) async {
    final gateway = FakeAuthGateway();
    await _pumpApp(tester, gateway);

    await tester.enterText(
      find.byKey(const Key('login-email')),
      'seller@example.com',
    );
    await tester.enterText(
      find.byKey(const Key('login-password')),
      'Password123!',
    );
    await tester.tap(find.byKey(const Key('login-submit')));
    await tester.pumpAndSettle();

    expect(find.text('Seller home'), findsOneWidget);
  });

  testWidgets('registration offers only seller and buyer roles', (
    tester,
  ) async {
    final gateway = FakeAuthGateway();
    await _pumpApp(tester, gateway);
    await tester.tap(find.byKey(const Key('go-register')));
    await tester.pumpAndSettle();

    await tester.ensureVisible(find.byKey(const Key('register-role')));
    await tester.tap(find.byKey(const Key('register-role')));
    await tester.pumpAndSettle();

    expect(find.text('Seller'), findsWidgets);
    expect(find.text('Buyer'), findsWidgets);
    expect(find.text('Manager'), findsNothing);

    await tester.tap(find.text('Seller').last);
    await tester.pumpAndSettle();
    await tester.enterText(
      find.byKey(const Key('register-email')),
      'seller@example.com',
    );
    await tester.enterText(
      find.byKey(const Key('register-password')),
      'Password123!',
    );
    await tester.enterText(
      find.byKey(const Key('profile-name')),
      'Test Seller',
    );
    await tester.enterText(
      find.byKey(const Key('profile-phone')),
      '0771234567',
    );
    await tester.enterText(find.byKey(const Key('profile-address')), 'Colombo');
    FocusManager.instance.primaryFocus?.unfocus();
    await tester.pumpAndSettle();
    await tester.ensureVisible(find.byKey(const Key('register-submit')));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const Key('register-submit')));
    await tester.pumpAndSettle();

    expect(gateway.registeredRole, AppRole.seller);
    expect(gateway.registeredProfile!.fullName, 'Test Seller');
    expect(find.text('Seller home'), findsOneWidget);
  });

  testWidgets('login displays progress and API errors', (tester) async {
    final gateway = FakeAuthGateway()
      ..loginCompleter = Completer<AuthSession>();
    await _pumpApp(tester, gateway);
    await tester.enterText(
      find.byKey(const Key('login-email')),
      'seller@example.com',
    );
    await tester.enterText(
      find.byKey(const Key('login-password')),
      'Password123!',
    );

    await tester.tap(find.byKey(const Key('login-submit')));
    await tester.pump();
    expect(find.byType(CircularProgressIndicator), findsOneWidget);

    gateway.loginCompleter!.completeError(
      const ApiException('Invalid email or password.', statusCode: 401),
    );
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('auth-error')), findsOneWidget);
    expect(find.text('Invalid email or password.'), findsOneWidget);
  });

  testWidgets('logout returns to login and protects home', (tester) async {
    final gateway = FakeAuthGateway()..restoredUser = sellerUser;
    await _pumpApp(tester, gateway);

    await tester.tap(find.byKey(const Key('logout-button')));
    await tester.pumpAndSettle();

    expect(gateway.logoutCalled, isTrue);
    expect(find.text('Welcome to SurplusLink'), findsOneWidget);
    expect(find.byKey(const Key('role-home-title')), findsNothing);
  });
}

Future<void> _pumpApp(
  WidgetTester tester,
  FakeAuthGateway gateway, {
  String initialLocation = AppRoutes.splash,
}) async {
  await tester.pumpWidget(
    SurplusLinkApp(
      authController: AuthController(gateway),
      initialLocation: initialLocation,
    ),
  );
  await tester.pumpAndSettle();
}
