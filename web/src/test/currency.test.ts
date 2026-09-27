import { describe, expect, it } from 'vitest';
import { formatLkr } from '../utils/currency';

describe('LKR currency', () => {
  it('formats rupees with thousands separators and exactly two decimals', () => {
    expect(formatLkr(2000)).toBe('LKR 2,000.00');
    expect(formatLkr(25500.5)).toBe('LKR 25,500.50');
    expect(formatLkr(0)).toBe('LKR 0.00');
    expect(formatLkr(null)).toBe('Not available');
    expect(formatLkr(NaN)).toBe('Not available');
  });
});
