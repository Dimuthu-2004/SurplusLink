import 'package:flutter/material.dart';

String unitLabel(String unit) => switch (unit) {
  'm2' => 'm²', 'm3' => 'm³', 'l' => 'L', _ => unit,
};

class SearchableUnitField extends StatelessWidget {
  const SearchableUnitField({required this.units, required this.onChanged,
    this.initialUnit, this.enabled = true, this.hint = 'Type to find a unit',
    this.validator, super.key});
  final List<String> units;
  final String? initialUnit;
  final bool enabled;
  final String hint;
  final ValueChanged<String?> onChanged;
  final FormFieldValidator<String>? validator;

  @override
  Widget build(BuildContext context) => FormField<String>(
    initialValue: initialUnit,
    validator: validator ?? (value) => value == null ? 'Choose an allowed unit.' : null,
    builder: (field) => Autocomplete<String>(
      initialValue: TextEditingValue(text: initialUnit == null ? '' : unitLabel(initialUnit!)),
      displayStringForOption: unitLabel,
      optionsBuilder: (value) => enabled ? units.where((unit) =>
        ('$unit ${unitLabel(unit)}').toLowerCase().contains(value.text.trim().toLowerCase())) : const Iterable<String>.empty(),
      onSelected: (value) { field.didChange(value); onChanged(value); },
      fieldViewBuilder: (context, controller, focus, submit) => TextField(
        controller: controller, focusNode: focus, enabled: enabled,
        decoration: InputDecoration(labelText: 'Unit', hintText: hint, errorText: field.errorText,
          suffixIcon: const Icon(Icons.search)),
        onChanged: (_) { field.didChange(null); onChanged(null); },
        onSubmitted: (_) => submit(),
      ),
    ),
  );
}
