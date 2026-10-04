import type { AxiosInstance } from 'axios';
import { apiClient, normalizeApiError } from '../../api/apiClient';

export const offerStatuses = ['PENDING', 'ACCEPTED', 'REJECTED', 'REVISION_REQUESTED'] as const;
export type OfferStatus = typeof offerStatuses[number];
export const transactionStatuses = ['PENDING_APPROVAL', 'APPROVED', 'HANDED_OVER', 'MANAGER_REVIEW_REQUIRED', 'NOT_COMPLETED', 'REJECTED', 'COMPLETED'] as const;
export type TransactionStatus = typeof transactionStatuses[number];
export interface Offer { buyerName?: string | null; sellerName?: string | null; sellerBusinessName?: string | null; materialName?: string | null; requirementTitle?: string | null; unit?: string | null; id: string; materialMatchId: string; buyerId: string; sellerId: string; quantity: number; unitValue: number; totalValue: number; status: OfferStatus; createdAt: string; updatedAt: string }
export interface Transaction { id: string; offerId: string; buyerId: string; sellerId: string; quantity: number; totalValue: number; reservedQuantity: number; status: TransactionStatus; createdAt: string; updatedAt: string; completedAt: string | null; managerApprovedAt?: string | null; confirmationDeadline?: string | null; sellerHandoverConfirmedAt?: string | null; buyerReceivedConfirmedAt?: string | null; resolvedAt?: string | null; resolutionReasonCode?: string | null; resolutionNote?: string | null }
export interface TransactionContact { fullName: string | null; email: string; phoneNumber: string | null }
export interface TransactionFollowUp { id: string; reference: string; materialTitle: string; unit: string | null; quantity: number; packageCount: number | null; totalValue: number; managerApprovedAt: string | null; confirmationDeadline: string | null; daysRemaining: number; status: TransactionStatus; sellerHandoverConfirmedAt: string | null; buyerReceivedConfirmedAt: string | null; buyer: TransactionContact; seller: TransactionContact }
export interface TransactionHistoryEntry { id: string; actorUserId: string | null; action: string; createdAt: string; note: string | null; transactionReference?: string | null; itemTitle?: string | null; quantity?: number | null; unit?: string | null }
export interface Page<T> { items: T[]; total: number; page: number; pageSize: number; totalPages: number }
export interface TransactionQuery { offerId?: string; status?: string; createdFrom?: string; createdTo?: string; userId?: string; sortBy: 'createdAt' | 'value' | 'status'; sortDir: 'asc' | 'desc'; page: number; pageSize: number }
export interface TransactionsApi { offer(id: string): Promise<Offer>; offers(query: TransactionQuery): Promise<Page<Offer>>; transactions(query: TransactionQuery): Promise<Page<Transaction>>; history(id: string, page: number): Promise<Page<TransactionHistoryEntry>> }
export function createTransactionsApi(client: Pick<AxiosInstance, 'get'> = apiClient): TransactionsApi {
  return { offer: id => read(client.get<Offer>('/api/offers/' + encodeURIComponent(id))), offers: query => read(client.get<Page<Offer>>('/api/offers', { params: compact(query) })), transactions: query => read(client.get<Page<Transaction>>('/api/transactions', { params: compact(query) })), history: (id, page) => read(client.get<Page<TransactionHistoryEntry>>(`/api/transactions/${encodeURIComponent(id)}/history`, { params: { page, pageSize: 20, sortBy: 'createdAt', sortDir: 'asc' } })) };
}
export const transactionsApi = createTransactionsApi();
export interface TransactionTimeSeriesPoint {
  period: string;
  label: string;
  transactionCount: number;
  totalValue: number;
  totalQuantity: number;
}

export interface TransactionTimeSeriesResponse {
  year: number | null;
  month: number | null;
  totalTransactions: number;
  totalValue: number;
  totalQuantity: number;
  averageValue: number;
  points: TransactionTimeSeriesPoint[];
}

export const transactionConfirmationsApi = {
  handover: async (id: string) => read(apiClient.post<Transaction>(`/api/transactions/${encodeURIComponent(id)}/handover`, {})),
  confirmReceipt: async (id: string) => read(apiClient.post<Transaction>(`/api/transactions/${encodeURIComponent(id)}/confirm-receipt`, {})),
  followUps: async () => read(apiClient.get<TransactionFollowUp[]>('/api/transactions/follow-ups')),
  resolveCompleted: async (id: string, note?: string) => read(apiClient.post<Transaction>(`/api/transactions/${encodeURIComponent(id)}/resolve-completed`, { note: note || null })),
  resolveNotCompleted: async (id: string, note: string) => read(apiClient.post<Transaction>(`/api/transactions/${encodeURIComponent(id)}/resolve-not-completed`, { note })),
  timeseries: async (year?: number, month?: number) => {
    const params: Record<string, number> = {};
    if (year) params.year = year;
    if (month) params.month = month;
    return read(apiClient.get<TransactionTimeSeriesResponse>('/api/transactions/analytics/timeseries', { params }));
  },
};
async function read<T>(request: Promise<{ data: T }>): Promise<T> { try { return (await request).data; } catch (error) { throw normalizeApiError(error); } }
function compact(query: TransactionQuery): Record<string, string | number> { return Object.fromEntries(Object.entries(query).filter(([, value]) => value !== '' && value !== undefined)) as Record<string, string | number>; }
