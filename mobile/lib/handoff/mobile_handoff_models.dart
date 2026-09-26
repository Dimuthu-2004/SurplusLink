class MobileHandoffRedemption {
  const MobileHandoffRedemption({
    required this.id,
    required this.categoryId,
    required this.categoryName,
    required this.source,
    required this.redeemedAt,
  });

  final String id;
  final String categoryId;
  final String categoryName;
  final String source;
  final DateTime redeemedAt;

  factory MobileHandoffRedemption.fromJson(Map<String, dynamic> json) {
    return MobileHandoffRedemption(
      id: json['id'] as String,
      categoryId: json['categoryId'] as String,
      categoryName: json['categoryName'] as String,
      source: json['source'] as String? ?? 'REACT_MARKETPLACE',
      redeemedAt: DateTime.parse(json['redeemedAt'] as String),
    );
  }
}
