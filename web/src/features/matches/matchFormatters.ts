import { formatLkr } from '../../utils/currency';
﻿import type { MatchCandidate } from './managerMatchesApi';

const reasons: Record<string, string> = {
  LISTING_EXPIRES_BEFORE_DELIVERY: 'This material listing expires before the required delivery date.',
  INSUFFICIENT_QUANTITY: 'The seller does not have enough available quantity.',
  BUDGET_EXCEEDED: 'The material cost exceeds the maximum budget.',
  TOTAL_COST_EXCEEDS_BUDGET: 'Total material and delivery cost exceeds the maximum budget.',
  ITEM_MISMATCH: 'This material does not match the requested item.',
  REQUIRED_SPECIFICATION_MISMATCH: 'This material does not meet the required specifications.',
  ROUTING_PROVIDER_ERROR: 'Delivery route information is currently unavailable.',
  NO_MATCHING_CANDIDATE: 'No matching material candidate is currently available.',
  NO_VALID_SELECTABLE_CANDIDATE: 'No valid selectable material candidate is currently available.',
  WORKFLOW_REJECTED: 'This matching attempt could not produce a selectable candidate.',
  TOTAL_COST_UNKNOWN: 'The total material and delivery cost could not be confirmed.',
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
export const matchCurrency = formatLkr;
export function matchRoute(candidate: MatchCandidate) {
  if (candidate.status === 'ROUTE_FAILED' || candidate.rejectionReason === 'ROUTE_UNAVAILABLE') return 'Route unavailable / failed';
  const values = [candidate.routeDistanceKm == null ? null : `${candidate.routeDistanceKm} km`,
    candidate.durationMinutes == null ? null : `${candidate.durationMinutes} min`].filter(Boolean);
  return values.length ? values.join(' · ') : candidate.status === 'ROUTED' ? 'Route evaluated; metrics unavailable' : 'Route not evaluated';
}
