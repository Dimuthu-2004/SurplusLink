import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/app.dart';
import 'package:mobile/auth/auth_controller.dart';
import 'package:mobile/core/api_exception.dart';
import 'package:mobile/handoff/mobile_handoff_gateway.dart';
import 'package:mobile/handoff/mobile_handoff_models.dart';
import 'package:mobile/requirements/requirement_models.dart';

import 'support/fakes.dart';
import 'support/requirement_fakes.dart';

class FakeMobileHandoffGateway implements MobileHandoffGateway {
  FakeMobileHandoffGateway({this.redemption, this.error});

  MobileHandoffRedemption? redemption;
  Object? error;
  String? lastCode;
  int callCount = 0;

  @override
  Future<MobileHandoffRedemption> redeem(String code) async {
    callCount++;
    lastCode = code;
    if (error != null) {
      throw error!;
    }
    return redemption ??
        MobileHandoffRedemption(
          id: 'h1',
          categoryId: 'c1',
          categoryName: 'Cement',
          source: 'REACT_MARKETPLACE',
          redeemedAt: DateTime.utc(2026, 9, 26),
        );
  }
}

void main() {
  testWidgets('valid same-user handoff redeems code and prefills category in requirement screen', (
    tester,
  ) async {
    tester.view.physicalSize = const Size(800, 1600);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    final authGateway = FakeAuthGateway()..restoredUser = buyerUser;
    final handoffGateway = FakeMobileHandoffGateway(
      redemption: MobileHandoffRedemption(
        id: 'handoff-abc',
        categoryId: 'c1',
        categoryName: 'Cement',
        source: 'REACT_MARKETPLACE',
        redeemedAt: DateTime.utc(2026, 9, 26),
      ),
    );
    final reqGateway = FakeRequirements()
      ..categoryItems = [
        const RequirementCategory('c1', 'Cement'),
        const RequirementCategory('c2', 'Steel'),
      ]
      ..unitItems = ['kg', 'bags'];

    await tester.pumpWidget(
      SurplusLinkApp(
        authController: AuthController(authGateway),
        handoffGateway: handoffGateway,
        requirementGateway: reqGateway,
        initialLocation: '/handoff/code-123',
      ),
    );
    await tester.pumpAndSettle();

    // Verify handoff was called with code
    expect(handoffGateway.callCount, 1);
    expect(handoffGateway.lastCode, 'code-123');

    // Verify requirement form screen is open
    expect(find.text('Create Requirement'), findsOneWidget);

    // Verify category was prefilled to Cement
    expect(find.text('Cement'), findsOneWidget);

    // Verify user must still fill in quantity and other normal requirement fields
    expect(find.byKey(const Key('requirement-quantity')), findsOneWidget);
    expect(find.byKey(const Key('requirement-budget')), findsOneWidget);
  });

  testWidgets('guest opening handoff is prompted to log in then continues to prefilled requirement', (
    tester,
  ) async {
    final authGateway = FakeAuthGateway()..loginUser = buyerUser;
    final handoffGateway = FakeMobileHandoffGateway(
      redemption: MobileHandoffRedemption(
        id: 'handoff-xyz',
        categoryId: 'c2',
        categoryName: 'Steel',
        source: 'REACT_MARKETPLACE',
        redeemedAt: DateTime.utc(2026, 9, 26),
      ),
    );
    final reqGateway = FakeRequirements()
      ..categoryItems = [
        const RequirementCategory('c1', 'Cement'),
        const RequirementCategory('c2', 'Steel'),
      ]
      ..unitItems = ['pcs'];

    await tester.pumpWidget(
      SurplusLinkApp(
        authController: AuthController(authGateway),
        handoffGateway: handoffGateway,
        requirementGateway: reqGateway,
        initialLocation: '/handoff/pending-code',
      ),
    );
    await tester.pumpAndSettle();

    // Unauthenticated guest sees login
    expect(find.text('Welcome to SurplusLink'), findsOneWidget);
    expect(handoffGateway.callCount, 0);

    // User logs in
    await tester.enterText(
      find.byKey(const Key('login-email')),
      'buyer@example.com',
    );
    await tester.enterText(
      find.byKey(const Key('login-password')),
      'Password123!',
    );
    await tester.tap(find.byKey(const Key('login-submit')));
    await tester.pumpAndSettle();

    // After login, router resumes to handoff code, redeems, and opens requirement form
    expect(handoffGateway.callCount, 1);
    expect(handoffGateway.lastCode, 'pending-code');
    expect(find.text('Create Requirement'), findsOneWidget);
    expect(find.text('Steel'), findsOneWidget);
  });

  testWidgets('wrong account shows account mismatch error and safe switch option', (
    tester,
  ) async {
    final authGateway = FakeAuthGateway()..restoredUser = buyerUser;
    final handoffGateway = FakeMobileHandoffGateway(
      error: const ApiException(
        'This handoff belongs to another account.',
        statusCode: 403,
      ),
    );

    await tester.pumpWidget(
      SurplusLinkApp(
        authController: AuthController(authGateway),
        handoffGateway: handoffGateway,
        initialLocation: '/handoff/foreign-code',
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('Account Mismatch'), findsOneWidget);
    expect(
      find.text('This handoff belongs to another account.'),
      findsOneWidget,
    );
    expect(find.text('Log Out & Switch Account'), findsOneWidget);
    expect(find.text('Continue to Dashboard'), findsOneWidget);

    // Clicking Continue to Dashboard goes to home without crashing
    await tester.tap(find.text('Continue to Dashboard'));
    await tester.pumpAndSettle();

    expect(find.text('Account Mismatch'), findsNothing);
  });

  testWidgets('expired or invalid token shows friendly error message and dashboard link', (
    tester,
  ) async {
    final authGateway = FakeAuthGateway()..restoredUser = buyerUser;
    final handoffGateway = FakeMobileHandoffGateway(
      error: const ApiException(
        'This handoff has expired.',
        statusCode: 400,
      ),
    );

    await tester.pumpWidget(
      SurplusLinkApp(
        authController: AuthController(authGateway),
        handoffGateway: handoffGateway,
        initialLocation: '/handoff/expired-code',
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('Handoff Issue'), findsOneWidget);
    expect(find.text('This handoff has expired.'), findsOneWidget);
    expect(find.text('Continue to Dashboard'), findsOneWidget);

    await tester.tap(find.text('Continue to Dashboard'));
    await tester.pumpAndSettle();

    expect(find.text('Handoff Issue'), findsNothing);
  });

  testWidgets('normal app flow still works with no handoff', (tester) async {
    final authGateway = FakeAuthGateway()..restoredUser = buyerUser;
    final reqGateway = FakeRequirements();

    await tester.pumpWidget(
      SurplusLinkApp(
        authController: AuthController(authGateway),
        requirementGateway: reqGateway,
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('Buyer'), findsOneWidget);
  });
}
