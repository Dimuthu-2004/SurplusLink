import 'package:mobile/widgets/dashboard_back_button.dart';

import 'dart:convert';
import 'dart:typed_data';

import 'package:mobile/categories/category_dropdown.dart';
import 'package:mobile/categories/material_category.dart';
import 'package:mobile/categories/seller_unit_field.dart';

import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:mobile/location/location_lookup.dart';
import 'package:mobile/materials/material_media.dart';
import 'package:mobile/requirements/requirement_location.dart';
import 'package:mobile/widgets/location_card.dart';
import 'package:mobile/core/api_exception.dart';
import 'package:mobile/materials/material_inventory_gateway.dart';
import 'package:mobile/materials/material_models.dart';
import 'package:mobile/materials/construction_item_template_models.dart';
import 'package:mobile/widgets/construction_item_picker.dart';
import 'package:mobile/widgets/location_picker.dart';

class MaterialListingFormScreen extends StatefulWidget {
  const MaterialListingFormScreen({
    required this.gateway,
    this.listingId,
    this.media,
    this.addressSearch,
    this.locationPicker = showLocationPicker,
    this.locationSource = const DeviceRequirementLocation(),
    this.locationLookup,
    super.key,
  });

  final MaterialInventoryGateway gateway;
  final String? listingId;
  final MaterialMedia? media;
  final AddressSearch? addressSearch;
  final LocationPicker locationPicker;
  final RequirementLocationSource locationSource;
  final AddressLookup? locationLookup;

  bool get isEditing => listingId != null;

  @override
  State<MaterialListingFormScreen> createState() =>
      _MaterialListingFormScreenState();
}

class _MaterialListingFormScreenState extends State<MaterialListingFormScreen> {
  final _formKey = GlobalKey<FormState>();
  List<MaterialCategory> _categories = [];
  List<ConstructionItemTemplate> _templates = [];
  ConstructionItemTemplate? _selectedTemplate;
  bool _isCustom = false;
  String _customSaleType = 'PIECE';
  final Map<String, dynamic> _specs = {};
  String? _category;
  bool _loadFailed = false;
  final _titleController = TextEditingController();
  final _descriptionController = TextEditingController();
  final _quantityController = TextEditingController();
  final _packageSizeController = TextEditingController();
  final _packageCountController = TextEditingController();
  String _quantityMode = 'CONTINUOUS';
  String _packageType = 'can';
  String? _unit;
  final _priceController = TextEditingController();
  final _addressController = TextEditingController();
  final List<String> _photoUrls = [];
  final List<_SelectedPhoto> _selectedPhotos = [];
  late final _media = widget.media ?? MaterialMedia();
  bool _mediaBusy = false, _locating = false, _addressBusy = false;
  List<AddressResult> _addressResults = [];
  double? _accuracy;
  String _condition = 'GOOD';
  DateTime _availableUntil = DateTime.now().add(const Duration(days: 7));
  double? _latitude;
  double? _longitude;
  bool _isLoading = false;
  bool _isSaving = false;
  String? _error;
  bool _hasAttemptedSubmit = false;
  final _tileWidthController = TextEditingController(text: '600');
  final _tileHeightController = TextEditingController(text: '600');
  final _tilesPerBoxController = TextEditingController(text: '4');

  @override
  void initState() {
    super.initState();
    _loadExisting();
  }

  @override
  void dispose() {
    _titleController.dispose();
    _descriptionController.dispose();
    _quantityController.dispose();
    _packageSizeController.dispose();
    _packageCountController.dispose();
    _priceController.dispose();
    _addressController.dispose();
    _tileWidthController.dispose();
    _tileHeightController.dispose();
    _tilesPerBoxController.dispose();
    super.dispose();
  }

  Future<void> _loadExisting() async {
    setState(() {
      _isLoading = true;
      _loadFailed = false;
      _error = null;
    });
    try {
      final categoriesFuture = widget.gateway.categories();
      final templatesFuture = widget.gateway.itemTemplates();
      final results = await Future.wait([categoriesFuture, templatesFuture]);
      if (!mounted) return;
      final loadedCategories = results[0] as List<MaterialCategory>;
      final loadedTemplates = results[1] as List<ConstructionItemTemplate>;
      setState(() {
        _categories = loadedCategories;
        _templates = loadedTemplates;
        if (loadedTemplates.isEmpty && !widget.isEditing) {
          _isCustom = true;
          _customSaleType = 'CONTINUOUS';
          _quantityMode = 'CONTINUOUS';
          _category = null;
        }
      });
      if (!widget.isEditing) return;
      final listing = await widget.gateway.getById(widget.listingId!);
      if (!mounted) return;
      if (!listing.canEdit) {
        setState(() {
          _loadFailed = true;
          _error = 'Only DRAFT or REJECTED listings can be edited.';
        });
        return;
      }
      ConstructionItemTemplate? foundTemplate;
      if (listing.constructionItemTemplateId != null) {
        try {
          foundTemplate = loadedTemplates.firstWhere((t) => t.id == listing.constructionItemTemplateId);
        } catch (_) {}
      }
      Map<String, dynamic> loadedSpecs = {};
      if (listing.specificationsJson != null && listing.specificationsJson!.isNotEmpty) {
        try {
          final decoded = jsonDecode(listing.specificationsJson!);
          if (decoded is Map<String, dynamic>) {
            loadedSpecs = decoded;
          }
        } catch (_) {}
      }
      if (foundTemplate != null && foundTemplate.name.toLowerCase().contains('tile')) {
        if (loadedSpecs['widthMm'] != null) _tileWidthController.text = loadedSpecs['widthMm'].toString();
        if (loadedSpecs['heightMm'] != null) _tileHeightController.text = loadedSpecs['heightMm'].toString();
        if (loadedSpecs['piecesPerBox'] != null) _tilesPerBoxController.text = loadedSpecs['piecesPerBox'].toString();
      }
      setState(() {
        _selectedTemplate = foundTemplate;
        _isCustom = listing.isCustomPendingReview || foundTemplate == null;
        _specs.clear();
        _specs.addAll(loadedSpecs);
        _category = listing.categoryId;
        _titleController.text = listing.title;
        _descriptionController.text = listing.description;
        _quantityController.text = listing.quantity == listing.quantity.roundToDouble()
            ? listing.quantity.toStringAsFixed(0)
            : listing.quantity.toString();
        _unit = listing.unit;
        _quantityMode = listing.quantityMode == 'LEGACY' ? 'CONTINUOUS' : listing.quantityMode;
        _customSaleType = _quantityMode == 'CONTINUOUS' ? 'CONTINUOUS' : (_quantityMode == 'PACKAGE' ? 'PACKAGE' : 'PIECE');
        _packageType = listing.packageType ?? 'can';
        _packageSizeController.text = listing.packageSize?.toString() ?? '';
        _packageCountController.text = listing.packageCount?.toString() ?? '';
        _priceController.text = listing.unitPrice.toString();
        _condition = listing.condition;
        _availableUntil = listing.availableUntil;
        _latitude = listing.latitude;
        _longitude = listing.longitude;
        _photoUrls
          ..clear()
          ..addAll(listing.photos.map((photo) => photo.photoUrl));
      });
    } on ApiException catch (error) {
      if (mounted) {
        setState(() {
          _loadFailed = true;
          _error = error.message;
        });
      }
    } on Object {
      if (mounted) {
        setState(() {
          _loadFailed = true;
          _error =
              'Unable to load categories or material details. Please retry.';
        });
      }
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  Future<void> _chooseDate() async {
    final chosen = await showDatePicker(
      context: context,
      initialDate: _availableUntil.isAfter(DateTime.now())
          ? _availableUntil
          : DateTime.now(),
      firstDate: DateTime.now(),
      lastDate: DateTime.now().add(const Duration(days: 3650)),
    );
    if (chosen == null || !mounted) return;
    setState(() {
      _availableUntil = DateTime(
        chosen.year,
        chosen.month,
        chosen.day,
        23,
        59,
        59,
      );
    });
  }

  Future<void> _pickImages({bool camera = false}) async {
    if (_mediaBusy || _isSaving) return;
    final remaining = 10 - _photoUrls.length - _selectedPhotos.length;
    if (remaining <= 0) {
      _showMessage('A listing can contain at most 10 photos.');
      return;
    }
    setState(() => _mediaBusy = true);
    try {
      final captured = camera ? await _media.takePhoto() : null;
      final files = camera
          ? [?captured]
          : await _media.pickGallery();
      if (files.length > remaining && mounted) {
        _showMessage('Only the first $remaining photos can be added.');
      }
      for (final file in files.take(remaining)) {
        if (await file.length() > 8 * 1024 * 1024) {
          if (mounted) _showMessage('Each photo must be 8 MB or smaller.');
          continue;
        }
        final photo = _SelectedPhoto(await file.readAsBytes(), camera);
        if (!mounted) return;
        setState(() => _selectedPhotos.add(photo));
        if (camera) await _saveCameraPhoto(photo);
      }
    } on Object {
      if (mounted) {
        _showMessage(
          'Unable to select photos. Check camera or photo permission and retry.',
        );
      }
    } finally {
      if (mounted) setState(() => _mediaBusy = false);
    }
  }

  Future<void> _saveCameraPhoto(_SelectedPhoto photo) async {
    try {
      await _media.saveToGallery(photo.bytes);
      if (mounted) setState(() => photo.gallerySaved = true);
    } on Object {
      if (mounted) {
        _showMessage(
          'Photo kept for upload, but gallery saving failed. Allow Photos access and use Save to gallery to retry.',
        );
      }
    }
  }

  Future<void> _chooseLocation() async {
    if (_locating || _isSaving) return;
    setState(() => _locating = true);
    try {
      final picked = await widget.locationPicker(
        context,
        latitude: _latitude,
        longitude: _longitude,
      );
      if (!mounted) return;
      if (picked != null) {
        setState(() {
          _accuracy = null;
          _latitude = picked.latitude;
          _longitude = picked.longitude;
        });
      }
    } on LocationCaptureException catch (error) {
      if (mounted) _showMessage(error.message);
    } on Object {
      if (mounted) _showMessage('Unable to choose a location. Please retry.');
    } finally {
      if (mounted) setState(() => _locating = false);
    }
  }

  Future<void> _captureGps() async {
    if (_locating || _isSaving) return;
    setState(() => _locating = true);
    try {
      final position = await widget.locationSource.capture();
      if (!mounted) return;
      setState(() {
        _accuracy = position.accuracy;
        _latitude = position.latitude;
        _longitude = position.longitude;
      });
    } on LocationCaptureException catch (error) {
      if (mounted) _showMessage(error.message);
    } on Object {
      if (mounted) _showMessage('Unable to capture location. Please retry.');
    } finally {
      if (mounted) setState(() => _locating = false);
    }
  }

  Future<void> _searchAddress() async {
    final query = _addressController.text.trim();
    if (query.length < 3) {
      _showMessage('Enter at least 3 characters.');
      return;
    }
    setState(() {
      _addressBusy = true;
      _addressResults = [];
    });
    try {
      final results = await widget.addressSearch!(query);
      if (mounted) setState(() => _addressResults = results);
    } on Object {
      if (mounted) _showMessage('Unable to search addresses. Please retry.');
    } finally {
      if (mounted) setState(() => _addressBusy = false);
    }
  }

  void _onSelectTemplate(ConstructionItemTemplate template) {
    setState(() {
      _selectedTemplate = template;
      _isCustom = false;
      _category = template.categoryId;
      _titleController.text = template.name;
      _unit = template.baseUnit;
      _specs.clear();

      final isTile = template.name.toLowerCase().contains('tile');
      if (isTile) {
        _tileWidthController.text = '600';
        _tileHeightController.text = '600';
        _tilesPerBoxController.text = '4';
        _recalcTileCoverage();
      }

      if (template.isPackage) {
        _quantityMode = 'PACKAGE';
        _packageType = template.packageType ?? (isTile ? 'box' : 'can');
        if (!isTile) {
          if (template.allowedPackageSizes.isNotEmpty) {
            _packageSizeController.text = template.allowedPackageSizes.first.toString();
          } else {
            _packageSizeController.text = '1';
          }
        }
        _packageCountController.text = '1';
        final size = double.tryParse(_packageSizeController.text) ?? 1;
        _quantityController.text = size.toString();
      } else if (template.isPiece) {
        _quantityMode = 'PIECE';
        _packageSizeController.text = '1';
        _packageCountController.text = '1';
        _quantityController.text = '1';
      } else {
        _quantityMode = 'CONTINUOUS';
        _packageSizeController.clear();
        _packageCountController.clear();
        _quantityController.text = '1';
      }
    });
  }

  void _recalcTileCoverage() {
    final w = double.tryParse(_tileWidthController.text.trim()) ?? 0;
    final h = double.tryParse(_tileHeightController.text.trim()) ?? 0;
    final pcs = int.tryParse(_tilesPerBoxController.text.trim()) ?? 0;
    if (w > 0 && h > 0 && pcs > 0) {
      final cov = ((w * h * pcs) / 1000000.0 * 100).round() / 100.0;
      _packageSizeController.text = cov.toString();
      _specs['widthMm'] = w;
      _specs['heightMm'] = h;
      _specs['piecesPerBox'] = pcs;
      _specs['coveragePerBoxSqm'] = cov;
      _specs['dimensionsMm'] = '${w.toInt()}x${h.toInt()} mm';
      final count = int.tryParse(_packageCountController.text.trim()) ?? 1;
      _quantityController.text = (cov * count).toString();
    }
  }

  void _onSelectCustom() {
    setState(() {
      _selectedTemplate = null;
      _isCustom = true;
      _customSaleType = 'PIECE';
      _quantityMode = 'PIECE';
      _specs.clear();
      _category ??= _categories.isNotEmpty ? _categories.first.id : null;
    });
  }

  Future<void> _save() async {
    setState(() {
      _hasAttemptedSubmit = true;
      _error = null;
    });
    if (_isSaving ||
        _mediaBusy ||
        _locating) {
      return;
    }
    if (!_formKey.currentState!.validate()) {
      _showMessage('Please review the highlighted fields before saving.');
      return;
    }
    final effectiveCategoryId = _selectedTemplate?.categoryId ?? _category;
    if (effectiveCategoryId == null || effectiveCategoryId.isEmpty) {
      _showMessage('Please select a material category.');
      return;
    }
    if (!_availableUntil.isAfter(DateTime.now())) {
      _showMessage('Available until must be in the future.');
      return;
    }
    setState(() {
      _isSaving = true;
      _error = null;
    });
    try {
      for (final photo in _selectedPhotos) {
        photo.uploadedUrl ??= await widget.gateway.uploadPhoto(photo.bytes);
      }

      final isPkg = _selectedTemplate?.isPackage == true || (_isCustom && _customSaleType == 'PACKAGE') || _quantityMode == 'PACKAGE';
      final isPc = _selectedTemplate?.isPiece == true || (_isCustom && _customSaleType == 'PIECE') || _quantityMode == 'PIECE';

      final double computedQuantity;
      final String effectiveQuantityMode;
      final double? computedPackageSize;
      final int? computedPackageCount;
      final String? computedPackageType;
      final double computedUnitPrice;

      if (isPkg) {
        effectiveQuantityMode = 'PACKAGE';
        final size = double.tryParse(_packageSizeController.text.trim()) ?? 1.0;
        final count = int.tryParse(_packageCountController.text.trim()) ?? 1;
        computedPackageSize = size;
        computedPackageCount = count;
        computedQuantity = ((size * count) * 10000).round() / 10000.0;
        computedPackageType = _selectedTemplate?.packageType ?? _packageType;
        final enteredPrice = double.tryParse(_priceController.text.trim()) ?? 0.0;
        computedUnitPrice = size > 0 ? enteredPrice / size : enteredPrice;
      } else if (isPc) {
        effectiveQuantityMode = 'PIECE';
        final count = int.tryParse(_quantityController.text.trim()) ?? (int.tryParse(_packageCountController.text.trim()) ?? 1);
        computedPackageSize = 1;
        computedPackageCount = count;
        computedQuantity = count.toDouble();
        computedPackageType = null;
        computedUnitPrice = double.tryParse(_priceController.text.trim()) ?? 0.0;
      } else {
        effectiveQuantityMode = 'CONTINUOUS';
        computedQuantity = double.tryParse(_quantityController.text.trim()) ?? 1.0;
        computedPackageSize = null;
        computedPackageCount = null;
        computedPackageType = null;
        computedUnitPrice = double.tryParse(_priceController.text.trim()) ?? 0.0;
      }

      final draft = MaterialListingDraft(
        categoryId: effectiveCategoryId,
        title: _titleController.text,
        description: _descriptionController.text,
        quantity: computedQuantity,
        unit: _unit ?? _selectedTemplate?.baseUnit ?? 'unit',
        quantityMode: effectiveQuantityMode,
        baseUnit: _unit ?? _selectedTemplate?.baseUnit,
        packageType: computedPackageType,
        packageSize: computedPackageSize,
        packageCount: computedPackageCount,
        constructionItemTemplateId: _selectedTemplate?.id,
        specificationsJson: _specs.isNotEmpty ? jsonEncode(_specs) : null,
        isCustomPendingReview: _isCustom,
        condition: _condition,
        unitPrice: computedUnitPrice,
        latitude: _latitude,
        longitude: _longitude,
        availableUntil: _availableUntil,
        photoUrls: [
          ..._photoUrls,
          ..._selectedPhotos.map((photo) => photo.uploadedUrl!),
        ],
      );
      final saved = widget.isEditing
          ? await widget.gateway.update(widget.listingId!, draft)
          : await widget.gateway.create(draft);
      if (mounted) {
        if (widget.isEditing && context.canPop()) {
          context.pop(true);
        } else {
          context.go('/materials/${saved.id}');
        }
      }
    } on ApiException catch (error) {
      if (mounted) setState(() => _error = error.message);
    } on Object {
      if (mounted) setState(() => _error = 'Unable to save the material.');
    } finally {
      if (mounted) setState(() => _isSaving = false);
    }
  }

  void _showMessage(String message) =>
      ScaffoldMessenger.of(context)
          .showSnackBar(SnackBar(content: Text(message)));

  String _packageTotalSummary() {
    final size = double.tryParse(_packageSizeController.text.trim());
    final count = int.tryParse(_packageCountController.text.trim());
    if (size == null || count == null) return 'Total sellable stock: —';
    final unitLabel = _unit ?? _selectedTemplate?.baseUnit ?? '';
    final pkgLabel = _selectedTemplate?.packageType ?? _packageType;
    final isTile = _selectedTemplate?.name.toLowerCase().contains('tile') == true;
    final pkgPlural = pkgLabel.toLowerCase() == 'box' ? 'boxes' : '${pkgLabel}s';
    final countDesc = count == 1 ? '1 $pkgLabel' : '$count $pkgPlural';
    final totalQuantity = size * count;
    if (isTile) {
      final pcs = int.tryParse(_tilesPerBoxController.text.trim()) ?? 0;
      final totalStr = totalQuantity == totalQuantity.roundToDouble()
          ? totalQuantity.toStringAsFixed(1)
          : totalQuantity.toStringAsFixed(2);
      return '$countDesc • $pcs tiles per box • Coverage per box: $size $unitLabel • Total coverage: $totalStr $unitLabel';
    }
    return 'Total sellable stock: $totalQuantity $unitLabel ($countDesc × $size $unitLabel)'.trim();
  }

  String? _getFieldHelperText(String fieldId, String? unit) {
    switch (fieldId) {
      case 'capacity_kva':
        return 'Example: 5 kVA';
      case 'dimensions_mm':
      case 'width_mm':
      case 'height_mm':
        return 'Example: 600 mm';
      case 'diameter_mm':
        return 'Example: 12 mm';
      case 'length_m':
        return 'Example: 6 m';
      case 'fuel_type':
        return 'Primary fuel required';
      case 'running_hours':
        return 'Current meter reading';
      default:
        return null;
    }
  }

  String? _wholePackageCount(String? value) {
    final input = value?.trim() ?? '';
    if (input.isEmpty) {
      return 'Enter the number of packages.';
    }
    final count = int.tryParse(input);
    return count != null && count > 0
        ? null
        : 'Enter a whole number of packages (at least 1).';
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(
      leading: const DashboardBackButton(fallback: '/materials'),
      title: Text(widget.isEditing ? 'Edit Material' : 'Add Material'),
    ),
    body: _isLoading
        ? const Center(child: CircularProgressIndicator())
        : _loadFailed || _categories.isEmpty
        ? Center(
            child: Padding(
              padding: const EdgeInsets.all(24),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text(
                    _error ??
                        'No categories are available yet. Try again later.',
                  ),
                  const SizedBox(height: 12),
                  OutlinedButton(
                    onPressed: _loadExisting,
                    child: const Text('Retry'),
                  ),
                ],
              ),
            ),
          )
        : SafeArea(
            child: Form(
              key: _formKey,
              autovalidateMode: _hasAttemptedSubmit
                  ? AutovalidateMode.onUserInteraction
                  : AutovalidateMode.disabled,
              child: ListView(
                padding: const EdgeInsets.all(16),
                children: [
                  if (_error != null) ...[
                    _InlineError(message: _error!),
                    const SizedBox(height: 12),
                  ],
                  // 1. "What are you listing?" Picker Card
                  Card(
                    elevation: 0,
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(12),
                      side: BorderSide(
                        color: _selectedTemplate != null
                            ? Colors.green.shade200
                            : (_isCustom ? Colors.orange.shade200 : Colors.grey.shade300),
                      ),
                    ),
                    color: _selectedTemplate != null
                        ? Colors.green.shade50
                        : (_isCustom ? Colors.orange.shade50 : Colors.grey.shade50),
                    child: Padding(
                      padding: const EdgeInsets.all(12),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Row(
                            mainAxisAlignment: MainAxisAlignment.spaceBetween,
                            children: [
                              Text(
                                _selectedTemplate != null
                                    ? 'Item: ${_selectedTemplate!.name}'
                                    : (_isCustom ? 'Custom Construction Item' : 'What are you listing?'),
                                style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 14),
                              ),
                              TextButton.icon(
                                key: const Key('choose-construction-item-button'),
                                icon: const Icon(Icons.search, size: 16),
                                label: Text(_selectedTemplate != null || _isCustom ? 'Change' : 'Choose Item'),
                                onPressed: _isSaving ? null : () => ConstructionItemPickerSheet.show(
                                  context: context,
                                  templates: _templates,
                                  onSelectTemplate: _onSelectTemplate,
                                  onSelectCustom: _onSelectCustom,
                                ),
                              ),
                            ],
                          ),
                          if (_selectedTemplate != null)
                            Text(
                              '${_selectedTemplate!.categoryName} • ${_selectedTemplate!.isPackage ? "Sold in ${_selectedTemplate!.packageType ?? 'packages'} (${_selectedTemplate!.baseUnit})" : (_selectedTemplate!.isPiece ? "Sold per piece/unit" : "Sold in bulk (${_selectedTemplate!.baseUnit})")}',
                              style: TextStyle(fontSize: 12, color: Colors.grey.shade800),
                            ),
                          if (_isCustom)
                            Text(
                              'Flagged for manager review upon submission.',
                              style: TextStyle(fontSize: 12, color: Colors.orange.shade900),
                            ),
                        ],
                      ),
                    ),
                  ),
                  const SizedBox(height: 12),

                  // If neither catalog item nor custom selected yet, show clean empty state
                  if (_selectedTemplate == null && !_isCustom)
                    Card(
                      elevation: 0,
                      color: Colors.grey.shade100,
                      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                      child: const Padding(
                        padding: EdgeInsets.all(24),
                        child: Column(
                          children: [
                            Icon(Icons.inventory_2_outlined, size: 48, color: Colors.grey),
                            SizedBox(height: 12),
                            Text(
                              'Choose an item above to continue',
                              style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16),
                            ),
                            SizedBox(height: 6),
                            Text(
                              'Select from standard construction materials, tools, equipment, or add a custom item to configure quantity and pricing.',
                              textAlign: TextAlign.center,
                              style: TextStyle(color: Colors.grey, fontSize: 13),
                            ),
                          ],
                        ),
                      ),
                    )
                  else ...[
                    // Custom Item: Seller enters title, selects category, and chooses friendly sale type
                    if (_isCustom) ...[
                      TextFormField(
                        key: const Key('material-title'),
                        controller: _titleController,
                        maxLength: 200,
                        decoration: const InputDecoration(
                          labelText: 'Item name *',
                          hintText: 'e.g. Hydraulic Breaker Attachment',
                        ),
                        validator: (value) => _requiredLength(value, 'Item name', 200),
                      ),
                      const SizedBox(height: 12),
                      CategoryDropdown(
                        key: const Key('material-category'),
                        categories: _categories,
                        value: _category,
                        onChanged: _isSaving
                            ? null
                            : (value) => setState(() {
                                _category = value;
                                _unit = null;
                              }),
                      ),
                      const SizedBox(height: 12),
                      const Text('How is this item sold?', style: TextStyle(fontWeight: FontWeight.w600, fontSize: 13)),
                      const SizedBox(height: 6),
                      SegmentedButton<String>(
                        key: const Key('custom-sale-type-toggle'),
                        segments: const [
                          ButtonSegment(value: 'PIECE', label: Text('Pieces / Units')),
                          ButtonSegment(value: 'PACKAGE', label: Text('Packages / Bags')),
                          ButtonSegment(value: 'CONTINUOUS', label: Text('Bulk Quantity')),
                        ],
                        selected: {_customSaleType},
                        onSelectionChanged: (val) {
                          setState(() {
                            _customSaleType = val.first;
                            _quantityMode = val.first;
                          });
                        },
                      ),
                      const SizedBox(height: 12),
                    ] else ...[
                      // Catalog Item: Category is auto-derived, seller customizes title if needed
                      TextFormField(
                        key: const Key('material-title'),
                        controller: _titleController,
                        maxLength: 200,
                        decoration: const InputDecoration(labelText: 'Title / Model *'),
                        validator: (value) => _requiredLength(value, 'Title', 200),
                      ),
                      const SizedBox(height: 12),
                    ],

                    TextFormField(
                      key: const Key('material-description'),
                      controller: _descriptionController,
                      minLines: 3,
                      maxLines: 6,
                      maxLength: 2000,
                      decoration: const InputDecoration(labelText: 'Description / Notes'),
                      validator: (value) => _requiredLength(value, 'Description', 2000),
                    ),
                    const SizedBox(height: 12),

                    // Dynamic Specifications: Tile Form or Standard Attributes
                    if (_selectedTemplate != null && _selectedTemplate!.name.toLowerCase().contains('tile')) ...[
                      Card(
                        elevation: 0,
                        color: Colors.blueGrey.shade50,
                        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
                        child: Padding(
                          padding: const EdgeInsets.all(12),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              const Text(
                                'Tile Dimensions & Packaging',
                                style: TextStyle(fontWeight: FontWeight.bold, fontSize: 14),
                              ),
                              const SizedBox(height: 10),
                              Row(
                                children: [
                                  Expanded(
                                    child: TextFormField(
                                      key: const Key('tile-width-mm'),
                                      controller: _tileWidthController,
                                      keyboardType: const TextInputType.numberWithOptions(decimal: true),
                                      decoration: const InputDecoration(
                                        labelText: 'Width (mm) *',
                                        hintText: '600',
                                        helperText: 'Example: 600 mm',
                                      ),
                                      onChanged: (_) => setState(_recalcTileCoverage),
                                      validator: (v) => _positiveNumber(v, 'Tile width'),
                                    ),
                                  ),
                                  const SizedBox(width: 10),
                                  Expanded(
                                    child: TextFormField(
                                      key: const Key('tile-height-mm'),
                                      controller: _tileHeightController,
                                      keyboardType: const TextInputType.numberWithOptions(decimal: true),
                                      decoration: const InputDecoration(
                                        labelText: 'Height (mm) *',
                                        hintText: '600',
                                        helperText: 'Example: 600 mm',
                                      ),
                                      onChanged: (_) => setState(_recalcTileCoverage),
                                      validator: (v) => _positiveNumber(v, 'Tile height'),
                                    ),
                                  ),
                                ],
                              ),
                              const SizedBox(height: 10),
                              TextFormField(
                                key: const Key('tile-pieces-per-box'),
                                controller: _tilesPerBoxController,
                                keyboardType: TextInputType.number,
                                decoration: const InputDecoration(
                                  labelText: 'Tiles per box *',
                                  hintText: '4',
                                  helperText: 'How many individual tiles are inside one unopened box?',
                                ),
                                onChanged: (_) => setState(_recalcTileCoverage),
                                validator: (v) => _positiveNumber(v, 'Tiles per box'),
                              ),
                              const SizedBox(height: 10),
                              Container(
                                padding: const EdgeInsets.all(10),
                                decoration: BoxDecoration(
                                  color: Colors.blue.shade50,
                                  borderRadius: BorderRadius.circular(8),
                                  border: Border.all(color: Colors.blue.shade200),
                                ),
                                child: Row(
                                  children: [
                                    Icon(Icons.calculate_outlined, size: 20, color: Colors.blue.shade800),
                                    const SizedBox(width: 8),
                                    Expanded(
                                      child: Text(
                                        'Coverage per box: ${_packageSizeController.text.isEmpty ? "1.44" : _packageSizeController.text} m² (auto-calculated)',
                                        style: TextStyle(fontWeight: FontWeight.w600, color: Colors.blue.shade900, fontSize: 13),
                                      ),
                                    ),
                                  ],
                                ),
                              ),
                              const SizedBox(height: 10),
                              for (final field in _selectedTemplate!.parsedAttributes) ...[
                                if (!['dimensionsMm', 'dimensions_mm', 'widthMm', 'heightMm', 'piecesPerBox', 'coveragePerBoxSqm'].contains(field.id)) ...[
                                  if (field.type == 'select' && field.options.isNotEmpty)
                                    DropdownButtonFormField<String>(
                                      key: Key('spec-field-${field.id}'),
                                      initialValue: _specs[field.id] as String?,
                                      decoration: InputDecoration(
                                        labelText: '${field.label}${field.required ? " *" : ""}',
                                        helperText: _getFieldHelperText(field.id, field.unit),
                                      ),
                                      items: field.options.map((opt) => DropdownMenuItem(value: opt, child: Text(opt))).toList(),
                                      onChanged: _isSaving ? null : (v) => setState(() => _specs[field.id] = v),
                                      validator: (v) => field.required && (v == null || v.isEmpty) ? '${field.label} is required.' : null,
                                    )
                                  else
                                    TextFormField(
                                      key: Key('spec-field-${field.id}'),
                                      initialValue: _specs[field.id]?.toString(),
                                      decoration: InputDecoration(
                                        labelText: '${field.label}${field.required ? " *" : ""}',
                                        helperText: _getFieldHelperText(field.id, field.unit),
                                      ),
                                      onChanged: (v) => _specs[field.id] = v,
                                      validator: (v) => field.required && (v == null || v.trim().isEmpty) ? '${field.label} is required.' : null,
                                    ),
                                  const SizedBox(height: 8),
                                ],
                              ],
                            ],
                          ),
                        ),
                      ),
                      const SizedBox(height: 12),
                    ] else if (_selectedTemplate != null && _selectedTemplate!.parsedAttributes.isNotEmpty) ...[
                      Card(
                        elevation: 0,
                        color: Colors.blueGrey.shade50,
                        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
                        child: Padding(
                          padding: const EdgeInsets.all(12),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                '${_selectedTemplate!.name} Specifications',
                                style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 14),
                              ),
                              const SizedBox(height: 8),
                              for (final field in _selectedTemplate!.parsedAttributes) ...[
                                if (field.type == 'select' && field.options.isNotEmpty)
                                  DropdownButtonFormField<String>(
                                    key: Key('spec-field-${field.id}'),
                                    initialValue: _specs[field.id] as String?,
                                    decoration: InputDecoration(
                                      labelText: '${field.label}${field.required ? " *" : ""}',
                                      helperText: _getFieldHelperText(field.id, field.unit),
                                    ),
                                    items: field.options.map((opt) => DropdownMenuItem(value: opt, child: Text(opt))).toList(),
                                    onChanged: _isSaving ? null : (v) => setState(() => _specs[field.id] = v),
                                    validator: (v) => field.required && (v == null || v.isEmpty) ? '${field.label} is required.' : null,
                                  )
                                else if (field.type == 'number')
                                  TextFormField(
                                    key: Key('spec-field-${field.id}'),
                                    initialValue: _specs[field.id]?.toString(),
                                    keyboardType: const TextInputType.numberWithOptions(decimal: true),
                                    decoration: InputDecoration(
                                      labelText: '${field.label}${field.required ? " *" : ""}${field.unit != null ? " (${field.unit})" : ""}',
                                      helperText: _getFieldHelperText(field.id, field.unit),
                                    ),
                                    onChanged: (v) => _specs[field.id] = double.tryParse(v) ?? v,
                                    validator: (v) => field.required && (v == null || v.trim().isEmpty) ? '${field.label} is required.' : null,
                                  )
                                else
                                  TextFormField(
                                    key: Key('spec-field-${field.id}'),
                                    initialValue: _specs[field.id]?.toString(),
                                    decoration: InputDecoration(
                                      labelText: '${field.label}${field.required ? " *" : ""}',
                                      helperText: _getFieldHelperText(field.id, field.unit),
                                    ),
                                    onChanged: (v) => _specs[field.id] = v,
                                    validator: (v) => field.required && (v == null || v.trim().isEmpty) ? '${field.label} is required.' : null,
                                  ),
                                const SizedBox(height: 8),
                              ],
                            ],
                          ),
                        ),
                      ),
                      const SizedBox(height: 12),
                    ],

                    // Quantity & Pricing (Clean, full-width fields to prevent truncation)
                    Card(
                      elevation: 0,
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(10),
                        side: BorderSide(color: Colors.grey.shade300),
                      ),
                      child: Padding(
                        padding: const EdgeInsets.all(12),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            const Text('Quantity & Pricing', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 14)),
                            const SizedBox(height: 10),

                            if (_selectedTemplate?.isPackage == true || (_isCustom && _customSaleType == 'PACKAGE') || _quantityMode == 'PACKAGE') ...[
                              TextFormField(
                                key: const Key('material-package-size'),
                                controller: _packageSizeController,
                                keyboardType: const TextInputType.numberWithOptions(decimal: true),
                                decoration: InputDecoration(
                                  labelText: 'Amount in each ${_selectedTemplate?.packageType ?? _packageType} (${_unit ?? _selectedTemplate?.baseUnit ?? "unit"}) *',
                                  helperText: _selectedTemplate?.name == 'Paint' ? 'Example: 4 L can' : 'Amount contained in one unopened package',
                                ),
                                onChanged: (_) {
                                  final size = double.tryParse(_packageSizeController.text.trim()) ?? 0;
                                  final count = int.tryParse(_packageCountController.text.trim()) ?? 0;
                                  setState(() => _quantityController.text = (size * count).toString());
                                },
                                validator: (v) => _positiveNumber(v, 'Package size'),
                              ),
                              const SizedBox(height: 10),
                              TextFormField(
                                key: const Key('material-package-count'),
                                controller: _packageCountController,
                                keyboardType: TextInputType.number,
                                decoration: InputDecoration(
                                  labelText: 'Number of ${_selectedTemplate?.packageType != null ? (_selectedTemplate!.packageType!.toLowerCase() == 'box' ? 'boxes' : "${_selectedTemplate!.packageType}s") : (_packageType.toLowerCase() == 'box' ? 'boxes' : "${_packageType}s")} available *',
                                  helperText: 'Whole number of packages in stock',
                                ),
                                onChanged: (_) {
                                  final size = double.tryParse(_packageSizeController.text.trim()) ?? 0;
                                  final count = int.tryParse(_packageCountController.text.trim()) ?? 0;
                                  setState(() => _quantityController.text = (size * count).toString());
                                },
                                validator: _wholePackageCount,
                              ),
                              const SizedBox(height: 10),
                              TextFormField(
                                key: const Key('material-unit-price'),
                                controller: _priceController,
                                keyboardType: const TextInputType.numberWithOptions(decimal: true),
                                decoration: InputDecoration(
                                  labelText: 'Price per ${_selectedTemplate?.packageType ?? _packageType} (LKR) *',
                                  helperText: 'Price for one complete package',
                                ),
                                validator: (v) => _positiveNumber(v, 'Price'),
                              ),
                              const SizedBox(height: 10),
                              Text(
                                _packageTotalSummary(),
                                style: TextStyle(fontWeight: FontWeight.w600, color: Colors.blueGrey.shade800),
                              ),
                            ] else if (_selectedTemplate?.isPiece == true || (_isCustom && _customSaleType == 'PIECE') || _quantityMode == 'PIECE') ...[
                              TextFormField(
                                key: const Key('material-quantity'),
                                controller: _quantityController,
                                keyboardType: TextInputType.number,
                                decoration: const InputDecoration(
                                  labelText: 'Number of units / items *',
                                  helperText: 'Quantity of individual pieces or equipment units',
                                ),
                                validator: (v) => _positiveNumber(v, 'Number of units'),
                              ),
                              const SizedBox(height: 10),
                              TextFormField(
                                key: const Key('material-unit-price'),
                                controller: _priceController,
                                keyboardType: const TextInputType.numberWithOptions(decimal: true),
                                decoration: const InputDecoration(
                                  labelText: 'Price per item / unit (LKR) *',
                                  helperText: 'Price for one piece or unit',
                                ),
                                validator: (v) => _positiveNumber(v, 'Price per unit'),
                              ),
                              const SizedBox(height: 10),
                              Text(
                                'Total sellable stock: ${_quantityController.text.isEmpty ? "—" : _quantityController.text} units',
                                style: TextStyle(fontWeight: FontWeight.w600, color: Colors.blueGrey.shade800),
                              ),
                            ] else ...[
                              TextFormField(
                                key: const Key('material-quantity'),
                                controller: _quantityController,
                                keyboardType: const TextInputType.numberWithOptions(decimal: true),
                                decoration: const InputDecoration(
                                  labelText: 'Total quantity *',
                                  helperText: 'Total available bulk quantity',
                                ),
                                validator: (v) => _positiveNumber(v, 'Quantity'),
                              ),
                              const SizedBox(height: 10),
                              SellerUnitField(
                                key: const Key('material-unit'),
                                categoryId: _category,
                                initialUnit: _unit,
                                load: widget.gateway.categoryUnits,
                                enabled: !_isSaving,
                                onChanged: (value) => setState(() => _unit = value),
                              ),
                              const SizedBox(height: 10),
                              TextFormField(
                                key: const Key('material-unit-price'),
                                controller: _priceController,
                                keyboardType: const TextInputType.numberWithOptions(decimal: true),
                                decoration: InputDecoration(
                                  labelText: 'Price per ${_unit ?? "unit"} (LKR) *',
                                ),
                                validator: (v) => _positiveNumber(v, 'Unit price'),
                              ),
                              const SizedBox(height: 10),
                              Text(
                                'Total sellable stock: ${_quantityController.text.isEmpty ? "—" : _quantityController.text} ${_unit ?? ""}',
                                style: TextStyle(fontWeight: FontWeight.w600, color: Colors.blueGrey.shade800),
                              ),
                            ],
                          ],
                        ),
                      ),
                    ),
                    const SizedBox(height: 12),

                  DropdownButtonFormField<String>(
                    key: const Key('material-condition'),
                    initialValue: _condition,
                    decoration: const InputDecoration(labelText: 'Condition'),
                    items: const [
                      DropdownMenuItem(value: 'NEW', child: Text('NEW')),
                      DropdownMenuItem(
                        value: 'EXCELLENT',
                        child: Text('EXCELLENT'),
                      ),
                      DropdownMenuItem(value: 'GOOD', child: Text('GOOD')),
                      DropdownMenuItem(value: 'FAIR', child: Text('FAIR')),
                      DropdownMenuItem(value: 'POOR', child: Text('POOR')),
                    ],
                    onChanged: (value) {
                      if (value != null) setState(() => _condition = value);
                    },
                  ),
                  const SizedBox(height: 12),
                  ListTile(
                    contentPadding: EdgeInsets.zero,
                    title: const Text('Available until'),
                    subtitle: Text(_availableUntil.toString().substring(0, 16)),
                    trailing: const Icon(Icons.calendar_month),
                    onTap: _chooseDate,
                  ),
                  const Divider(),
                  if (_latitude != null && _longitude != null)
                    LocationCard(
                      latitude: _latitude!,
                      longitude: _longitude!,
                      accuracy: _accuracy,
                      lookup: widget.locationLookup ?? unavailableAddress,
                    )
                  else
                    const Text('No location selected yet.'),
                  if (widget.addressSearch != null) ...[
                    TextField(
                      key: const Key('material-address-search'),
                      controller: _addressController,
                      enabled: !_isSaving && !_addressBusy,
                      maxLength: 400,
                      decoration: const InputDecoration(labelText: 'Search address'),
                    ),
                    OutlinedButton.icon(
                      key: const Key('material-find-address'),
                      onPressed: _isSaving || _addressBusy ? null : _searchAddress,
                      icon: _addressBusy
                          ? const SizedBox(width: 16, height: 16, child: CircularProgressIndicator(strokeWidth: 2))
                          : const Icon(Icons.search),
                      label: Text(_addressBusy ? 'Searching addresses...' : 'Find address'),
                    ),
                    for (final result in _addressResults)
                      ListTile(
                        title: Text(result.displayName),
                        onTap: _isSaving
                            ? null
                            : () => setState(() {
                                  _latitude = result.latitude;
                                  _longitude = result.longitude;
                                  _accuracy = null;
                                  _addressResults = [];
                                }),
                      ),
                  ],
                  OutlinedButton.icon(
                    key: const Key('material-map'),
                    onPressed: _locating || _isSaving ? null : _chooseLocation,
                    icon: const Icon(Icons.map_outlined),
                    label: Text(
                      _locating ? 'Opening map...' : 'Choose location on map',
                    ),
                  ),
                  OutlinedButton.icon(
                    key: const Key('capture-gps'),
                    onPressed: _locating || _isSaving ? null : _captureGps,
                    icon: const Icon(Icons.my_location),
                    label: Text(_locating ? 'Getting location...' : 'Use my current location'),
                  ),
                  const Divider(),
                  Text(
                    'Photos',
                    style: Theme.of(context).textTheme.titleMedium,
                  ),
                  const SizedBox(height: 4),
                  const Text(
                    'Choose up to 10 photos (JPEG, PNG or WebP; 8 MB each). Camera photos are also saved to your gallery.',
                  ),
                  Wrap(
                    spacing: 8,
                    children: [
                      OutlinedButton.icon(
                        key: const Key('pick-material-images'),
                        onPressed: _mediaBusy || _isSaving
                            ? null
                            : () => _pickImages(),
                        icon: const Icon(Icons.photo_library_outlined),
                        label: const Text('Choose photos'),
                      ),
                      OutlinedButton.icon(
                        key: const Key('capture-material-photo'),
                        onPressed: _mediaBusy || _isSaving
                            ? null
                            : () => _pickImages(camera: true),
                        icon: const Icon(Icons.camera_alt_outlined),
                        label: const Text('Take photo'),
                      ),
                    ],
                  ),
                  if (_mediaBusy) const LinearProgressIndicator(),
                  for (final photo in _selectedPhotos)
                    Card(
                      child: Column(
                        children: [
                          Image.memory(
                            photo.bytes,
                            height: 140,
                            errorBuilder: (_, _, _) =>
                                const Icon(Icons.broken_image),
                          ),
                          Text(
                            photo.uploadedUrl == null
                                ? 'Ready to upload when saved'
                                : 'Uploaded',
                          ),
                          if (photo.camera)
                            Text(
                              photo.gallerySaved
                                  ? 'Saved to device gallery'
                                  : 'Not yet saved to gallery',
                            ),
                          if (photo.camera && !photo.gallerySaved)
                            TextButton(
                              onPressed: _isSaving || _mediaBusy
                                  ? null
                                  : () => _saveCameraPhoto(photo),
                              child: const Text('Save to gallery'),
                            ),
                          TextButton(
                            onPressed: _isSaving
                                ? null
                                : () => setState(
                                    () => _selectedPhotos.remove(photo),
                                  ),
                            child: const Text('Remove photo'),
                          ),
                        ],
                      ),
                    ),
                  for (final url in _photoUrls)
                    ListTile(
                      leading: Image.network(
                        widget.gateway.photoUrl(url),
                        width: 56,
                        errorBuilder: (_, _, _) =>
                            const Icon(Icons.image_outlined),
                      ),
                      title: const Text('Saved photo'),
                      trailing: IconButton(
                        tooltip: 'Remove photo',
                        onPressed: _isSaving
                            ? null
                            : () => setState(() => _photoUrls.remove(url)),
                        icon: const Icon(Icons.delete_outline),
                      ),
                    ),
                  const SizedBox(height: 20),
                  FilledButton.icon(
                    key: const Key('save-material'),
                    onPressed: _isSaving || _mediaBusy || _locating
                        ? null
                        : _save,
                    icon: _isSaving
                        ? const SizedBox(
                            width: 18,
                            height: 18,
                            child: CircularProgressIndicator(strokeWidth: 2),
                          )
                        : const Icon(Icons.save),
                    label: Text(
                      widget.isEditing ? 'Save draft changes' : 'Save draft',
                    ),
                  ),
                ],
                ],
              ),
            ),
          ),
  );
}

class _InlineError extends StatelessWidget {
  const _InlineError({required this.message});
  final String message;

  @override
  Widget build(BuildContext context) => Material(
    color: Theme.of(context).colorScheme.errorContainer,
    borderRadius: BorderRadius.circular(8),
    child: Padding(padding: const EdgeInsets.all(12), child: Text(message)),
  );
}

String? _requiredLength(String? value, String label, int maximum) {
  final input = value?.trim() ?? '';
  if (input.isEmpty) return '$label is required.';
  if (input.length > maximum) {
    return '$label must be at most $maximum characters.';
  }
  return null;
}

String? _positiveNumber(String? value, String label) {
  final number = double.tryParse(value?.trim() ?? '');
  return number != null && number > 0
      ? null
      : '$label must be greater than zero.';
}

class _SelectedPhoto {
  _SelectedPhoto(this.bytes, this.camera);
  final Uint8List bytes;
  final bool camera;
  bool gallerySaved = false;
  String? uploadedUrl;
}
