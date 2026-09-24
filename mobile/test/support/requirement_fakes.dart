import 'dart:async';

import 'package:mobile/requirements/requirement_gateway.dart';
import 'package:mobile/requirements/requirement_location.dart';
import 'package:mobile/requirements/requirement_models.dart';

import 'fakes.dart';

BuyerRequirement testRequirement({
  String status = 'DRAFT',
  String notes = 'Deliver to site',
  DateTime? deadline,
}) => BuyerRequirement(
  id: 'r1',
  buyerId: buyerUser.id,
  categoryId: 'c1',
  requiredQuantity: 12,
  unit: 'kg',
  maximumBudget: 100,
  deadline: deadline ?? DateTime(2030, 12, 31),
  status: status,
  notes: notes,
  createdAt: DateTime.utc(2026, 1, 1),
  updatedAt: DateTime.utc(2026, 1, 2),
  latitude: 6.123456,
  longitude: 79.123456,
);

class FakeRequirements implements RequirementGateway {
  BuyerRequirement row = testRequirement();
  List<RequirementCategory> categoryItems = [
    const RequirementCategory('c1', 'Cement'),
  ];
  List<BuyerRequirement> items = [];
  List<String> unitItems = ['kg'];
  Map<String, List<String>> unitsByCategory = {};
  final unitRequests = <String>[];
  final pendingUnits = <String, Completer<List<String>>>{};
  Object? unitError;
  Object? error, startError, saveError;
  RequirementDraft? saved;
  final queries = <RequirementQuery>[];
  final historyPages = <int>[];
  int reads = 0, saves = 0, submits = 0, starts = 0, cancels = 0, deletes = 0;
  Completer<RequirementPage<BuyerRequirement>>? pendingList;
  Completer<BuyerRequirement>? pendingSave;
  Completer<List<RequirementCategory>>? pendingCategories;
  RequirementPage<RequirementHistoryEntry> historyData = const RequirementPage(
    items: [],
    total: 0,
    page: 1,
    pageSize: 20,
  );
  @override
  Future<List<RequirementCategory>> categories() async {
    if (pendingCategories != null) return pendingCategories!.future;
    if (error != null) throw error!;
    return categoryItems;
  }

  @override
  Future<List<String>> activeUnits(String categoryId) async {
    unitRequests.add(categoryId);
    if (unitError != null) throw unitError!;
    if (pendingUnits.containsKey(categoryId)) {
      return pendingUnits[categoryId]!.future;
    }
    if (error != null) throw error!;
    return unitsByCategory[categoryId] ?? unitItems;
  }

  @override
  Future<RequirementPage<BuyerRequirement>> my(RequirementQuery query) async {
    queries.add(query);
    if (pendingList != null) return pendingList!.future;
    if (error != null) throw error!;
    return RequirementPage(
      items: items,
      total: items.length,
      page: query.page,
      pageSize: query.pageSize,
    );
  }

  @override
  Future<BuyerRequirement> get(String id) async {
    reads++;
    if (error != null) throw error!;
    return row;
  }

  @override
  Future<BuyerRequirement> create(RequirementDraft draft) async {
    saves++;
    saved = draft;
    if (pendingSave != null) return pendingSave!.future;
    if (saveError != null) throw saveError!;
    return row;
  }

  @override
  Future<BuyerRequirement> update(String id, RequirementDraft draft) =>
      create(draft);
  @override
  Future<void> delete(String id) async {
    deletes++;
  }

  @override
  Future<BuyerRequirement> submit(String id) async {
    submits++;
    return row = testRequirement(status: 'OPEN');
  }

  @override
  Future<RequirementStartResult> startMatching(String id) async {
    starts++;
    if (startError != null) throw startError!;
    row = testRequirement(status: 'MATCHING');
    return RequirementStartResult(row, 'workflow-from-api');
  }

  @override
  Future<BuyerRequirement> cancel(String id) async {
    cancels++;
    return row = testRequirement(status: 'CANCELLED');
  }

  @override
  Future<RequirementPage<RequirementHistoryEntry>> history(
    String id, {
    int page = 1,
  }) async {
    historyPages.add(page);
    if (error != null) throw error!;
    return RequirementPage(
      items: historyData.items,
      total: historyData.total,
      page: page,
      pageSize: historyData.pageSize,
    );
  }
}

class FakeRequirementLocation implements RequirementLocationSource {
  Object? error;
  int calls = 0;
  @override
  Future<RequirementLocation> capture() async {
    calls++;
    if (error != null) throw error!;
    return const RequirementLocation(6.123456789, 79.123456789);
  }
}
