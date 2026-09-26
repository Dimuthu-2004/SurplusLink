import 'package:mobile/auth/auth_models.dart';
import 'package:mobile/offers/offer_gateway.dart';
import 'package:mobile/offers/offer_models.dart';

import 'marketplace_mode.dart';

/// Filters presentation only; every mutation and detail request is delegated.
/// The existing API has no participation filter, so dual-role lists are
/// filtered across API pages before applying the visible page boundaries.
final class MarketplaceOfferView implements OfferGateway {
  MarketplaceOfferView(this.source, this.user, this.mode);
  final OfferGateway source;
  final AppUser user;
  final MarketplaceMode mode;

  static OfferGateway forUser(
    OfferGateway source,
    AppUser user,
    MarketplaceMode? mode,
  ) => isDualMarketplaceUser(user) && mode != null
      ? MarketplaceOfferView(source, user, mode)
      : source;

  bool _includes(String buyerId, String sellerId) =>
      (mode == MarketplaceMode.buyer ? buyerId : sellerId) == user.id;

  OfferQuery _page(OfferQuery q, int page) => OfferQuery(
    status: q.status,
    offerId: q.offerId,
    sortBy: q.sortBy,
    sortDir: q.sortDir,
    page: page,
    pageSize: q.pageSize,
  );

  @override
  Future<OfferPage> offers(OfferQuery query) async {
    final rows = <Offer>[];
    var page = 1;
    while (true) {
      final result = await source.offers(_page(query, page));
      rows.addAll(result.items.where((r) => _includes(r.buyerId, r.sellerId)));
      if (page >= result.totalPages) break;
      page++;
    }
    return OfferPage(
      rows
          .skip((query.page - 1) * query.pageSize)
          .take(query.pageSize)
          .toList(),
      rows.length,
      query.page,
      (rows.length / query.pageSize).ceil(),
    );
  }

  @override
  Future<TransactionPage> transactions(OfferQuery query) async {
    final rows = <Transaction>[];
    var page = 1;
    while (true) {
      final result = await source.transactions(_page(query, page));
      rows.addAll(result.items.where((r) => _includes(r.buyerId, r.sellerId)));
      if (page >= result.totalPages) break;
      page++;
    }
    return TransactionPage(
      rows
          .skip((query.page - 1) * query.pageSize)
          .take(query.pageSize)
          .toList(),
      rows.length,
      query.page,
      (rows.length / query.pageSize).ceil(),
    );
  }

  @override
  Future<Transaction> transaction(String id) => source.transaction(id);
  @override
  Future<Transaction> handover(String id) => source.handover(id);
  @override
  Future<Transaction> confirmReceipt(String id) => source.confirmReceipt(id);
  @override
  Future<TransactionHistoryPage> history(
    String transactionId, {
    int page = 1,
  }) => source.history(transactionId, page: page);
}
