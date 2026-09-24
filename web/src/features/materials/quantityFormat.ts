/** Display precision only; stored quantities remain unchanged. */
export function formatMaterialQuantity(quantity: number, unit: string): string {
  const discrete = ['pcs', 'bag', 'bags', 'box', 'set', 'roll', 'sheet'];
  return Number(quantity.toFixed(discrete.includes(unit.trim().toLowerCase()) ? 0 : 2)).toString();
}
