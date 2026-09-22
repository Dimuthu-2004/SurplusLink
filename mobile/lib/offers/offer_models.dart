const offerStatuses = ['PENDING', 'ACCEPTED', 'REJECTED', 'REVISION_REQUESTED'];
const transactionStatuses = [
  'PENDING_APPROVAL',
  'APPROVED',
  'REJECTED',
  'COMPLETED',
];
String offerStatusLabel(String value) => value
    .toLowerCase()
    .split('_')
    .map((x) => x.isEmpty ? x : x[0].toUpperCase() + x.substring(1))
    .join(' ');

class OfferQuery {
  const OfferQuery({
    this.status,
    this.sortBy = 'createdAt',
    this.sortDir = 'desc',
    this.page = 1,
    this.pageSize = 20,
  });
  final String? status, sortBy, sortDir;
  final int page, pageSize;
  Map<String, String> toParams() => {
    'status': ?status,
    'sortBy': sortBy!,
    'sortDir': sortDir!,
    'page': '$page',
    'pageSize': '$pageSize',
  };
}

class Offer {
  const Offer({
    required this.id,
    required this.buyerId,
    required this.sellerId,
    required this.quantity,
    required this.totalValue,
    required this.status,
    required this.createdAt,
  });
  factory Offer.fromJson(Map<String, dynamic> j) => Offer(
    id: j['id'] as String,
    buyerId: j['buyerId'] as String,
    sellerId: j['sellerId'] as String,
    quantity: (j['quantity'] as num).toDouble(),
    totalValue: (j['totalValue'] as num).toDouble(),
    status: j['status'] as String,
    createdAt: DateTime.parse(j['createdAt'] as String),
  );
  final String id, buyerId, sellerId, status;
  final double quantity, totalValue;
  final DateTime createdAt;
}

class Transaction {
  const Transaction({
    required this.id,
    required this.offerId,
    required this.status,
    required this.reservedQuantity,
    required this.totalValue,
    required this.updatedAt,
  });
  factory Transaction.fromJson(Map<String, dynamic> j) => Transaction(
    id: j['id'] as String,
    offerId: j['offerId'] as String,
    status: j['status'] as String,
    reservedQuantity: (j['reservedQuantity'] as num).toDouble(),
    totalValue: (j['totalValue'] as num).toDouble(),
    updatedAt: DateTime.parse(j['updatedAt'] as String),
  );
  final String id, offerId, status;
  final double reservedQuantity, totalValue;
  final DateTime updatedAt;
}

class OfferPage {
  const OfferPage(this.items, this.total, this.page, this.totalPages);
  factory OfferPage.fromJson(Map<String, dynamic> j) => OfferPage(
    (j['items'] as List)
        .whereType<Map<String, dynamic>>()
        .map(Offer.fromJson)
        .toList(),
    j['total'] as int,
    j['page'] as int,
    j['totalPages'] as int,
  );
  final List<Offer> items;
  final int total, page, totalPages;
}

class TransactionPage {
  const TransactionPage(this.items, this.total, this.page, this.totalPages);
  factory TransactionPage.fromJson(Map<String, dynamic> j) => TransactionPage(
    (j['items'] as List)
        .whereType<Map<String, dynamic>>()
        .map(Transaction.fromJson)
        .toList(),
    j['total'] as int,
    j['page'] as int,
    j['totalPages'] as int,
  );
  final List<Transaction> items;
  final int total, page, totalPages;
}

class TransactionHistoryEntry {
  const TransactionHistoryEntry(this.id, this.action, this.createdAt);
  factory TransactionHistoryEntry.fromJson(Map<String, dynamic> j) =>
      TransactionHistoryEntry(
        j['id'] as String,
        j['action'] as String,
        DateTime.parse(j['createdAt'] as String),
      );
  final String id, action;
  final DateTime createdAt;
}

class TransactionHistoryPage {
  const TransactionHistoryPage(
    this.items,
    this.total,
    this.page,
    this.totalPages,
  );
  factory TransactionHistoryPage.fromJson(Map<String, dynamic> j) =>
      TransactionHistoryPage(
        (j['items'] as List)
            .whereType<Map<String, dynamic>>()
            .map(TransactionHistoryEntry.fromJson)
            .toList(),
        j['total'] as int,
        j['page'] as int,
        j['totalPages'] as int,
      );
  final List<TransactionHistoryEntry> items;
  final int total, page, totalPages;
}
