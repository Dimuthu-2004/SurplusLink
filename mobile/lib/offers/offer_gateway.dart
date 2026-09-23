import 'offer_models.dart';

abstract interface class OfferGateway {
  Future<Transaction> transaction(String id);
  Future<Transaction> handover(String id);
  Future<Transaction> confirmReceipt(String id);
  Future<OfferPage> offers(OfferQuery query);
  Future<TransactionPage> transactions(OfferQuery query);
  Future<TransactionHistoryPage> history(String transactionId, {int page = 1});
}
