import 'dart:math' as math;

import 'package:mobile/materials/quantity_format.dart' as quantities;

class MatchAllocation {
  const MatchAllocation({
    required this.matchId,
    required this.quantity,
    this.packageCount,
    this.unitPrice = 0,
    this.transportCost = 0,
  });

  final String matchId;
  final double quantity;
  final int? packageCount;
  final double unitPrice, transportCost;
  double get materialValue => (packageCount ?? quantity) * unitPrice;
  double get totalValue => materialValue + transportCost;
  factory MatchAllocation.fromMatch(RecommendedMatch match, double quantity) =>
      MatchAllocation(
        matchId: match.id,
        quantity: quantity,
        packageCount: match.packageCountFor(quantity),
        unitPrice: match.unitPrice ?? 0,
        transportCost: match.estimatedTransportCost ?? 0,
      );

  Map<String, dynamic> toJson() => {
    'matchId': matchId,
    'quantity': quantity,
    if (packageCount != null) 'packageCount': packageCount,
  };
}

class MatchListingContext {
  const MatchListingContext({
    this.description,
    this.templateName,
    this.specificationsJson,
    this.photos = const [],
  });
  final String? description, templateName, specificationsJson;
  final List<String> photos;
  factory MatchListingContext.fromJson(Map<String, dynamic> json) =>
      MatchListingContext(
        description: json['description'] as String?,
        templateName: json['constructionItemTemplateName'] as String?,
        specificationsJson: json['specificationsJson'] as String?,
        photos: (json['photos'] as List? ?? const [])
            .whereType<Map>()
            .map((x) => x['photoUrl']?.toString())
            .whereType<String>()
            .toList(),
      );
}

class PreferenceMismatch {
  const PreferenceMismatch({
    required this.label,
    required this.requestedValue,
    this.sellerValue,
    required this.behavior,
  });
  factory PreferenceMismatch.fromJson(Map<String, dynamic> json) =>
      PreferenceMismatch(
        label: json['label'] as String? ?? 'Specification',
        requestedValue: json['requestedValue']?.toString() ?? '',
        sellerValue: json['sellerValue']?.toString(),
        behavior: json['behavior'] as String? ?? 'SOFT_PREFERENCE',
      );
  final String label, requestedValue, behavior;
  final String? sellerValue;
}

class PreferenceCompatibility {
  const PreferenceCompatibility({
    required this.status,
    required this.matchedCount,
    required this.consideredCount,
    this.mismatches = const [],
  });
  factory PreferenceCompatibility.fromJson(Map<String, dynamic> json) =>
      PreferenceCompatibility(
        status: json['status'] as String? ?? 'NOT_SELECTED',
        matchedCount: (json['matchedCount'] as num?)?.toInt() ?? 0,
        consideredCount: (json['consideredCount'] as num?)?.toInt() ?? 0,
        mismatches: (json['mismatches'] as List? ?? const [])
            .whereType<Map>()
            .map(
              (x) => PreferenceMismatch.fromJson(Map<String, dynamic>.from(x)),
            )
            .toList(),
      );
  final String status;
  final int matchedCount, consideredCount;
  final List<PreferenceMismatch> mismatches;
  bool get hasMismatch => mismatches.isNotEmpty;
}

class RecommendedMatch {
  const RecommendedMatch({
    required this.id,
    required this.requirementId,
    required this.listingId,
    required this.score,
    required this.status,
    required this.createdAt,
    this.valid,
    this.rejected,
    this.distance,
    this.estimatedTransportCost,
    this.rejectionReason,
    this.invalidReason,
    this.durationMinutes,
    this.materialTitle,
    this.categoryName,
    this.sellerId,
    this.quantity,
    this.unit,
    this.unitPrice,
    this.availableUntil,
    this.requiredBy,
    this.availableQuantity,
    this.maximumContribution,
    this.maximumBudget,
    this.requirementStatus,
    this.sellerName,
    this.sellerBusinessName,
    this.sellerDisplayName,
    this.condition,
    this.latitude,
    this.longitude,
    this.sellerAddress,
    this.displayLocation,
    this.fullAddress,
    this.recommendedMatchId,
    this.quantityMode,
    this.packageType,
    this.packageSize,
    this.packageCountAvailable,
    this.listingContext,
    this.preferenceCompatibility,
    bool? isPartial,
    // ignore: prefer_initializing_formals
  }) : _isPartial = isPartial;
  final String id, requirementId, listingId, status;
  final double score;
  final bool? valid, rejected;
  final double? distance, estimatedTransportCost;
  final double? durationMinutes, quantity, unitPrice;
  final String? materialTitle, categoryName, sellerId, unit;
  final String? quantityMode, packageType;
  final double? packageSize;
  final int? packageCountAvailable;
  final MatchListingContext? listingContext;
  final PreferenceCompatibility? preferenceCompatibility;
  final String? rejectionReason, invalidReason;
  final DateTime createdAt;
  final DateTime? availableUntil, requiredBy;
  final double? availableQuantity, maximumContribution, maximumBudget;
  final String? requirementStatus;
  final String? sellerName,
      sellerBusinessName,
      sellerDisplayName,
      condition,
      sellerAddress,
      displayLocation,
      fullAddress;
  final double? latitude, longitude;
  final String? recommendedMatchId;
  final bool? _isPartial;
  bool get aiRecommended => id == recommendedMatchId;

  bool get isRejected =>
      rejected == true || status == 'REJECTED' || rejectionReason != null;
  String? get selectabilityReason => invalidReason ?? rejectionReason;
  double? get estimatedMaterialCost {
    if (quantity == null || unitPrice == null) return null;
    return materialCostFor(quantity!);
  }

  double? materialCostFor(double selectedBaseQuantity) {
    if (unitPrice == null) return null;
    final packages = isPackaged ? packageCountFor(selectedBaseQuantity) : null;
    return (packages ?? selectedBaseQuantity) * unitPrice!;
  }

  bool get isSelectable => valid == true && status == 'ROUTED' && !isRejected;
  // This is an eligibility ceiling calculated by the backend, not an
  // allocation. The buyer still explicitly chooses a quantity at or below it.
  double? get selectableQuantity => maximumContribution == null
      ? availableQuantity
      : availableQuantity == null
      ? maximumContribution
      : math.min(maximumContribution!, availableQuantity!);
  bool get isPackaged => quantityMode == 'PACKAGE' || quantityMode == 'PIECE';
  double get selectionStep => isPackaged
      ? (packageSize ?? 1)
      : (quantities.isDiscreteUnit(unit ?? '') ? 1 : .1);
  int? packageCountFor(double baseQuantity) => isPackaged && packageSize != null
      ? (baseQuantity / packageSize!).round()
      : null;
  bool isWholePackageQuantity(double value) =>
      !isPackaged ||
      packageSize == null ||
      ((value / packageSize!) - (value / packageSize!).round()).abs() <
          0.000001;
  double packageQuantityFor(double requestedBaseQuantity) =>
      isPackaged && packageSize != null
      ? (requestedBaseQuantity / packageSize!).ceilToDouble() * packageSize!
      : requestedBaseQuantity;

  bool get isPartial =>
      _isPartial ??
      (availableQuantity != null &&
          quantity != null &&
          availableQuantity! < quantity!);

  String partialWarning({double? requested}) {
    final req = requested ?? quantity ?? 0;
    final avail = availableQuantity ?? 0;
    final u = unit?.trim() ?? '';
    final reqStr = u.isNotEmpty
        ? '${quantities.formatQuantity(req, u)} $u'
        : quantities.formatQuantity(req, '');
    final availStr = u.isNotEmpty
        ? '${quantities.formatQuantity(avail, u)} $u'
        : quantities.formatQuantity(avail, '');
    return 'You need $reqStr, but this seller currently has only $availStr available.';
  }

  factory RecommendedMatch.fromJson(
    Map<String, dynamic> json,
  ) => RecommendedMatch(
    id: matchString(json, 'id'),
    requirementId: matchString(json, 'requirementId'),
    listingId: matchString(json, 'listingId'),
    score: _number(json, 'score')!,
    status: matchString(json, 'status'),
    createdAt: DateTime.parse(matchString(json, 'createdAt')),
    valid: _boolean(json, 'valid'),
    rejected: _boolean(json, 'rejected'),
    distance: _number(json, 'distance', optional: true),
    estimatedTransportCost: _number(
      json,
      'estimatedTransportCost',
      optional: true,
    ),
    rejectionReason: _optionalString(json, 'rejectionReason'),
    invalidReason: _optionalString(json, 'invalidReason'),
    durationMinutes: _number(json, 'durationMinutes', optional: true),
    quantity: _number(json, 'quantity', optional: true),
    unitPrice: _number(json, 'unitPrice', optional: true),
    materialTitle: _optionalString(json, 'materialTitle'),
    categoryName: _optionalString(json, 'categoryName'),
    sellerId: _optionalString(json, 'sellerId'),
    unit: _optionalString(json, 'unit'),
    availableUntil: _date(json, 'availableUntil'),
    requiredBy: _date(json, 'requiredBy'),
    availableQuantity: _number(json, 'availableQuantity', optional: true),
    maximumContribution: _number(json, 'maximumContribution', optional: true),
    maximumBudget: _number(json, 'maximumBudget', optional: true),
    requirementStatus: _optionalString(json, 'requirementStatus'),
    sellerName: _optionalString(json, 'sellerName'),
    sellerBusinessName: _optionalString(json, 'sellerBusinessName'),
    sellerDisplayName: _optionalString(json, 'sellerDisplayName'),
    condition: _optionalString(json, 'condition'),
    latitude: _number(json, 'latitude', optional: true),
    longitude: _number(json, 'longitude', optional: true),
    sellerAddress: _optionalString(json, 'sellerAddress'),
    displayLocation: _optionalString(json, 'displayLocation'),
    fullAddress: _optionalString(json, 'fullAddress'),
    recommendedMatchId: _optionalString(json, 'recommendedMatchId'),
    isPartial: _boolean(json, 'isPartial'),
    quantityMode: _optionalString(json, 'quantityMode'),
    packageType: _optionalString(json, 'packageType'),
    packageSize: _number(json, 'packageSize', optional: true),
    packageCountAvailable: _number(
      json,
      'packageCountAvailable',
      optional: true,
    )?.toInt(),
    listingContext: json['listingContext'] is Map
        ? MatchListingContext.fromJson(
            Map<String, dynamic>.from(json['listingContext'] as Map),
          )
        : null,
    preferenceCompatibility: json['preferenceCompatibility'] is Map
        ? PreferenceCompatibility.fromJson(
            Map<String, dynamic>.from(json['preferenceCompatibility'] as Map),
          )
        : null,
  );
}

DateTime? _date(Map<String, dynamic> json, String key) {
  final value = json[key];
  if (value == null) return null;
  if (value is String) {
    final parsed = DateTime.tryParse(value);
    if (parsed != null) return parsed;
  }
  throw FormatException('Invalid $key.');
}

class MatchQuery {
  const MatchQuery({
    this.valid,
    this.rejected,
    this.status,
    this.sortBy = 'score',
    this.sortDir = 'desc',
    this.page = 1,
    this.pageSize = 20,
  });
  final bool? valid, rejected;
  final String? status;
  final String sortBy, sortDir;
  final int page, pageSize;
  Map<String, String> toParameters() => {
    if (valid != null) 'valid': '$valid',
    if (rejected != null) 'rejected': '$rejected',
    'status': ?status,
    'sortBy': sortBy,
    'sortDir': sortDir,
    'page': '$page',
    'pageSize': '$pageSize',
  };
}

class MatchPage<T> {
  const MatchPage({
    required this.items,
    required this.total,
    required this.page,
    required this.pageSize,
    required this.totalPages,
  });
  final List<T> items;
  final int total, page, pageSize, totalPages;
  factory MatchPage.fromJson(
    Map<String, dynamic> json,
    T Function(Map<String, dynamic>) parse,
  ) {
    final items = json['items'];
    if (items is! List || items.any((x) => x is! Map)) {
      throw const FormatException('Invalid match page.');
    }
    int integer(String key, int minimum) {
      final value = json[key];
      if (value is! int || value < minimum) {
        throw const FormatException('Invalid match pagination.');
      }
      return value;
    }

    return MatchPage(
      items: items
          .map((item) => parse(Map<String, dynamic>.from(item as Map)))
          .toList(),
      total: integer('total', 0),
      page: integer('page', 1),
      pageSize: integer('pageSize', 1),
      totalPages: integer('totalPages', 0),
    );
  }
}

class MatchHistoryEntry {
  const MatchHistoryEntry({
    required this.id,
    required this.action,
    required this.createdAt,
    this.outcome,
  });
  final String id, action;
  final String? outcome;
  final DateTime createdAt;
  factory MatchHistoryEntry.fromJson(Map<String, dynamic> json) =>
      MatchHistoryEntry(
        id: matchString(json, 'id'),
        action: matchString(json, 'action'),
        outcome: _optionalString(json, 'outcome'),
        createdAt: DateTime.parse(matchString(json, 'createdAt')),
      );
}

String matchString(Map<String, dynamic> json, String key) {
  final value = json[key];
  if (value is! String || value.isEmpty) throw FormatException('Invalid $key.');
  return value;
}

double? _number(
  Map<String, dynamic> json,
  String key, {
  bool optional = false,
}) {
  final value = json[key];
  if (value == null && optional) return null;
  if (value is! num ||
      !value.isFinite ||
      value < 0 ||
      (key == 'score' && value > 1)) {
    throw FormatException('Invalid $key.');
  }
  return value.toDouble();
}

String? _optionalString(Map<String, dynamic> json, String key) {
  final value = json[key];
  if (value == null || value is String) return value as String?;
  throw FormatException('Invalid $key.');
}

bool? _boolean(Map<String, dynamic> json, String key) {
  final value = json[key];
  if (value == null || value is bool) return value as bool?;
  throw FormatException('Invalid $key.');
}
