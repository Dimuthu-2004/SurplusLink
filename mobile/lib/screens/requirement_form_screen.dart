import 'package:mobile/widgets/location_card.dart';
import 'package:mobile/location/location_lookup.dart';
import 'package:mobile/categories/category_dropdown.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:mobile/requirements/requirement_gateway.dart';
import 'package:mobile/requirements/requirement_location.dart';
import 'package:mobile/requirements/requirement_models.dart';
import 'package:mobile/requirements/requirement_widgets.dart';
import 'package:mobile/widgets/location_picker.dart';

class RequirementFormScreen extends StatefulWidget {
  const RequirementFormScreen({
    required this.gateway,
    this.requirementId,
    this.addressSearch,
    this.locationPicker = showLocationPicker,
    this.locationSource = const DeviceRequirementLocation(),
    this.locationLookup,
    super.key,
  });
  final RequirementGateway gateway;
  final String? requirementId;
  final AddressSearch? addressSearch;
  final LocationPicker locationPicker;
  final RequirementLocationSource locationSource;
  final AddressLookup? locationLookup;
  @override
  State<RequirementFormScreen> createState() => _RequirementFormScreenState();
}

class _RequirementFormScreenState extends State<RequirementFormScreen> {
  final _form = GlobalKey<FormState>();
  final _quantity = TextEditingController();
  List<String> _units = [];
  final _budget = TextEditingController();
  final _notes = TextEditingController();
  final _latitude = TextEditingController();
  final _longitude = TextEditingController();
  List<RequirementCategory> _categories = [];
  String? _category, _unit, _error, _locationError, _unitLoadError;
  double? _capturedLatitude, _capturedLongitude, _accuracy;
  DateTime _deadline = DateTime.now().add(const Duration(days: 7));
  bool _loadFailed = false;
  bool _loading = true,
      _loadingUnits = false,
      _saving = false,
      _locating = false,
      _editable = true;
  int _unitRequest = 0;
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
          _unit = row.unit;
          _budget.text = row.maximumBudget.toString();
          _notes.text = row.notes;
          _deadline = row.deadline.toLocal();
          _capturedLatitude = row.latitude;
          _capturedLongitude = row.longitude;
          _latitude.text = row.latitude?.toStringAsFixed(6) ?? '';
          _longitude.text = row.longitude?.toStringAsFixed(6) ?? '';
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
    if (_category != null && _editable) {
      await _loadUnits(_category!, preferredUnit: _unit);
    }
  }

  Future<void> _onCategoryChanged(String? categoryId) async {
    if (categoryId == _category) return;
    setState(() => _category = categoryId);
    if (categoryId == null) {
      _unitRequest++;
      setState(() {
        _units = [];
        _unit = null;
        _unitLoadError = null;
      });
      return;
    }
    await _loadUnits(categoryId);
  }

  Future<void> _loadUnits(String categoryId, {String? preferredUnit}) async {
    final request = ++_unitRequest;
    setState(() {
      _loadingUnits = true;
      _units = [];
      _unit = null;
      _unitLoadError = null;
    });
    try {
      final units = await widget.gateway.activeUnits(categoryId);
      if (!mounted || request != _unitRequest) return;
      String? selected;
      for (final unit in units) {
        if (unit.toLowerCase() == preferredUnit?.trim().toLowerCase()) {
          selected = unit;
          break;
        }
      }
      setState(() {
        _units = units;
        _unit = selected;
      });
    } on Object catch (error) {
      if (mounted && request == _unitRequest) {
        setState(() => _unitLoadError = requirementError(error));
      }
    } finally {
      if (mounted && request == _unitRequest) {
        setState(() => _loadingUnits = false);
      }
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

  Future<void> _chooseLocation() async {
    if (_locating || _saving) return;
    setState(() {
      _locating = true;
      _locationError = null;
    });
    try {
      final picked = await widget.locationPicker(
        context,
        latitude: _capturedLatitude,
        longitude: _capturedLongitude,
      );
      if (!mounted) return;
      if (picked != null) {
        setState(() {
          _capturedLatitude = picked.latitude;
          _capturedLongitude = picked.longitude;
          _accuracy = null;
          _latitude.text = picked.latitude.toStringAsFixed(6);
          _longitude.text = picked.longitude.toStringAsFixed(6);
        });
      }
    } on LocationCaptureException catch (error) {
      if (mounted) setState(() => _locationError = error.message);
    } on Object {
      if (mounted) {
        setState(
          () => _locationError = 'Unable to choose a location. Please retry.',
        );
      }
    } finally {
      if (mounted) setState(() => _locating = false);
    }
  }

  Future<void> _captureGps() async {
    if (_locating || _saving) return;
    setState(() { _locating = true; _locationError = null; });
    try {
      final position = await widget.locationSource.capture();
      if (!mounted) return;
      setState(() {
        _capturedLatitude = position.latitude;
        _capturedLongitude = position.longitude;
        _accuracy = position.accuracy;
        _latitude.text = position.latitude.toStringAsFixed(6);
        _longitude.text = position.longitude.toStringAsFixed(6);
      });
    } on LocationCaptureException catch (error) {
      if (mounted) setState(() => _locationError = error.message);
    } on Object {
      if (mounted) setState(() => _locationError = 'Unable to capture location. Please retry.');
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

  Future<void> _save() async {
    if (_saving ||
        _locating ||
        _loadingUnits ||
        !_editable ||
        !_form.currentState!.validate()) {
      return;
    }
    if (!_deadline.isAfter(DateTime.now())) {
      setState(() => _error = 'Deadline must be in the future.');
      return;
    }
    if (_latitude.text.isEmpty || _longitude.text.isEmpty) {
      setState(() => _locationError = 'Choose a delivery location on the map before saving.');
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
        unit: _unit!,
        maximumBudget: num.parse(_budget.text.trim()),
        deadline: _deadline,
        latitude: double.parse(_latitude.text),
        longitude: double.parse(_longitude.text),
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
                      onChanged: _saving ? null : _onCategoryChanged,
                    ),
                    const SizedBox(height: 16),
                    _field(
                      _quantity,
                      'Required quantity',
                      'requirement-quantity',
                      validator: (v) => _positive(v, 3, 15),
                      numeric: true,
                    ),
                    _unitField(),
                    _field(
                      _budget,
                      'Maximum budget (LKR)',
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
                      onPressed: _saving || _locating ? null : _captureGps,
                      icon: const Icon(Icons.my_location),
                      label: Text(
                        _locating ? 'Getting location…' : 'Use my current location',
                      ),
                    ),
                    OutlinedButton.icon(
                      key: const Key('requirement-map'),
                      onPressed: _saving || _locating ? null : _chooseLocation,
                      icon: const Icon(Icons.map_outlined),
                      label: const Text('Choose location on map'),
                    ),
                    if (_capturedLatitude != null && _capturedLongitude != null)
                      LocationCard(
                        latitude: _capturedLatitude!,
                        longitude: _capturedLongitude!,
                        accuracy: _accuracy,
                        lookup: widget.locationLookup ?? unavailableAddress,
                      ),
                    if (_locationError != null)
                      RequirementErrorBox(_locationError!),
                    FilledButton(
                      key: const Key('requirement-save'),
                      onPressed:
                          _saving ||
                              _locating ||
                              _loadingUnits ||
                              (_category != null &&
                                  (_units.isEmpty || _unit == null))
                          ? null
                          : _save,
                      child: Text(
                        _saving
                            ? 'Saving…'
                            : _editing
                            ? 'Save changes'
                            : 'Save draft',
                      ),
                    ),
                    Opacity(
                      opacity: 0,
                      child: Column(
                        children: [
                          SizedBox(height: 24, child: TextFormField(key: const Key('requirement-latitude'), controller: _latitude)),
                          SizedBox(height: 24, child: TextFormField(key: const Key('requirement-longitude'), controller: _longitude)),
                        ],
                      ),
                    ),
                    const SizedBox(height: 24),
                  ],
                ),
              ),
            ),
          ),
  );

  Widget _unitField() => Padding(
    padding: const EdgeInsets.only(bottom: 16),
    child: _loadingUnits
        ? const InputDecorator(
            decoration: InputDecoration(labelText: 'Unit'),
            child: Text('Loading available units…'),
          )
        : _unitLoadError != null
        ? InputDecorator(
            decoration: const InputDecoration(labelText: 'Unit'),
            child: Row(
              children: [
                Expanded(child: Text(_unitLoadError!)),
                TextButton(
                  onPressed: _category == null
                      ? null
                      : () => _loadUnits(_category!),
                  child: const Text('Retry'),
                ),
              ],
            ),
          )
        : DropdownButtonFormField<String>(
            key: const Key('requirement-unit'),
            initialValue: _units.contains(_unit) ? _unit : null,
            isExpanded: true,
            decoration: const InputDecoration(labelText: 'Unit'),
            hint: Text(
              _category == null
                  ? 'Choose a category first'
                  : _units.isEmpty
                  ? 'No available units for this category'
                  : 'Choose a unit',
            ),
            items: _units
                .map((unit) => DropdownMenuItem(value: unit, child: Text(unit)))
                .toList(),
            onChanged: _saving || _category == null || _units.isEmpty
                ? null
                : (value) => setState(() => _unit = value),
            validator: (value) {
              if (_category == null) return null;
              if (_units.isEmpty) {
                return 'No available units for this category.';
              }
              return value == null ? 'Choose a unit.' : null;
            },
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
