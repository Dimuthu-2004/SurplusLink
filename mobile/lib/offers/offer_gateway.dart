import 'offer_models.dart';
abstract interface class OfferGateway {
  Future<OfferPage> offers(OfferQuery query);
  Future<TransactionPage> transactions(OfferQuery query);
  Future<TransactionHistoryPage> history(String transactionId, {int page = 1});
}
