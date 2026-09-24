import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/materials/material_quantity_display.dart';

void main() {
  test('discrete and continuous material quantities use display precision', () {
    for (final unit in ['pcs', 'bag', 'box', 'set', 'roll', 'sheet', ' PCS ']) {
      expect(formatMaterialQuantity(12, unit), '12');
      expect(formatMaterialQuantity(12.25, unit), '12');
      expect(formatMaterialQuantity(0, unit), '0');
    }
    for (final unit in ['m', 'm2', 'm\u00b2', 'kg', 'L']) {
      expect(formatMaterialQuantity(12, unit), '12');
      expect(formatMaterialQuantity(12.25, unit), '12.25');
      expect(formatMaterialQuantity(12.5, unit), '12.5');
      expect(formatMaterialQuantity(10, unit), '10');
      expect(formatMaterialQuantity(0.1 + 0.2, unit), '0.3');
    }
  });
}
