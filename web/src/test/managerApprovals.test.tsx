import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';
import { ManagerWorkflowDetailsPage } from '../pages/manager/ManagerWorkflowDetailsPage';
import type { ManagerWorkflowsApi, Workflow } from '../features/workflows/managerWorkflowsApi';

const workflow: Workflow = { id: 'workflow-1', materialRequestId: 'request-1', materialMatchId: 'match-1', status: 'PENDING_APPROVAL', currentStage: 'REVIEW', retryCount: 1, startedAtUtc: '2030-01-01T00:00:00Z', completedAtUtc: null, errorJson: null, inputJson: '{}', outputJson: '{"recommendation":"reserve 12 kg"}', validationJson: '{"valid":true}', decision: null, steps: [{ id: 'step-1', sequence: 1, stage: 'MATCH', status: 'COMPLETED', inputJson: '{}', outputJson: '{"score":98}', validationJson: '{}', errorJson: null, retryCount: 0, startedAtUtc: '2030-01-01T00:00:00Z', completedAtUtc: '2030-01-01T00:00:01Z', durationMilliseconds: 1000, toolCalls: [{ id: 'tool-1', toolName: 'routing', inputJson: '{}', outputJson: '{"distanceKm":4}', errorJson: null, retryCount: 0, startedAtUtc: '2030-01-01T00:00:00Z', completedAtUtc: '2030-01-01T00:00:01Z', durationMilliseconds: 1000 }] }], approvals: [] };
function api(): ManagerWorkflowsApi { return { transactions: vi.fn().mockResolvedValue({ items: [], page: 1, pageSize: 10, total: 0, totalPages: 0 }), transactionHistory: vi.fn(), list: vi.fn(), get: vi.fn().mockResolvedValue(workflow), approve: vi.fn().mockResolvedValue({ ...workflow, status: 'APPROVED' }), reject: vi.fn().mockResolvedValue({ ...workflow, status: 'REJECTED' }), revise: vi.fn().mockResolvedValue({ ...workflow, status: 'REVISION_REQUESTED' }), transactionAnalytics: vi.fn().mockResolvedValue({ pendingApprovalCount: 1, approvedCount: 2, rejectedCount: 0, reservedQuantity: 12, completionCount: 0, completedValue: 0, completionRate: null }) }; }
function renderDetails(client: ManagerWorkflowsApi) { render(<MemoryRouter initialEntries={['/app/manager/workflows/workflow-1']}><Routes><Route path="/app/manager/workflows/:workflowId" element={<ManagerWorkflowDetailsPage api={client} />} /></Routes></MemoryRouter>); }
describe('manager workflow decisions', () => {
  it('shows reserved quantity without a manager completion action', async () => {
    const client = api();
    vi.mocked(client.transactions).mockResolvedValue({ items: [{ id: 'transaction-1', offerId: 'offer-1', status: 'APPROVED', quantity: 400, reservedQuantity: 400, totalValue: 320000, completedAt: null }], page: 1, pageSize: 10, total: 1, totalPages: 1 });
    renderDetails(client);
    await screen.findByRole('button', { name: 'History' }, { timeout: 10000 });
    expect(client.transactions).toHaveBeenCalledWith('match-1', 1);
    expect(screen.getAllByText('400')).toHaveLength(2);
    expect(screen.queryByRole('button', { name: 'Complete transfer' })).not.toBeInTheDocument();
  });
  it('submits an approval and shows the reservation outcome', async () => { const client = api(); renderDetails(client); await screen.findByRole('button', { name: 'Approve' }); await userEvent.click(screen.getByRole('button', { name: 'Approve' })); await waitFor(() => expect(client.approve).toHaveBeenCalledWith('workflow-1', '')); expect(await screen.findByText(/reservation created/i)).toBeInTheDocument(); });
  it('requires a note before reject or revision, then submits the selected decision', async () => { const client = api(); renderDetails(client); await screen.findByRole('button', { name: 'Reject' }); await userEvent.click(screen.getByRole('button', { name: 'Reject' })); expect(screen.getByRole('alert')).toHaveTextContent('A note is required'); await userEvent.type(screen.getByLabelText('Decision note'), 'Quantity does not meet policy'); await userEvent.click(screen.getByRole('button', { name: 'Reject' })); await waitFor(() => expect(client.reject).toHaveBeenCalledWith('workflow-1', 'Quantity does not meet policy')); });
  it('submits a revision request with the manager note', async () => { const client = api(); renderDetails(client); await screen.findByRole('button', { name: 'Request revision' }); await userEvent.type(screen.getByLabelText('Decision note'), 'Recheck supplier evidence'); await userEvent.click(screen.getByRole('button', { name: 'Request revision' })); await waitFor(() => expect(client.revise).toHaveBeenCalledWith('workflow-1', 'Recheck supplier evidence')); });
});
