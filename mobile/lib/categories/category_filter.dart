import 'package:flutter/material.dart';

import 'category_dropdown.dart';
import 'material_category.dart';

class CategoryFilter extends StatefulWidget {
  const CategoryFilter({
    required this.loadCategories,
    required this.onChanged,
    super.key,
  });
  final Future<List<MaterialCategory>> Function() loadCategories;
  final ValueChanged<String?> onChanged;

  @override
  State<CategoryFilter> createState() => _CategoryFilterState();
}

class _CategoryFilterState extends State<CategoryFilter> {
  List<MaterialCategory> _categories = [];
  String? _selected;
  bool _loading = true, _failed = false;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _failed = false;
    });
    try {
      final categories = await widget.loadCategories();
      if (mounted) setState(() => _categories = categories);
    } on Object {
      if (mounted) setState(() => _failed = true);
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_loading) {
      return const LinearProgressIndicator(
        semanticsLabel: 'Loading categories',
      );
    }
    if (_failed || _categories.isEmpty) {
      return Column(
        children: [
          Text(
            _failed
                ? 'Unable to load categories.'
                : 'No categories are available yet.',
          ),
          TextButton(onPressed: _load, child: const Text('Retry categories')),
        ],
      );
    }
    return CategoryDropdown(
      categories: _categories,
      value: _selected,
      required: false,
      onChanged: (value) {
        setState(() => _selected = value);
        widget.onChanged(value == '' ? null : value);
      },
    );
  }
}
