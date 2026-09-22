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
  durationMinutes?: number | null;
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
  const list = async (requirementId: string, query: MatchComparisonQuery): Promise<MatchComparisonPage> => {
    const data = await read(client.get<ApiMatchPage>(`/api/matches/requirement/${encodeURIComponent(requirementId)}`, {
      params: { page: query.page, pageSize: query.pageSize, sortDir: query.sortDir,
        sortBy: query.sort === 'cost' ? 'estimatedTransportCost' : query.sort,
        ...(query.status === 'VALID' ? { valid: true } : query.status === 'REJECTED' ? { rejected: true } : {}) },
    }));
    return { ...data, items: data.items.map(toCandidate) };
  };
  return {
    list,
    requirement: (requirementId) => read(client.get<BuyerRequirement>(`/api/requirements/${encodeURIComponent(requirementId)}`)),
    summary: async (requirementId) => {
      const data = await read(client.get<{ total: number; validCount: number; rejectedCount: number;
        averageScore: number | null; averageDistance: number | null; averageCost: number | null }>(
        '/api/matches/analytics/summary', { params: requirementId === 'all' ? {} : { requirementId } }));
      const top = requirementId === 'all' ? null : await list(requirementId, { status: 'VALID', sort: 'score', sortDir: 'desc', page: 1, pageSize: 1 });
      return { ...data, averageScore: data.averageScore === null ? null : data.averageScore * 100,
        averageRouteKm: data.averageDistance, topCandidate: top?.items[0] ?? null };
    },
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

interface ApiMatch { id: string; requirementId: string; listingId: string; sellerId: string;
  materialTitle: string; categoryName: string; score: number; distance: number | null;
  durationMinutes: number | null; estimatedTransportCost: number | null; status: string;
  rejected: boolean; rejectionReason: string | null; createdAt: string }
interface ApiMatchPage { items: ApiMatch[]; total: number; page: number; pageSize: number }
function toCandidate(row: ApiMatch): MatchCandidate {
  return { ...row, materialListingId: row.listingId, score: row.score * 100,
    status: row.rejected ? 'REJECTED' : 'VALID', routeDistanceKm: row.distance,
    estimatedCost: row.estimatedTransportCost, updatedAt: row.createdAt };
}
