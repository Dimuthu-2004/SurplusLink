import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mobile/app.dart';
import 'package:mobile/auth/auth_controller.dart';
import 'package:mobile/auth/auth_models.dart';
import 'package:mobile/core/api_client.dart';
import 'package:mobile/core/api_exception.dart';
import 'package:mobile/offers/offer_models.dart';
import 'package:mobile/offers/offer_repository.dart';
import 'package:mobile/screens/my_offers_screen.dart';
import 'package:mobile/screens/requirement_status_screen.dart';

import 'offer_status_test.dart';
import 'support/fakes.dart';
import 'support/requirement_fakes.dart';

void main() {
  for (final code in [401, 403, 500]) {
    test('plain text HTTP $code retains error status', () async {
      final client = ApiClient(
        baseUri: Uri.parse('http://localhost:5170'),
        tokenStorage: MemoryTokenStorage()..token = 'session',
        httpClient: MockClient(
          (_) async => http.Response('upstream error', code),
        ),
      );
      await expectLater(
        OfferRepository(client).offers(const OfferQuery()),
        throwsA(
          isA<ApiException>().having(
            (error) => error.statusCode,
            'status',
            code,
          ),
        ),
      );
    });
  }
  testWidgets('dual role shows buyer and seller participation together', (
    tester,
  ) async {
    await tester.pumpWidget(
      MaterialApp(
        home: MyOffersScreen(
          gateway: MixedOffers(),
          user: const AppUser(
            id: 'buyer',
            email: 'test@test.local',
            roles: [AppRole.buyer, AppRole.seller],
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();
    expect(find.textContaining('Buyer participation'), findsOneWidget);
    expect(find.textContaining('Seller participation'), findsOneWidget);
    expect(find.text('Approve'), findsNothing);
  });
  for (final roles in [
    [AppRole.seller],
    [AppRole.buyer],
    [AppRole.seller, AppRole.buyer],
  ]) {
    testWidgets('$roles can open offers route with a genuine empty state', (
      tester,
    ) async {
      final auth = AuthController(
        FakeAuthGateway()
          ..restoredUser = AppUser(
            id: 'buyer',
            email: 'test@test.local',
            roles: roles,
          ),
      );
      await tester.pumpWidget(
        SurplusLinkApp(
          authController: auth,
          offerGateway: EmptyOffers(),
          initialLocation: '/offers',
        ),
      );
      await tester.pumpAndSettle();
      expect(find.text('My Offers'), findsOneWidget);
      expect(find.text('No offers yet.'), findsOneWidget);
      expect(find.textContaining('Access denied'), findsNothing);
      await tester.pumpWidget(const SizedBox());
      auth.dispose();
    });
  }
  for (final code in [401, 403, 500]) {
    testWidgets('offers distinguish API $code from empty data', (tester) async {
      await tester.pumpWidget(
        MaterialApp(
          home: MyOffersScreen(
            gateway: EmptyOffers()
              ..failure = ApiException('failure', statusCode: code),
            user: buyerUser,
          ),
        ),
      );
      await tester.pumpAndSettle();
      expect(
        find.textContaining(
          code == 401
              ? 'session has expired'
              : code == 403
              ? 'Access denied'
              : 'API 500',
        ),
        findsOneWidget,
      );
      expect(find.text('No offers yet.'), findsNothing);
    });
  }
  for (final terminal in [
    'MATCH_FOUND',
    'OPEN',
    'APPROVED',
    'REJECTED',
    'COMPLETED',
    'CANCELLED',
    'FAILED',
    'PENDING_APPROVAL',
  ]) {
    testWidgets('matching polls then stops at $terminal and disposes safely', (
      tester,
    ) async {
      final gateway = FakeRequirements()
        ..row = testRequirement(status: 'MATCHING');
      await tester.pumpWidget(
        MaterialApp(
          home: RequirementStatusScreen(
            gateway: gateway,
            requirementId: 'r1',
            showMatches: true,
          ),
        ),
      );
      await tester.pumpAndSettle();
      expect(gateway.reads, 1);
      gateway.row = testRequirement(status: terminal);
      await tester.pump(const Duration(seconds: 4));
      await tester.pumpAndSettle();
      expect(gateway.reads, 2);
      expect(find.text('Recommended Matches'), findsOneWidget);
      await tester.pump(const Duration(seconds: 12));
      expect(gateway.reads, 2);
      await tester.pumpWidget(const SizedBox());
    });
  }
  testWidgets(
    'matching polling survives connection errors and cancels on disposal',
    (tester) async {
      final gateway = FakeRequirements()
        ..row = testRequirement(status: 'MATCHING');
      await tester.pumpWidget(
        MaterialApp(
          home: RequirementStatusScreen(gateway: gateway, requirementId: 'r1'),
        ),
      );
      await tester.pumpAndSettle();
      gateway.error = const ApiException('offline');
      await tester.pump(const Duration(seconds: 4));
      await tester.pumpAndSettle();
      gateway.error = null;
      await tester.pump(const Duration(seconds: 4));
      await tester.pumpAndSettle();
      expect(gateway.reads, 3);
      await tester.pumpWidget(const SizedBox());
      await tester.pump(const Duration(seconds: 8));
      expect(gateway.reads, 3);
    },
  );
  test(
    'offer repository sends bearer token without client identity filter',
    () async {
      final client = ApiClient(
        baseUri: Uri.parse('http://localhost:5170'),
        tokenStorage: MemoryTokenStorage()..token = 'session',
        httpClient: MockClient((request) async {
          expect(request.headers['Authorization'], 'Bearer session');
          expect(request.url.queryParameters.containsKey('userId'), false);
          return http.Response(
            '{"items":[],"total":0,"page":1,"totalPages":0}',
            200,
          );
        }),
      );
      final repository = OfferRepository(client);
      expect((await repository.offers(const OfferQuery())).items, isEmpty);
      expect(
        (await repository.transactions(const OfferQuery())).items,
        isEmpty,
      );
    },
  );
}

class EmptyOffers extends FakeOffers {
  Object? failure;
  @override
  Future<OfferPage> offers(OfferQuery query) async {
    if (failure != null) throw failure!;
    return const OfferPage([], 0, 1, 0);
  }
}

class MixedOffers extends FakeOffers {
  @override
  Future<OfferPage> offers(OfferQuery query) async {
    final first = await super.offers(query);
    return OfferPage(
      [
        ...first.items,
        Offer(
          id: '87654321-offer',
          buyerId: 'other',
          sellerId: 'buyer',
          quantity: 10,
          totalValue: 100,
          status: 'PENDING',
          createdAt: DateTime.utc(2026, 9, 22),
        ),
      ],
      2,
      1,
      1,
    );
  }
}
