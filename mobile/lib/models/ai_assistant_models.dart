final class AiCitation {
  const AiCitation({
    required this.title,
    required this.source,
    required this.url,
    required this.section,
  });

  final String title;
  final String source;
  final String url;
  final String section;

  factory AiCitation.fromJson(Map<String, dynamic> json) {
    return AiCitation(
      title: json['title'] as String? ?? '',
      source: json['source'] as String? ?? '',
      url: json['url'] as String? ?? '',
      section: json['section'] as String? ?? '',
    );
  }
}

final class AiRequirementDraft {
  const AiRequirementDraft({
    this.templateId,
    this.categoryId,
    required this.itemName,
    this.displayName,
    this.isCustomItem = false,
    required this.inputMode,
    required this.enteredQuantity,
    required this.enteredUnit,
    this.packageCount,
    this.packageSize,
    this.packageUnit,
    required this.normalizedQuantity,
    required this.normalizedBaseUnit,
    this.coverageArea,
    this.calculatedPhysicalQuantity,
    this.calculatedPackageCount,
    this.preferences,
    this.locationText,
    this.locationSource,
    this.locationPending = false,
    this.latitude,
    this.longitude,
    this.resolvedAddress,
    this.notes,
    required this.missingRequiredFields,
    required this.readyForReview,
  });

  final String? templateId;
  final String? categoryId;
  final String itemName;
  final String? displayName;
  final bool isCustomItem;
  final String inputMode;
  final double enteredQuantity;
  final String enteredUnit;
  final int? packageCount;
  final double? packageSize;
  final String? packageUnit;
  final double normalizedQuantity;
  final String normalizedBaseUnit;
  final double? coverageArea;
  final int? calculatedPhysicalQuantity;
  final int? calculatedPackageCount;
  final Map<String, String>? preferences;
  final String? locationText;
  final String? locationSource;
  final bool locationPending;
  final double? latitude;
  final double? longitude;
  final String? resolvedAddress;
  final String? notes;
  final List<String> missingRequiredFields;
  final bool readyForReview;

  factory AiRequirementDraft.fromJson(Map<String, dynamic> json) {
    Map<String, String>? prefs;
    if (json['preferences'] is Map) {
      prefs = (json['preferences'] as Map).map(
        (key, value) => MapEntry(key.toString(), value.toString()),
      );
    }

    List<String> missing = [];
    if (json['missing_required_fields'] is List) {
      missing = (json['missing_required_fields'] as List)
          .map((e) => e.toString())
          .toList();
    }

    return AiRequirementDraft(
      templateId: json['template_id'] as String?,
      categoryId: json['category_id'] as String?,
      itemName: json['item_name'] as String? ?? '',
      displayName: json['display_name'] as String?,
      isCustomItem: json['is_custom_item'] as bool? ?? false,
      inputMode: json['input_mode'] as String? ?? 'BASE_QUANTITY',
      enteredQuantity: (json['entered_quantity'] as num?)?.toDouble() ?? 0.0,
      enteredUnit: json['entered_unit'] as String? ?? '',
      packageCount: (json['package_count'] as num?)?.toInt(),
      packageSize: (json['package_size'] as num?)?.toDouble(),
      packageUnit: json['package_unit'] as String?,
      normalizedQuantity:
          (json['normalized_quantity'] as num?)?.toDouble() ?? 0.0,
      normalizedBaseUnit: json['normalized_base_unit'] as String? ?? '',
      coverageArea: (json['coverage_area'] as num?)?.toDouble(),
      calculatedPhysicalQuantity:
          (json['calculated_physical_quantity'] as num?)?.toInt(),
      calculatedPackageCount:
          (json['calculated_package_count'] as num?)?.toInt(),
      preferences: prefs,
      locationText: json['location_text'] as String?,
      locationSource: json['location_source'] as String?,
      locationPending: json['location_pending'] as bool? ?? false,
      latitude: (json['latitude'] as num?)?.toDouble(),
      longitude: (json['longitude'] as num?)?.toDouble(),
      resolvedAddress: json['resolved_address'] as String?,
      notes: json['notes'] as String?,
      missingRequiredFields: missing,
      readyForReview: json['ready_for_review'] as bool? ?? false,
    );
  }
}

/// Typed handoff contract consumed by RequirementFormScreen.
final class AiRequirementPrefill {
  const AiRequirementPrefill({required this.draft});
  final AiRequirementDraft draft;

  String get itemName => draft.displayName ?? draft.itemName;
  bool get isCustomItem => draft.isCustomItem;
  String? get templateId => draft.templateId;
  String? get categoryId => draft.categoryId;
  String get inputMode => draft.inputMode;

  double get quantity => draft.enteredQuantity > 0
      ? draft.enteredQuantity
      : (draft.calculatedPhysicalQuantity?.toDouble() ?? draft.normalizedQuantity);

  String get unit => draft.enteredUnit.isNotEmpty
      ? draft.enteredUnit
      : draft.normalizedBaseUnit;

  int? get packageCount => draft.packageCount ?? draft.calculatedPackageCount;
  double? get packageSize => draft.packageSize;
  String? get packageUnit => draft.packageUnit;

  double? get coverageArea => draft.coverageArea;
  int? get calculatedPhysicalQuantity => draft.calculatedPhysicalQuantity;
  int? get calculatedPackageCount => draft.calculatedPackageCount;

  String? get locationText => draft.locationText;
  String? get locationSource => draft.locationSource;
  bool get locationPending => draft.locationPending;
  double? get latitude => draft.latitude;
  double? get longitude => draft.longitude;
  String? get resolvedAddress => draft.resolvedAddress;

  Map<String, String>? get preferences => draft.preferences;
  String? get notes => draft.notes;
}

enum ChatRole { user, assistant }

final class AiChatMessage {
  AiChatMessage({
    required this.id,
    required this.conversationId,
    required this.role,
    required this.content,
    this.intent,
    this.citations = const [],
    this.draft,
    this.suggestedActions = const [],
    required this.timestamp,
    this.isError = false,
  });

  final String id;
  final String conversationId;
  final ChatRole role;
  final String content;
  final String? intent;
  final List<AiCitation> citations;
  final AiRequirementDraft? draft;
  final List<String> suggestedActions;
  final DateTime timestamp;
  final bool isError;
}
