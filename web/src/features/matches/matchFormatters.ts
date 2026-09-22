import type { MatchCandidate } from './managerMatchesApi';

const reasons: Record<string, string> = {
  LISTING_EXPIRES_BEFORE_DELIVERY: 'This material listing expires before the required delivery date.',
  INSUFFICIENT_QUANTITY: 'The seller does not have enough available quantity.',
  BUDGET_EXCEEDED: 'The material cost exceeds the maximum budget.',
  TOTAL_COST_EXCEEDS_BUDGET: 'The total cost exceeds the maximum budget.',
  CATEGORY_MISMATCH: 'This material does not match the required category.',
  UNIT_MISMATCH: 'The material unit is not compatible with the requirement.',
  SELF_MATCH_NOT_ALLOWED: 'You cannot match your own material listing.',
  LISTING_INACTIVE: 'This material listing is no longer active.',
  LISTING_NOT_ACTIVE: 'This material listing is not active.',
  LISTING_EXPIRED: 'This material listing has expired.',
  ROUTE_UNAVAILABLE: 'Delivery route information is currently unavailable.',
  DELIVERY_DEADLINE_EXCEEDED: 'The estimated delivery time exceeds the required deadline.',
  TRANSPORT_OVER_BUDGET: 'The transport cost exceeds the budget.',
};
export const matchReason = (code: string) => reasons[code] ?? 'Unable to use this match.';
export const matchCurrency = (value?: number | null) => value == null || !Number.isFinite(value) ? 'Not available' :
  'LKR ' + new Intl.NumberFormat('en-LK', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(value);
export function matchRoute(candidate: MatchCandidate) {
  if (candidate.status === 'ROUTE_FAILED' || candidate.rejectionReason === 'ROUTE_UNAVAILABLE') return 'Route unavailable / failed';
  const values = [candidate.routeDistanceKm == null ? null : `${candidate.routeDistanceKm} km`,
    candidate.durationMinutes == null ? null : `${candidate.durationMinutes} min`].filter(Boolean);
  return values.length ? values.join(' · ') : candidate.status === 'ROUTED' ? 'Route evaluated; metrics unavailable' : 'Route not evaluated';
}
