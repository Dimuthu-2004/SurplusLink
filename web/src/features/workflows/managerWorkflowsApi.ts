import type { AxiosInstance } from 'axios';
import { apiClient, normalizeApiError } from '../../api/apiClient';

export const workflowStatuses = ['RUNNING', 'PENDING_APPROVAL', 'REVISION_REQUESTED', 'APPROVED', 'REJECTED', 'FAILED', 'COMPLETED'] as const;
export type WorkflowStatus = typeof workflowStatuses[number];
export interface WorkflowListItem {
  id: string; materialRequestId: string | null; materialMatchId: string | null; status: WorkflowStatus;
  currentStage: string; retryCount: number; startedAtUtc: string; completedAtUtc: string | null; errorJson: string | null;
  approvalGroup?: ApprovalGroupSummary | null;
}
export interface ApprovalGroupSummary {
  requirementTitle: string; buyerName: string; requestedQuantity: number; selectedQuantity: number;
  remainingQuantity: number; unit: string; fulfillmentStatus: 'FULL' | 'PARTIAL'; sellerCount: number; totalValue: number;
}
export interface ApprovalAllocation {
  transactionId: string; sellerId: string; sellerName: string; sellerBusinessName: string | null;
  listingId: string; listingTitle: string; allocatedQuantity: number; availableQuantity: number;
  unit: string; unitPrice: number; materialValue: number; score: number | null;
  distance: number | null; transportCost: number | null; status: string;
}
export interface ApprovalGroup extends ApprovalGroupSummary { allocations: ApprovalAllocation[] }
export interface ToolCall {
  id: string; toolName: string; inputJson: string; outputJson: string; errorJson: string | null;
  retryCount: number; startedAtUtc: string; completedAtUtc: string | null; durationMilliseconds: number | null;
}
export interface AgentStep {
  id: string; sequence: number; stage: string; status: string; inputJson: string; outputJson: string;
  validationJson: string; errorJson: string | null; retryCount: number; startedAtUtc: string;
  completedAtUtc: string | null; durationMilliseconds: number | null; toolCalls: ToolCall[];
}
export interface Approval { id: string; decidedByUserId: string; decision: string; note: string; decidedAtUtc: string }
export interface Workflow extends WorkflowListItem {
  inputJson: string; outputJson: string; validationJson: string; decision: string | null; steps: AgentStep[]; approvals: Approval[];
  recommendationReason?: string | null; approvalGroup?: ApprovalGroup | null;
}
export interface WorkflowPage { items: WorkflowListItem[]; total: number; page: number; pageSize: number; totalPages: number }
export interface WorkflowQuery { search: string; status: string; sortBy: 'startedAt' | 'status' | 'stage'; sortDir: 'asc' | 'desc'; page: number; pageSize: number }
export interface TransactionAnalytics { pendingApprovalCount: number; approvedCount: number; rejectedCount: number; reservedQuantity: number; completionCount: number; completedValue: number; completionRate: number | null }
export interface TransactionOutcome { id: string; offerId: string; status: string; quantity: number; totalValue: number; reservedQuantity: number; completedAt: string | null }
export interface TransactionOutcomePage { items: TransactionOutcome[]; page: number; pageSize: number; total: number; totalPages: number }
export interface TransactionHistory { items: { id: string; action: string; createdAt: string }[]; page: number; totalPages: number }
export interface ManagerWorkflowsApi {
  transactions(matchId: string, page: number): Promise<TransactionOutcomePage>;
  transactionHistory(id: string, page: number): Promise<TransactionHistory>;
  list(query: WorkflowQuery): Promise<WorkflowPage>;
  get(id: string): Promise<Workflow>;
  approve(id: string, note?: string): Promise<Workflow>;
  reject(id: string, note: string): Promise<Workflow>;
  revise(id: string, note: string): Promise<Workflow>;
  transactionAnalytics(): Promise<TransactionAnalytics>;
}

export function createManagerWorkflowsApi(client: Pick<AxiosInstance, 'get' | 'post'> = apiClient): ManagerWorkflowsApi {
  return {
    transactions: (matchId, page) => read(client.get<TransactionOutcomePage>('/api/transactions', { params: { matchId, page, pageSize: 10 } })),
    transactionHistory: (id, page) => read(client.get<TransactionHistory>('/api/transactions/' + encodeURIComponent(id) + '/history', { params: { page, pageSize: 20 } })),
    list: (query) => read(client.get<WorkflowPage>('/api/workflows', { params: compact(query) })),
    get: (id) => read(client.get<Workflow>('/api/workflows/' + encodeURIComponent(id))),
    approve: (id, note) => read(client.post<Workflow>('/api/workflows/' + encodeURIComponent(id) + '/approve', { note: note?.trim() || undefined })),
    reject: (id, note) => read(client.post<Workflow>('/api/workflows/' + encodeURIComponent(id) + '/reject', { note: note.trim() })),
    revise: (id, note) => read(client.post<Workflow>('/api/workflows/' + encodeURIComponent(id) + '/revise', { note: note.trim() })),
    transactionAnalytics: () => read(client.get<TransactionAnalytics>('/api/transactions/analytics/summary')),
  };
}
export const managerWorkflowsApi = createManagerWorkflowsApi();
async function read<T>(request: Promise<{ data: T }>): Promise<T> { try { return (await request).data; } catch (error) { throw normalizeApiError(error); } }
function compact(query: WorkflowQuery): Record<string, string | number> {
  return Object.fromEntries(Object.entries(query).filter(([, value]) => value !== '')) as Record<string, string | number>;
}
