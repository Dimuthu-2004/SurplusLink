import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:mobile/matches/match_models.dart';
import 'package:mobile/matches/match_formatters.dart';
import 'package:mobile/screens/recommended_matches_screen.dart';
import 'partial_multi_match_test.dart' show FakeMultiMatchGateway;

void main() {
  testWidgets('6 L accepts two 4 L packages and confirmation prices the same allocation', (tester) async {
    tester.view.physicalSize = const Size(390, 844);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    final gateway = FakeMultiMatchGateway()..matches = [RecommendedMatch(
      id: 'paint', requirementId: 'r1', listingId: 'listing', score: .9,
      status: 'ROUTED', valid: true, createdAt: DateTime.now(),
      requirementStatus: 'MATCH_FOUND', quantity: 6, unit: 'L',
      quantityMode: 'PACKAGE', packageType: 'CAN', packageSize: 4,
      packageCountAvailable: 3, availableQuantity: 12, unitPrice: 4000,
      estimatedTransportCost: 200, sellerName: 'Hardware', materialTitle: 'Wall Paint',
    )];
    final router = GoRouter(routes: [
      GoRoute(path: '/', builder: (_, _) => RecommendedMatchesScreen(gateway: gateway, requirementId: 'r1')),
      GoRoute(path: '/requirements/r1', builder: (_, _) => const Scaffold(body: Text('Requirement'))),
    ]);
    addTearDown(router.dispose);
    await tester.pumpWidget(MaterialApp.router(routerConfig: router));
    await tester.pumpAndSettle();
    await tester.ensureVisible(find.byKey(const Key('select-checkbox-paint')));
    await tester.tap(find.byKey(const Key('select-checkbox-paint')));
    await tester.pumpAndSettle();
    expect(find.text('Selected: 8 L'), findsOneWidget);
    expect(find.textContaining('Remaining: 0 L'), findsOneWidget);
    expect(find.textContaining('Package overage: 2 L'), findsOneWidget);
    final submit = find.byKey(const Key('submit-selections'));
    expect(tester.widget<FilledButton>(submit).onPressed, isNotNull);
    expect(tester.takeException(), isNull);
    await tester.tap(submit);
    await tester.pumpAndSettle();
    expect(find.text(formatCurrency(8000)), findsWidgets);
    expect(find.text(formatCurrency(8200)), findsWidgets);
    await tester.tap(find.byKey(const Key('confirm-multi-selection')));
    await tester.pumpAndSettle();
    final allocation = gateway.submittedAllocations.single.single;
    expect(allocation.packageCount, 2);
    expect(allocation.quantity, 8);
    expect(tester.takeException(), isNull);
  });
}
