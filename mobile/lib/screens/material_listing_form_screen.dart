import 'dart:typed_data';

import 'package:mobile/categories/category_dropdown.dart';
import 'package:mobile/categories/material_category.dart';

import 'package:flutter/material.dart';
import 'package:geolocator/geolocator.dart';
import 'package:go_router/go_router.dart';
import 'package:image_picker/image_picker.dart';
import 'package:mobile/core/api_exception.dart';
import 'package:mobile/materials/material_inventory_gateway.dart';
import 'package:mobile/materials/material_models.dart';

class MaterialListingFormScreen extends StatefulWidget {
  const MaterialListingFormScreen({
    required this.gateway,
    this.listingId,
    super.key,
  });

  final MaterialInventoryGateway gateway;
  final String? listingId;

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
  final _unitController = TextEditingController(text: 'kg');
  final _priceController = TextEditingController();
  final _photoUrlController = TextEditingController();
  final List<String> _photoUrls = [];
  final List<Uint8List> _pickedImageBytes = [];
  String _condition = 'GOOD';
  DateTime _availableUntil = DateTime.now().add(const Duration(days: 7));
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
    _unitController.dispose();
    _priceController.dispose();
    _photoUrlController.dispose();
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
        _unitController.text = listing.unit;
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

  Future<void> _pickImages() async {
    try {
      final files = await ImagePicker().pickMultiImage(imageQuality: 80);
      if (!mounted || files.isEmpty) return;
      final bytes = await Future.wait(files.map((file) => file.readAsBytes()));
      if (!mounted) return;
      setState(() => _pickedImageBytes.addAll(bytes));
    } on Object {
      if (mounted) {
        _showMessage(
          'Unable to select images. Check photo permission and try again.',
        );
      }
    }
  }

  Future<void> _captureGps() async {
    try {
      if (!await Geolocator.isLocationServiceEnabled()) {
        throw StateError('Location services are disabled.');
      }
      var permission = await Geolocator.checkPermission();
      if (permission == LocationPermission.denied) {
        permission = await Geolocator.requestPermission();
      }
      if (permission == LocationPermission.denied ||
          permission == LocationPermission.deniedForever) {
        throw StateError('Location permission was not granted.');
      }
      final position = await Geolocator.getCurrentPosition();
      if (!mounted) return;
      setState(() {
        _latitude = position.latitude;
        _longitude = position.longitude;
      });
    } on StateError catch (error) {
      if (mounted) _showMessage(error.message);
    } on Object {
      if (mounted) _showMessage('Unable to capture GPS location.');
    }
  }

  void _addPhotoUrl() {
    final value = _photoUrlController.text.trim();
    final uri = Uri.tryParse(value);
    if (uri == null || !(uri.isScheme('https') || uri.isScheme('http'))) {
      _showMessage('Enter a valid http or https image URL.');
      return;
    }
    if (_photoUrls.length >= 10) {
      _showMessage('A listing can contain at most 10 photo URLs.');
      return;
    }
    setState(() {
      _photoUrls.add(value);
      _photoUrlController.clear();
    });
  }

  Future<void> _save() async {
    if (_isSaving || !_formKey.currentState!.validate()) return;
    if (!_availableUntil.isAfter(DateTime.now())) {
      _showMessage('Available until must be in the future.');
      return;
    }
    final draft = MaterialListingDraft(
      categoryId: _category!,
      title: _titleController.text,
      description: _descriptionController.text,
      quantity: double.parse(_quantityController.text.trim()),
      unit: _unitController.text,
      condition: _condition,
      unitPrice: double.parse(_priceController.text.trim()),
      latitude: _latitude,
      longitude: _longitude,
      availableUntil: _availableUntil,
      photoUrls: List.unmodifiable(_photoUrls),
    );
    setState(() {
      _isSaving = true;
      _error = null;
    });
    try {
      if (widget.isEditing) {
        await widget.gateway.update(widget.listingId!, draft);
      } else {
        await widget.gateway.create(draft);
      }
      if (mounted) context.pop(true);
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
                        : (value) => setState(() => _category = value),
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
                        child: TextFormField(
                          key: const Key('material-unit'),
                          controller: _unitController,
                          maxLength: 32,
                          decoration: const InputDecoration(labelText: 'Unit'),
                          validator: (value) =>
                              _requiredLength(value, 'Unit', 32),
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
                    decoration: const InputDecoration(labelText: 'Unit price'),
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
                  Row(
                    children: [
                      Expanded(
                        child: Text(
                          _latitude == null
                              ? 'No GPS location captured'
                              : 'GPS: ${_latitude!.toStringAsFixed(6)}, ${_longitude!.toStringAsFixed(6)}',
                        ),
                      ),
                      OutlinedButton.icon(
                        key: const Key('capture-gps'),
                        onPressed: _captureGps,
                        icon: const Icon(Icons.my_location),
                        label: const Text('Capture GPS'),
                      ),
                    ],
                  ),
                  const Divider(),
                  Text(
                    'Photos',
                    style: Theme.of(context).textTheme.titleMedium,
                  ),
                  const SizedBox(height: 4),
                  const Text(
                    'The API accepts hosted image URLs. Picked local images are preview-only until an upload API is provided.',
                  ),
                  const SizedBox(height: 8),
                  OutlinedButton.icon(
                    key: const Key('pick-material-images'),
                    onPressed: _pickImages,
                    icon: const Icon(Icons.photo_library_outlined),
                    label: const Text('Pick images for preview'),
                  ),
                  if (_pickedImageBytes.isNotEmpty)
                    SizedBox(
                      height: 100,
                      child: ListView.separated(
                        scrollDirection: Axis.horizontal,
                        itemCount: _pickedImageBytes.length,
                        separatorBuilder: (_, _) => const SizedBox(width: 8),
                        itemBuilder: (_, index) => ClipRRect(
                          borderRadius: BorderRadius.circular(8),
                          child: Image.memory(
                            _pickedImageBytes[index],
                            width: 100,
                            height: 100,
                            fit: BoxFit.cover,
                          ),
                        ),
                      ),
                    ),
                  const SizedBox(height: 8),
                  Row(
                    children: [
                      Expanded(
                        child: TextField(
                          key: const Key('material-photo-url'),
                          controller: _photoUrlController,
                          keyboardType: TextInputType.url,
                          decoration: const InputDecoration(
                            labelText: 'Hosted image URL',
                          ),
                        ),
                      ),
                      const SizedBox(width: 8),
                      IconButton(
                        tooltip: 'Add image URL',
                        onPressed: _addPhotoUrl,
                        icon: const Icon(Icons.add_photo_alternate_outlined),
                      ),
                    ],
                  ),
                  for (var index = 0; index < _photoUrls.length; index++)
                    ListTile(
                      contentPadding: EdgeInsets.zero,
                      leading: const Icon(Icons.image_outlined),
                      title: Text(
                        _photoUrls[index],
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                      ),
                      trailing: IconButton(
                        tooltip: 'Remove image URL',
                        onPressed: () =>
                            setState(() => _photoUrls.removeAt(index)),
                        icon: const Icon(Icons.delete_outline),
                      ),
                    ),
                  const SizedBox(height: 20),
                  FilledButton.icon(
                    key: const Key('save-material'),
                    onPressed: _isSaving ? null : _save,
                    icon: _isSaving
                        ? const SizedBox(
                            width: 18,
                            height: 18,
                            child: CircularProgressIndicator(strokeWidth: 2),
                          )
                        : const Icon(Icons.save),
                    label: Text(
                      widget.isEditing ? 'Save changes' : 'Create material',
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
