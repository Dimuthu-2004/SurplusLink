class RecommendedMatch {
  const RecommendedMatch({
    required this.id,
    required this.requirementId,
    required this.listingId,
    required this.score,
    required this.status,
    required this.createdAt,
    this.distance,
    this.estimatedTransportCost,
    this.rejectionReason,
    this.durationMinutes, this.materialTitle, this.quantity, this.unit, this.unitPrice,
  });
  final String id, requirementId, listingId, status;
  final double score;
  final double? distance, estimatedTransportCost;
  final double? durationMinutes, quantity, unitPrice;
  final String? materialTitle, unit;
  final String? rejectionReason;
  final DateTime createdAt;
  factory RecommendedMatch.fromJson(Map<String, dynamic> json) =>
      RecommendedMatch(
        id: matchString(json, 'id'),
        requirementId: matchString(json, 'requirementId'),
        listingId: matchString(json, 'listingId'),
        score: _number(json, 'score')!,
        status: matchString(json, 'status'),
        createdAt: DateTime.parse(matchString(json, 'createdAt')),
        distance: _number(json, 'distance', optional: true),
        estimatedTransportCost: _number(
          json,
          'estimatedTransportCost',
          optional: true,
        ),
        rejectionReason: json['rejectionReason'] as String?,
        durationMinutes: _number(json, 'durationMinutes', optional: true),
        quantity: _number(json, 'quantity', optional: true),
        unitPrice: _number(json, 'unitPrice', optional: true),
        materialTitle: json['materialTitle'] as String?, unit: json['unit'] as String?,
      );
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
    if (items is! List || items.any((x) => x is! Map<String, dynamic>)) {
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
      items: items.cast<Map<String, dynamic>>().map(parse).toList(),
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
        outcome: json['outcome'] as String?,
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
