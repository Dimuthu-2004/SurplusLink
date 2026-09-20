import type { AxiosInstance } from 'axios';
import { apiClient, normalizeApiError } from '../../api/apiClient';
import type { BuyerRequirement } from '../requirements/managerRequirementsApi';

export type MatchStatus = 'VALID' | 'REJECTED';
export type MatchSort = 'score' | 'distance' | 'cost';

export interface MatchCandidate {
  id: string;
  requirementId: string;
  materialListingId: string;
  sellerId: string;
  materialTitle: string;
  categoryName: string;
  status: MatchStatus;
  score: number;
  routeDistanceKm: number | null;
  estimatedCost: number | null;
  rejectionReason: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface MatchComparisonQuery {
  status?: 'ALL' | MatchStatus;
  sort: MatchSort;
  sortDir: 'asc' | 'desc';
  page: number;
  pageSize: number;
}

export interface MatchComparisonPage {
  items: MatchCandidate[];
  total: number;
  page: number;
  pageSize: number;
}

export interface MatchSummary {
  total: number;
  validCount: number;
  rejectedCount: number;
  averageScore: number | null;
  averageRouteKm: number | null;
  averageCost: number | null;
  topCandidate: MatchCandidate | null;
}

export interface ManagerMatchesApi {
  list(requirementId: string, query: MatchComparisonQuery): Promise<MatchComparisonPage>;
  requirement(requirementId: string): Promise<BuyerRequirement>;
  summary(requirementId: string): Promise<MatchSummary>;
}

export function createManagerMatchesApi(
  client: Pick<AxiosInstance, 'get'> = apiClient,
): ManagerMatchesApi {
  return {
    list: (requirementId, query) => read(client.get<MatchComparisonPage>(`/api/requirements/${encodeURIComponent(requirementId)}/matches`, {
      params: compact(query),
    })),
    requirement: (requirementId) => read(client.get<BuyerRequirement>(`/api/requirements/${encodeURIComponent(requirementId)}`)),
    summary: (requirementId) => read(client.get<MatchSummary>(`/api/requirements/${encodeURIComponent(requirementId)}/matches/analytics`)),
  };
}

export const managerMatchesApi = createManagerMatchesApi();

async function read<T>(operation: Promise<{ data: T }>): Promise<T> {
  try {
    return (await operation).data;
  } catch (error) {
    throw normalizeApiError(error);
  }
}

function compact(query: MatchComparisonQuery): Record<string, string | number> {
  return Object.fromEntries(
    Object.entries(query).filter(([, value]) => value !== '' && value !== undefined && value !== 'ALL'),
  ) as Record<string, string | number>;
}
