import 'package:mobile/core/api_client.dart';

import 'offer_gateway.dart';
import 'offer_models.dart';

final class OfferRepository implements OfferGateway {
  OfferRepository(this.api);
  final ApiClient api;
  @override
  Future<Transaction> transaction(String id) async => Transaction.fromJson(
    await api.getJson('/api/transactions/$id', authenticated: true),
  );
  @override
  Future<Transaction> handover(String id) async => Transaction.fromJson(
    await api.postJson(
      '/api/transactions/$id/handover',
      {},
      authenticated: true,
    ),
  );
  @override
  Future<Transaction> confirmReceipt(String id) async => Transaction.fromJson(
    await api.postJson(
      '/api/transactions/$id/confirm-receipt',
      {},
      authenticated: true,
    ),
  );
  @override
  Future<OfferPage> offers(OfferQuery q) async => OfferPage.fromJson(
    await api.getJson(
      Uri(path: '/api/offers', queryParameters: q.toParams()).toString(),
      authenticated: true,
    ),
  );
  @override
  Future<TransactionPage> transactions(OfferQuery q) async =>
      TransactionPage.fromJson(
        await api.getJson(
          Uri(
            path: '/api/transactions',
            queryParameters: q.toParams(),
          ).toString(),
          authenticated: true,
        ),
      );
  @override
  Future<TransactionHistoryPage> history(String id, {int page = 1}) async =>
      TransactionHistoryPage.fromJson(
        await api.getJson(
          '/api/transactions/$id/history?page=$page&pageSize=20',
          authenticated: true,
        ),
      );
}
