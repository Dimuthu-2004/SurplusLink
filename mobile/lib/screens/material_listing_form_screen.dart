import 'package:mobile/widgets/manual_location_fields.dart';
import 'package:mobile/categories/seller_unit_field.dart';
import 'package:mobile/widgets/dashboard_back_button.dart';

import 'dart:typed_data';

import 'package:mobile/categories/category_dropdown.dart';
import 'package:mobile/categories/material_category.dart';

import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:mobile/location/location_lookup.dart';
import 'package:mobile/materials/material_media.dart';
import 'package:mobile/requirements/requirement_location.dart';
import 'package:mobile/widgets/location_card.dart';
import 'package:mobile/core/api_exception.dart';
import 'package:mobile/materials/material_inventory_gateway.dart';
import 'package:mobile/materials/material_models.dart';
import 'package:mobile/widgets/location_picker.dart';

class MaterialListingFormScreen extends StatefulWidget {
  const MaterialListingFormScreen({
    required this.gateway,
    this.listingId,
    this.media,
    this.locationSource = const DeviceRequirementLocation(),
    this.locationLookup,
    this.addressSearch,
    this.locationPicker = showLocationPicker,
    super.key,
  });

  final MaterialInventoryGateway gateway;
  final String? listingId;
  final MaterialMedia? media;
  final RequirementLocationSource locationSource;
  final AddressLookup? locationLookup;
  final AddressSearch? addressSearch;
  final LocationPicker locationPicker;

  bool get isEditing => listingId != null;

  @override
  State<MaterialListingFormScreen> createState() =>
      _MaterialListingFormScreenState();
}

class _MaterialListingFormScreenState extends State<MaterialListingFormScreen> {
  final _formKey = GlobalKey<FormState>();
  List<MaterialCategory> _categories = [];
  String? _category;
  bool _loadFailed = false;
  final _titleController = TextEditingController();
  final _descriptionController = TextEditingController();
  final _quantityController = TextEditingController();
  String? _unit;
  final _priceController = TextEditingController();
  final List<String> _photoUrls = [];
  final List<_SelectedPhoto> _selectedPhotos = [];
  late final _media = widget.media ?? MaterialMedia();
  bool _mediaBusy = false, _locating = false;
  double? _accuracy;
  String _condition = 'GOOD';
  DateTime _availableUntil = DateTime.now().add(const Duration(days: 7));
  final _latitudeText = TextEditingController();
  final _longitudeText = TextEditingController();
  double? _latitude;
  double? _longitude;
  bool _isLoading = false;
  bool _isSaving = false;
  String? _error;

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

    _priceController.dispose();
    _latitudeText.dispose();
    _longitudeText.dispose();
    super.dispose();
  }

  Future<void> _loadExisting() async {
    setState(() {
      _isLoading = true;
      _loadFailed = false;
      _error = null;
    });
    try {
      final categories = await widget.gateway.categories();
      if (!mounted) return;
      setState(() => _categories = categories);
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
      setState(() {
        _category = listing.categoryId;
        _titleController.text = listing.title;
        _descriptionController.text = listing.description;
        _quantityController.text = listing.quantity.toString();
        _unit = listing.unit;
        _priceController.text = listing.unitPrice.toString();
        _condition = listing.condition;
        _availableUntil = listing.availableUntil;
        _latitude = listing.latitude;
        _longitude = listing.longitude;
        _latitudeText.text = listing.latitude?.toString() ?? '';
        _longitudeText.text = listing.longitude?.toString() ?? '';
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
      final files = camera ? [?captured] : await _media.pickGallery();
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
          _latitudeText.text = picked.latitude.toStringAsFixed(6);
          _longitudeText.text = picked.longitude.toStringAsFixed(6);
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

  Future<void> _save() async {
    if (_isSaving ||
        _mediaBusy ||
        _locating ||
        !_formKey.currentState!.validate()) {
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
      final draft = MaterialListingDraft(
        categoryId: _category!,
        title: _titleController.text,
        description: _descriptionController.text,
        quantity: double.parse(_quantityController.text.trim()),
        unit: _unit!,
        condition: _condition,
        unitPrice: double.parse(_priceController.text.trim()),
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
              autovalidateMode: AutovalidateMode.onUserInteraction,
              child: ListView(
                padding: const EdgeInsets.all(16),
                children: [
                  if (_error != null) ...[
                    _InlineError(message: _error!),
                    const SizedBox(height: 12),
                  ],
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
                  TextFormField(
                    key: const Key('material-title'),
                    controller: _titleController,
                    maxLength: 200,
                    decoration: const InputDecoration(labelText: 'Title'),
                    validator: (value) => _requiredLength(value, 'Title', 200),
                  ),
                  const SizedBox(height: 12),
                  TextFormField(
                    key: const Key('material-description'),
                    controller: _descriptionController,
                    minLines: 3,
                    maxLines: 6,
                    maxLength: 2000,
                    decoration: const InputDecoration(labelText: 'Description'),
                    validator: (value) =>
                        _requiredLength(value, 'Description', 2000),
                  ),
                  const SizedBox(height: 12),
                  Row(
                    children: [
                      Expanded(
                        child: TextFormField(
                          key: const Key('material-quantity'),
                          controller: _quantityController,
                          keyboardType: const TextInputType.numberWithOptions(
                            decimal: true,
                          ),
                          decoration: const InputDecoration(
                            labelText: 'Quantity',
                          ),
                          validator: (value) =>
                              _positiveNumber(value, 'Quantity'),
                        ),
                      ),
                      const SizedBox(width: 8),
                      Expanded(
                        child: SellerUnitField(
                          key: ValueKey('material-unit-$_category'),
                          categoryId: _category,
                          initialUnit: _unit,
                          load: widget.gateway.categoryUnits,
                          enabled: !_isSaving,
                          onChanged: (value) => _unit = value,
                        ),
                      ),
                    ],
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
                  TextFormField(
                    key: const Key('material-unit-price'),
                    controller: _priceController,
                    keyboardType: const TextInputType.numberWithOptions(
                      decimal: true,
                    ),
                    decoration: const InputDecoration(
                      labelText: 'Unit price (LKR)',
                    ),
                    validator: (value) => _positiveNumber(value, 'Unit price'),
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
                  ManualLocationFields(
                    latitude: _latitudeText,
                    longitude: _longitudeText,
                    prefix: 'material',
                    search: widget.addressSearch,
                    enabled: !_isSaving && !_locating,
                    onChanged: () => setState(() {
                      _latitude = double.tryParse(_latitudeText.text.trim());
                      _longitude = double.tryParse(_longitudeText.text.trim());
                      _accuracy = null;
                    }),
                  ),
                  if (_latitude != null &&
                      _longitude != null &&
                      _latitude!.isFinite &&
                      _longitude!.isFinite &&
                      _latitude!.abs() <= 90 &&
                      _longitude!.abs() <= 180)
                    LocationCard(
                      latitude: _latitude!,
                      longitude: _longitude!,
                      accuracy: _accuracy,
                      lookup: widget.locationLookup ?? unavailableAddress,
                    )
                  else
                    const Text('No location selected yet.'),
                  OutlinedButton.icon(
                    key: const Key('capture-gps'),
                    onPressed: _locating || _isSaving ? null : _chooseLocation,
                    icon: const Icon(Icons.map_outlined),
                    label: Text(
                      _locating ? 'Opening map...' : 'Choose location on map',
                    ),
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
