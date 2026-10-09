import { describe, expect, it } from 'vitest';
import { matchReason } from '../features/matches/matchFormatters';

describe('match rejection reason messages', () => {
  it('renders factual persisted codes as readable messages', () => {
    expect(matchReason('TOTAL_COST_EXCEEDS_BUDGET')).toBe(
      'Total material and delivery cost exceeds the maximum budget.',
    );
    for (const code of ['ITEM_MISMATCH', 'REQUIRED_SPECIFICATION_MISMATCH', 'ROUTING_PROVIDER_ERROR']) {
      expect(matchReason(code)).not.toBe('Unable to use this match.');
    }
  });
});
