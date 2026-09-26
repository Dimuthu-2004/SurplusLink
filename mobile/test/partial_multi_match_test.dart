import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:mobile/matches/match_gateway.dart';
import 'package:mobile/matches/match_models.dart';
import 'package:mobile/screens/match_details_screen.dart';
import 'package:mobile/screens/recommended_matches_screen.dart';
import 'package:mobile/theme/surplus_link_theme.dart';

class FakeMultiMatchGateway implements MatchGateway {
  List<RecommendedMatch> matches = [];
  final selectedSingleMatches = <String>[];
  final selectedSingleQuantities = <String, double?>{};
  final List<List<MatchAllocation>> submittedAllocations = [];
  Object? selectError;
  Object? selectMatchesError;

  @override
  Future<MatchPage<RecommendedMatch>> list(
    String requirementId,
    MatchQuery query,
  ) async =>
      MatchPage(
        items: matches,
        total: matches.length,
        page: query.page,
        pageSize: query.pageSize,
        totalPages: (matches.length / query.pageSize).ceil(),
      );

  @override
  Future<RecommendedMatch> get(String requirementId, String matchId) async =>
      matches.firstWhere((m) => m.id == matchId);

  @override
  Future<void> select(
    String requirementId,
    String matchId, {
    double? quantity,
  }) async {
    selectedSingleMatches.add(matchId);
    selectedSingleQuantities[matchId] = quantity;
    if (selectError != null) throw selectError!;
  }

  @override
  Future<void> selectMatches(
    String requirementId,
    List<MatchAllocation> allocations,
  ) async {
    submittedAllocations.add(allocations);
    if (selectMatchesError != null) throw selectMatchesError!;
  }

  @override
  Future<void> cancelPendingApproval(String requirementId) async {}

  @override
  Future<MatchPage<MatchHistoryEntry>> history(
    String matchId, {
    int page = 1,
  }) async =>
      const MatchPage(
        items: [],
        total: 0,
        page: 1,
        pageSize: 20,
        totalPages: 0,
      );
}

RecommendedMatch createTestMatch({
  required String id,
  required String requirementId,
  required String listingId,
  required String sellerName,
  required double requiredQuantity,
  required double availableQuantity,
  required String unit,
  double unitPrice = 100.0,
  double estimatedTransportCost = 500.0,
  String status = 'ROUTED',
  String requirementStatus = 'MATCH_FOUND',
  bool valid = true,
  bool rejected = false,
  double score = 0.9,
}) =>
    RecommendedMatch(
      id: id,
      requirementId: requirementId,
      listingId: listingId,
      sellerName: sellerName,
      sellerBusinessName: '$sellerName Store',
      quantity: requiredQuantity,
      availableQuantity: availableQuantity,
      unit: unit,
      unitPrice: unitPrice,
      estimatedTransportCost: estimatedTransportCost,
      status: status,
      requirementStatus: requirementStatus,
      valid: valid,
      rejected: rejected,
      score: score,
      createdAt: DateTime.utc(2026, 9, 26),
    );

void main() {
  group('Partial fulfillment models and formatting', () {
    test('MatchAllocation serializes to JSON correctly', () {
      const allocation = MatchAllocation(matchId: 'm1', quantity: 20.0);
      expect(allocation.toJson(), {'matchId': 'm1', 'quantity': 20.0});
    });

    test('isPartial detects partial vs full quantity', () {
      final partial = createTestMatch(
        id: 'm1',
        requirementId: 'r1',
        listingId: 'l1',
        sellerName: 'Seller A',
        requiredQuantity: 50,
        availableQuantity: 20,
        unit: 'pcs',
      );
      expect(partial.isPartial, isTrue);

      final full = createTestMatch(
        id: 'm2',
        requirementId: 'r1',
        listingId: 'l2',
        sellerName: 'Seller B',
        requiredQuantity: 50,
        availableQuantity: 50,
        unit: 'pcs',
      );
      expect(full.isPartial, isFalse);

      final excess = createTestMatch(
        id: 'm3',
        requirementId: 'r1',
        listingId: 'l3',
        sellerName: 'Seller C',
        requiredQuantity: 50,
        availableQuantity: 100,
        unit: 'pcs',
      );
      expect(excess.isPartial, isFalse);
    });

    test('partialWarning formats required warning text correctly', () {
      final match = createTestMatch(
        id: 'm1',
        requirementId: 'r1',
        listingId: 'l1',
        sellerName: 'Seller A',
        requiredQuantity: 50,
        availableQuantity: 20,
        unit: 'pcs',
      );
      expect(
        match.partialWarning(),
        'You need 50 pcs, but this seller currently has only 20 pcs available.',
      );

      final continuousMatch = createTestMatch(
        id: 'm2',
        requirementId: 'r1',
        listingId: 'l2',
        sellerName: 'Seller B',
        requiredQuantity: 50.5,
        availableQuantity: 20.25,
        unit: 'kg',
      );
      expect(
        continuousMatch.partialWarning(),
        'You need 50.5 kg, but this seller currently has only 20.25 kg available.',
      );
    });
  });

  group('MatchDetailsScreen partial quantity workflow', () {
    testWidgets('shows warning and allows partial quantity selection', (
      tester,
    ) async {
      tester.view.physicalSize = const Size(800, 1600);
      tester.view.devicePixelRatio = 1;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);

      final gateway = FakeMultiMatchGateway();
      gateway.matches = [
        createTestMatch(
          id: 'm1',
          requirementId: 'r1',
          listingId: 'l1',
          sellerName: 'Seller A',
          requiredQuantity: 50,
          availableQuantity: 20,
          unit: 'pcs',
        ),
      ];

      final router = GoRouter(
        initialLocation: '/details',
        routes: [
          GoRoute(
            path: '/details',
            builder: (_, _) => MatchDetailsScreen(
              gateway: gateway,
              requirementId: 'r1',
              matchId: 'm1',
            ),
          ),
          GoRoute(
            path: '/requirements/r1',
            builder: (_, _) => const Scaffold(body: Text('Requirement r1')),
          ),
        ],
      );
      addTearDown(router.dispose);

      await tester.pumpWidget(
        MaterialApp.router(
          theme: SurplusLinkTheme.light,
          routerConfig: router,
        ),
      );
      await tester.pumpAndSettle();

      // Warning is visible
      expect(find.byKey(const Key('partial-quantity-warning')), findsOneWidget);
      expect(
        find.text(
          'You need 50 pcs, but this seller currently has only 20 pcs available.',
        ),
        findsOneWidget,
      );

      // Quantity input defaults to available quantity (20)
      final inputFinder = find.byKey(const Key('details-quantity-input'));
      expect(inputFinder, findsOneWidget);
      expect(find.text('20'), findsWidgets);

      // Tap select
      await tester.tap(find.byKey(const Key('select-match')));
      await tester.pumpAndSettle();

      // Confirm dialog contains partial note
      expect(
        find.textContaining('You selected 20 pcs.'),
        findsOneWidget,
      );

      await tester.tap(find.text('Confirm selection'));
      await tester.pumpAndSettle();

      expect(gateway.selectedSingleMatches, ['m1']);
      expect(gateway.selectedSingleQuantities['m1'], 20.0);
      expect(find.text('Requirement r1'), findsOneWidget);
    });
  });

  group('RecommendedMatchesScreen multi-match selection workflow', () {
    testWidgets(
      'multi-match selection calculates totals, validates, and submits allocations',
      (tester) async {
        tester.view.physicalSize = const Size(900, 2000);
        tester.view.devicePixelRatio = 1;
        addTearDown(tester.view.resetPhysicalSize);
        addTearDown(tester.view.resetDevicePixelRatio);

        final gateway = FakeMultiMatchGateway();
        gateway.matches = [
          createTestMatch(
            id: 'm1',
            requirementId: 'r1',
            listingId: 'l1',
            sellerName: 'Seller A',
            requiredQuantity: 50,
            availableQuantity: 20,
            unit: 'pcs',
            unitPrice: 10,
            estimatedTransportCost: 500,
          ),
          createTestMatch(
            id: 'm2',
            requirementId: 'r1',
            listingId: 'l2',
            sellerName: 'Seller B',
            requiredQuantity: 50,
            availableQuantity: 15,
            unit: 'pcs',
            unitPrice: 12,
            estimatedTransportCost: 400,
          ),
          createTestMatch(
            id: 'm3',
            requirementId: 'r1',
            listingId: 'l3',
            sellerName: 'Seller C',
            requiredQuantity: 50,
            availableQuantity: 30,
            unit: 'pcs',
            unitPrice: 11,
            estimatedTransportCost: 600,
          ),
        ];

        final router = GoRouter(
          initialLocation: '/matches',
          routes: [
            GoRoute(
              path: '/matches',
              builder: (_, _) => RecommendedMatchesScreen(
                gateway: gateway,
                requirementId: 'r1',
              ),
            ),
            GoRoute(
              path: '/requirements/r1',
              builder: (_, _) =>
                  const Scaffold(body: Text('Requirement r1 page')),
            ),
          ],
        );
        addTearDown(router.dispose);

        await tester.pumpWidget(
          MaterialApp.router(
            theme: SurplusLinkTheme.light,
            routerConfig: router,
          ),
        );
        await tester.pumpAndSettle();

        // Check warning banner on partial candidate m1
        expect(find.byKey(const Key('partial-warning-m1')), findsOneWidget);
        expect(
          find.text(
            'You need 50 pcs, but this seller currently has only 20 pcs available.',
          ),
          findsOneWidget,
        );

        // Initially no bottom summary bar
        expect(find.byKey(const Key('multi-match-summary-bar')), findsNothing);

        // Select Seller A (available: 20)
        await tester.tap(find.byKey(const Key('select-checkbox-m1')));
        await tester.pumpAndSettle();

        // Summary bar appears
        expect(find.byKey(const Key('multi-match-summary-bar')), findsOneWidget);
        expect(find.text('Selected: 20 pcs'), findsOneWidget);
        expect(find.text('Remaining: 30 pcs'), findsOneWidget);

        // Select Seller B (available: 15)
        await tester.tap(find.byKey(const Key('select-checkbox-m2')));
        await tester.pumpAndSettle();

        expect(find.text('Selected: 35 pcs'), findsOneWidget);
        expect(find.text('Remaining: 15 pcs'), findsOneWidget);

        // Select Seller C (available: 30, but remaining needed is 15 -> defaults to 15)
        await tester.tap(find.byKey(const Key('select-checkbox-m3')));
        await tester.pumpAndSettle();

        expect(find.text('Selected: 50 pcs'), findsOneWidget);
        expect(find.text('Remaining: 0 pcs'), findsOneWidget);

        // Total cost calculation:
        // A: 20 * 10 + 500 = 700
        // B: 15 * 12 + 400 = 580
        // C: 15 * 11 + 600 = 765
        // Total = 700 + 580 + 765 = 2045.00
        expect(find.text('LKR 2,045.00'), findsOneWidget);

        // Submit button is enabled
        final submitButtonFinder = find.byKey(const Key('submit-selections'));
        expect(submitButtonFinder, findsOneWidget);
        expect(tester.widget<FilledButton>(submitButtonFinder).onPressed, isNotNull);

        // Tap submit
        await tester.tap(submitButtonFinder);
        await tester.pumpAndSettle();

        // Confirmation dialog appears
        expect(find.text('Submit 3 Match Selections?'), findsOneWidget);
        expect(find.textContaining('Seller A Store: 20 pcs'), findsOneWidget);
        expect(find.textContaining('Seller B Store: 15 pcs'), findsOneWidget);
        expect(find.textContaining('Seller C Store: 15 pcs'), findsOneWidget);
        expect(find.text('LKR 2,045.00'), findsWidgets);

        // Confirm
        await tester.tap(find.byKey(const Key('confirm-multi-selection')));
        await tester.pumpAndSettle();

        expect(gateway.submittedAllocations, hasLength(1));
        final submitted = gateway.submittedAllocations.first;
        expect(submitted, hasLength(3));
        expect(submitted.map((a) => a.matchId), ['m1', 'm2', 'm3']);
        expect(submitted.map((a) => a.quantity), [20.0, 15.0, 15.0]);
        expect(find.text('Requirement r1 page'), findsOneWidget);
      },
    );

    testWidgets(
      'over-quantity validation prevents submission when total exceeds requirement',
      (tester) async {
        tester.view.physicalSize = const Size(900, 2000);
        tester.view.devicePixelRatio = 1;
        addTearDown(tester.view.resetPhysicalSize);
        addTearDown(tester.view.resetDevicePixelRatio);

        final gateway = FakeMultiMatchGateway();
        gateway.matches = [
          createTestMatch(
            id: 'm1',
            requirementId: 'r1',
            listingId: 'l1',
            sellerName: 'Seller A',
            requiredQuantity: 50,
            availableQuantity: 40,
            unit: 'pcs',
          ),
          createTestMatch(
            id: 'm2',
            requirementId: 'r1',
            listingId: 'l2',
            sellerName: 'Seller B',
            requiredQuantity: 50,
            availableQuantity: 30,
            unit: 'pcs',
          ),
        ];

        await tester.pumpWidget(
          MaterialApp(
            theme: SurplusLinkTheme.light,
            home: RecommendedMatchesScreen(
              gateway: gateway,
              requirementId: 'r1',
            ),
          ),
        );
        await tester.pumpAndSettle();

        // Select m1 (40)
        await tester.tap(find.byKey(const Key('select-checkbox-m1')));
        await tester.pumpAndSettle();

        // Select m2 (available: 30, remaining: 10 -> defaults to 10)
        await tester.tap(find.byKey(const Key('select-checkbox-m2')));
        await tester.pumpAndSettle();

        // Enter 25 in m2, so 40 + 25 = 65 (> 50)
        await tester.enterText(
          find.byKey(const Key('quantity-input-m2')),
          '25',
        );
        await tester.pumpAndSettle();

        // Validation error appears
        expect(find.byKey(const Key('summary-validation-error')), findsOneWidget);
        expect(
          find.text('Total selected exceeds requirement by 15 pcs.'),
          findsOneWidget,
        );

        // Submit button is disabled
        final submitButton = tester.widget<FilledButton>(
          find.byKey(const Key('submit-selections')),
        );
        expect(submitButton.onPressed, isNull);
      },
    );
  });
}
