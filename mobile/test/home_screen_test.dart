import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/auth/auth_controller.dart';
import 'package:mobile/auth/auth_models.dart';
import 'package:mobile/requirements/requirement_gateway.dart';
import 'package:mobile/requirements/requirement_models.dart';
import 'package:mobile/screens/home_screen.dart';
import 'package:mobile/theme/surplus_link_theme.dart';

import 'support/fakes.dart';

const dualUser = AppUser(
  id: 'dual',
  email: 'dual@example.com',
  roles: [AppRole.buyer, AppRole.seller],
  fullName: 'Dual User',
);

void main() {
  testWidgets('buyer home shows buyer dashboard and permitted actions', (
    tester,
  ) async {
    await _pump(tester, buyerUser);
    expect(find.text('Buyer'), findsOneWidget);
    expect(find.byKey(const Key('marketplace-mode-switcher')), findsNothing);
    await tester.drag(
      find.byKey(const Key('home-dashboard-scroll')),
      const Offset(0, -500),
    );
    await tester.pump();
    expect(find.byKey(const Key('home-create-requirement')), findsOneWidget);
    expect(find.byKey(const Key('open-my-requirements')), findsOneWidget);
    expect(find.byKey(const Key('home-add-material')), findsNothing);
  });

  testWidgets('seller home shows seller actions only', (tester) async {
    await _pump(tester, sellerUser);
    expect(find.text('Seller'), findsOneWidget);
    expect(find.byKey(const Key('marketplace-mode-switcher')), findsNothing);
    await tester.drag(
      find.byKey(const Key('home-dashboard-scroll')),
      const Offset(0, -500),
    );
    await tester.pump();
    expect(find.byKey(const Key('home-add-material')), findsOneWidget);
    expect(find.byKey(const Key('open-my-materials')), findsOneWidget);
    expect(find.byKey(const Key('home-create-requirement')), findsNothing);
  });

  testWidgets('dual role selects one dashboard and navigation at a time', (
    tester,
  ) async {
    final auth = await _pump(tester, dualUser);
    expect(find.byKey(const Key('marketplace-mode-switcher')), findsOneWidget);
    expect(find.text('Active Requirements'), findsOneWidget);
    expect(find.text('Active Listings'), findsNothing);
    expect(find.text('Needs'), findsOneWidget);
    expect(find.text('Listings'), findsNothing);
    await tester.tap(find.byKey(const Key('mode-seller')));
    await tester.pumpAndSettle();
    expect(find.text('Active Listings'), findsOneWidget);
    expect(find.text('Active Requirements'), findsNothing);
    expect(find.text('Needs'), findsNothing);
    expect(find.text('Listings'), findsOneWidget);
    await tester.drag(
      find.byKey(const Key('home-dashboard-scroll')),
      const Offset(0, -500),
    );
    await tester.pumpAndSettle();
    expect(find.byKey(const Key('home-add-material')), findsOneWidget);
    expect(find.byKey(const Key('home-create-requirement')), findsNothing);
    expect(auth.user, same(dualUser));
    expect(auth.isAuthenticated, isTrue);
    await tester.drag(
      find.byKey(const Key('home-dashboard-scroll')),
      const Offset(0, 1000),
    );
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const Key('mode-buyer')));
    await tester.pumpAndSettle();
    expect(find.text('Active Requirements'), findsOneWidget);
    expect(find.text('Active Listings'), findsNothing);
  });

  testWidgets('quick actions use their existing navigation callbacks', (
    tester,
  ) async {
    var openedRequirements = 0;
    await _pump(tester, buyerUser, onRequirements: () => openedRequirements++);
    await tester.drag(
      find.byKey(const Key('home-dashboard-scroll')),
      const Offset(0, -350),
    );
    await tester.pump();
    await tester.tap(find.byKey(const Key('open-my-requirements')));
    expect(openedRequirements, 1);
  });

  testWidgets('dashboard shows a loading skeleton and retry state', (
    tester,
  ) async {
    final response = Completer<RequirementPage<BuyerRequirement>>();
    final gate = _RequirementGate(myResult: () => response.future);
    await _pump(tester, buyerUser, requirements: gate, settle: false);
    expect(find.byKey(const Key('home-loading-skeleton')), findsWidgets);
    response.completeError(StateError('offline'));
    await tester.pump();
    await tester.pump();
    await tester.drag(
      find.byKey(const Key('home-dashboard-scroll')),
      const Offset(0, -1000),
    );
    await tester.pump();
    expect(find.text('Retry'), findsOneWidget);
  });
}

Future<AuthController> _pump(
  WidgetTester tester,
  AppUser user, {
  RequirementGateway? requirements,
  VoidCallback? onRequirements,
  bool settle = true,
}) async {
  final auth = AuthController(FakeAuthGateway()..restoredUser = user);
  await auth.initialize();
  await tester.pumpWidget(
    MaterialApp(
      theme: SurplusLinkTheme.light,
      home: HomeScreen(
        authController: auth,
        requirementGateway: requirements,
        onOpenRequirements: onRequirements,
      ),
    ),
  );
  await tester.pump();
  if (settle) await tester.pump(const Duration(milliseconds: 750));
  return auth;
}

final class _RequirementGate implements RequirementGateway {
  _RequirementGate({required this.myResult});
  final Future<RequirementPage<BuyerRequirement>> Function() myResult;
  @override
  Future<RequirementPage<BuyerRequirement>> my(RequirementQuery query) =>
      myResult();
  @override
  Future<List<RequirementCategory>> categories() async => const [];
  @override
  Future<List<String>> activeUnits(String categoryId) async => const [];
  @override
  Future<BuyerRequirement> get(String id) => throw UnimplementedError();
  @override
  Future<BuyerRequirement> create(RequirementDraft draft) =>
      throw UnimplementedError();
  @override
  Future<BuyerRequirement> update(String id, RequirementDraft draft) =>
      throw UnimplementedError();
  @override
  Future<void> delete(String id) => throw UnimplementedError();
  @override
  Future<BuyerRequirement> submit(String id) => throw UnimplementedError();
  @override
  Future<RequirementStartResult> startMatching(String id) =>
      throw UnimplementedError();
  @override
  Future<BuyerRequirement> cancel(String id) => throw UnimplementedError();
  @override
  Future<RequirementPage<RequirementHistoryEntry>> history(
    String id, {
    int page = 1,
  }) => throw UnimplementedError();
}
