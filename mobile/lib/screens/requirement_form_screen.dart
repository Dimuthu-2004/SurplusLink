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
import 'package:mobile/widgets/construction_item_picker.dart';
import 'package:mobile/materials/construction_item_template_models.dart';
import 'dart:convert';
import 'package:mobile/l10n/app_localizations.dart';

class RequirementFormScreen extends StatefulWidget {
  const RequirementFormScreen({
    required this.gateway,
    this.requirementId,
    this.initialCategoryId,
    this.locationPicker = showLocationPicker,
    this.locationSource = const DeviceRequirementLocation(),
    this.locationLookup,
    super.key,
  });
  final RequirementGateway gateway;
  final String? requirementId;
  final String? initialCategoryId;
  final LocationPicker locationPicker;
  final RequirementLocationSource locationSource;
  final AddressLookup? locationLookup;
  @override
  State<RequirementFormScreen> createState() => _RequirementFormScreenState();
}

class _RequirementFormScreenState extends State<RequirementFormScreen> {
  final _form = GlobalKey<FormState>();
  final _quantity = TextEditingController();
  final _packageCount = TextEditingController();
  final _packageSize = TextEditingController();
  List<String> _units = [];
  final _budget = TextEditingController();
  final _notes = TextEditingController();
  List<RequirementCategory> _categories = [];
  List<ConstructionItemTemplate> _templates = [];
  ConstructionItemTemplate? _selectedTemplate;
  final Map<String, dynamic> _preferences = {};
  String? _category, _unit, _error, _locationError, _unitLoadError;
  String _inputMode = 'BASE_QUANTITY';
  bool _packageSizeIsOther = false;
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
      _packageCount,
      _packageSize,
      _budget,
      _notes,
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
      final categoriesFuture = widget.gateway.categories();
      final templatesFuture = widget.gateway is RequirementTemplateCatalogGateway
          ? (widget.gateway as RequirementTemplateCatalogGateway).itemTemplates()
          : Future.value(const <ConstructionItemTemplate>[]);
      final categories = await categoriesFuture;
      final templates = await templatesFuture;
      final row = _editing
          ? await widget.gateway.get(widget.requirementId!)
          : null;
      if (!mounted) return;
      setState(() {
        _categories = categories;
        _templates = templates.where((item) => item.isActive).toList();
        if (row != null) {
          _editable = row.canEdit;
          _category = row.categoryId;
          if (row.constructionItemTemplateId != null) {
            _selectedTemplate = templates.where((item) => item.id == row.constructionItemTemplateId).firstOrNull;
          }
          if (row.buyerPreferencesJson != null) {
            try {
              final value = jsonDecode(row.buyerPreferencesJson!);
              if (value is Map<String, dynamic>) _preferences.addAll(value);
            } catch (_) {}
          }
          _inputMode = row.inputMode;
          _quantity.text = row.requiredQuantity.toString();
          _packageCount.text = row.inputMode == 'PACKAGE_COUNT' ? (row.enteredQuantity ?? '').toString() : '';
          _packageSize.text = row.inputMode == 'PACKAGE_COUNT' ? (row.preferredPackageSize ?? '').toString() : '';
          _packageSizeIsOther = row.inputMode == 'PACKAGE_COUNT' && _selectedTemplate != null &&
              !_selectedTemplate!.allowedPackageSizes.map((size) => size.toString()).contains(_packageSize.text);
          _unit = row.unit;
          _budget.text = row.maximumBudget.toString();
          _notes.text = row.notes;
          _deadline = row.deadline.toLocal();
          _capturedLatitude = row.latitude;
          _capturedLongitude = row.longitude;
        } else if (widget.initialCategoryId != null &&
            widget.initialCategoryId!.trim().isNotEmpty) {
          _category = widget.initialCategoryId!.trim();
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
    if (_selectedTemplate != null && _editable) {
      _useTemplate(_selectedTemplate!);
    } else if (_category != null && _editable) {
      await _loadUnits(_category!, preferredUnit: _unit);
    }
  }

  void _useTemplate(ConstructionItemTemplate template) {
    final units = template.allowedUnits.where((unit) =>
        unit.toLowerCase() != template.packageType?.toLowerCase()).toList();
    if (units.isEmpty) units.add(template.baseUnit);
    final changed = _selectedTemplate?.id != template.id;
    setState(() {
      _selectedTemplate = template;
      _category = template.categoryId;
      _units = units;
      _unit = units.firstWhere((value) => value.toLowerCase() == template.baseUnit.toLowerCase(), orElse: () => units.first);
      _unitLoadError = null;
      if (changed) {
        _inputMode = template.effectiveBuyerInputModes.first;
        _packageCount.clear();
        _packageSize.text = template.allowedPackageSizes.length == 1 ? template.allowedPackageSizes.single.toString() : '';
        _packageSizeIsOther = template.allowedPackageSizes.isEmpty;
      }
    });
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
    if (_category == null || (_templates.isNotEmpty && _selectedTemplate == null)) {
      setState(() => _error = 'Choose the construction item you need.');
      return;
    }
    if (_capturedLatitude == null || _capturedLongitude == null) {
      setState(() => _locationError = 'Choose a delivery location on the map before saving.');
      return;
    }
    setState(() {
      _saving = true;
      _error = null;
    });
    try {
      final packageMode = _inputMode == 'PACKAGE_COUNT';
      final entered = num.parse((packageMode ? _packageCount : _quantity).text.trim());
      final packageSize = packageMode ? num.tryParse(_packageSize.text.trim()) : null;
      if (packageMode && (packageSize == null || packageSize <= 0)) {
        setState(() => _error = 'Select or enter a package size before saving.');
        return;
      }
      final canonicalQuantity = packageMode ? entered * packageSize! : entered;
      final draft = RequirementDraft(
        categoryId: _category!,
        requiredQuantity: canonicalQuantity,
        unit: _selectedTemplate?.baseUnit ?? _unit!,
        maximumBudget: num.parse(_budget.text.trim()),
        deadline: _deadline,
        latitude: _capturedLatitude!,
        longitude: _capturedLongitude!,
        notes: _notes.text,
        constructionItemTemplateId: _selectedTemplate?.id,
        buyerPreferencesJson: _preferences.isEmpty ? null : jsonEncode(_preferences),
        inputMode: _inputMode,
        enteredQuantity: entered,
        enteredUnit: packageMode ? (_selectedTemplate?.packageType ?? 'PACKAGE') : _unit,
        preferredPackageSize: packageSize,
        packageBaseUnit: packageMode ? _selectedTemplate?.baseUnit : null,
      );
      final row = _editing
          ? await widget.gateway.update(widget.requirementId!, draft)
          : await widget.gateway.create(draft);
      if (!mounted) return;
      if (_editing) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Requirement updated.')),
        );
      }
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
  Widget build(BuildContext context) {
    final text = AppLocalizations.of(context);
    return Scaffold(
    appBar: AppBar(
      title: Text(_editing ? 'Edit Requirement' : text?.createRequirement ?? 'Create Requirement'),
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
                    if (_templates.isNotEmpty) ...[
                      Text(text?.whatDoYouNeed ?? 'What do you need?', style: Theme.of(context).textTheme.titleMedium),
                      const SizedBox(height: 6),
                      OutlinedButton.icon(
                        key: const Key('requirement-item-picker'),
                        icon: const Icon(Icons.search),
                        label: Text(_selectedTemplate?.name ?? text?.searchConstructionItems ?? 'Search construction items...'),
                        onPressed: _saving ? null : () => ConstructionItemPickerSheet.show(
                          context: context,
                          templates: _templates,
                          onSelectTemplate: _useTemplate,
                          // Buyers must request a catalog item; custom seller stock stays seller-only.
                          onSelectCustom: () {},
                          allowCustom: false,
                        ),
                      ),
                      if (_selectedTemplate != null) ...[
                        const SizedBox(height: 8),
                        InputDecorator(
                          decoration: InputDecoration(labelText: text?.category ?? 'Category'),
                          child: Text(_selectedTemplate!.categoryName),
                        ),
                        const SizedBox(height: 8),
                        Text('${_selectedTemplate!.packageType == null ? 'Sold per' : 'Sold in'} ${_selectedTemplate!.packageType ?? _selectedTemplate!.baseUnit}', style: Theme.of(context).textTheme.bodySmall),
                      ],
                    ] else
                      CategoryDropdown(
                        key: const Key('requirement-category'),
                        categories: _categories,
                        value: _category,
                        onChanged: _saving ? null : _onCategoryChanged,
                      ),
                    const SizedBox(height: 16),
                    _requirementQuantityInput(),
                    if (_selectedTemplate != null && _selectedTemplate!.parsedAttributes.where((field) => field.buyerPreference).isNotEmpty) ...[
                      const SizedBox(height: 4),
                      Text(text?.preferencesOptional ?? 'Preferences (optional)', style: Theme.of(context).textTheme.titleMedium),
                      const Text('Leave blank when any option is suitable.'),
                      const SizedBox(height: 8),
                      for (final field in _selectedTemplate!.parsedAttributes.where((field) => field.buyerPreference))
                        Padding(
                          padding: const EdgeInsets.only(bottom: 12),
                          child: field.type == 'select'
                              ? DropdownButtonFormField<String>(
                                  initialValue: _preferences[field.id] as String?,
                                  isExpanded: true,
                                  decoration: InputDecoration(labelText: field.labelFor(Localizations.localeOf(context).languageCode), helperText: field.helper),
                                  hint: const Text('Any / No preference'),
                                  items: [
                                    if (field.allowAnyPreference)
                                      const DropdownMenuItem<String>(value: null, child: Text('Any / No preference')),
                                    ...field.options.map((option) => DropdownMenuItem(value: option, child: Text(option))),
                                  ],
                                  onChanged: _saving ? null : (value) => setState(() { if (value == null) { _preferences.remove(field.id); } else { _preferences[field.id] = value; } }),
                                )
                              : TextFormField(
                                  initialValue: _preferences[field.id]?.toString(),
                                  decoration: InputDecoration(labelText: field.labelFor(Localizations.localeOf(context).languageCode), helperText: field.helper ?? field.placeholder),
                                  onChanged: (value) => setState(() {
                                    if (value.trim().isEmpty) {
                                      _preferences.remove(field.id);
                                    } else {
                                      _preferences[field.id] = value;
                                    }
                                  }),
                                ),
                        ),
                    ],
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
                            : text?.saveDraft ?? 'Save draft',
                      ),
                    ),
                    const SizedBox(height: 24),
                  ],
                ),
              ),
            ),
          ),
  );
  }

  Widget _requirementQuantityInput() {
    final template = _selectedTemplate;
    final modes = template?.effectiveBuyerInputModes ?? const ['BASE_QUANTITY'];
    final packageMode = _inputMode == 'PACKAGE_COUNT';
    final packageUnit = template?.packageType?.toLowerCase() ?? 'packages';
    final baseUnit = template?.baseUnit ?? _unit ?? '';
    final packageSize = num.tryParse(_packageSize.text.trim());
    final count = num.tryParse(_packageCount.text.trim());
    final equivalent = packageMode && packageSize != null && count != null ? count * packageSize : null;
    return Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
      if (modes.length > 1) ...[
        const Text('How do you want to enter your requirement?', style: TextStyle(fontWeight: FontWeight.w600)),
        const SizedBox(height: 8),
        SegmentedButton<String>(
          segments: modes.map((mode) => ButtonSegment(value: mode, label: Text(mode == 'PACKAGE_COUNT' ? 'By packages' : 'By quantity'))).toList(),
          selected: {_inputMode},
          onSelectionChanged: _saving ? null : (value) => setState(() => _inputMode = value.first),
        ),
        const SizedBox(height: 16),
      ],
      if (packageMode) ...[
        _field(_packageCount, 'Number of $packageUnit', 'requirement-package-count', validator: (v) {
          final error = _positive(v, 0, 15);
          if (error != null) return error;
          return num.parse(v!.trim()) % 1 == 0 ? null : 'Package count must be a whole number.';
        }, numeric: true),
        if (template != null && template.allowedPackageSizes.isNotEmpty)
          Padding(padding: const EdgeInsets.only(bottom: 16), child: DropdownButtonFormField<String>(
            key: const Key('requirement-package-size'), initialValue: template.allowedPackageSizes.map((x) => x.toString()).contains(_packageSize.text) ? _packageSize.text : null,
            decoration: InputDecoration(labelText: 'Package size ($baseUnit each)'),
            items: [...template.allowedPackageSizes.map((x) => DropdownMenuItem(value: x.toString(), child: Text('$x $baseUnit'))), const DropdownMenuItem(value: 'OTHER', child: Text('Other'))],
            onChanged: (value) => setState(() {
              _packageSizeIsOther = value == 'OTHER';
              _packageSize.text = value == 'OTHER' ? '' : value ?? '';
            }),
            validator: (_) => _packageSize.text.trim().isEmpty ? 'Select or enter a package size.' : null,
          )),
        if (_packageSizeIsOther || template == null || template.allowedPackageSizes.isEmpty)
          _field(_packageSize, 'Custom package size ($baseUnit)', 'requirement-package-size-other', validator: (v) => _positive(v, 3, 15), numeric: true),
        if (equivalent != null) Padding(padding: const EdgeInsets.only(bottom: 16), child: Text('Equivalent requirement: $equivalent $baseUnit', key: const Key('requirement-equivalent'))),
      ] else ...[
        _field(_quantity, 'Required quantity', 'requirement-quantity', validator: (v) => _positive(v, 3, 15), numeric: true),
        _unitField(),
      ],
    ]);
  }

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
        : _units.length == 1 && _selectedTemplate != null
        ? InputDecorator(
            decoration: const InputDecoration(labelText: 'Unit'),
            child: Text(_unit ?? _units.single),
          )
        : DropdownButtonFormField<String>(
            key: const Key('requirement-unit'),
            initialValue: _units.contains(_unit) ? _unit : null,
            isExpanded: true,
            decoration: const InputDecoration(labelText: 'Unit'),
            hint: Text(
              _category == null
                  ? 'Choose an item first'
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
