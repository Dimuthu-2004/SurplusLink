import type { AxiosInstance } from 'axios';
import { apiClient, normalizeApiError } from '../../api/apiClient';

export const offerStatuses = ['PENDING', 'ACCEPTED', 'REJECTED', 'REVISION_REQUESTED'] as const;
export type OfferStatus = typeof offerStatuses[number];
export const transactionStatuses = ['PENDING_APPROVAL', 'APPROVED', 'REJECTED', 'COMPLETED'] as const;
export type TransactionStatus = typeof transactionStatuses[number];
export interface Offer { id: string; materialMatchId: string; buyerId: string; sellerId: string; quantity: number; unitValue: number; totalValue: number; status: OfferStatus; createdAt: string; updatedAt: string }
export interface Transaction { id: string; offerId: string; buyerId: string; sellerId: string; quantity: number; totalValue: number; reservedQuantity: number; status: TransactionStatus; createdAt: string; updatedAt: string; completedAt: string | null }
export interface TransactionHistoryEntry { id: string; actorUserId: string | null; action: string; createdAt: string; note: string | null }
export interface Page<T> { items: T[]; total: number; page: number; pageSize: number; totalPages: number }
export interface TransactionQuery { offerId?: string; status?: string; createdFrom?: string; createdTo?: string; userId?: string; sortBy: 'createdAt' | 'value' | 'status'; sortDir: 'asc' | 'desc'; page: number; pageSize: number }
export interface TransactionsApi { offer(id: string): Promise<Offer>; offers(query: TransactionQuery): Promise<Page<Offer>>; transactions(query: TransactionQuery): Promise<Page<Transaction>>; history(id: string, page: number): Promise<Page<TransactionHistoryEntry>> }
export function createTransactionsApi(client: Pick<AxiosInstance, 'get'> = apiClient): TransactionsApi {
  return { offer: id => read(client.get<Offer>('/api/offers/' + encodeURIComponent(id))), offers: query => read(client.get<Page<Offer>>('/api/offers', { params: compact(query) })), transactions: query => read(client.get<Page<Transaction>>('/api/transactions', { params: compact(query) })), history: (id, page) => read(client.get<Page<TransactionHistoryEntry>>(`/api/transactions/${encodeURIComponent(id)}/history`, { params: { page, pageSize: 20, sortBy: 'createdAt', sortDir: 'asc' } })) };
}
export const transactionsApi = createTransactionsApi();
async function read<T>(request: Promise<{ data: T }>): Promise<T> { try { return (await request).data; } catch (error) { throw normalizeApiError(error); } }
function compact(query: TransactionQuery): Record<string, string | number> { return Object.fromEntries(Object.entries(query).filter(([, value]) => value !== '' && value !== undefined)) as Record<string, string | number>; }
