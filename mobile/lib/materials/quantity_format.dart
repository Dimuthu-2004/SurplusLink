/// Quantity formatting helpers that respect the unit type.
///
/// Discrete units (pcs, bag, box, set, roll, sheet, pair, tonne) are shown as
/// integers when the value is a whole number (e.g. "12 bag"), because writing
/// "12.00 bag" is unnatural.  Continuous units (m, m², kg, L, …) display up
/// to [maxFractionDigits] significant decimal places.
library;

export 'material_quantity_display.dart' show formatMaterialQuantity;

// Units whose quantity is conceptually countable/whole-number.
const _discreteUnits = {
  'pcs',
  'bag',
  'box',
  'set',
  'roll',
  'sheet',
  'pair',
  'tonne',
};

/// Returns true when [unit] should display as a whole number when possible.
bool isDiscreteUnit(String unit) =>
    _discreteUnits.contains(unit.trim().toLowerCase());

/// Formats [quantity] for display next to [unit].
///
/// - Discrete units: whole number if no fractional part, otherwise up to 2 dp.
/// - Continuous units: up to [maxFractionDigits] decimal places (trailing
///   zeros stripped) but always at least the number of significant digits
///   needed to represent the value faithfully.
String formatQuantity(
  double quantity,
  String unit, {
  int maxFractionDigits = 3,
}) {
  if (isDiscreteUnit(unit)) {
    // Show as integer when the value is whole; otherwise fall back to 2 dp.
    if (quantity == quantity.truncateToDouble()) {
      return quantity.toInt().toString();
    }
    return quantity.toStringAsFixed(2);
  }

  // Continuous: strip trailing zeros up to [maxFractionDigits].
  final fixed = quantity.toStringAsFixed(maxFractionDigits);
  // Remove trailing zeros after decimal point.
  final stripped = fixed.contains('.')
      ? fixed.replaceAll(RegExp(r'0+$'), '').replaceAll(RegExp(r'\.$'), '')
      : fixed;
  return stripped.isEmpty ? '0' : stripped;
}
