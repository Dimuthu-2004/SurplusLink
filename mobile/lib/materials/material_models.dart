final class MaterialPhoto {
  const MaterialPhoto({
    required this.id,
    required this.photoUrl,
    required this.sortOrder,
  });

  factory MaterialPhoto.fromJson(Map<String, dynamic> json) => MaterialPhoto(
    id: _requiredString(json, 'id'),
    photoUrl: _requiredString(json, 'photoUrl'),
    sortOrder: _number(json, 'sortOrder').toInt(),
  );

  final String id;
  final String photoUrl;
  final int sortOrder;
}

final class MaterialListing {
  const MaterialListing({
    required this.id,
    required this.sellerId,
    required this.categoryId,
    required this.categoryName,
    required this.title,
    required this.description,
    required this.quantity,
    required this.reservedQuantity,
    required this.unit,
    required this.condition,
    required this.unitPrice,
    required this.latitude,
    required this.longitude,
    required this.availableUntil,
    required this.status,
    required this.createdAtUtc,
    required this.updatedAtUtc,
    required this.photos,
    this.seller,
  });

  factory MaterialListing.fromJson(Map<String, dynamic> json) =>
      MaterialListing(
        seller: json['seller'] is Map<String, dynamic>
            ? SellerContact.fromJson(json['seller'] as Map<String, dynamic>)
            : null,
        id: _requiredString(json, 'id'),
        sellerId: _requiredString(json, 'sellerId'),
        categoryId: _requiredString(json, 'categoryId'),
        categoryName: _requiredString(json, 'categoryName'),
        title: _requiredString(json, 'title'),
        description: _requiredString(json, 'description'),
        quantity: _number(json, 'quantity'),
        reservedQuantity: _number(json, 'reservedQuantity'),
        unit: _requiredString(json, 'unit'),
        condition: _requiredString(json, 'condition'),
        unitPrice: _number(json, 'unitPrice'),
        latitude: _nullableNumber(json['latitude']),
        longitude: _nullableNumber(json['longitude']),
        availableUntil: DateTime.parse(_requiredString(json, 'availableUntil'))
            .toLocal(),
        status: _requiredString(json, 'status'),
        createdAtUtc: DateTime.parse(_requiredString(json, 'createdAtUtc'))
            .toLocal(),
        updatedAtUtc: DateTime.parse(_requiredString(json, 'updatedAtUtc'))
            .toLocal(),
        photos: (json['photos'] as List<dynamic>? ?? const [])
            .whereType<Map<String, dynamic>>()
            .map(MaterialPhoto.fromJson)
            .toList(),
      );

  final String id;
  final String sellerId;
  final SellerContact? seller;
  final String categoryId;
  final String categoryName;
  final String title;
  final String description;
  final double quantity;
  final double reservedQuantity;
  final String unit;
  final String condition;
  final double unitPrice;
  final double? latitude;
  final double? longitude;
  final DateTime availableUntil;
  final String status;
  final DateTime createdAtUtc;
  final DateTime updatedAtUtc;
  final List<MaterialPhoto> photos;

  double get remainingQuantity => quantity - reservedQuantity;
  bool get canEdit => status == 'DRAFT' || status == 'REJECTED';
  bool get canPublish => canEdit;
}

final class MaterialListingPage {
  const MaterialListingPage({
    required this.items,
    required this.totalCount,
    required this.totalPages,
    required this.page,
    required this.pageSize,
  });

  factory MaterialListingPage.fromJson(Map<String, dynamic> json) =>
      MaterialListingPage(
        items: (json['items'] as List<dynamic>? ?? const [])
            .whereType<Map<String, dynamic>>()
            .map(MaterialListing.fromJson)
            .toList(),
        totalCount: _number(json, 'totalCount').toInt(),
        totalPages: _number(json, 'totalPages').toInt(),
        page: _number(json, 'page').toInt(),
        pageSize: _number(json, 'pageSize').toInt(),
      );

  final List<MaterialListing> items;
  final int totalCount;
  final int totalPages;
  final int page;
  final int pageSize;

  bool get hasNextPage => page < totalPages;
}

final class MaterialListingHistoryEntry {
  const MaterialListingHistoryEntry({
    required this.id,
    required this.actorUserId,
    required this.action,
    required this.createdAtUtc,
  });

  factory MaterialListingHistoryEntry.fromJson(Map<String, dynamic> json) =>
      MaterialListingHistoryEntry(
        id: _requiredString(json, 'id'),
        actorUserId: json['actorUserId'] as String?,
        action: _requiredString(json, 'action'),
        createdAtUtc: DateTime.parse(_requiredString(json, 'createdAtUtc'))
            .toLocal(),
      );

  final String id;
  final String? actorUserId;
  final String action;
  final DateTime createdAtUtc;
}

final class MaterialListingQuery {
  const MaterialListingQuery({
    this.mineOnly = false,
    this.search,
    this.category,
    this.status,
    this.condition,
    this.minPrice,
    this.maxPrice,
    this.sortBy = 'createdAt',
    this.sortDir = 'desc',
    this.page = 1,
    this.pageSize = 20,
  });

  final bool mineOnly;
  final String? search;
  final String? category;
  final String? status;
  final String? condition;
  final double? minPrice;
  final double? maxPrice;
  final String sortBy;
  final String sortDir;
  final int page;
  final int pageSize;

  Map<String, String> toQueryParameters() {
    final values = <String, String>{
      if (mineOnly) 'mineOnly': 'true',
      'sortBy': sortBy,
      'sortDir': sortDir,
      'page': '$page',
      'pageSize': '$pageSize',
    };
    void add(String key, String? value) {
      if (value != null && value.trim().isNotEmpty) values[key] = value.trim();
    }

    add('search', search);
    add('category', category);
    add('status', status);
    add('condition', condition);
    if (minPrice != null) values['minPrice'] = '$minPrice';
    if (maxPrice != null) values['maxPrice'] = '$maxPrice';
    return values;
  }
}

final class MaterialListingDraft {
  const MaterialListingDraft({
    required this.categoryId,
    required this.title,
    required this.description,
    required this.quantity,
    required this.unit,
    required this.condition,
    required this.unitPrice,
    required this.availableUntil,
    required this.photoUrls,
    this.latitude,
    this.longitude,
  });

  final String categoryId;
  final String title;
  final String description;
  final double quantity;
  final String unit;
  final String condition;
  final double unitPrice;
  final double? latitude;
  final double? longitude;
  final DateTime availableUntil;
  final List<String> photoUrls;

  Map<String, dynamic> toJson() => {
    'categoryId': categoryId,
    'title': title.trim(),
    'description': description.trim(),
    'quantity': quantity,
    'unit': unit.trim(),
    'condition': condition,
    'unitPrice': unitPrice,
    'latitude': latitude,
    'longitude': longitude,
    'availableUntil': availableUntil.toUtc().toIso8601String(),
    'photos': [
      for (var index = 0; index < photoUrls.length; index++)
        {'photoUrl': photoUrls[index], 'sortOrder': index},
    ],
  };
}

String _requiredString(Map<String, dynamic> json, String key) {
  final value = json[key];
  if (value is! String || value.isEmpty) {
    throw FormatException('Missing or invalid $key.');
  }
  return value;
}

double _number(Map<String, dynamic> json, String key) {
  final value = json[key];
  if (value is num) return value.toDouble();
  throw FormatException('Missing or invalid $key.');
}

double? _nullableNumber(Object? value) =>
    value is num ? value.toDouble() : null;

class SellerContact {
  const SellerContact({
    this.fullName,
    this.businessName,
    this.phoneNumber,
    required this.email,
  });
  factory SellerContact.fromJson(Map<String, dynamic> json) => SellerContact(
    fullName: json['fullName'] as String?,
    businessName: json['businessName'] as String?,
    phoneNumber: json['phoneNumber'] as String?,
    email: json['email'] as String,
  );
  final String? fullName, businessName, phoneNumber;
  final String email;
}
