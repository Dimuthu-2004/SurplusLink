import { apiClient } from '../../api/apiClient';
import { managerMaterialsApi } from '../materials/managerMaterialsApi';
import { managerRequirementsApi } from '../requirements/managerRequirementsApi';
import { managerWorkflowsApi } from '../workflows/managerWorkflowsApi';

// The portfolio summary differs from the per-requirement comparison summary.
export interface MatchAnalyticsSummary {
  total: number;
  validCount?: number;
  rejectedCount?: number;
  averageScore: number | null;
  averageDistance: number | null;
  topRejectionReasons: { reason: string; count: number }[];
  routeSuccessCount: number;
  routeFailureCount: number;
  routeSuccessRate: number | null;
  routeFailureRate: number | null;
}

export interface UserSummary {
  totalUsers: number;
  buyers: number;
  sellers: number;
  dualRoleUsers: number;
  managers: number;
}

export const managerDashboardApi = {
  approvals: () => managerWorkflowsApi.list({ search: '', status: 'PENDING_APPROVAL', sortBy: 'startedAt', sortDir: 'desc', page: 1, pageSize: 1 }),
  inventory: () => managerMaterialsApi.getAnalytics(),
  requirements: () => managerRequirementsApi.summary(7),
  matches: async () => (await apiClient.get<MatchAnalyticsSummary>('/api/matches/analytics/summary')).data,
  transactions: () => managerWorkflowsApi.transactionAnalytics(),
  usersSummary: async () => (await apiClient.get<UserSummary>('/api/users/summary')).data,
};
