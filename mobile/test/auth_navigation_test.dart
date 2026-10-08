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

    expect(find.text('Buyer'), findsOneWidget);
    expect(find.text('buyer'), findsOneWidget);
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

    expect(find.text('Seller'), findsOneWidget);
  });

  for (final choice in [
    'Sell surplus materials',
    'Buy / request materials',
    'Both',
  ]) {
    testWidgets('registration maps $choice after account details', (
      tester,
    ) async {
      final gateway = FakeAuthGateway();
      await _pumpApp(tester, gateway);
      await tester.tap(find.byKey(const Key('go-register')));
      await tester.pump(const Duration(milliseconds: 180));
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
        find.byKey(const Key('profile-nic')),
        '199912345678',
      );
      expect(find.text('NIC'), findsOneWidget);
      await tester.enterText(
        find.byKey(const Key('profile-phone')),
        '0771234567',
      );
      await tester.enterText(
        find.byKey(const Key('profile-address')),
        'Colombo',
      );
      FocusManager.instance.primaryFocus?.unfocus();
      await tester.pumpAndSettle();
      await tester.ensureVisible(find.byKey(const Key('register-submit')));
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const Key('register-submit')));
      await tester.pumpAndSettle();

      expect(find.text('How will you use SurplusLink?'), findsWidgets);
      await tester.tap(find.byKey(const Key('register-submit')));
      await tester.pumpAndSettle();
      expect(find.text('Choose how you will use SurplusLink.'), findsOneWidget);
      expect(gateway.registeredRoles, isNull);
      await tester.tap(find.byKey(const Key('register-usage')));
      await tester.pumpAndSettle();
      expect(find.text('Sell surplus materials'), findsWidgets);
      expect(find.text('Buy / request materials'), findsWidgets);
      expect(find.text('Both'), findsWidgets);
      expect(find.text('Manager'), findsNothing);
      await tester.tap(find.text(choice).last);
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const Key('register-submit')));
      await tester.pumpAndSettle();
      expect(
        gateway.registeredRoles,
        choice == 'Both'
            ? [AppRole.seller, AppRole.buyer]
            : choice == 'Sell surplus materials'
            ? [AppRole.seller]
            : [AppRole.buyer],
      );
      expect(gateway.registeredProfile!.fullName, 'Test Seller');
      expect(find.text('Verify your email'), findsOneWidget);
      expect(
        find.text('Enter the six-digit code sent to seller@example.com.'),
        findsOneWidget,
      );
      expect(find.textContaining('Resend available in 01:00'), findsOneWidget);
    });
  }

  testWidgets('unverified login offers verify and resend with retained email', (
    tester,
  ) async {
    final gateway = FakeAuthGateway()
      ..loginError = const ApiException(
        'Your email has not been verified.',
        statusCode: 403,
        code: 'EMAIL_NOT_VERIFIED',
      );
    await _pumpApp(tester, gateway);
    await tester.enterText(
      find.byKey(const Key('login-email')),
      'dual@example.com',
    );
    await tester.enterText(
      find.byKey(const Key('login-password')),
      'Password123!',
    );
    await tester.tap(find.byKey(const Key('login-submit')));
    await tester.pumpAndSettle();

    expect(find.text('Your email is not verified yet.'), findsOneWidget);
    expect(find.text('Verify your email to continue.'), findsOneWidget);
    expect(find.text('dual@example.com'), findsWidgets);
    expect(find.byKey(const Key('unverified-verify')), findsOneWidget);
    expect(find.byKey(const Key('unverified-resend')), findsOneWidget);

    await tester.tap(find.byKey(const Key('unverified-verify')));
    await tester.pumpAndSettle();
    expect(
      find.text('Enter the six-digit code sent to dual@example.com.'),
      findsOneWidget,
    );
  });

  testWidgets(
    'OTP accepts pasted digits, enables verify, and errors remain retryable',
    (tester) async {
      final gateway = FakeAuthGateway()
        ..loginError = const ApiException(
          'Your email has not been verified.',
          statusCode: 403,
          code: 'EMAIL_NOT_VERIFIED',
        )
        ..verifyError = const ApiException(
          'The code is invalid. Please try again.',
          statusCode: 400,
          code: 'EMAIL_VERIFICATION_CODE_INVALID',
        );
      await _pumpApp(tester, gateway);
      await _openVerification(tester, 'buyer@example.com');

      final submit = find.byKey(const Key('verify-email-submit'));
      expect(tester.widget<FilledButton>(submit).onPressed, isNull);
      await tester.enterText(
        find.byKey(const Key('verification-code')),
        '12a34567',
      );
      await tester.pump();
      expect(find.text('123456'), findsOneWidget);
      expect(tester.widget<FilledButton>(submit).onPressed, isNotNull);
      await tester.tap(submit);
      await tester.pumpAndSettle();

      expect(gateway.verifiedEmail, 'buyer@example.com');
      expect(gateway.verifiedCode, '123456');
      expect(
        find.text('The code is invalid. Please try again.'),
        findsOneWidget,
      );
      expect(
        tester
            .widget<TextField>(find.byKey(const Key('verification-code')))
            .controller!
            .text,
        '123456',
      );
      expect(tester.widget<FilledButton>(submit).onPressed, isNotNull);
    },
  );

  testWidgets('resend confirms success only after API and starts cooldown', (
    tester,
  ) async {
    final gateway = FakeAuthGateway()
      ..loginError = const ApiException(
        'Unverified',
        statusCode: 403,
        code: 'EMAIL_NOT_VERIFIED',
      );
    await _pumpApp(tester, gateway);
    await _openVerification(tester, 'seller@example.com');
    await tester.tap(find.byKey(const Key('verification-resend')));
    await tester.pump();

    expect(gateway.resentEmail, 'seller@example.com');
    expect(find.textContaining('Resend available in 01:00'), findsOneWidget);
    expect(
      tester
          .widget<TextButton>(find.byKey(const Key('verification-resend')))
          .onPressed,
      isNull,
    );
  });

  testWidgets('expired OTP shows a friendly error and remains editable', (
    tester,
  ) async {
    final gateway = FakeAuthGateway()
      ..loginError = const ApiException(
        'Unverified',
        statusCode: 403,
        code: 'EMAIL_NOT_VERIFIED',
      )
      ..verifyError = const ApiException(
        'This code has expired.',
        statusCode: 400,
        code: 'EMAIL_VERIFICATION_CODE_EXPIRED',
      );
    await _pumpApp(tester, gateway);
    await _openVerification(tester, 'buyer@example.com');
    await tester.enterText(
      find.byKey(const Key('verification-code')),
      '123456',
    );
    await tester.pump();
    await tester.tap(find.byKey(const Key('verify-email-submit')));
    await tester.pumpAndSettle();

    expect(find.text('This code has expired.'), findsOneWidget);
    await tester.enterText(
      find.byKey(const Key('verification-code')),
      '654321',
    );
    await tester.pump();
    expect(
      tester
          .widget<FilledButton>(find.byKey(const Key('verify-email-submit')))
          .onPressed,
      isNotNull,
    );
  });

  testWidgets('SMTP resend failure shows error without a fake cooldown', (
    tester,
  ) async {
    final gateway = FakeAuthGateway()
      ..loginError = const ApiException(
        'Unverified',
        statusCode: 403,
        code: 'EMAIL_NOT_VERIFIED',
      )
      ..resendError = const ApiException(
        'We could not send the email. Please try again shortly.',
        statusCode: 503,
        code: 'EMAIL_DELIVERY_FAILED',
      );
    await _pumpApp(tester, gateway);
    await _openVerification(tester, 'seller@example.com');
    await tester.tap(find.byKey(const Key('verification-resend')));
    await tester.pumpAndSettle();

    expect(
      find.text('We could not send the email. Please try again shortly.'),
      findsOneWidget,
    );
    expect(find.textContaining('Resend available in'), findsNothing);
    expect(
      tester
          .widget<TextButton>(find.byKey(const Key('verification-resend')))
          .onPressed,
      isNotNull,
    );
  });

  testWidgets(
    'successful verification returns to sign in with email retained',
    (tester) async {
      final gateway = FakeAuthGateway()
        ..loginError = const ApiException(
          'Unverified',
          statusCode: 403,
          code: 'EMAIL_NOT_VERIFIED',
        );
      await _pumpApp(tester, gateway);
      await _openVerification(tester, 'buyer@example.com');
      await tester.enterText(
        find.byKey(const Key('verification-code')),
        '654321',
      );
      await tester.pump();
      await tester.tap(find.byKey(const Key('verify-email-submit')));
      await tester.pump(const Duration(milliseconds: 100));
      expect(find.text('Email verified successfully'), findsOneWidget);
      await tester.pump(const Duration(milliseconds: 950));
      await tester.pumpAndSettle();

      expect(find.text('Welcome to SurplusLink'), findsOneWidget);
      expect(
        tester
            .widget<TextFormField>(find.byKey(const Key('login-email')))
            .controller!
            .text,
        'buyer@example.com',
      );
    },
  );

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
    await tester.tap(find.text('Log Out'));
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
  tester.view.physicalSize = const Size(1000, 1800);
  tester.view.devicePixelRatio = 1;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);
  await tester.pumpWidget(
    TickerMode(
      enabled: false,
      child: SurplusLinkApp(
        authController: AuthController(gateway),
        initialLocation: initialLocation,
      ),
    ),
  );
  await tester.pumpAndSettle();
}

Future<void> _openVerification(WidgetTester tester, String email) async {
  await tester.enterText(find.byKey(const Key('login-email')), email);
  await tester.enterText(
    find.byKey(const Key('login-password')),
    'Password123!',
  );
  await tester.tap(find.byKey(const Key('login-submit')));
  await tester.pumpAndSettle();
  await tester.tap(find.byKey(const Key('unverified-verify')));
  await tester.pumpAndSettle();
}
