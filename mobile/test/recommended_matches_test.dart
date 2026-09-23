import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:mobile/app.dart';
import 'package:mobile/auth/auth_controller.dart';
import 'package:mobile/auth/auth_models.dart';
import 'package:mobile/core/api_exception.dart';
import 'package:mobile/matches/match_gateway.dart';
import 'package:mobile/matches/match_models.dart';
import 'package:mobile/screens/recommended_matches_screen.dart';
import 'package:mobile/screens/match_details_screen.dart';

import 'support/fakes.dart';
import 'support/requirement_fakes.dart';

final match = RecommendedMatch(
  id: 'm1',
  requirementId: 'r1',
  listingId: 'l1',
  score: .8,
  status: 'ROUTED',
  createdAt: DateTime.utc(2026),
  distance: 12.5,
  estimatedTransportCost: 1000,
);

void main() {
  for (final roles in <List<AppRole>?>[
    null,
    [AppRole.seller],
    [AppRole.manager],
    [AppRole.buyer],
    [AppRole.seller, AppRole.buyer],
    [AppRole.buyer, AppRole.seller],
  ]) {
    for (final details in [false, true]) {
      testWidgets(
        'match ${details ? 'details' : 'list'} deep link uses role membership $roles',
        (tester) async {
          final gateway = FakeMatches();
          final auth = AuthController(
            FakeAuthGateway()
              ..restoredUser = roles == null
                  ? null
                  : AppUser(
                      id: buyerUser.id,
                      email: buyerUser.email,
                      roles: roles,
                    ),
          );
          await tester.pumpWidget(
            SurplusLinkApp(
              authController: auth,
              matchGateway: gateway,
              initialLocation:
                  '/requirements/r1/matches${details ? '/m1' : ''}',
            ),
          );
          await tester.pumpAndSettle();
          final allowed = roles?.contains(AppRole.buyer) == true;
          expect(
            find.text(details ? 'Match Details' : 'Recommended Matches'),
            allowed ? findsOneWidget : findsNothing,
          );
          expect(gateway.reads > 0, allowed);
          if (allowed) {
            await auth.logout();
            await tester.pumpAndSettle();
            expect(find.byKey(const Key('login-submit')), findsOneWidget);
          }
        },
      );
    }
  }
  testWidgets(
    'dual-role requirement opens read-only matches and retains filters on back',
    (tester) async {
      final authGateway = FakeAuthGateway()
        ..restoredUser = AppUser(
          id: buyerUser.id,
          email: buyerUser.email,
          roles: const [AppRole.seller, AppRole.buyer],
        );
      final auth = AuthController(authGateway);
      final requirements = FakeRequirements();
      requirements.row = testRequirement(status: 'OPEN');
      final gateway = FakeMatches();
      await tester.pumpWidget(
        SurplusLinkApp(
          authController: auth,
          matchGateway: gateway,
          requirementGateway: requirements,
          initialLocation: '/requirements/r1',
        ),
      );
      await tester.pumpAndSettle();
      await tester.scrollUntilVisible(
        find.byKey(const Key('open-recommended-matches')),
        300,
      );
      await tester.tap(find.byKey(const Key('open-recommended-matches')));
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const Key('match-sort')));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Distance').last);
      await tester.pumpAndSettle();
      expect(gateway.queries.last.sortBy, 'distance');
      await tester.ensureVisible(find.byKey(const Key('match-m1')));
      await tester.tap(find.byKey(const Key('match-m1')));
      await tester.pumpAndSettle();
      expect(find.text('Match Details'), findsOneWidget);
      expect(find.text('Transport estimate'), findsOneWidget);
      expect(find.text('LKR 1,000.00'), findsWidgets);
      expect(find.text('Approve'), findsNothing);
      expect(find.text('Reserve'), findsNothing);
      await tester.tap(find.byType(BackButton));
      await tester.pumpAndSettle();
      expect(find.text('Distance'), findsOneWidget);
      expect(auth.user, same(authGateway.restoredUser));
      expect(
        requirements.saves +
            requirements.starts +
            requirements.submits +
            requirements.cancels +
            requirements.deletes,
        0,
      );
      final router = GoRouter.of(
        tester.element(find.byType(RecommendedMatchesScreen)),
      );
      router.go('/requirements/r1/status');
      await tester.pumpAndSettle();
      expect(
        find.textContaining('Recommendation details are not available'),
        findsNothing,
      );
      await tester.tap(find.text('Recommended Matches'));
      await tester.pumpAndSettle();
      expect(find.byType(RecommendedMatchesScreen), findsOneWidget);
      router.go('/home');
      await tester.pumpAndSettle();
      expect(find.text('Marketplace Home'), findsOneWidget);
    },
  );
  testWidgets('loading empty error retry and pagination keep query state', (
    tester,
  ) async {
    final gateway = FakeMatches()
      ..pending = Completer<MatchPage<RecommendedMatch>>();
    await tester.pumpWidget(
      MaterialApp(
        home: RecommendedMatchesScreen(gateway: gateway, requirementId: 'r1'),
      ),
    );
    await tester.pump();
    expect(find.byType(CircularProgressIndicator), findsOneWidget);
    gateway.pending!.complete(
      const MatchPage(
        items: [],
        total: 0,
        page: 1,
        pageSize: 20,
        totalPages: 0,
      ),
    );
    await tester.pumpAndSettle();
    expect(find.textContaining('No matches found.'), findsOneWidget);
    gateway.pending = null;
    gateway.error = const ApiException('Offline');
    await tester.tap(find.byKey(const Key('match-eligibility')));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Rejected').last);
    await tester.pumpAndSettle();
    expect(find.text('Offline'), findsOneWidget);
    gateway.error = null;
    await tester.ensureVisible(find.text('Retry'));
    await tester.tap(find.text('Retry'));
    await tester.pumpAndSettle();
    expect(gateway.queries.last.rejected, isTrue);
    await tester.ensureVisible(find.text('Next'));
    await tester.tap(find.text('Next'));
    await tester.pumpAndSettle();
    expect(gateway.queries.last.page, 2);
    expect(gateway.queries.last.rejected, isTrue);
    await tester.ensureVisible(find.byKey(const Key('match-direction')));
    await tester.tap(find.byKey(const Key('match-direction')));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Ascending').last);
    await tester.pumpAndSettle();
    expect(gateway.queries.last.page, 1);
    expect(gateway.queries.last.sortDir, 'asc');
  });
  testWidgets(
    'history error keeps details readable and retry restores history',
    (tester) async {
      final gateway = FakeMatches()
        ..historyError = const ApiException('History unavailable');
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
      expect(find.text('Score 80.0%'), findsOneWidget);
      await tester.scrollUntilVisible(find.text('History unavailable'), 300);
      expect(find.text('History unavailable'), findsOneWidget);
      gateway.historyError = null;
      await tester.ensureVisible(find.text('Retry'));
      await tester.tap(find.text('Retry'));
      await tester.pumpAndSettle();
      expect(find.text('No match history yet.'), findsOneWidget);
    },
  );
}

class FakeMatches implements MatchGateway {
  int reads = 0;
  Object? error, historyError;
  Completer<MatchPage<RecommendedMatch>>? pending;
  final queries = <MatchQuery>[];
  @override
  Future<MatchPage<RecommendedMatch>> list(
    String requirementId,
    MatchQuery query,
  ) async {
    reads++;
    queries.add(query);
    if (pending != null) return pending!.future;
    if (error != null) throw error!;
    return MatchPage(
      items: [match],
      total: 21,
      page: query.page,
      pageSize: 20,
      totalPages: 2,
    );
  }

  @override
  Future<RecommendedMatch> get(String requirementId, String matchId) async {
    reads++;
    if (error != null) throw error!;
    return match;
  }

  @override
  Future<void> select(String requirementId, String matchId) async {}

  @override
  Future<void> cancelPendingApproval(String requirementId) async {}

  @override
  Future<MatchPage<MatchHistoryEntry>> history(
    String matchId, {
    int page = 1,
  }) async {
    if (historyError != null) throw historyError!;
    return MatchPage(
      items: const [],
      total: 0,
      page: page,
      pageSize: 20,
      totalPages: 0,
    );
  }
}
