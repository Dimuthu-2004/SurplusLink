import 'package:mobile/categories/material_category.dart';

const requirementStatuses = [
  'DRAFT',
  'OPEN',
  'MATCHING',
  'MATCH_FOUND',
  'PENDING_APPROVAL',
  'APPROVED',
  'REJECTED',
  'COMPLETED',
  'CANCELLED',
];

String statusLabel(String status) => status
    .toLowerCase()
    .split('_')
    .map(
      (word) => word.isEmpty ? word : word[0].toUpperCase() + word.substring(1),
    )
    .join(' ');

typedef RequirementCategory = MaterialCategory;

class BuyerRequirement {
  const BuyerRequirement({
    required this.id,
    required this.buyerId,
    required this.categoryId,
    required this.requiredQuantity,
    required this.unit,
    required this.maximumBudget,
    required this.deadline,
    required this.status,
    required this.createdAt,
    required this.updatedAt,
    this.workflowId, this.workflowStatus, this.decisionNote,
    this.latitude,
    this.longitude,
    this.notes = '',
  });
  final String id, buyerId, categoryId, unit, status, notes;
  final String? workflowId, workflowStatus, decisionNote;
  final num requiredQuantity, maximumBudget;
  final double? latitude, longitude;
  final DateTime deadline, createdAt, updatedAt;
  bool get canEdit => status == 'DRAFT' || status == 'OPEN' || status == 'MATCH_FOUND';
  bool get canSubmit => status == 'DRAFT' && deadline.isAfter(DateTime.now());
  bool get canStart => (status == 'OPEN' || status == 'MATCH_FOUND') && deadline.isAfter(DateTime.now());
  bool get canCancel => status == 'DRAFT' || status == 'OPEN';
  factory BuyerRequirement.fromJson(Map<String, dynamic> json) =>
      BuyerRequirement(
        id: json['id'] as String,
        buyerId: json['buyerId'] as String,
        categoryId: json['categoryId'] as String,
        requiredQuantity: json['requiredQuantity'] as num,
        unit: json['unit'] as String,
        maximumBudget: json['maximumBudget'] as num,
        deadline: DateTime.parse(json['deadline'] as String),
          status: json['status'] as String,
          workflowId: json['workflowId'] as String?, workflowStatus: json['workflowStatus'] as String?,
          decisionNote: json['decisionNote'] as String?,
        notes: json['notes'] as String? ?? '',
        latitude: (json['latitude'] as num?)?.toDouble(),
        longitude: (json['longitude'] as num?)?.toDouble(),
        createdAt: DateTime.parse(json['createdAt'] as String),
        updatedAt: DateTime.parse(json['updatedAt'] as String),
      );
}

class RequirementDraft {
  const RequirementDraft({
    required this.categoryId,
    required this.requiredQuantity,
    required this.unit,
    required this.maximumBudget,
    required this.deadline,
    required this.latitude,
    required this.longitude,
    this.notes = '',
  });
  final String categoryId, unit, notes;
  final num requiredQuantity, maximumBudget;
  final DateTime deadline;
  final double latitude, longitude;
  Map<String, dynamic> toJson() => {
    'categoryId': categoryId,
    'requiredQuantity': requiredQuantity,
    'unit': unit.trim(),
    'maximumBudget': maximumBudget,
    'deadline': deadline.toUtc().toIso8601String(),
    'latitude': double.parse(latitude.toStringAsFixed(6)),
    'longitude': double.parse(longitude.toStringAsFixed(6)),
    'notes': notes.trim(),
  };
}

class RequirementQuery {
  const RequirementQuery({
    this.search = '',
    this.status,
    this.categoryId,
    this.deadlineFrom,
    this.deadlineTo,
    this.sort = 'createdAt',
    this.sortDir = 'desc',
    this.page = 1,
    this.pageSize = 10,
  });
  final String search, sort, sortDir;
  final String? status, categoryId;
  final DateTime? deadlineFrom, deadlineTo;
  final int page, pageSize;
  Map<String, String> toQueryParameters() => {
    if (search.trim().isNotEmpty) 'search': search.trim(),
    'status': ?status,
    'categoryId': ?categoryId,
    if (deadlineFrom != null)
      'deadlineFrom': deadlineFrom!.toUtc().toIso8601String(),
    if (deadlineTo != null) 'deadlineTo': deadlineTo!.toUtc().toIso8601String(),
    'sort': sort,
    'sortDir': sortDir,
    'page': '$page',
    'pageSize': '$pageSize',
  };
}

class RequirementPage<T> {
  const RequirementPage({
    required this.items,
    required this.total,
    required this.page,
    required this.pageSize,
  });
  final List<T> items;
  final int total, page, pageSize;
  int get totalPages => (total / pageSize).ceil();
  factory RequirementPage.fromJson(
    Map<String, dynamic> json,
    T Function(Map<String, dynamic>) parse,
  ) => RequirementPage(
    items: (json['items'] as List)
        .map((item) => parse(item as Map<String, dynamic>))
        .toList(),
    total: json['total'] as int,
    page: json['page'] as int,
    pageSize: json['pageSize'] as int,
  );
}

class RequirementHistoryEntry {
  const RequirementHistoryEntry({
    required this.id,
    required this.action,
    required this.createdAt,
    this.actorUserId,
    this.fromStatus,
    this.toStatus,
  });
  final String id, action;
  final String? actorUserId, fromStatus, toStatus;
  final DateTime createdAt;
  factory RequirementHistoryEntry.fromJson(Map<String, dynamic> json) =>
      RequirementHistoryEntry(
        id: json['id'] as String,
        action: json['action'] as String,
        actorUserId: json['actorUserId'] as String?,
        fromStatus: json['fromStatus'] as String?,
        toStatus: json['toStatus'] as String?,
        createdAt: DateTime.parse(json['createdAt'] as String),
      );
}

class RequirementStartResult {
  const RequirementStartResult(this.requirement, this.workflowId);
  final BuyerRequirement requirement;
  final String workflowId;
}
