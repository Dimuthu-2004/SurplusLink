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
    required this.inputMode,
    required this.enteredQuantity,
    required this.enteredUnit,
    this.packageCount,
    this.packageSize,
    required this.normalizedQuantity,
    required this.normalizedBaseUnit,
    this.preferences,
    this.locationText,
    this.notes,
    required this.missingRequiredFields,
    required this.readyForReview,
  });

  final String? templateId;
  final String? categoryId;
  final String itemName;
  final String inputMode;
  final double enteredQuantity;
  final String enteredUnit;
  final int? packageCount;
  final double? packageSize;
  final double normalizedQuantity;
  final String normalizedBaseUnit;
  final Map<String, String>? preferences;
  final String? locationText;
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
      inputMode: json['input_mode'] as String? ?? 'BASE_QUANTITY',
      enteredQuantity: (json['entered_quantity'] as num?)?.toDouble() ?? 0.0,
      enteredUnit: json['entered_unit'] as String? ?? '',
      packageCount: (json['package_count'] as num?)?.toInt(),
      packageSize: (json['package_size'] as num?)?.toDouble(),
      normalizedQuantity:
          (json['normalized_quantity'] as num?)?.toDouble() ?? 0.0,
      normalizedBaseUnit: json['normalized_base_unit'] as String? ?? '',
      preferences: prefs,
      locationText: json['location_text'] as String?,
      notes: json['notes'] as String?,
      missingRequiredFields: missing,
      readyForReview: json['ready_for_review'] as bool? ?? false,
    );
  }
}

/// Typed handoff consumed by the normal requirement form after its template loads.
final class AiRequirementPrefill {
  const AiRequirementPrefill({required this.draft});
  final AiRequirementDraft draft;
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
