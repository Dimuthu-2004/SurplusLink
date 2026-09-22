import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/auth/auth_models.dart';
import 'package:mobile/offers/offer_gateway.dart';
import 'package:mobile/offers/offer_models.dart';
import 'package:mobile/screens/my_offers_screen.dart';

void main() {
  testWidgets('dual-role participant sees offer details without approval controls', (tester) async {
    final gateway = FakeOffers();
    await tester.pumpWidget(MaterialApp(home: MyOffersScreen(gateway: gateway,
      user: const AppUser(id: 'buyer', email: 'buyer@test.local', roles: [AppRole.buyer, AppRole.seller]))));
    await tester.pumpAndSettle();
    expect(find.textContaining('Buyer participation'), findsOneWidget);
    expect(find.text('Approve'), findsNothing);
    await tester.tap(find.text('Offer 12345678'));
    await tester.pumpAndSettle();
    expect(find.text('Offer Details'), findsOneWidget);
    expect(find.text('Your participation: Buyer'), findsOneWidget);
    expect(find.text('Material value: LKR 320000.00'), findsOneWidget);
  });

  testWidgets('transaction history retries and loads every server page', (tester) async {
    final gateway = FakeOffers()..failHistory = true;
    await tester.pumpWidget(MaterialApp(home: TransactionHistoryScreen(gateway: gateway, transactionId: 'transaction')));
    await tester.pumpAndSettle();
    expect(find.text('Unable to load transaction history.'), findsOneWidget);
    gateway.failHistory = false;
    await tester.tap(find.text('Retry'));
    await tester.pumpAndSettle();
    expect(find.text('WORKFLOW APPROVED'), findsOneWidget);
    await tester.tap(find.text('Next'));
    await tester.pumpAndSettle();
    expect(find.text('TRANSACTION COMPLETED'), findsOneWidget);
    expect(find.text('Page 2 of 2'), findsOneWidget);
    expect(gateway.historyPages, [1, 1, 2]);
  });
}

class FakeOffers implements OfferGateway {
  bool failHistory = false;
  final historyPages = <int>[];
  @override Future<OfferPage> offers(OfferQuery query) async => OfferPage([
    Offer(id: '12345678-offer', buyerId: 'buyer', sellerId: 'seller', quantity: 400,
      totalValue: 320000, status: 'ACCEPTED', createdAt: DateTime.utc(2026, 9, 22)),
  ], 1, 1, 1);
  @override Future<TransactionPage> transactions(OfferQuery query) async => const TransactionPage([], 0, 1, 0);
  @override Future<TransactionHistoryPage> history(String id, {int page = 1}) async {
    historyPages.add(page);
    if (failHistory) throw Exception('offline');
    return TransactionHistoryPage([
      TransactionHistoryEntry('event-$page', page == 1 ? 'WORKFLOW_APPROVED' : 'TRANSACTION_COMPLETED', DateTime.utc(2026, 9, 22)),
    ], 21, page, 2);
  }
}
