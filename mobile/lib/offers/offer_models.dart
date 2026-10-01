import 'package:mobile/auth/auth_models.dart';

const offerStatuses = ['PENDING', 'ACCEPTED', 'REJECTED', 'REVISION_REQUESTED'];
const transactionStatuses = [
  'PENDING_APPROVAL',
  'APPROVED',
  'HANDED_OVER',
  'MANAGER_REVIEW_REQUIRED',
  'NOT_COMPLETED',
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
    this.offerId,
    this.sortBy = 'createdAt',
    this.sortDir = 'desc',
    this.page = 1,
    this.pageSize = 20,
  });
  final String? status, sortBy, sortDir, offerId;
  final int page, pageSize;
  Map<String, String> toParams() => {
    'status': ?status,
    'offerId': ?offerId,
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
    this.buyerName,
    this.sellerName,
    this.sellerBusinessName,
    this.materialName,
    this.requirementTitle,
    this.unit,
    this.listingPhotoUrl,
    this.packageType,
    this.packageSize,
    this.packageCount,
  });
  factory Offer.fromJson(Map<String, dynamic> j) => Offer(
    id: j['id'] as String,
    buyerId: j['buyerId'] as String,
    sellerId: j['sellerId'] as String,
    quantity: (j['quantity'] as num).toDouble(),
    totalValue: (j['totalValue'] as num).toDouble(),
    status: j['status'] as String,
    createdAt: DateTime.parse(j['createdAt'] as String),
    buyerName: j['buyerName'] as String?, sellerName: j['sellerName'] as String?,
    sellerBusinessName: j['sellerBusinessName'] as String?, materialName: j['materialName'] as String?,
    requirementTitle: j['requirementTitle'] as String?, unit: j['unit'] as String?,
    listingPhotoUrl: j['listingPhotoUrl'] as String?, packageType: j['packageType'] as String?,
    packageSize: (j['packageSize'] as num?)?.toDouble(), packageCount: (j['packageCount'] as num?)?.toInt(),
  );
  final String id, buyerId, sellerId, status;
  final double quantity, totalValue;
  final DateTime createdAt;
  final String? buyerName, sellerName, sellerBusinessName, materialName, requirementTitle, unit, listingPhotoUrl, packageType;
  final double? packageSize;
  final int? packageCount;
  String titleFor(AppUser user) => buyerId == user.id ? (materialName ?? requirementTitle ?? 'Material offer') : (requirementTitle ?? materialName ?? 'Material requirement');
  String counterpartyFor(AppUser user) => buyerId == user.id ? (sellerBusinessName ?? sellerName ?? 'Seller') : (buyerName ?? 'Buyer');
  String get quantitySummary => packageType != null && packageSize != null
      ? '${packageCount ?? (quantity / packageSize!).round()} ${packageType!.toLowerCase()} × $packageSize ${unit ?? ''}'
      : '$quantity ${unit ?? ''}';
}

class Transaction {
  const Transaction({
    required this.id,
    required this.offerId,
    this.buyerId = '',
    this.sellerId = '',
    this.buyerContact,
    this.sellerContact,
    required this.status,
    required this.reservedQuantity,
    required this.totalValue,
    required this.updatedAt,
    this.confirmationDeadline,
    this.sellerHandoverConfirmedAt,
    this.buyerReceivedConfirmedAt,
  });
  factory Transaction.fromJson(Map<String, dynamic> j) => Transaction(
    id: j['id'] as String,
    offerId: j['offerId'] as String,
    buyerId: j['buyerId'] as String,
    sellerId: j['sellerId'] as String,
    buyerContact: j['buyerContact'] == null
        ? null
        : TransactionContact.fromJson(j['buyerContact']),
    sellerContact: j['sellerContact'] == null
        ? null
        : TransactionContact.fromJson(j['sellerContact']),
    status: j['status'] as String,
    reservedQuantity: (j['reservedQuantity'] as num).toDouble(),
    totalValue: (j['totalValue'] as num).toDouble(),
    updatedAt: DateTime.parse(j['updatedAt'] as String),
    confirmationDeadline: j['confirmationDeadline'] == null ? null : DateTime.parse(j['confirmationDeadline'] as String),
    sellerHandoverConfirmedAt: j['sellerHandoverConfirmedAt'] == null ? null : DateTime.parse(j['sellerHandoverConfirmedAt'] as String),
    buyerReceivedConfirmedAt: j['buyerReceivedConfirmedAt'] == null ? null : DateTime.parse(j['buyerReceivedConfirmedAt'] as String),
  );
  final String id, offerId, status, buyerId, sellerId;
  final TransactionContact? buyerContact, sellerContact;
  bool get contactsVisible =>
      const ['APPROVED', 'HANDED_OVER', 'COMPLETED'].contains(status);
  bool canHandover(AppUser user) =>
      status == 'APPROVED' &&
      sellerHandoverConfirmedAt == null &&
      sellerId == user.id &&
      user.hasRole(AppRole.seller);
  bool canConfirmReceipt(AppUser user) =>
      status == 'HANDED_OVER' &&
      sellerHandoverConfirmedAt != null &&
      buyerReceivedConfirmedAt == null &&
      buyerId == user.id &&
      user.hasRole(AppRole.buyer);
  final double reservedQuantity, totalValue;
  final DateTime updatedAt;
  final DateTime? confirmationDeadline, sellerHandoverConfirmedAt, buyerReceivedConfirmedAt;
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

class TransactionContact {
  const TransactionContact({
    this.fullName,
    required this.email,
    this.phoneNumber,
  });
  factory TransactionContact.fromJson(Map<String, dynamic> j) =>
      TransactionContact(
        fullName: j['fullName'] as String?,
        email: j['email'] as String,
        phoneNumber: j['phoneNumber'] as String?,
      );
  final String? fullName, phoneNumber;
  final String email;
}
