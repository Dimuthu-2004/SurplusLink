import 'searchable_unit_field.dart';
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
  List<String> _units = [];
  String? _selected, _error;
  bool _loading = false;
  int _request = 0;

  @override
  void didUpdateWidget(covariant SellerUnitField oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.categoryId != widget.categoryId ||
        oldWidget.initialUnit != widget.initialUnit) {
      _load();
    }
  }
  @override
  void initState() {
    super.initState();
    _load();
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
      SearchableUnitField(
        key: ValueKey('seller-unit-${widget.categoryId}-$_request-${_units.join(",")}'),
        initialUnit: _selected,
        units: _units,
        enabled: widget.enabled && !_loading && _error == null && _units.isNotEmpty,
        hint: widget.categoryId == null ? 'Choose a category first' : 'Type to find a unit',
        onChanged: (value) { _selected = value; widget.onChanged(value); },
        validator: (_) => widget.categoryId == null ? null : _loading ? 'Wait for units to load.' : _error != null ? 'Retry loading units.' : _selected == null ? 'Choose an allowed unit.' : null,
      ),
      if (widget.categoryId != null && !_loading && _error == null && _units.isEmpty)
        const Text('No units assigned to this category. Ask a manager to assign units.'),
    ],
  );
}
