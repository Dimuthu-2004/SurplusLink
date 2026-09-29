import 'dart:convert';

final class TemplateAttributeField {
  const TemplateAttributeField({
    required this.id,
    required this.label,
    required this.type,
    this.required = false,
    this.options = const [],
    this.placeholder,
    this.unit,
    this.helper,
    this.priority = 'OPTIONAL',
    this.buyerPreference = false,
    this.allowOther = false,
    this.labelI18n = const {},
  });

  factory TemplateAttributeField.fromJson(Map<String, dynamic> json) {
    return TemplateAttributeField(
      id: json['id'] as String? ?? '',
      label: json['label'] as String? ?? '',
      type: json['type'] as String? ?? 'string',
      required: json['required'] as bool? ?? false,
      options: (json['options'] as List<dynamic>? ?? const [])
          .map((e) => e.toString())
          .toList(),
      placeholder: json['placeholder'] as String?,
      unit: json['unit'] as String?,
      helper: json['helper'] as String?,
      priority: json['priority'] as String? ?? ((json['required'] as bool? ?? false) ? 'REQUIRED' : 'OPTIONAL'),
      buyerPreference: json['buyerPreference'] as bool? ?? false,
      allowOther: json['allowOther'] as bool? ?? false,
      labelI18n: (json['labelI18n'] as Map<String, dynamic>? ?? const {})
          .map((key, value) => MapEntry(key, value.toString())),
    );
  }

  final String id;
  final String label;
  final String type; // 'string', 'number', 'select'
  final bool required;
  final List<String> options;
  final String? placeholder;
  final String? unit;
  final String? helper;
  final String priority;
  final bool buyerPreference, allowOther;
  final Map<String, String> labelI18n;

  bool get isRequired => priority == 'REQUIRED' || required;
  String labelFor(String languageCode) => labelI18n[languageCode] ?? labelI18n['en'] ?? label;
}

final class ConstructionItemTemplate {
  const ConstructionItemTemplate({
    required this.id,
    required this.name,
    required this.categoryId,
    required this.categoryName,
    required this.itemClass,
    required this.quantityMode,
    required this.baseUnit,
    this.packageType,
    this.allowedUnits = const [],
    this.allowedPackageSizes = const [],
    this.attributeSchema = '[]',
    this.priceBasis = 'PER_UNIT',
    this.isActive = true,
  });

  factory ConstructionItemTemplate.fromJson(Map<String, dynamic> json) {
    final rawSizes = json['allowedPackageSizes'] as List<dynamic>? ?? const [];
    final sizes = rawSizes
        .map((e) => (e is num) ? e.toDouble() : double.tryParse(e.toString()))
        .whereType<double>()
        .toList();

    final rawUnits = json['allowedUnits'] as List<dynamic>? ?? const [];
    final units = rawUnits.map((e) => e.toString()).toList();

    return ConstructionItemTemplate(
      id: json['id'] as String? ?? '',
      name: json['name'] as String? ?? '',
      categoryId: json['categoryId'] as String? ?? '',
      categoryName: json['categoryName'] as String? ?? '',
      itemClass: json['itemClass'] as String? ?? 'MATERIAL',
      quantityMode: json['quantityMode'] as String? ?? 'PIECE',
      baseUnit: json['baseUnit'] as String? ?? 'unit',
      packageType: json['packageType'] as String?,
      allowedUnits: units,
      allowedPackageSizes: sizes,
      attributeSchema: json['attributeSchema'] as String? ?? '[]',
      priceBasis: json['priceBasis'] as String? ?? 'PER_UNIT',
      isActive: json['isActive'] as bool? ?? true,
    );
  }

  final String id;
  final String name;
  final String categoryId;
  final String categoryName;
  final String itemClass; // 'MATERIAL', 'TOOL', 'EQUIPMENT', 'FIXTURE', etc.
  final String quantityMode; // 'PACKAGE', 'PIECE', 'CONTINUOUS_BULK', etc.
  final String baseUnit;
  final String? packageType;
  final List<String> allowedUnits;
  final List<double> allowedPackageSizes;
  final String attributeSchema;
  final String priceBasis;
  final bool isActive;

  bool get isPackage => quantityMode == 'PACKAGE';
  bool get isPiece => quantityMode == 'PIECE' || itemClass == 'TOOL' || itemClass == 'EQUIPMENT';
  bool get isContinuous => !isPackage && !isPiece;

  List<TemplateAttributeField> get parsedAttributes {
    try {
      final decoded = jsonDecode(attributeSchema);
      if (decoded is List) {
        return decoded
            .whereType<Map>()
            .map((e) => TemplateAttributeField.fromJson(e.cast<String, dynamic>()))
            .toList();
      }
    } catch (_) {}
    return const [];
  }
}
