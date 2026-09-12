import { apiClient } from '../../api/apiClient';

export interface CategoryListingTotal {
  categoryId: string;
  categoryName: string;
  listingCount: number;
  totalQuantity: number;
}

export interface ListingAnalyticsSummary {
  activeListings: number;
  categoryTotals: CategoryListingTotal[];
}

export interface UpcomingRequirement {
  id: string;
  title: string;
  categoryName: string;
  deadlineUtc: string;
  status: string;
}

export interface RequirementAnalyticsSummary {
  openRequirements: number;
  pendingRequirements: number;
  upcomingDeadlines: UpcomingRequirement[];
}

export interface RejectionReasonTotal {
  reason: string;
  count: number;
}

export interface MatchAnalyticsSummary {
  averageScore: number | null;
  averageDistanceKm: number | null;
  topRejectionReasons: RejectionReasonTotal[];
}

export interface TransactionAnalyticsSummary {
  pendingApprovals: number;
  approvedTransactions: number;
  rejectedTransactions: number;
  completedTransactions: number;
}

export interface ManagerDashboardData {
  listings: ListingAnalyticsSummary;
  requirements: RequirementAnalyticsSummary;
  matches: MatchAnalyticsSummary;
  transactions: TransactionAnalyticsSummary;
}

export async function getManagerDashboard(
  signal?: AbortSignal,
): Promise<ManagerDashboardData> {
  const [listings, requirements, matches, transactions] = await Promise.all([
    apiClient.get<ListingAnalyticsSummary>('/api/materials/analytics/summary', { signal }),
    apiClient.get<RequirementAnalyticsSummary>('/api/requirements/analytics/summary', { signal }),
    apiClient.get<MatchAnalyticsSummary>('/api/matches/analytics/summary', { signal }),
    apiClient.get<TransactionAnalyticsSummary>('/api/transactions/analytics/summary', { signal }),
  ]);

  return {
    listings: listings.data,
    requirements: requirements.data,
    matches: matches.data,
    transactions: transactions.data,
  };
}
