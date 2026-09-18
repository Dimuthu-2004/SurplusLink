import 'package:mobile/categories/category_dropdown.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:mobile/requirements/requirement_gateway.dart';
import 'package:mobile/requirements/requirement_location.dart';
import 'package:mobile/requirements/requirement_models.dart';
import 'package:mobile/requirements/requirement_widgets.dart';

class RequirementFormScreen extends StatefulWidget {
  const RequirementFormScreen({
    required this.gateway,
    this.requirementId,
    this.locationSource = const DeviceRequirementLocation(),
    super.key,
  });
  final RequirementGateway gateway;
  final String? requirementId;
  final RequirementLocationSource locationSource;
  @override
  State<RequirementFormScreen> createState() => _RequirementFormScreenState();
}

class _RequirementFormScreenState extends State<RequirementFormScreen> {
  final _form = GlobalKey<FormState>();
  final _quantity = TextEditingController();
  final _unit = TextEditingController();
  final _budget = TextEditingController();
  final _notes = TextEditingController();
  final _latitude = TextEditingController();
  final _longitude = TextEditingController();
  List<RequirementCategory> _categories = [];
  String? _category, _error, _locationError;
  DateTime _deadline = DateTime.now().add(const Duration(days: 7));
  bool _loadFailed = false;
  bool _loading = true, _saving = false, _locating = false, _editable = true;
  bool get _editing => widget.requirementId != null;

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void dispose() {
    for (final controller in [
      _quantity,
      _unit,
      _budget,
      _notes,
      _latitude,
      _longitude,
    ]) {
      controller.dispose();
    }
    super.dispose();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _loadFailed = false;
      _error = null;
    });
    try {
      final categories = await widget.gateway.categories();
      final row = _editing
          ? await widget.gateway.get(widget.requirementId!)
          : null;
      if (!mounted) return;
      setState(() {
        _categories = categories;
        if (row != null) {
          _editable = row.canEdit;
          _category = row.categoryId;
          _quantity.text = row.requiredQuantity.toString();
          _unit.text = row.unit;
          _budget.text = row.maximumBudget.toString();
          _notes.text = row.notes;
          _latitude.text = row.latitude?.toString() ?? '';
          _longitude.text = row.longitude?.toString() ?? '';
          _deadline = row.deadline.toLocal();
        }
      });
    } on Object catch (error) {
      if (mounted) {
        setState(() {
          _loadFailed = true;
          _error = requirementError(error);
        });
      }
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _pickDate() async {
    final now = DateTime.now();
    final last = DateTime(now.year + 100, 12, 31);
    var initial = _deadline.isBefore(now) ? now : _deadline;
    if (initial.isAfter(last)) initial = last;
    final date = await showDatePicker(
      context: context,
      initialDate: initial,
      firstDate: now,
      lastDate: last,
    );
    if (date == null || !mounted) return;
    setState(
      () => _deadline = DateTime(date.year, date.month, date.day, 23, 59, 59),
    );
  }

  Future<void> _gps() async {
    if (_locating || _saving) return;
    setState(() {
      _locating = true;
      _locationError = null;
    });
    try {
      final position = await widget.locationSource.capture();
      if (!mounted) return;
      _latitude.text = position.latitude.toStringAsFixed(6);
      _longitude.text = position.longitude.toStringAsFixed(6);
    } on LocationCaptureException catch (error) {
      if (mounted) setState(() => _locationError = error.message);
    } on Object {
      if (mounted) {
        setState(
          () => _locationError = 'Unable to capture location. Retry or enter coordinates manually.',
        );
      }
    } finally {
      if (mounted) setState(() => _locating = false);
    }
  }

  String? _positive(String? raw, int decimals, int integerDigits) {
    final value = raw?.trim() ?? '';
    if (!RegExp(r'^\d+(\.\d+)?$').hasMatch(value)) {
      return 'Enter a positive number.';
    }
    final parsed = num.tryParse(value);
    if (parsed == null || !parsed.isFinite || parsed <= 0) {
      return 'Must be greater than zero.';
    }
    final parts = value.split('.');
    if (parts.length == 2 && parts[1].length > decimals) {
      return 'Use at most $decimals decimal places.';
    }
    if (parts[0].replaceFirst(RegExp(r'^0+'), '').length > integerDigits) {
      return 'This value is too large.';
    }
    return null;
  }

  String? _coordinate(String? raw, int max) {
    final value = raw?.trim() ?? '';
    final parsed = double.tryParse(value);
    if (!RegExp(r'^-?\d+(\.\d{1,6})?$').hasMatch(value) ||
        parsed == null ||
        !parsed.isFinite ||
        parsed < -max ||
        parsed > max) {
      return 'Enter a value from -$max to $max (up to 6 decimals).';
    }
    return null;
  }

  Future<void> _save() async {
    if (_saving || _locating || !_editable || !_form.currentState!.validate()) {
      return;
    }
    if (!_deadline.isAfter(DateTime.now())) {
      setState(() => _error = 'Deadline must be in the future.');
      return;
    }
    setState(() {
      _saving = true;
      _error = null;
    });
    try {
      final draft = RequirementDraft(
        categoryId: _category!,
        requiredQuantity: num.parse(_quantity.text.trim()),
        unit: _unit.text,
        maximumBudget: num.parse(_budget.text.trim()),
        deadline: _deadline,
        latitude: double.parse(_latitude.text.trim()),
        longitude: double.parse(_longitude.text.trim()),
        notes: _notes.text,
      );
      final row = _editing
          ? await widget.gateway.update(widget.requirementId!, draft)
          : await widget.gateway.create(draft);
      if (!mounted) return;
      if (_editing && context.canPop()) {
        context.pop(true);
      } else {
        context.go('/requirements/${row.id}');
      }
    } on Object catch (error) {
      if (mounted) setState(() => _error = requirementError(error));
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(
      title: Text(_editing ? 'Edit Requirement' : 'Create Requirement'),
      leading: const RequirementBackButton(),
    ),
    body: _loading
        ? const Center(child: CircularProgressIndicator())
        : !_editable
        ? const Center(
            child: Padding(
              padding: EdgeInsets.all(24),
              child: Text(
                'Only draft requirements can be edited. Return to details to see the current status.',
              ),
            ),
          )
        : _loadFailed || _categories.isEmpty
        ? Center(
            child: Padding(
              padding: const EdgeInsets.all(24),
              child: RequirementErrorBox(
                _error ?? 'No categories are available yet. Try again later.',
                onRetry: _load,
              ),
            ),
          )
        : Center(
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 720),
              child: Form(
                key: _form,
                child: ListView(
                  padding: const EdgeInsets.all(20),
                  children: [
                    if (_error != null) RequirementErrorBox(_error!),
                    const Text(
                      'Save your requirement as a draft, then review and submit it.',
                    ),
                    const SizedBox(height: 16),
                    CategoryDropdown(
                      key: const Key('requirement-category'),
                      categories: _categories,
                      value: _category,
                      onChanged: _saving
                          ? null
                          : (value) => setState(() => _category = value),
                    ),
                    const SizedBox(height: 16),
                    _field(
                      _quantity,
                      'Required quantity',
                      'requirement-quantity',
                      validator: (v) => _positive(v, 3, 15),
                      numeric: true,
                    ),
                    _field(
                      _unit,
                      'Unit (for example kg or bags)',
                      'requirement-unit',
                      validator: (v) => v == null || v.trim().isEmpty
                          ? 'Enter a unit.'
                          : v.trim().length > 32
                          ? 'Use at most 32 characters.'
                          : null,
                    ),
                    _field(
                      _budget,
                      'Maximum budget',
                      'requirement-budget',
                      validator: (v) => _positive(v, 2, 16),
                      numeric: true,
                    ),
                    OutlinedButton.icon(
                      key: const Key('requirement-deadline'),
                      onPressed: _saving ? null : _pickDate,
                      icon: const Icon(Icons.calendar_today),
                      label: Text('Deadline: ${requirementDate(_deadline)}'),
                    ),
                    const Text(
                      'Deadline is shown in your local time. Selecting a date sets it to the end of that day.',
                    ),
                    const SizedBox(height: 16),
                    _field(
                      _notes,
                      'Notes (optional)',
                      'requirement-notes',
                      lines: 3,
                      validator: (v) => (v?.length ?? 0) > 2000
                          ? 'Use at most 2,000 characters.'
                          : null,
                    ),
                    const Text(
                      'Delivery location',
                      style: TextStyle(fontWeight: FontWeight.bold),
                    ),
                    const SizedBox(height: 8),
                    OutlinedButton.icon(
                      key: const Key('requirement-gps'),
                      onPressed: _saving || _locating ? null : _gps,
                      icon: const Icon(Icons.my_location),
                      label: Text(
                        _locating ? 'Getting location…' : 'Use my GPS location',
                      ),
                    ),
                    if (_locationError != null)
                      RequirementErrorBox(_locationError!),
                    const SizedBox(height: 8),
                    _field(
                      _latitude,
                      'Latitude',
                      'requirement-latitude',
                      validator: (v) => _coordinate(v, 90),
                      numeric: true,
                    ),
                    _field(
                      _longitude,
                      'Longitude',
                      'requirement-longitude',
                      validator: (v) => _coordinate(v, 180),
                      numeric: true,
                    ),
                    FilledButton(
                      key: const Key('requirement-save'),
                      onPressed: _saving || _locating ? null : _save,
                      child: Text(
                        _saving
                            ? 'Saving…'
                            : _editing
                            ? 'Save changes'
                            : 'Save draft',
                      ),
                    ),
                    const SizedBox(height: 24),
                  ],
                ),
              ),
            ),
          ),
  );

  Widget _field(
    TextEditingController controller,
    String label,
    String key, {
    required FormFieldValidator<String> validator,
    bool numeric = false,
    int lines = 1,
  }) => Padding(
    padding: const EdgeInsets.only(bottom: 16),
    child: TextFormField(
      key: Key(key),
      controller: controller,
      enabled: !_saving,
      maxLines: lines,
      decoration: InputDecoration(labelText: label),
      keyboardType: numeric
          ? const TextInputType.numberWithOptions(decimal: true, signed: true)
          : TextInputType.text,
      validator: validator,
    ),
  );
}
