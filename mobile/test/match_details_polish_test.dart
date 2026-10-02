import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:mobile/core/api_exception.dart';
import 'package:mobile/matches/match_models.dart';
import 'package:mobile/matches/match_widgets.dart';
import 'package:mobile/screens/match_details_screen.dart';
import 'package:mobile/theme/surplus_link_theme.dart';

import 'match_ux_test.dart' show DetailsFake;

RecommendedMatch candidate([Map<String, dynamic> overrides = const {}]) =>
    RecommendedMatch.fromJson({
      'id': 'm1',
      'requirementId': 'r1',
      'listingId': 'l1',
      'score': .876543,
      'status': 'ROUTED',
      'valid': true,
      'createdAt': '2026-09-24T00:00:00Z',
      'requirementStatus': 'MATCH_FOUND',
      'materialTitle': 'Bricks',
      'sellerBusinessName': 'Colombo Supplies',
      'sellerName': 'Seller',
      'sellerAddress': 'Colombo',
      'condition': 'Good',
      'quantity': 400,
      'availableQuantity': 500,
      'unit': 'pcs',
      'unitPrice': 3,
      'distance': 33.23456789,
      'durationMinutes': 39.123456,
      'estimatedTransportCost': 1234.56789,
      'sellerPhone': '0771234567',
      'sellerEmail': 'private@example.com',
      ...overrides,
    });

class SelectionFake extends DetailsFake {
  final selections = <String>[];
  Object? selectionError;
  Completer<void>? pendingSelection;
  @override
  Future<void> select(String requirementId, String matchId, {double? quantity, int? packageCount}) async {
    selections.add('$requirementId/$matchId');
    if (selectionError != null) throw selectionError!;
    if (pendingSelection != null) await pendingSelection!.future;
  }
}

Future<void> show(
  WidgetTester tester,
  SelectionFake fake, {
  bool reducedMotion = false,
  Size size = const Size(800, 3000),
  Future<bool> Function(Uri)? launchMap,
}) async {
  await tester.binding.setSurfaceSize(size);
  addTearDown(() => tester.binding.setSurfaceSize(null));
  final router = GoRouter(
    initialLocation: '/details',
    routes: [
      GoRoute(
        path: '/details',
        builder: (_, _) => MatchDetailsScreen(
          gateway: fake,
          requirementId: 'r1',
          matchId: 'm1',
          launchMap: launchMap ?? launchExternalMap,
        ),
      ),
      GoRoute(
        path: '/requirements/r1',
        builder: (_, _) =>
            const Scaffold(body: Text('Requirement destination')),
      ),
    ],
  );
  addTearDown(router.dispose);
  await tester.pumpWidget(
    MaterialApp.router(
      theme: SurplusLinkTheme.light,
      routerConfig: router,
      builder: (context, child) => MediaQuery(
        data: MediaQuery.of(context).copyWith(disableAnimations: reducedMotion),
        child: child!,
      ),
    ),
  );
  await tester.pump();
  await tester.pump(const Duration(milliseconds: 100));
}

void main() {
  testWidgets('recommended card keeps the concise display location', (tester) async {
    await tester.pumpWidget(MaterialApp(
      home: Scaffold(body: MatchListCard(
        match: candidate({
          'displayLocation': 'Negombo',
          'fullAddress': 'No. 42, Main Street, Negombo, Gampaha',
        }),
        onTap: () {},
      )),
    ));
    expect(find.text('Negombo'), findsOneWidget);
    expect(find.text('No. 42, Main Street, Negombo, Gampaha'), findsNothing);
  });

  testWidgets('shows the full address and opens coordinates in Google Maps', (tester) async {
    Uri? launched;
    await show(
      tester,
      SelectionFake()..value = candidate({
        'displayLocation': 'Negombo',
        'fullAddress': 'No. 42, Main Street, Negombo, Gampaha',
        'latitude': 7.2083,
        'longitude': 79.8358,
      }),
      launchMap: (uri) async { launched = uri; return true; },
    );
    expect(find.text('No. 42, Main Street, Negombo, Gampaha'), findsOneWidget);
    expect(find.text('Negombo'), findsNothing);
    await tester.tap(find.byKey(const Key('view-location-on-google-maps')));
    await tester.pump();
    expect(launched.toString(), 'https://www.google.com/maps/search/?api=1&query=7.2083%2C79.8358');
  });

  testWidgets('uses the encoded full address when coordinates are unavailable', (tester) async {
    Uri? launched;
    const address = 'No. 42, Main Street, Negombo, Gampaha';
    await show(
      tester,
      SelectionFake()..value = candidate({'fullAddress': address}),
      launchMap: (uri) async { launched = uri; return true; },
    );
    await tester.tap(find.byKey(const Key('view-location-on-google-maps')));
    await tester.pump();
    expect(launched?.queryParameters['query'], address);
  });

  testWidgets('hides the map action when no location is available', (tester) async {
    await show(tester, SelectionFake()..value = candidate({'sellerAddress': null}));
    expect(find.text('Location details unavailable'), findsOneWidget);
    expect(find.byKey(const Key('view-location-on-google-maps')), findsNothing);
  });

  testWidgets('valid details group and format values, keep contact private', (
    tester,
  ) async {
    await show(tester, SelectionFake()..value = candidate());
    for (final label in [
      'Bricks',
      'Colombo Supplies',
      'Valid',
      'Material',
      'Delivery / Logistics',
      'Match evaluation',
      '400 Pcs',
      '500 Pcs',
      '33.2 km',
      '39 min',
      'LKR 1,234.57',
      'LKR 2,434.57',
      'Select this match',
    ]) {
      expect(find.text(label), findsOneWidget);
    }
    await tester.tap(find.byKey(const Key('technical-details-tile')));
    await tester.pumpAndSettle();
    expect(find.textContaining('0771234567'), findsNothing);
    expect(find.textContaining('private@example.com'), findsNothing);
    expect(find.byIcon(Icons.phone), findsNothing);
    expect(find.byKey(const Key('ai-recommended-badge')), findsNothing);
    expect(tester.takeException(), isNull);
  });

  for (final state in [
    {'status': 'REJECTED', 'rejectionReason': 'INSUFFICIENT_QUANTITY'},
    {'status': 'ROUTE_FAILED'},
    {'valid': false},
    {'valid': null},
    {'status': 'RANKED'},
    {'requirementStatus': 'PENDING_APPROVAL'},
    {'requirementStatus': 'APPROVED'},
  ]) {
    testWidgets('selection unavailable for $state', (tester) async {
      await show(tester, SelectionFake()..value = candidate(state));
      expect(find.text('Select this match'), findsNothing);
      if (['REJECTED', 'ROUTE_FAILED'].contains(state['status'])) {
        final warning = tester.widget<Card>(
          find.byKey(const Key('match-warning')),
        );
        expect(
          warning.color,
          SurplusLinkTheme.light.colorScheme.errorContainer,
        );
        expect(find.text('This match cannot be selected.'), findsOneWidget);
        expect(
          find.text(
            state['status'] == 'REJECTED'
                ? 'The seller does not have enough available quantity.'
                : 'Delivery route could not be calculated. Please try again later.',
          ),
          findsOneWidget,
        );
      }
    });
  }

  testWidgets('hours and missing totals are honest and readable', (
    tester,
  ) async {
    await show(
      tester,
      SelectionFake()
        ..value = candidate({'durationMinutes': 75.123, 'unitPrice': null}),
    );
    expect(find.text('1 h 15 min'), findsOneWidget);
    expect(find.text('LKR 1,234.57'), findsOneWidget);
    expect(find.text('Not available'), findsNWidgets(3));
  });

  testWidgets(
    'AI badge has subtle animated outline and supports reduced motion',
    (tester) async {
      final fake = SelectionFake()..value = candidate({'recommendedMatchId': 'm1'});
      await show(tester, fake);
      expect(find.text('AI Recommended'), findsOneWidget);
      BoxDecoration decoration() =>
          tester
                  .widget<Container>(find.byKey(const Key('match-summary')))
                  .decoration!
              as BoxDecoration;
      final first = decoration();
      await tester.pump(const Duration(milliseconds: 1500));
      expect(decoration().border, isNot(first.border));
      expect(decoration().boxShadow, isNotEmpty);
      await show(tester, fake, reducedMotion: true);
      final reduced = decoration();
      await tester.pump(const Duration(seconds: 2));
      expect(decoration(), reduced);
    },
  );

  testWidgets(
    'selection confirms, cancels, and calls existing API once before navigation',
    (tester) async {
      final fake = SelectionFake()
        ..value = candidate()
        ..pendingSelection = Completer<void>();
      await show(tester, fake);
      await tester.tap(find.text('Select this match'));
      await tester.pumpAndSettle();
      expect(fake.selections, isEmpty);
      await tester.tap(find.text('Cancel'));
      await tester.pumpAndSettle();
      expect(fake.selections, isEmpty);
      await tester.tap(find.text('Select this match'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Confirm selection'));
      await tester.pumpAndSettle();
      expect(fake.selections, ['r1/m1']);
      expect(
        tester
            .widget<FilledButton>(find.byKey(const Key('select-match')))
            .onPressed,
        isNull,
      );
      fake.pendingSelection!.complete();
      await tester.pumpAndSettle();
      expect(find.text('Requirement destination'), findsOneWidget);
    },
  );

  testWidgets('server revalidation failure remains visible on details', (
    tester,
  ) async {
    final fake = SelectionFake()
      ..value = candidate()
      ..selectionError = const ApiException(
        'This match is no longer valid.',
        statusCode: 409,
      );
    await show(tester, fake);
    await tester.tap(find.text('Select this match'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Confirm selection'));
    await tester.pumpAndSettle();
    expect(find.text('This match is no longer valid.'), findsOneWidget);
    expect(find.text('Requirement destination'), findsNothing);
  });

  testWidgets('narrow layout remains usable', (tester) async {
    await show(
      tester,
      SelectionFake()..value = candidate(),
      size: const Size(320, 700),
    );
    await tester.pumpAndSettle();
    expect(find.text('Select this match'), findsOneWidget);
    await tester.scrollUntilVisible(
      find.text('Total estimated cost'),
      250,
      scrollable: find.byType(Scrollable).first,
    );
    expect(tester.takeException(), isNull);
  });
}
