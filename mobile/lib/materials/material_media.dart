import 'dart:typed_data';

import 'package:gal/gal.dart';
import 'package:image_picker/image_picker.dart';

class MaterialMedia {
  Future<List<XFile>> pickGallery() =>
      ImagePicker().pickMultiImage(imageQuality: 85, maxWidth: 2400);
  Future<XFile?> takePhoto() => ImagePicker().pickImage(
    source: ImageSource.camera,
    imageQuality: 85,
    maxWidth: 2400,
  );
  Future<void> saveToGallery(Uint8List bytes) => Gal.putImageBytes(
    bytes,
    name: 'SurplusLink-${DateTime.now().millisecondsSinceEpoch}',
  );
}
