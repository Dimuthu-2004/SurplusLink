import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/app.dart';
import 'package:mobile/auth/auth_controller.dart';
import 'package:mobile/handoff/qr_payload_parser.dart';
import 'package:mobile/handoff/mobile_handoff_gateway.dart';
import 'package:mobile/handoff/mobile_handoff_models.dart';
import 'package:mobile/requirements/requirement_models.dart';
import 'package:mobile/screens/qr_scanner_screen.dart';

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
          categoryName: 'Timber',
          source: 'REACT_MARKETPLACE',
          redeemedAt: DateTime.utc(2026, 9, 26),
        );
  }
}

void main() {
  group('QrPayloadParser', () {
    test('extracts code from SLH1 versioned payload', () {
      expect(QrPayloadParser.parse('SLH1:abc-123-xyz'), 'abc-123-xyz');
      expect(QrPayloadParser.parse('  SLH1:my_code_999  '), 'my_code_999');
    });

    test('extracts code from custom scheme deep links', () {
      expect(
        QrPayloadParser.parse('surpluslink://handoff/code-from-path'),
        'code-from-path',
      );
      expect(
        QrPayloadParser.parse('surpluslink://handoff?code=code-from-query'),
        'code-from-query',
      );
    });

    test('extracts code from raw valid handoff code', () {
      expect(QrPayloadParser.parse('SLH-123456'), 'SLH-123456');
      expect(QrPayloadParser.parse('1a2b3c4d5e6f'), '1a2b3c4d5e6f');
    });

    test('rejects invalid or unsafe payloads', () {
      expect(QrPayloadParser.parse(null), isNull);
      expect(QrPayloadParser.parse(''), isNull);
      expect(QrPayloadParser.parse('   '), isNull);
      expect(QrPayloadParser.parse('https://evil.com'), isNull);
      expect(QrPayloadParser.parse('SLH1:'), isNull);
      expect(QrPayloadParser.parse('short'), isNull);
      expect(QrPayloadParser.parse('invalid chars!@#'), isNull);
    });
  });

  group('QrScannerScreen Widget', () {
    testWidgets('renders title, instructions, and manual entry action', (tester) async {
      final authGateway = FakeAuthGateway()..restoredUser = buyerUser;
      final authController = AuthController(authGateway);

      await tester.pumpWidget(
        MaterialApp(
          home: QrScannerScreen(
            authController: authController,
            scannerWidget: const Center(child: Text('Simulated Scanner Area')),
          ),
        ),
      );
      await tester.pumpAndSettle();

      expect(find.text('Scan Web Handoff QR'), findsOneWidget);
      expect(
        find.text('Align the QR code from the SurplusLink web marketplace within the frame'),
        findsOneWidget,
      );
      expect(find.text('Enter code manually'), findsOneWidget);
    });

    testWidgets('manual code entry triggers callback with parsed code', (tester) async {
      final authGateway = FakeAuthGateway()..restoredUser = buyerUser;
      final authController = AuthController(authGateway);
      String? detectedCode;

      await tester.pumpWidget(
        MaterialApp(
          home: QrScannerScreen(
            authController: authController,
            scannerWidget: const Center(child: Text('Simulated Scanner Area')),
            onCodeDetected: (code) => detectedCode = code,
          ),
        ),
      );
      await tester.pumpAndSettle();

      // Tap Enter code manually
      await tester.tap(find.byKey(const Key('scan-qr-manual-btn')));
      await tester.pumpAndSettle();

      expect(find.text('Enter Handoff Code'), findsOneWidget);

      // Enter SLH1:code-manual-test
      await tester.enterText(
        find.byKey(const Key('scan-qr-manual-field')),
        'SLH1:code-manual-test',
      );
      await tester.tap(find.byKey(const Key('scan-qr-manual-submit')));
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 100));

      expect(detectedCode, 'code-manual-test');
    });

    testWidgets('unauthenticated user can open scanner from login screen', (tester) async {
      final authGateway = FakeAuthGateway();
      final authController = AuthController(authGateway);

      await tester.pumpWidget(
        SurplusLinkApp(
          authController: authController,
          initialLocation: '/login',
        ),
      );
      await tester.pumpAndSettle();

      expect(find.text('Welcome to SurplusLink'), findsOneWidget);
      expect(find.byKey(const Key('login-scan-qr')), findsOneWidget);

      // Tap Scan Web Handoff QR
      await tester.tap(find.byKey(const Key('login-scan-qr')));
      await tester.pumpAndSettle();

      expect(find.text('Scan Web Handoff QR'), findsOneWidget);
    });

    testWidgets('authenticated buyer can open scanner from home quick actions', (tester) async {
      final authGateway = FakeAuthGateway()..restoredUser = buyerUser;
      final authController = AuthController(authGateway);
      final reqGateway = FakeRequirements();

      await tester.pumpWidget(
        SurplusLinkApp(
          authController: authController,
          requirementGateway: reqGateway,
          initialLocation: '/home',
        ),
      );
      await tester.pumpAndSettle();

      await tester.drag(
        find.byKey(const Key('home-dashboard-scroll')),
        const Offset(0, -500),
      );
      await tester.pumpAndSettle();

      expect(find.byKey(const Key('home-scan-web-qr')), findsOneWidget);

      await tester.tap(find.byKey(const Key('home-scan-web-qr')));
      await tester.pumpAndSettle();

      expect(find.text('Scan Web Handoff QR'), findsOneWidget);
    });

    testWidgets('end-to-end: scanned handoff redeems and prefills requirement category', (tester) async {
      tester.view.physicalSize = const Size(800, 1600);
      tester.view.devicePixelRatio = 1;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);

      final authGateway = FakeAuthGateway()..restoredUser = buyerUser;
      final handoffGateway = FakeMobileHandoffGateway(
        redemption: MobileHandoffRedemption(
          id: 'handoff-e2e',
          categoryId: 'c-timber',
          categoryName: 'Structural Timber',
          source: 'REACT_MARKETPLACE',
          redeemedAt: DateTime.utc(2026, 9, 26),
        ),
      );
      final reqGateway = FakeRequirements()
        ..categoryItems = [
          const RequirementCategory('c-timber', 'Structural Timber'),
          const RequirementCategory('c-cement', 'Cement'),
        ]
        ..unitItems = ['pcs', 'm'];

      await tester.pumpWidget(
        SurplusLinkApp(
          authController: AuthController(authGateway),
          handoffGateway: handoffGateway,
          requirementGateway: reqGateway,
          initialLocation: '/scan-qr',
        ),
      );
      await tester.pumpAndSettle();

      // Enter code via manual entry dialog on scanner screen
      await tester.tap(find.byKey(const Key('scan-qr-manual-btn')));
      await tester.pumpAndSettle();

      await tester.enterText(
        find.byKey(const Key('scan-qr-manual-field')),
        'SLH1:e2e-timber-code',
      );
      await tester.tap(find.byKey(const Key('scan-qr-manual-submit')));
      await tester.pumpAndSettle();

      // Handoff redeemed
      expect(handoffGateway.callCount, 1);
      expect(handoffGateway.lastCode, 'e2e-timber-code');

      // Requirement screen opened with prefilled Structural Timber
      expect(find.text('Create Requirement'), findsOneWidget);
      expect(find.text('Structural Timber'), findsOneWidget);
    });
  });
}
