import 'package:flutter/material.dart';
import 'package:mobile/location/location_lookup.dart';

class LocationCard extends StatefulWidget {
  const LocationCard({
    required this.latitude,
    required this.longitude,
    this.accuracy,
    this.lookup = unavailableAddress,
    super.key,
  });
  final double latitude, longitude;
  final double? accuracy;
  final AddressLookup lookup;
  @override
  State<LocationCard> createState() => _LocationCardState();
}

class _LocationCardState extends State<LocationCard> {
  String? _address;
  bool _loading = true;
  int _request = 0;
  @override
  void initState() {
    super.initState();
    _lookup();
  }

  @override
  void didUpdateWidget(covariant LocationCard oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.latitude != widget.latitude ||
        oldWidget.longitude != widget.longitude) {
      _lookup();
    }
  }

  Future<void> _lookup() async {
    final request = ++_request;
    setState(() {
      _loading = true;
      _address = null;
    });
    String? address;
    try {
      address = await widget.lookup(widget.latitude, widget.longitude);
    } on Object {
      /* Keep the captured position. */
    }
    if (mounted && request == _request) {
      setState(() {
        _address = address;
        _loading = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) => Card(
    child: Padding(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text('Location', style: TextStyle(fontWeight: FontWeight.bold)),
          const SizedBox(height: 8),
          if (_loading)
            const Text('Finding address...')
          else
            Text(
              _address ??
                  'Location captured, but address lookup is temporarily unavailable.',
            ),
          if (_address != null)
            const Text('© OpenStreetMap contributors'),
          Text('Latitude: ${widget.latitude.toStringAsFixed(6)}'),
          Text('Longitude: ${widget.longitude.toStringAsFixed(6)}'),
          if (widget.accuracy != null)
            Text('GPS accuracy: approximately ${widget.accuracy!.round()} m'),
          if (widget.accuracy != null && widget.accuracy! > 100)
            const Text(
              'Low GPS accuracy. Move outdoors and capture again before saving.',
            ),
          Wrap(
            spacing: 8,
            children: [
              if (!_loading && _address == null)
                TextButton(
                  onPressed: _lookup,
                  child: const Text('Retry address'),
                ),
            ],
          ),
        ],
      ),
    ),
  );
}
