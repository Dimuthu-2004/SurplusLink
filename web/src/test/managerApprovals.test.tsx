import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';
import { ManagerWorkflowDetailsPage } from '../pages/manager/ManagerWorkflowDetailsPage';
import type { ManagerWorkflowsApi, Workflow } from '../features/workflows/managerWorkflowsApi';

vi.mock('../features/requirements/managerRequirementsApi', () => ({ managerRequirementsApi: {
  get: vi.fn().mockResolvedValue({ id: 'request-1', categoryId: 'category-1', requiredQuantity: 12, unit: 'kg', maximumBudget: 1500 }),
  categories: vi.fn().mockResolvedValue([{ id: 'category-1', name: 'Steel' }]),
} }));
vi.mock('../features/materials/managerMaterialsApi', () => ({ managerMaterialsApi: {
  getListing: vi.fn().mockResolvedValue({ id: 'listing-1', title: 'Steel offcuts' }),
} }));

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

it('shows the safe agent trace, with technical details collapsed and no global analytics', async () => {
  const client = api();
  vi.mocked(client.get).mockResolvedValue({ ...workflow,
    outputJson: JSON.stringify({ recommendation: { listingId: 'listing-1', distanceKm: 4, transportCost: 0 } }),
    validationJson: JSON.stringify({ valid: false, warnings: ['ROUTE_ESTIMATE_ONLY'], violations: ['BUDGET_EXCEEDED'] }),
  });
  renderDetails(client);
  expect(await screen.findByText('No current valid recommendation is available.')).toBeInTheDocument();
  expect(screen.queryByRole('link', { name: 'Steel offcuts' })).not.toBeInTheDocument();
  expect(await screen.findByRole('link', { name: 'Steel request' })).toHaveAttribute('href', '/app/manager/requirements/request-1');
  expect(screen.getByText('12 kg')).toBeInTheDocument();
  expect(screen.getByText('LKR 1,500.00')).toBeInTheDocument();
  expect(screen.queryByText('4 km')).not.toBeInTheDocument();
  expect(screen.getByText('Budget Exceeded')).toBeInTheDocument();
  expect(screen.getByText('Route Estimate Only')).toBeInTheDocument();
  const technical = screen.getByText('Technical details').closest('details')!;
  expect(technical).not.toHaveAttribute('open');
  expect(screen.getByRole('heading', { name: 'Agent Trace' })).toBeVisible();
  expect(screen.getByText(/buyer notes, addresses, coordinates/i)).toBeVisible();
  await userEvent.click(screen.getByText('Technical details'));
  expect(within(technical).getByText(/Sensitive raw inputs/i)).toBeVisible();
  expect(client.transactionAnalytics).not.toHaveBeenCalled();
  expect(screen.queryByText('Transaction analytics')).not.toBeInTheDocument();
});

it('renders persisted stage summaries without exposing notes or coordinates', async () => {
  const client = api();
  const stages = [
    { stage: 'PLANNER', outputJson: JSON.stringify({ status: 'ok', normalizedCriteria: { itemName: 'Concrete blocks', constructionItemTemplateId: '12345678-1234-1234-1234-123456789012', category: 'Masonry', requiredQuantity: 10, unit: 'bag', normalizedRequiredQuantity: 500, normalizedBaseUnit: 'kg', inputMode: 'PACKAGE_COUNT', preferredPackageSize: 50, packageBaseUnit: 'kg', maximumBudget: 2000, deadline: '2030-02-01T00:00:00Z', notes: 'private buyer note', targetLatitude: 6.9, targetLongitude: 79.8 } }) },
    { stage: 'MATCHING', outputJson: JSON.stringify({ status: 'ok', candidates: [{ listingId: 'listing-123456', condition: 'NEW', availableQuantity: 20, baseUnit: 'kg', unitPrice: 120, basicFitScore: 92, reason: 'Catalog match', sellerPhone: 'hidden' }], exclusions: [{ listingId: 'listing-999999', code: 'ITEM_MISMATCH' }] }) },
    { stage: 'LOGISTICS', outputJson: JSON.stringify({ candidates: [{ listingId: 'listing-123456', deliveryFeasible: true, distanceKm: 4, durationMinutes: 15, estimatedTransportCost: 200, reason: 'DELIVERY_WITHIN_DEADLINE' }] }) },
    { stage: 'VALIDATION', outputJson: JSON.stringify({ valid: true, requiresApproval: true, recommendedMatchId: 'match-123456', warnings: ['TRANSACTION_THRESHOLD_REQUIRES_REVIEW'], violations: [], scoreBreakdown: { score: 88, totalEstimatedCost: 1400 } }) },
  ].map((entry, index) => ({ ...workflow.steps[0], id: `step-${index}`, sequence: index + 1, ...entry, toolCalls: index === 3 ? Array.from({ length: 6 }, (_, tool) => ({ ...workflow.steps[0].toolCalls[0], id: `tool-${tool}`, toolName: `check_tool_${tool}`, outputJson: '{"passed":true,"code":"CHECK_PASSED"}' })) : [] }));
  vi.mocked(client.get).mockResolvedValue({ ...workflow, steps: stages, traceSourceWorkflowId: 'trace-source-123456' });
  renderDetails(client);
  expect(await screen.findByText('Requirement Planner')).toBeVisible();
  expect(screen.getByText('Material Matching')).toBeVisible();
  expect(screen.getByText('Logistics')).toBeVisible();
  expect(screen.getByText('Validation and Recommendation')).toBeVisible();
  expect(screen.getByText('Concrete blocks')).toBeVisible();
  expect(screen.getByText('Planning result')).toBeVisible();
  expect(screen.getByText('Ready for matching')).toBeVisible();
  expect(screen.getByText(/Buyer requires 10 bag Concrete blocks/i)).toBeVisible();
  expect(screen.getByText('Handoff to Material Matching')).toBeVisible();
  expect(screen.getByText('Planner checks')).toBeVisible();
  expect(screen.getAllByText('Listing listing-')).toHaveLength(2);
  expect(screen.getByText('Validation checks')).toBeVisible();
  expect(screen.getByText(/Showing the persisted matching trace/i)).toBeVisible();
  expect(screen.queryByText('private buyer note')).not.toBeInTheDocument();
  expect(screen.queryByText('6.9')).not.toBeInTheDocument();
  expect(screen.queryByText('hidden')).not.toBeInTheDocument();
  expect(screen.queryByText('12345678-1234-1234-1234-123456789012')).not.toBeInTheDocument();
});

it('uses Not specified for an absent persisted Planner category', async () => {
  const client = api();
  const planner = { ...workflow.steps[0], stage: 'PLANNER', outputJson: JSON.stringify({ status: 'ok', normalizedCriteria: { itemName: 'Door', constructionItemTemplateId: 'template-123456', categoryId: 'category-123', requiredQuantity: 1, unit: 'piece', maximumBudget: 30000, deadline: '2030-10-12T16:48:00Z' } }), toolCalls: [] };
  vi.mocked(client.get).mockResolvedValue({ ...workflow, steps: [planner] });
  renderDetails(client);
  expect(await screen.findByText('Ready for matching')).toBeVisible();
  expect(screen.getByText('Not specified')).toBeVisible();
  expect(screen.queryByText('template-123456')).not.toBeInTheDocument();
});

it('shows Planning unavailable when the persisted Planner output has no normalized criteria', async () => {
  const client = api();
  const planner = { ...workflow.steps[0], stage: 'PLANNER', outputJson: '{}', toolCalls: [] };
  vi.mocked(client.get).mockResolvedValue({ ...workflow, steps: [planner] });
  renderDetails(client);
  expect(await screen.findByText('Planning unavailable')).toBeVisible();
  expect(screen.getByText('No normalized requirement data was persisted for this Planner stage.')).toBeVisible();
  expect(screen.queryByText('Handoff to Material Matching')).not.toBeInTheDocument();
});

it('shows the legacy notice without fabricating stage cards when no trace was persisted', async () => {
  const client = api();
  vi.mocked(client.get).mockResolvedValue({ ...workflow, status: 'APPROVED', completedAtUtc: null, steps: [] });
  renderDetails(client);
  expect(await screen.findByText('This workflow was completed before agent-trace persistence was enabled. Run a new matching workflow to view its agent trace.')).toBeVisible();
  expect(screen.getAllByText('Not recorded')).toHaveLength(2);
  expect(screen.queryByText('Requirement Planner')).not.toBeInTheDocument();
  expect(screen.queryByText('NOT RECORDED')).not.toBeInTheDocument();
});

it.each(['COMPLETED', 'REJECTED', 'APPROVED', 'FAILED', 'REVISION_REQUESTED'] as const)('hides the decision section for %s workflows', async status => {
  const client = api();
  vi.mocked(client.get).mockResolvedValue({ ...workflow, status, outputJson: 'invalid JSON', validationJson: '{}' });
  renderDetails(client);
  await screen.findByRole('heading', { name: 'Review summary' });
  expect(screen.queryByRole('heading', { name: 'Manager decision' })).not.toBeInTheDocument();
  expect(screen.queryByRole('button', { name: 'Approve' })).not.toBeInTheDocument();
  expect(await screen.findByText('No current valid recommendation is available.')).toBeInTheDocument();
  expect(screen.queryByText('Current valid match')).not.toBeInTheDocument();
});
