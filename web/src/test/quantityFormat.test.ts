import { describe, expect, it } from 'vitest';
import { formatMaterialQuantity } from '../features/materials/quantityFormat';

describe('material quantity display', () => {
  it.each(['pcs', 'bag', 'box', 'set', 'roll', 'sheet', ' PCS '])('formats discrete %s', unit => {
    expect(formatMaterialQuantity(12, unit)).toBe('12');
    expect(formatMaterialQuantity(12.25, unit)).toBe('12');
    expect(formatMaterialQuantity(0, unit)).toBe('0');
  });
  it.each(['m', 'm2', 'm\u00b2', 'kg', 'L'])('formats continuous %s', unit => {
    expect(formatMaterialQuantity(12.25, unit)).toBe('12.25');
    expect(formatMaterialQuantity(12.5, unit)).toBe('12.5');
    expect(formatMaterialQuantity(10, unit)).toBe('10');
    expect(formatMaterialQuantity(0.1 + 0.2, unit)).toBe('0.3');
  });
});
