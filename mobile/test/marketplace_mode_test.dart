import 'dart:async';
import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mobile/app.dart';
import 'package:mobile/auth/auth_controller.dart';
import 'package:mobile/auth/auth_models.dart';
import 'package:mobile/auth/auth_repository.dart';
import 'package:mobile/core/api_client.dart';
import 'package:mobile/core/api_exception.dart';
import 'package:mobile/matches/match_repository.dart';
import 'package:mobile/marketplace/marketplace_mode.dart';
import 'package:mobile/marketplace/marketplace_mode_controller.dart';
import 'package:mobile/marketplace/marketplace_mode_storage.dart';
import 'package:mobile/marketplace/marketplace_offer_view.dart';
import 'package:mobile/home/home_dashboard_controller.dart';
import 'package:mobile/offers/offer_models.dart';
import 'package:mobile/screens/home_screen.dart';
import 'package:mobile/screens/my_offers_screen.dart';
import 'package:mobile/screens/recommended_matches_screen.dart';

import 'category_forms_test.dart' show FakeCategoryMaterials;
import 'offer_status_test.dart' show FakeOffers;
import 'recommended_matches_test.dart' show FakeMatches;
import 'support/fakes.dart';
import 'support/requirement_fakes.dart';

const dual = AppUser(
  id: 'buyer',
  email: 'dual@test.local',
  roles: [AppRole.buyer, AppRole.seller],
);

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  testWidgets(
    'phone-sized switcher stays visible and disables during a pending save',
    (tester) async {
      tester.view.physicalSize = const Size(360, 800);
      tester.view.devicePixelRatio = 1;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);
      final auth = AuthController(FakeAuthGateway()..restoredUser = dual);
      await tester.pumpWidget(SurplusLinkApp(authController: auth));
      await tester.pumpAndSettle();
      final pending = Completer<void>();
      final saving = auth.marketplace.mutations.track(() => pending.future);
      await tester.pump();
      expect(
        tester
            .widget<TextButton>(find.byKey(const Key('mode-seller')))
            .onPressed,
        isNull,
      );
      expect(
        find.text('Finish saving before switching modes.'),
        findsOneWidget,
      );
      pending.complete();
      await saving;
      await tester.pump();
      await tester.tap(find.byKey(const Key('mode-seller')));
      await tester.pumpAndSettle();
      expect(auth.marketplace.activeMode, MarketplaceMode.seller);
      expect(tester.takeException(), isNull);
      await tester.pumpWidget(const SizedBox());
      auth.dispose();
    },
  );

  test(
    'secure preference restores per user; logout clears preference and mode',
    () async {
      FlutterSecureStorage.setMockInitialValues({});
      const storage = SecureMarketplaceModeStorage();
      final first = MarketplaceModeController(storage: storage);
      await first.bind(dual);
      expect(first.activeMode, MarketplaceMode.buyer);
      await first.select(MarketplaceMode.seller);
      final restored = MarketplaceModeController(storage: storage);
      final auth = AuthController(
        FakeAuthGateway()..restoredUser = dual,
        marketplace: restored,
      );
      await auth.initialize();
      expect(restored.activeMode, MarketplaceMode.seller);
      final other = MarketplaceModeController(storage: storage);
      await other.bind(
        const AppUser(
          id: 'other',
          email: 'other@test.local',
          roles: [AppRole.buyer, AppRole.seller],
        ),
      );
      expect(other.activeMode, MarketplaceMode.buyer);
      await auth.logout();
      expect(restored.activeMode, isNull);
      expect(restored.canSwitch, isFalse);
      expect(await storage.read(dual.id), isNull);
      expect(auth.isAuthenticated, isFalse);
      await restored.bind(dual);
      expect(restored.activeMode, MarketplaceMode.buyer);
      first.dispose();
      other.dispose();
      auth.dispose();
    },
  );

  test(
    'single-role and invalid preferences safely resolve to available roles',
    () async {
      final storage = MemoryMarketplaceModeStorage();
      for (final user in [buyerUser, sellerUser, dual]) {
        for (final saved in ['buyer', 'seller', 'invalid']) {
          storage.values[user.id] = saved;
          final mode = MarketplaceModeController(storage: storage);
          await mode.bind(user);
          if (user == dual) {
            expect(
              mode.activeMode,
              saved == 'seller'
                  ? MarketplaceMode.seller
                  : MarketplaceMode.buyer,
            );
          } else {
            expect(
              mode.activeMode,
              user == buyerUser
                  ? MarketplaceMode.buyer
                  : MarketplaceMode.seller,
            );
            expect(
              await mode.select(
                user == buyerUser
                    ? MarketplaceMode.seller
                    : MarketplaceMode.buyer,
              ),
              isFalse,
            );
            expect(mode.canSwitch, isFalse);
          }
          mode.dispose();
        }
      }
    },
  );

  test(
    'switching keeps the real user, roles and bearer token without auth calls',
    () async {
      final tokens = MemoryTokenStorage()..token = 'original-dual-token';
      var requests = 0;
      final api = ApiClient(
        baseUri: Uri.parse('http://localhost'),
        tokenStorage: tokens,
        httpClient: MockClient((request) async {
          requests++;
          expect(request.url.path, '/api/auth/me');
          expect(
            request.headers['Authorization'],
            'Bearer original-dual-token',
          );
          return http.Response(
            jsonEncode({
              'id': dual.id,
              'email': dual.email,
              'roles': ['BUYER', 'SELLER'],
            }),
            200,
          );
        }),
      );
      final auth = AuthController(
        AuthRepository(apiClient: api, tokenStorage: tokens),
      );
      await auth.initialize();
      final originalUser = auth.user;
      await auth.marketplace.select(MarketplaceMode.seller);
      await auth.marketplace.select(MarketplaceMode.buyer);
      expect(auth.isAuthenticated, isTrue);
      expect(auth.user, same(originalUser));
      expect(auth.user!.roles, [AppRole.buyer, AppRole.seller]);
      expect(await tokens.readToken(), 'original-dual-token');
      expect(requests, 1);
      auth.dispose();
    },
  );

  test('mode changes preserve the server self-match rejection and original selection request', () async {
    final mode = MarketplaceModeController();
    await mode.bind(dual);
    final tokens = MemoryTokenStorage()..token = 'dual-token';
    var requests = 0;
    final api = ApiClient(
      baseUri: Uri.parse('http://localhost'),
      tokenStorage: tokens,
      mutations: mode.mutations,
      httpClient: MockClient((request) async {
        requests++;
        expect(request.url.path, '/api/requirements/r1/select-match');
        expect(request.headers['Authorization'], 'Bearer dual-token');
        expect(jsonDecode(request.body), {
          'matchId': 'own-listing-match',
          'quantity': 2,
        });
        return http.Response('{"message":"SELF_MATCH_NOT_ALLOWED"}', 409);
      }),
    );
    final matches = MatchRepository(api);
    for (final selected in [
      MarketplaceMode.buyer,
      MarketplaceMode.seller,
      MarketplaceMode.buyer,
    ]) {
      await mode.select(selected);
      await expectLater(
        matches.select('r1', 'own-listing-match', quantity: 2),
        throwsA(
          isA<ApiException>()
              .having((e) => e.statusCode, 'status', 409)
              .having((e) => e.message, 'message', 'SELF_MATCH_NOT_ALLOWED'),
        ),
      );
    }
    expect(requests, 3);
    expect(tokens.token, 'dual-token');
    mode.dispose();
  });

  for (final fail in [false, true]) {
    test(
      'in-flight API mutation blocks switching and releases lock (failure=$fail)',
      () async {
        final mode = MarketplaceModeController();
        await mode.bind(dual);
        final response = Completer<http.Response>();
        final started = Completer<void>();
        final api = ApiClient(
          baseUri: Uri.parse('http://localhost'),
          tokenStorage: MemoryTokenStorage()..token = 'unchanged',
          mutations: mode.mutations,
          httpClient: MockClient((request) {
            started.complete();
            return response.future;
          }),
        );
        final request = api.postJson(
          '/api/transactions/t/handover',
          {},
          authenticated: true,
        );
        final checked = fail
            ? expectLater(request, throwsException)
            : expectLater(request, completion(isA<Map<String, dynamic>>()));
        await started.future;
        expect(mode.canSwitch, isFalse);
        expect(await mode.select(MarketplaceMode.seller), isFalse);
        expect(mode.activeMode, MarketplaceMode.buyer);
        response.complete(
          http.Response(
            fail ? '{"message":"Rejected"}' : '{}',
            fail ? 409 : 200,
          ),
        );
        await checked;
        expect(mode.canSwitch, isTrue);
        expect(await mode.select(MarketplaceMode.seller), isTrue);
        mode.dispose();
      },
    );
  }

  test(
    'logout waits for an older preference write before clearing it',
    () async {
      final storage = DelayedStorage();
      final mode = MarketplaceModeController(storage: storage);
      await mode.bind(dual);
      final saving = mode.select(MarketplaceMode.seller);
      final clearing = mode.clear();
      storage.pending.complete();
      await Future.wait([saving, clearing]);
      expect(storage.values, isEmpty);
      expect(mode.activeMode, isNull);
      mode.dispose();
    },
  );

  test(
    'storage failures preserve authentication and use safe mode fallback',
    () async {
      final auth = AuthController(
        FakeAuthGateway()..restoredUser = dual,
        marketplace: MarketplaceModeController(storage: BrokenStorage()),
      );
      await auth.initialize();
      expect(auth.isAuthenticated, isTrue);
      expect(auth.marketplace.activeMode, MarketplaceMode.buyer);
      await auth.marketplace.select(MarketplaceMode.seller);
      expect(auth.marketplace.persistenceError, isNotNull);
      expect(auth.marketplace.activeMode, MarketplaceMode.seller);
      await auth.logout();
      expect(auth.marketplace.activeMode, isNull);
      expect(auth.isAuthenticated, isFalse);
      auth.dispose();
    },
  );

  testWidgets(
    'mode changes reset pushed matches and seller forms to home, blocking stale routes',
    (tester) async {
      final auth = AuthController(FakeAuthGateway()..restoredUser = dual);
      await tester.pumpWidget(
        SurplusLinkApp(
          authController: auth,
          requirementGateway: FakeRequirements(),
          matchGateway: FakeMatches(),
          materialGateway: FakeCategoryMaterials(),
        ),
      );
      await tester.pumpAndSettle();
      final router = GoRouter.of(tester.element(find.byType(HomeScreen)));
      unawaited(router.push('/requirements/r1/matches'));
      await tester.pumpAndSettle();
      expect(find.byType(RecommendedMatchesScreen), findsOneWidget);
      await auth.marketplace.select(MarketplaceMode.seller);
      await tester.pumpAndSettle();
      expect(router.routeInformationProvider.value.uri.path, '/home');
      expect(router.canPop(), isFalse);
      expect(find.byType(RecommendedMatchesScreen), findsNothing);
      expect(find.text('Needs'), findsNothing);
      expect(find.text('Listings'), findsOneWidget);
      router.go('/requirements/r1/matches');
      await tester.pumpAndSettle();
      expect(router.routeInformationProvider.value.uri.path, '/home');
      unawaited(router.push('/materials/new'));
      await tester.pumpAndSettle();
      await auth.marketplace.select(MarketplaceMode.buyer);
      await tester.pumpAndSettle();
      expect(router.routeInformationProvider.value.uri.path, '/home');
      expect(router.canPop(), isFalse);
      router.go('/materials/new');
      await tester.pumpAndSettle();
      expect(router.routeInformationProvider.value.uri.path, '/home');
      expect(auth.user, same(dual));
      await tester.pumpWidget(const SizedBox());
      auth.dispose();
    },
  );

  testWidgets(
    'shared offers route shows only selected participation and resets detail stack',
    (tester) async {
      final auth = AuthController(FakeAuthGateway()..restoredUser = dual);
      await tester.pumpWidget(
        SurplusLinkApp(
          authController: auth,
          offerGateway: PagedOffers(),
          initialLocation: '/offers',
        ),
      );
      await tester.pumpAndSettle();
      expect(find.textContaining('Buyer participation'), findsWidgets);
      expect(find.textContaining('Seller participation'), findsNothing);
      final router = GoRouter.of(tester.element(find.byType(MyOffersScreen)));
      await tester.tap(find.byType(ListTile).first);
      await tester.pumpAndSettle();
      expect(find.text('Offer Details'), findsOneWidget);
      await auth.marketplace.select(MarketplaceMode.seller);
      await tester.pumpAndSettle();
      expect(find.text('Offer Details'), findsNothing);
      expect(router.canPop(), isFalse);
      router.go('/offers');
      await tester.pumpAndSettle();
      expect(find.textContaining('Buyer participation'), findsNothing);
      expect(find.textContaining('Seller participation'), findsWidgets);
      await tester.pumpWidget(const SizedBox());
      auth.dispose();
    },
  );

  test('dual lists filter before pagination and preserve queries and mutation delegation', () async {
    final source = PagedOffers();
    final buyer = MarketplaceOfferView(source, dual, MarketplaceMode.buyer);
    final seller = MarketplaceOfferView(source, dual, MarketplaceMode.seller);
    final page = await buyer.offers(
      const OfferQuery(
        pageSize: 1,
        page: 2,
        status: 'ACCEPTED',
        sortBy: 'value',
      ),
    );
    expect(page.items.single.id, 'o3');
    expect(page.total, 2);
    expect(page.totalPages, 2);
    expect(source.queries.map((q) => q.page), [1, 2, 3, 4]);
    expect(
      source.queries.every(
        (q) => q.status == 'ACCEPTED' && q.sortBy == 'value',
      ),
      isTrue,
    );
    final transactions = await seller.transactions(
      const OfferQuery(pageSize: 1),
    );
    expect(transactions.items.single.id, 't2');
    expect(transactions.total, 2);
    expect((await seller.handover('t2')).status, 'HANDED_OVER');
    expect((await buyer.confirmReceipt('t1')).status, 'COMPLETED');
    await buyer.history('t1', page: 2);
    expect(source.historyPages, [2]);
    expect(
      MarketplaceOfferView.forUser(source, buyerUser, MarketplaceMode.buyer),
      same(source),
    );
    expect(
      MarketplaceOfferView.forUser(source, sellerUser, MarketplaceMode.seller),
      same(source),
    );
  });

  test(
    'dashboard loads only the selected role and filters transaction activity',
    () async {
      final requirements = FakeRequirements();
      final offers = PagedOffers();
      final buyer = HomeDashboardController(
        user: dual,
        mode: MarketplaceMode.buyer,
        requirements: requirements,
        offers: offers,
      );
      await buyer.load();
      expect(requirements.queries.length, 6);
      expect(buyer.data.seller, isNull);
      expect(
        buyer.data.activities.where((a) => a.kind == 'transaction').length,
        2,
      );
      final seller = HomeDashboardController(
        user: dual,
        mode: MarketplaceMode.seller,
        requirements: requirements,
        offers: offers,
      );
      await seller.load();
      expect(requirements.queries.length, 6);
      expect(seller.data.buyer, isNull);
      expect(
        seller.data.activities
            .where((a) => a.label == 'Handover is ready')
            .length,
        2,
      );
      buyer.dispose();
      seller.dispose();
    },
  );

  test('an old dashboard request completing after disposal cannot notify the new mode', () async {
    final requirements = FakeRequirements()..pendingList = Completer();
    final old = HomeDashboardController(
      user: dual,
      mode: MarketplaceMode.buyer,
      requirements: requirements,
    );
    final loading = old.load();
    old.dispose();
    requirements.pendingList!.complete(
      await FakeRequirements().my(requirements.queries.first),
    );
    await loading;
  });
}

class DelayedStorage implements MarketplaceModeStorage {
  final values = <String, String>{};
  final pending = Completer<void>();
  @override
  Future<String?> read(String id) async => values[id];
  @override
  Future<void> write(String id, String mode) async {
    await pending.future;
    values[id] = mode;
  }

  @override
  Future<void> clear(String id) async => values.remove(id);
}

class BrokenStorage implements MarketplaceModeStorage {
  @override
  Future<String?> read(String id) async => throw StateError('unavailable');
  @override
  Future<void> write(String id, String mode) async =>
      throw StateError('unavailable');
  @override
  Future<void> clear(String id) async => throw StateError('unavailable');
}

class PagedOffers extends FakeOffers {
  final queries = <OfferQuery>[];
  List<int> _indices(OfferQuery q) => List.generate(
    4,
    (i) => i + 1,
  ).skip((q.page - 1) * q.pageSize).take(q.pageSize).toList();
  @override
  Future<OfferPage> offers(OfferQuery query) async {
    queries.add(query);
    return OfferPage(
      [
        for (final i in _indices(query))
          Offer(
            id: 'o$i',
            buyerId: i.isOdd ? dual.id : 'other',
            sellerId: i.isEven ? dual.id : 'other',
            quantity: 10,
            totalValue: 100,
            status: 'ACCEPTED',
            createdAt: DateTime.utc(2026),
          ),
      ],
      4,
      query.page,
      (4 / query.pageSize).ceil(),
    );
  }

  @override
  Future<TransactionPage> transactions(OfferQuery query) async =>
      TransactionPage(
        [
          for (final i in _indices(query))
            Transaction(
              id: 't$i',
              offerId: 'o$i',
              buyerId: i.isOdd ? dual.id : 'other',
              sellerId: i.isEven ? dual.id : 'other',
              status: 'APPROVED',
              reservedQuantity: 10,
              totalValue: 100,
              updatedAt: DateTime.utc(2026),
            ),
        ],
        4,
        query.page,
        (4 / query.pageSize).ceil(),
      );
}
