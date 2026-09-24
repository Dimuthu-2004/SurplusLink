import 'package:flutter/material.dart';
import 'package:mobile/location/location_lookup.dart';

class ManualLocationFields extends StatefulWidget {
  const ManualLocationFields({
    required this.latitude,
    required this.longitude,
    required this.onChanged,
    required this.prefix,
    this.search,
    this.enabled = true,
    this.requiredLocation = false,
    this.showCoordinateFields = true,
    this.emphasizeSearchAction = false,
    super.key,
  });
  final TextEditingController latitude, longitude;
  final VoidCallback onChanged;
  final String prefix;
  final AddressSearch? search;
  final bool enabled,
      requiredLocation,
      showCoordinateFields,
      emphasizeSearchAction;
  @override
  State<ManualLocationFields> createState() => _ManualLocationFieldsState();
}

class _ManualLocationFieldsState extends State<ManualLocationFields> {
  final _address = TextEditingController();
  List<AddressResult> _results = [];
  String? _message;
  bool _busy = false;
  int _request = 0;
  @override
  void initState() {
    super.initState();
    widget.latitude.addListener(_coordinatesChanged);
    widget.longitude.addListener(_coordinatesChanged);
  }

  void _coordinatesChanged() {
    setState(() {
      _request++;
      _results = [];
      _message = null;
      _busy = false;
    });
  }

  @override
  void dispose() {
    widget.latitude.removeListener(_coordinatesChanged);
    widget.longitude.removeListener(_coordinatesChanged);
    _address.dispose();
    super.dispose();
  }

  Future<void> _search() async {
    final query = _address.text.trim();
    if (query.length < 3) {
      setState(() => _message = 'Enter at least 3 characters.');
      return;
    }
    final request = ++_request;
    setState(() {
      _busy = true;
      _message = null;
      _results = [];
    });
    try {
      final results = await widget.search!(query);
      if (!mounted || request != _request) return;
      setState(() {
        _results = results;
        _message = results.isEmpty
            ? 'No addresses found. Try another address, coordinates, or the map.'
            : 'Select an address to use its location.';
      });
    } on Object {
      if (mounted && request == _request) {
        setState(
          () => _message = 'Unable to search addresses. Retry, enter coordinates, or choose on the map.',
        );
      }
    } finally {
      if (mounted && request == _request) setState(() => _busy = false);
    }
  }

  String? _validate(String? text, double limit, TextEditingController other) {
    if ((text?.trim().isEmpty ?? true) &&
        !widget.requiredLocation &&
        other.text.trim().isEmpty) {
      return null;
    }
    final value = double.tryParse(text?.trim() ?? '');
    return value == null || !value.isFinite || value < -limit || value > limit
        ? 'Enter a value from -$limit to $limit.'
        : null;
  }

  @override
  Widget build(BuildContext context) => Column(
    crossAxisAlignment: CrossAxisAlignment.start,
    children: [
      Text(
        widget.showCoordinateFields
            ? 'Enter an address or coordinates, or choose a location on the map.'
            : 'Search for an address, use your current location, or choose a location on the map.',
      ),
      if (widget.search != null) ...[
        TextField(
          controller: _address,
          enabled: widget.enabled,
          maxLength: 400,
          decoration: const InputDecoration(labelText: 'Search address'),
          onChanged: (_) {
            setState(() {
              _request++;
              _busy = false;
              _results = [];
              _message = null;
            });
          },
        ),
        if (widget.emphasizeSearchAction)
          Align(
            alignment: Alignment.centerLeft,
            child: OutlinedButton.icon(
              onPressed: widget.enabled && !_busy ? _search : null,
              icon: _busy
                  ? const SizedBox(
                      width: 16,
                      height: 16,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    )
                  : const Icon(Icons.search),
              label: Text(_busy ? 'Searching addresses...' : 'Find address'),
              style: OutlinedButton.styleFrom(
                backgroundColor: Theme.of(context)
                    .colorScheme
                    .surfaceContainerHighest,
                padding: const EdgeInsets.symmetric(
                  horizontal: 14,
                  vertical: 10,
                ),
                visualDensity: VisualDensity.compact,
              ),
            ),
          )
        else
          TextButton(
            onPressed: widget.enabled && !_busy ? _search : null,
            child: Text(_busy ? 'Searching addresses...' : 'Find address'),
          ),
        if (_message != null) Text(_message!),
        for (final result in _results)
          ListTile(
            title: Text(result.displayName),
            onTap: !widget.enabled
                ? null
                : () {
                    widget.latitude.text = result.latitude.toStringAsFixed(6);
                    widget.longitude.text = result.longitude.toStringAsFixed(6);
                    setState(() {
                      _results = [];
                      _message = 'Address selected: ${result.displayName}';
                    });
                    widget.onChanged();
                  },
          ),
      ],
      if (widget.showCoordinateFields) ...[
        TextFormField(
          key: Key('${widget.prefix}-latitude'),
          controller: widget.latitude,
          enabled: widget.enabled,
          decoration: const InputDecoration(labelText: 'Latitude'),
          keyboardType: const TextInputType.numberWithOptions(
            decimal: true,
            signed: true,
          ),
          onChanged: (_) => widget.onChanged(),
          validator: (v) => _validate(v, 90, widget.longitude),
        ),
        TextFormField(
          key: Key('${widget.prefix}-longitude'),
          controller: widget.longitude,
          enabled: widget.enabled,
          decoration: const InputDecoration(labelText: 'Longitude'),
          keyboardType: const TextInputType.numberWithOptions(
            decimal: true,
            signed: true,
          ),
          onChanged: (_) => widget.onChanged(),
          validator: (v) => _validate(v, 180, widget.latitude),
        ),
      ],
      const SizedBox(height: 12),
    ],
  );
}
