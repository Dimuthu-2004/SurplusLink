import 'package:geolocator/geolocator.dart';

class RequirementLocation {
  const RequirementLocation(this.latitude, this.longitude, {this.accuracy});
  final double latitude, longitude;
  final double? accuracy;
}

abstract interface class RequirementLocationSource {
  Future<RequirementLocation> capture();
}

class DeviceRequirementLocation implements RequirementLocationSource {
  const DeviceRequirementLocation();
  @override
  Future<RequirementLocation> capture() async {
    if (!await Geolocator.isLocationServiceEnabled()) {
      throw const LocationCaptureException(
        'Location services are off. Turn them on or enter coordinates manually.',
      );
    }
    var permission = await Geolocator.checkPermission();
    if (permission == LocationPermission.denied) {
      permission = await Geolocator.requestPermission();
    }
    if (permission == LocationPermission.deniedForever) {
      throw const LocationCaptureException(
        'Location permission is blocked. Enable it in device settings or enter coordinates manually.',
      );
    }
    if (permission != LocationPermission.whileInUse &&
        permission != LocationPermission.always) {
      throw const LocationCaptureException(
        'Location permission was not granted. You can enter coordinates manually.',
      );
    }
    final position = await Geolocator.getCurrentPosition(
      locationSettings: const LocationSettings(accuracy: LocationAccuracy.high),
    ).timeout(const Duration(seconds: 20));
    return RequirementLocation(
      position.latitude,
      position.longitude,
      accuracy: position.accuracy,
    );
  }
}

class LocationCaptureException implements Exception {
  const LocationCaptureException(this.message);
  final String message;
}
