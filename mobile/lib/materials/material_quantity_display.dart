/// Formats inventory quantities for display without changing stored precision.
String formatMaterialQuantity(num quantity, String unit) {
  const discrete = {'pcs', 'bag', 'bags', 'box', 'set', 'roll', 'sheet'};
  if (discrete.contains(unit.trim().toLowerCase())) {
    return quantity.toStringAsFixed(0);
  }
  return quantity.toStringAsFixed(2).replaceFirst(RegExp(r'\.?0+$'), '');
}
