import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, expect, it, vi } from 'vitest';
import { WorkflowSummary } from '../pages/manager/WorkflowSummary';
import { apiClient } from '../api/apiClient';
import { managerRequirementsApi } from '../features/requirements/managerRequirementsApi';
import { managerMaterialsApi } from '../features/materials/managerMaterialsApi';
import type { Workflow } from '../features/workflows/managerWorkflowsApi';

vi.mock('../api/apiClient', () => ({ apiClient: { get: vi.fn() }, ApiError: class extends Error {} }));
vi.mock('../features/requirements/managerRequirementsApi', () => ({ managerRequirementsApi: { get: vi.fn(), categories: vi.fn().mockResolvedValue([]) } }));
vi.mock('../features/materials/managerMaterialsApi', () => ({ managerMaterialsApi: { getListing: vi.fn() } }));
const workflow = { id: 'workflow', materialRequestId: 'request', materialMatchId: 'match', status: 'PENDING_APPROVAL', inputJson: '{}',
  outputJson: '{"selectedMatchId":"match","buyerConfirmed":true}', validationJson: '{"valid":true,"recommendedMatchId":"match"}' } as Workflow;
const request = { id: 'request', workflowId: 'workflow', status: 'PENDING_APPROVAL', deadline: '2099-01-01', requiredQuantity: 2, maximumBudget: 100, unit: 'kg' };
const match = { id: 'match', requirementId: 'request', listingId: 'listing', valid: true, rejected: false, status: 'ROUTED', score: .85, distance: 4, durationMinutes: 10, estimatedTransportCost: 0 };
const listing = { id: 'listing', title: 'Steel offcuts', status: 'ACTIVE', availableUntil: '2099-01-01', unitPrice: 20, unit: 'kg', seller: { businessName: 'Circular Metals' } };
beforeEach(() => {
  vi.mocked(apiClient.get).mockResolvedValue({ data: match });
  vi.mocked(managerRequirementsApi.get).mockResolvedValue(request as never);
  vi.mocked(managerMaterialsApi.getListing).mockResolvedValue(listing as never);
});
function show(value = workflow) { render(<MemoryRouter><WorkflowSummary workflow={value} /></MemoryRouter>); }
it('resolves buyer-confirmed recommendation IDs against current APIs and preserves zero transport cost', async () => {
  show();
  expect(await screen.findByRole('link', { name: 'Steel offcuts' })).toHaveAttribute('href', '/app/manager/materials/listing');
  expect(apiClient.get).toHaveBeenCalledWith('/api/matches/match');
  for (const text of ['Circular Metals', '85%', '4 km', '10 min', '0', 'Highest deterministic final score among valid routed candidates; ties use condition, total estimated cost, distance, then listing ID.']) expect(screen.getByText(text)).toBeInTheDocument();
});
it.each([
  { valid: false }, { rejected: true }, { status: 'ROUTE_FAILED' }, { status: 'REJECTED' },
  { rejectionReason: 'INACTIVE_LISTING' }, { id: 'old-match' }, { requirementId: 'other-request' },
])('hides invalid current matches: %j', async patch => {
  vi.mocked(apiClient.get).mockResolvedValue({ data: { ...match, ...patch } }); show();
  expect(await screen.findByText('No current valid recommendation is available.')).toBeInTheDocument();
  expect(screen.queryByRole('link', { name: 'Steel offcuts' })).not.toBeInTheDocument();
});
it.each([{ status: 'INACTIVE' }, { availableUntil: '2020-01-01' }])('hides inactive or expired listings: %j', async patch => {
  vi.mocked(managerMaterialsApi.getListing).mockResolvedValue({ ...listing, ...patch } as never); show();
  expect(await screen.findByText('No current valid recommendation is available.')).toBeInTheDocument();
});
it('hides stale workflows after a newer workflow starts', async () => {
  vi.mocked(managerRequirementsApi.get).mockResolvedValue({ ...request, workflowId: 'new-workflow' } as never); show();
  expect(await screen.findByText('No current valid recommendation is available.')).toBeInTheDocument();
});
it('does not infer a recommendation from a listing or selected match without validated identity', async () => {
  show({ ...workflow, validationJson: '{}', outputJson: '{"recommendation":{"listingId":"listing"}}' });
  expect(await screen.findByText('No current valid recommendation is available.')).toBeInTheDocument();
});
it('shows a recoverable verification error without presenting a recommendation', async () => {
  vi.mocked(apiClient.get).mockRejectedValue(new Error('Offline')); show();
  expect(await screen.findByRole('alert')).toHaveTextContent('Recommendation could not be verified');
  expect(screen.getByRole('button', { name: 'Retry recommendation' })).toBeInTheDocument();
});
