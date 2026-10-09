import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/auth/auth_models.dart';
import 'package:mobile/core/api_exception.dart';
import 'package:mobile/offers/offer_gateway.dart';
import 'package:mobile/offers/offer_models.dart';
import 'package:mobile/screens/my_offers_screen.dart';

void main() {
  testWidgets(
    'dual-role participant sees offer details without approval controls',
    (tester) async {
      final gateway = FakeOffers();
      await tester.pumpWidget(
        MaterialApp(
          home: MyOffersScreen(
            gateway: gateway,
            user: const AppUser(
              id: 'buyer',
              email: 'buyer@test.local',
              roles: [AppRole.buyer, AppRole.seller],
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();
      expect(find.textContaining('Buyer participation'), findsOneWidget);
      expect(find.text('Approve'), findsNothing);
      await tester.tap(find.byType(ListTile).first);
      await tester.pumpAndSettle();
      expect(find.text('Offer Details'), findsOneWidget);
      expect(find.text('Your participation: Buyer'), findsOneWidget);
      expect(find.text('Material value: LKR 320000.00'), findsOneWidget);
    },
  );

  for (final role in ['seller', 'buyer', 'outsider']) {
    testWidgets('$role sees only their transfer action and approved contacts', (
      tester,
    ) async {
      final gateway = FakeOffers()
        ..status = role == 'buyer' ? 'HANDED_OVER' : 'APPROVED';
      await tester.pumpWidget(
        MaterialApp(
          home: TransactionDetailsScreen(
            gateway: gateway,
            transactionId: 'transaction',
            user: AppUser(
              id: role,
              email: '$role@test.local',
              roles: const [AppRole.buyer, AppRole.seller],
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();
      expect(
        find.text('Handed Over'),
        role == 'seller' ? findsOneWidget : findsNothing,
      );
      expect(
        find.text('Received'),
        role == 'buyer' ? findsOneWidget : findsNothing,
      );
      if (role == 'seller') {
        expect(find.text('buyer@test.local'), findsOneWidget);
        await tester.tap(find.text('Handed Over'));
        await tester.pumpAndSettle();
        expect(gateway.status, 'HANDED_OVER');
        expect(
          find.text('Waiting for the buyer to confirm receipt.'),
          findsOneWidget,
        );
      }
      if (role == 'buyer') {
        expect(find.text('seller@test.local'), findsOneWidget);
        await tester.scrollUntilVisible(find.text('Received'), 100);
        await tester.tap(find.text('Received'));
        await tester.pumpAndSettle();
        expect(gateway.status, 'COMPLETED');
        expect(find.text('Completed'), findsWidgets);
      }
    });
  }
  testWidgets('pending contacts are hidden until refreshed after approval', (
    tester,
  ) async {
    final gateway = FakeOffers()..status = 'PENDING_APPROVAL';
    await tester.pumpWidget(
      MaterialApp(
        home: TransactionDetailsScreen(
          gateway: gateway,
          transactionId: 'transaction',
          user: const AppUser(
            id: 'buyer',
            email: 'buyer@test.local',
            roles: [AppRole.buyer],
          ),
        ),
      ),
    );
    await tester.pump();
    await tester.pump();
    expect(find.text('seller@test.local'), findsNothing);
    expect(
      find.text('Contact details are hidden until approval.'),
      findsOneWidget,
    );
    gateway.status = 'APPROVED';
    await tester.tap(find.text('Refresh transaction'));
    await tester.pump();
    await tester.pump();
    expect(find.text('seller@test.local'), findsOneWidget);
    expect(find.text('Received'), findsNothing);
    expect(find.byTooltip('Call seller'), findsOneWidget);
  });

  testWidgets(
    'transaction details label material value and show transport total',
    (tester) async {
      final gateway = FakeOffers()..transportCost = 294.41;
      await tester.pumpWidget(
        MaterialApp(
          home: TransactionDetailsScreen(
            gateway: gateway,
            transactionId: 'transaction',
            user: const AppUser(
              id: 'buyer',
              email: 'buyer@test.local',
              roles: [AppRole.buyer],
            ),
          ),
        ),
      );
      await tester.pump();
      await tester.pump();
      expect(find.text('Material value'), findsOneWidget);
      expect(find.text('Transport estimate'), findsOneWidget);
      expect(find.text('Total cost'), findsOneWidget);
      expect(find.text('LKR 320294.41'), findsOneWidget);
    },
  );

  testWidgets('piece offers use a simple pieces quantity display', (
    tester,
  ) async {
    final offer = Offer(
      id: 'offer',
      buyerId: 'buyer',
      sellerId: 'seller',
      quantity: 25,
      totalValue: 100,
      status: 'ACCEPTED',
      createdAt: DateTime.utc(2026),
      unit: 'piece',
      packageType: 'PIECE',
      packageSize: 1,
      packageCount: 25,
    );
    expect(offer.quantitySummary, '25 pieces');
  });

  testWidgets(
    'action and refresh failures remain distinguishable and keep details visible',
    (tester) async {
      final gateway = FakeOffers()..status = 'HANDED_OVER';
      await tester.pumpWidget(
        MaterialApp(
          home: TransactionDetailsScreen(
            gateway: gateway,
            transactionId: 'transaction',
            user: const AppUser(
              id: 'buyer',
              email: 'buyer@test.local',
              roles: [AppRole.buyer],
            ),
          ),
        ),
      );
      await tester.pump();
      await tester.pump();
      gateway.failAction = true;
      await tester.scrollUntilVisible(find.text('Yes, Received'), 100);
      await tester.tap(find.text('Yes, Received'));
      await tester.pump();
      await tester.pump();
      expect(find.textContaining('Could not confirm receipt.'), findsOneWidget);
      expect(find.text('Transaction summary'), findsOneWidget);

      gateway.failAction = false;
      gateway.failRefreshAfterAction = true;
      await tester.scrollUntilVisible(find.text('Yes, Received'), 100);
      await tester.tap(find.text('Yes, Received'));
      await tester.pump();
      await tester.pump();
      expect(
        find.text(
          'Action succeeded, but the latest transaction details could not be refreshed.',
        ),
        findsOneWidget,
      );
      expect(find.text('Transaction summary'), findsOneWidget);
    },
  );

  testWidgets('transaction history retries and loads every server page', (
    tester,
  ) async {
    final gateway = FakeOffers()..failHistory = true;
    await tester.pumpWidget(
      MaterialApp(
        home: TransactionHistoryScreen(
          gateway: gateway,
          transactionId: 'transaction',
        ),
      ),
    );
    await tester.pump();
    await tester.pump();
    expect(find.text('Unable to load transaction history.'), findsOneWidget);
    gateway.failHistory = false;
    await tester.tap(find.text('Retry'));
    await tester.pump();
    await tester.pump();
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
  bool failAction = false;
  bool failRefreshAfterAction = false;
  bool actionSucceeded = false;
  double? transportCost;
  String status = 'APPROVED';
  @override
  Future<Transaction> transaction(String id) async {
    if (failRefreshAfterAction && actionSucceeded) {
      throw Exception('refresh failed');
    }
    return Transaction(
      id: id,
      offerId: '12345678-offer',
      buyerId: 'buyer',
      sellerId: 'seller',
      status: status,
      reservedQuantity: 400,
      totalValue: 320000,
      updatedAt: DateTime.utc(2026),
      sellerHandoverConfirmedAt:
          status == 'HANDED_OVER' || status == 'COMPLETED'
          ? DateTime.utc(2026, 10, 1)
          : null,
      buyerReceivedConfirmedAt: status == 'COMPLETED'
          ? DateTime.utc(2026, 10, 2)
          : null,
      transportCost: transportCost,
      buyerContact: const TransactionContact(email: 'buyer@test.local'),
      sellerContact: const TransactionContact(
        email: 'seller@test.local',
        phoneNumber: '0771234567',
      ),
    );
  }

  @override
  Future<Transaction> handover(String id) async {
    if (failAction) {
      throw const ApiException('Server rejected handover.', statusCode: 409);
    }
    status = 'HANDED_OVER';
    final result = await transaction(id);
    actionSucceeded = true;
    return result;
  }

  @override
  Future<Transaction> confirmReceipt(String id) async {
    if (failAction) {
      throw const ApiException(
        'Materials must be handed over before receipt can be confirmed.',
        statusCode: 409,
      );
    }
    status = 'COMPLETED';
    final result = await transaction(id);
    actionSucceeded = true;
    return result;
  }

  final historyPages = <int>[];
  @override
  Future<OfferPage> offers(OfferQuery query) async => OfferPage(
    [
      Offer(
        id: '12345678-offer',
        buyerId: 'buyer',
        sellerId: 'seller',
        quantity: 400,
        totalValue: 320000,
        status: 'ACCEPTED',
        createdAt: DateTime.utc(2026, 9, 22),
      ),
    ],
    1,
    1,
    1,
  );
  @override
  Future<TransactionPage> transactions(OfferQuery query) async =>
      const TransactionPage([], 0, 1, 0);
  @override
  Future<TransactionHistoryPage> history(String id, {int page = 1}) async {
    historyPages.add(page);
    if (failHistory) throw Exception('offline');
    return TransactionHistoryPage(
      [
        TransactionHistoryEntry(
          'event-$page',
          page == 1 ? 'WORKFLOW_APPROVED' : 'TRANSACTION_COMPLETED',
          DateTime.utc(2026, 9, 22),
        ),
      ],
      21,
      page,
      2,
    );
  }
}
