import 'package:mobile/core/api_client.dart';

import 'material_category.dart';

class CategoryRepository {
  CategoryRepository(this._api);
  final ApiClient _api;

  Future<List<String>> units(String categoryId) async => normalizeUnits(
    await _api.getStringListJson(
      '/api/material-categories/${Uri.encodeComponent(categoryId)}/units',
      authenticated: true,
    ),
  );

  Future<List<MaterialCategory>> categories() async => (await _api.getListJson(
    '/api/material-categories',
    authenticated: true,
  )).map(MaterialCategory.fromJson).toList();
}

String normalizeUnit(String unit) =>
    unit.trim().toLowerCase().replaceAll(RegExp(r'\s+'), ' ');
List<String> normalizeUnits(Iterable<String> units) =>
    units.map(normalizeUnit).where((unit) => unit.isNotEmpty).toSet().toList()
      ..sort();
