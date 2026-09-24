import 'package:flutter/material.dart';

import 'category_repository.dart';

class SellerUnitField extends StatefulWidget {
  const SellerUnitField({
    required this.categoryId,
    required this.load,
    required this.onChanged,
    this.initialUnit,
    this.enabled = true,
    super.key,
  });
  final String? categoryId, initialUnit;
  final Future<List<String>> Function(String) load;
  final ValueChanged<String?> onChanged;
  final bool enabled;
  @override
  State<SellerUnitField> createState() => _SellerUnitFieldState();
}

class _SellerUnitFieldState extends State<SellerUnitField> {
  final _newUnit = TextEditingController();
  List<String> _units = [];
  String? _selected, _error, _entryError;
  bool _loading = false;
  int _request = 0;
  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void dispose() {
    _newUnit.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    if (widget.categoryId == null) return;
    final request = ++_request;
    setState(() {
      _loading = true;
      _error = null;
      _selected = null;
      _units = [];
    });
    try {
      final units = normalizeUnits(await widget.load(widget.categoryId!));
      if (!mounted || request != _request) return;
      final preferred = normalizeUnit(widget.initialUnit ?? '');
      setState(() {
        _units = units;
        _selected = units.contains(preferred)
            ? preferred
            : units.length == 1
            ? units.first
            : null;
      });
      widget.onChanged(_selected);
    } on Object {
      if (mounted && request == _request) {
        setState(() => _error = 'Unable to load units. Please retry.');
      }
    } finally {
      if (mounted && request == _request) setState(() => _loading = false);
    }
  }

  void _addUnit() {
    final unit = normalizeUnit(_newUnit.text);
    if (unit.length > 32 ||
        !RegExp(r'^[a-z][a-z0-9 /²³^.-]*$').hasMatch(unit)) {
      setState(
        () => _entryError =
            'Enter a unit of up to 32 characters, starting with a letter.',
      );
      return;
    }
    setState(() {
      _units = [unit];
      _selected = unit;
      _entryError = null;
    });
    widget.onChanged(unit);
  }

  @override
  Widget build(BuildContext context) => Column(
    crossAxisAlignment: CrossAxisAlignment.start,
    children: [
      if (_loading) const Text('Loading available units...'),
      if (_error != null) ...[
        Text(_error!),
        TextButton(
          onPressed: widget.enabled ? _load : null,
          child: const Text('Retry units'),
        ),
      ],
      DropdownButtonFormField<String>(
        key: ValueKey(
          'seller-unit-${widget.categoryId}-${_selected ?? ""}-${_units.join(",")}',
        ),
        initialValue: _selected,
        isExpanded: true,
        decoration: const InputDecoration(labelText: 'Unit'),
        hint: Text(
          widget.categoryId == null
              ? 'Choose a category first'
              : 'Choose a unit',
        ),
        items: _units
            .map((unit) => DropdownMenuItem(value: unit, child: Text(unit)))
            .toList(),
        onChanged:
            widget.enabled && !_loading && _error == null && _units.isNotEmpty
            ? (value) {
                setState(() => _selected = value);
                widget.onChanged(value);
              }
            : null,
        validator: (_) => widget.categoryId == null
            ? null
            : _loading
            ? 'Wait for units to load.'
            : _error != null
            ? 'Retry loading units.'
            : _selected == null
            ? 'Choose or add a unit.'
            : null,
      ),
      if (widget.categoryId != null &&
          !_loading &&
          _error == null &&
          _units.isEmpty) ...[
        const Text('No units recorded for this category. Add its first unit.'),
        TextField(
          controller: _newUnit,
          enabled: widget.enabled,
          maxLength: 32,
          decoration: InputDecoration(
            labelText: 'New unit',
            errorText: _entryError,
          ),
        ),
        TextButton(
          onPressed: widget.enabled ? _addUnit : null,
          child: const Text('Use new unit'),
        ),
      ],
    ],
  );
}
