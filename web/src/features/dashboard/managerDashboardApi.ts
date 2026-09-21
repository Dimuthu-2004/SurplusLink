import { apiClient } from '../../api/apiClient';
import { managerMaterialsApi } from '../materials/managerMaterialsApi';
import { managerRequirementsApi } from '../requirements/managerRequirementsApi';
import { managerWorkflowsApi } from '../workflows/managerWorkflowsApi';

// The portfolio summary differs from the per-requirement comparison summary.
export interface MatchAnalyticsSummary {
  total: number;
  averageScore: number | null;
  averageDistance: number | null;
  topRejectionReasons: { reason: string; count: number }[];
  routeSuccessCount: number;
  routeFailureCount: number;
  routeSuccessRate: number | null;
  routeFailureRate: number | null;
}

export const managerDashboardApi = {
  inventory: () => managerMaterialsApi.getAnalytics(),
  requirements: () => managerRequirementsApi.summary(7),
  matches: async () => (await apiClient.get<MatchAnalyticsSummary>('/api/matches/analytics/summary')).data,
  transactions: () => managerWorkflowsApi.transactionAnalytics(),
};
