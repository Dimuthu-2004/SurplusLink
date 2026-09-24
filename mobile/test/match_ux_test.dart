import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/core/api_exception.dart';
import 'package:mobile/matches/match_formatters.dart';
import 'package:mobile/matches/match_models.dart';
import 'package:mobile/screens/match_details_screen.dart';

import 'recommended_matches_test.dart' show FakeMatches;

RecommendedMatch sample({
  String status = 'ROUTED',
  String? reason,
  String? workflow,
}) => RecommendedMatch(
  id: 'technical-match',
  requirementId: 'technical-requirement',
  listingId: 'technical-listing',
  sellerId: 'technical-seller',
  score: .8,
  status: status,
  createdAt: DateTime(2026, 9, 22, 16, 59),
  materialTitle: 'Bricks',
  categoryName: 'Masonry',
  quantity: 400,
  unit: 'pcs',
  unitPrice: 3,
  availableQuantity: 500,
  maximumBudget: 2000,
  availableUntil: DateTime(2026, 9, 22),
  requiredBy: DateTime(2026, 9, 24),
  rejectionReason: reason,
  requirementStatus: workflow,
);

class DetailsFake extends FakeMatches {
  RecommendedMatch value = sample();
  final historyPages = <int>[];
  @override
  Future<RecommendedMatch> get(String requirementId, String matchId) async {
    if (error != null) throw error!;
    return value;
  }

  @override
  Future<MatchPage<MatchHistoryEntry>> history(
    String matchId, {
    int page = 1,
  }) async {
    historyPages.add(page);
    return MatchPage(
      items: [
        MatchHistoryEntry(
          id: 'history-$page',
          action: page == 1 ? 'GENERATE' : 'ROUTE',
          outcome: page == 1 ? null : 'FAILED',
          createdAt: DateTime(2026, 9, 22, 16, 59),
        ),
      ],
      total: 2,
      page: page,
      pageSize: 1,
      totalPages: 2,
    );
  }
}

Future<void> showDetails(WidgetTester tester, DetailsFake gateway) async {
  await tester.binding.setSurfaceSize(const Size(800, 3000));
  addTearDown(() => tester.binding.setSurfaceSize(null));
  await tester.pumpWidget(
    MaterialApp(
      home: MatchDetailsScreen(
        gateway: gateway,
        requirementId: 'r1',
        matchId: 'm1',
      ),
    ),
  );
  await tester.pumpAndSettle();
}

void main() {
  test('currency, quantity, unit, dates and history are readable', () {
    expect(formatCurrency(1200), 'LKR 1,200.00');
    expect(formatCurrency(1200, decimal: false), 'LKR 1,200');
    expect(formatCurrency(null), 'Not available');
    expect(formatQuantity(400.0), '400');
    expect(formatQuantity(400.5), '400.5');
    expect(formatUnit('m2'), 'm²');
    expect(formatUnit('l'), 'L');
    expect(formatMatchDate(DateTime(2026, 9, 22)), '22 Sep 2026');
    expect(
      formatMatchDateTime(DateTime(2026, 9, 22, 16, 59)),
      '22 Sep 2026, 4:59 PM',
    );
    expect(formatMatchDate(null), 'Not available');
    for (final entry in {
      'GENERATE': 'Match candidate created',
      'RANK': 'Match ranked',
      'ROUTE_SUCCEEDED': 'Delivery route evaluated',
      'ROUTE_FAILED': 'Delivery route failed',
      'REJECT': 'Match rejected',
      'VALIDATE': 'Match validated',
    }.entries) {
      expect(readableHistoryAction(entry.key), entry.value);
    }
    expect(
      readableHistoryAction('ROUTE', outcome: 'FAILED'),
      'Delivery route failed',
    );
    expect(readableRejectionReason('unknown'), 'Unable to use this match.');
  });
  test('routing follows persisted status instead of missing distance', () {
    expect(
      getRoutingState(sample(status: 'GENERATED')),
      RoutingUiState.notEvaluated,
    );
    expect(
      getRoutingState(sample(status: 'RANKED')),
      RoutingUiState.notEvaluated,
    );
    expect(
      getRoutingState(
        sample(status: 'REJECTED', reason: 'INSUFFICIENT_QUANTITY'),
      ),
      RoutingUiState.notEvaluated,
    );
    expect(getRoutingState(sample()), RoutingUiState.available);
    expect(
      getRoutingState(sample(status: 'ROUTE_FAILED')),
      RoutingUiState.failed,
    );
  });
  test('API fields parse once and malformed optional fields fail safely', () {
    final json = <String, dynamic>{
      'id': 'm',
      'requirementId': 'r',
      'listingId': 'l',
      'score': .8,
      'status': 'RANKED',
      'createdAt': '2026-09-22T00:00:00Z',
      'valid': true,
      'rejected': false,
      'quantity': 400,
      'unitPrice': 3,
      'availableUntil': null,
    };
    final parsed = RecommendedMatch.fromJson(json);
    expect(parsed.valid, true);
    expect(parsed.rejected, false);
    expect(parsed.estimatedMaterialCost, 1200);
    expect(parsed.availableUntil, isNull);
    for (final entry in {
      'quantity': 'bad',
      'availableUntil': 'bad',
      'materialTitle': 123,
      'valid': 'yes',
      'score': double.nan,
    }.entries) {
      expect(
        () => RecommendedMatch.fromJson({...json, entry.key: entry.value}),
        throwsFormatException,
      );
    }
    expect(sample().estimatedMaterialCost, 1200);
  });
  testWidgets(
    'valid match shows summary, evaluation and collapsed technical IDs',
    (tester) async {
      await showDetails(
        tester,
        DetailsFake()..value = sample(workflow: 'MATCHING'),
      );
      expect(find.text('Bricks'), findsOneWidget);
      expect(find.text('LKR 1,200.00'), findsOneWidget);
      expect(find.text('500 Pcs'), findsOneWidget);
      expect(find.text('LKR 2,000.00'), findsOneWidget);
      expect(find.text('Matching and evaluation in progress.'), findsOneWidget);
      expect(find.text('Waiting for manager approval'), findsNothing);
      expect(find.textContaining('technical-match'), findsNothing);
      await tester.tap(find.byKey(const Key('technical-details-tile')));
      await tester.pumpAndSettle();
      expect(find.text('Match ID: technical-match'), findsOneWidget);
      expect(find.text('Seller ID: technical-seller'), findsOneWidget);
    },
  );
  for (final workflow in ['PENDING_APPROVAL', 'APPROVED']) {
    testWidgets('workflow label uses $workflow', (tester) async {
      await showDetails(
        tester,
        DetailsFake()..value = sample(workflow: workflow),
      );
      expect(
        find.text(
          workflow == 'APPROVED'
              ? 'Requirement approved.'
              : 'Waiting for manager approval',
        ),
        findsOneWidget,
      );
    });
  }
  testWidgets('expiry rejection explains both dates without exposing code', (
    tester,
  ) async {
    await showDetails(
      tester,
      DetailsFake()
        ..value = sample(
          status: 'REJECTED',
          reason: 'LISTING_EXPIRES_BEFORE_DELIVERY',
        ),
    );
    expect(find.text('Why this match was rejected'), findsOneWidget);
    expect(
      find.text(readableRejectionReason('LISTING_EXPIRES_BEFORE_DELIVERY')),
      findsOneWidget,
    );
    expect(find.text('Available until: 22 Sep 2026'), findsOneWidget);
    expect(find.text('Required by: 24 Sep 2026'), findsOneWidget);
    expect(
      find.textContaining('LISTING_EXPIRES_BEFORE_DELIVERY'),
      findsNothing,
    );
    expect(
      find.text('Delivery route has not been evaluated yet.'),
      findsOneWidget,
    );
  });
  testWidgets('route failure and paginated history use readable text', (
    tester,
  ) async {
    final fake = DetailsFake()..value = sample(status: 'ROUTE_FAILED');
    await showDetails(tester, fake);
    expect(
      find.text(
        'Delivery route could not be calculated. Please try again later.',
      ),
      findsOneWidget,
    );
    expect(find.text('Match candidate created'), findsOneWidget);
    await tester.ensureVisible(find.text('Next'));
    await tester.tap(find.text('Next'));
    await tester.pumpAndSettle();
    expect(fake.historyPages, [1, 2]);
    expect(find.text('Delivery route failed'), findsNWidgets(2));
    expect(find.textContaining('22 Sep 2026, 4:59 PM'), findsOneWidget);
  });
  for (final entry in <Object, String>{
    const ApiException('Denied', statusCode: 403):
        'You do not have access to this match.',
    const ApiException('Missing', statusCode: 404): 'Match not found.',
    const ApiException('Offline'): 'Unable to load match details.',
    const ApiException('Server error', statusCode: 500):
        'Unable to load match details.',
    const FormatException('Bad data'):
        'The server returned invalid match data. Please retry.',
  }.entries) {
    testWidgets('details error ${entry.key} retries successfully', (
      tester,
    ) async {
      final fake = DetailsFake()..error = entry.key;
      await showDetails(tester, fake);
      expect(find.text(entry.value), findsOneWidget);
      fake.error = null;
      await tester.tap(find.text('Retry'));
      await tester.pumpAndSettle();
      expect(find.text('Bricks'), findsOneWidget);
    });
  }
}
