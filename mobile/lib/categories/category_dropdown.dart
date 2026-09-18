import 'package:flutter/material.dart';

import 'material_category.dart';

class CategoryDropdown extends StatelessWidget {
  const CategoryDropdown({
    required this.categories,
    required this.value,
    required this.onChanged,
    this.required = true,
    super.key,
  });

  final List<MaterialCategory> categories;
  final String? value;
  final ValueChanged<String?>? onChanged;
  final bool required;

  @override
  Widget build(BuildContext context) => DropdownButtonFormField<String>(
    initialValue: categories.any((category) => category.id == value)
        ? value
        : null,
    isExpanded: true,
    decoration: const InputDecoration(labelText: 'Category'),
    hint: Text(required ? 'Choose a category' : 'All categories'),
    items: [
      if (!required)
        const DropdownMenuItem(value: '', child: Text('All categories')),
      ...categories.map(
        (category) =>
            DropdownMenuItem(value: category.id, child: Text(category.name)),
      ),
    ],
    onChanged: onChanged,
    validator: (value) =>
        required && !categories.any((category) => category.id == value)
        ? 'Choose a category.'
        : null,
  );
}
