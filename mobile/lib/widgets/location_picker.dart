import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:latlong2/latlong.dart';

class PickedLocation {
  const PickedLocation(this.latitude, this.longitude);
  final double latitude;
  final double longitude;
}

typedef LocationPicker = Future<PickedLocation?> Function(
  BuildContext context, {
  double? latitude,
  double? longitude,
});

Future<PickedLocation?> showLocationPicker(
  BuildContext context, {
  double? latitude,
  double? longitude,
}) => showModalBottomSheet<PickedLocation>(
  context: context,
  isScrollControlled: true,
  builder: (_) => _LocationPickerSheet(
    latitude: latitude != null && latitude.isFinite && latitude.abs() <= 90
        ? latitude
        : null,
    longitude: longitude != null && longitude.isFinite && longitude.abs() <= 180
        ? longitude
        : null,
  ),
);

class _LocationPickerSheet extends StatefulWidget {
  const _LocationPickerSheet({this.latitude, this.longitude});
  final double? latitude;
  final double? longitude;

  @override
  State<_LocationPickerSheet> createState() => _LocationPickerSheetState();
}

class _LocationPickerSheetState extends State<_LocationPickerSheet> {
  late LatLng _center = LatLng(
    widget.latitude ?? 6.9271,
    widget.longitude ?? 79.8612,
  );

  @override
  Widget build(BuildContext context) => SafeArea(
    child: SizedBox(
      height: MediaQuery.sizeOf(context).height * .72,
      child: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(20, 16, 12, 12),
            child: Row(
              children: [
                const Expanded(
                  child: Text(
                    'Choose location on map',
                    style: TextStyle(fontSize: 18, fontWeight: FontWeight.w800),
                  ),
                ),
                IconButton(
                  onPressed: () => Navigator.pop(context),
                  icon: const Icon(Icons.close),
                ),
              ],
            ),
          ),
          const Padding(
            padding: EdgeInsets.symmetric(horizontal: 20),
            child: Align(
              alignment: Alignment.centerLeft,
              child: Text('Move the map until the pin is over the location.'),
            ),
          ),
          const SizedBox(height: 12),
          Expanded(
            child: Stack(
              alignment: Alignment.center,
              children: [
                FlutterMap(
                  options: MapOptions(
                    initialCenter: _center,
                    initialZoom: 13,
                    onPositionChanged: (position, _) {
                      setState(() => _center = position.center);
                    },
                  ),
                  children: [
                    TileLayer(
                      urlTemplate:
                          'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
                      userAgentPackageName: 'com.surpluslink.mobile',
                    ),
                  ],
                ),
                const IgnorePointer(
                  child: Icon(
                    Icons.location_pin,
                    size: 48,
                    color: Color(0xFFF47B20),
                  ),
                ),
              ],
            ),
          ),
          Padding(
            padding: const EdgeInsets.all(16),
            child: SizedBox(
              width: double.infinity,
              child: FilledButton.icon(
                onPressed: () => Navigator.pop(
                  context,
                  PickedLocation(_center.latitude, _center.longitude),
                ),
                icon: const Icon(Icons.check),
                label: const Text('Confirm location'),
              ),
            ),
          ),
        ],
      ),
    ),
  );
}
