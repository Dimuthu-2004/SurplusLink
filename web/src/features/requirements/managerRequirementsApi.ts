import type { AxiosInstance } from 'axios';
import { apiClient, normalizeApiError } from '../../api/apiClient';
import type { MaterialCategory } from '../materials/managerMaterialsApi';

export const requirementStatuses = ['DRAFT', 'OPEN', 'MATCHING', 'MATCH_FOUND',
  'PENDING_APPROVAL', 'APPROVED', 'REJECTED', 'COMPLETED', 'CANCELLED'] as const;
export type RequirementStatus = typeof requirementStatuses[number];
export interface BuyerRequirement {
  workflowId?: string | null; workflowStatus?: string | null; decisionNote?: string | null;
  id: string; buyerId: string; categoryId: string; requiredQuantity: number; unit: string;
  maximumBudget: number; deadline: string; latitude: number | null; longitude: number | null;
  status: RequirementStatus; notes: string; createdAt: string; updatedAt: string;
}
export interface RequirementQuery {
  search?: string; status?: string; categoryId?: string; deadlineFrom?: string; deadlineTo?: string;
  sort: 'deadline' | 'budget' | 'createdAt'; sortDir: 'asc' | 'desc'; page: number; pageSize: number;
}
export interface RequirementPage<T> { items: T[]; total: number; page: number; pageSize: number }
export interface RequirementHistory {
  id: string; actorUserId: string | null; action: string; fromStatus: string | null;
  toStatus: string | null; createdAt: string;
}
export interface RequirementAnalytics {
  total: number; openCount: number; asOf: string; upcomingUntil: string;
  upcomingDeadlineCount: number; upcomingDeadlines: BuyerRequirement[];
  countsByStatus: { status: string; count: number }[];
  countsByCategory: { categoryId: string; categoryName: string; count: number }[];
  averageMaximumBudget: number | null;
  averageQuantityByUnit: { unit: string; averageRequiredQuantity: number; count: number }[];
}
export interface ManagerRequirementsApi {
  list(query: RequirementQuery): Promise<RequirementPage<BuyerRequirement>>;
  get(id: string): Promise<BuyerRequirement>;
  history(id: string, page: number): Promise<RequirementPage<RequirementHistory>>;
  categories(): Promise<MaterialCategory[]>;
  summary(upcomingDays: number): Promise<RequirementAnalytics>;
}
export function createManagerRequirementsApi(client: Pick<AxiosInstance, 'get'> = apiClient): ManagerRequirementsApi {
  return {
    list: (query) => read(client.get<RequirementPage<BuyerRequirement>>('/api/requirements', {
      params: Object.fromEntries(Object.entries(query).filter(([, value]) => value !== '' && value !== undefined)),
    })),
    get: (id) => read(client.get<BuyerRequirement>('/api/requirements/' + encodeURIComponent(id))),
    history: (id, page) => read(client.get<RequirementPage<RequirementHistory>>(
      '/api/requirements/' + encodeURIComponent(id) + '/history', { params: { page, pageSize: 20 } })),
    categories: () => read(client.get<MaterialCategory[]>('/api/material-categories')),
    summary: (upcomingDays) => read(client.get<RequirementAnalytics>('/api/requirements/analytics/summary',
      { params: { upcomingDays } })),
  };
}
async function read<T>(request: Promise<{ data: T }>): Promise<T> {
  try { return (await request).data; } catch (error) { throw normalizeApiError(error); }
}
export const managerRequirementsApi = createManagerRequirementsApi();
