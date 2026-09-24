import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { afterEach, expect, it, vi } from 'vitest';
import { apiClient, ApiError } from '../api/apiClient';
import { App } from '../app/App';
import { AuthProvider } from '../auth/AuthContext';
import { ManagerMatchComparisonPage } from '../pages/manager/ManagerMatchComparisonPage';
import { createManagerMatchesApi, type ManagerMatchesApi } from '../features/matches/managerMatchesApi';
const context = { id: 'r1', buyerId: 'b1', categoryId: 'c1', requiredQuantity: 400, unit: 'pcs', maximumBudget: 400000,
  deadline: '2030-01-01', status: 'OPEN', notes: 'Tiles requirement', latitude: 6.9, longitude: 79.8, createdAt: '2026-09-22', updatedAt: '2026-09-22' };
const raw = { id: 'm1', requirementId: 'r1', listingId: 'l1', sellerId: 's1', score: .8, status: 'ROUTE_FAILED', valid: false, rejected: false,
  distance: null, durationMinutes: null, estimatedTransportCost: null, materialTitle: 'Tiles', categoryName: 'Floor tiles',
  quantity: 400, availableQuantity: 500, unit: 'pcs', unitPrice: 800, rejectionReason: null, createdAt: '2026-09-22' };
const original = apiClient.defaults.adapter;
afterEach(() => { apiClient.defaults.adapter = original; });
it('real API adapter keeps failed routes visible with metadata, auth, sort, filters and pagination', async () => {
  window.sessionStorage.setItem('surpluslink.jwt', 'manager-token');
  const calls: Record<string, unknown>[] = [];
  apiClient.defaults.adapter = async config => {
    expect(config.headers.get('Authorization')).toBe('Bearer manager-token');
    expect(config.method).toBe('get');
    let data: unknown;
    if (config.url === '/api/auth/me') data = { id: 'manager', email: 'manager@test.local', roles: ['MANAGER'] };
    else if (config.url === '/api/requirements/r1') data = context;
    else if (config.url === '/api/matches/analytics/summary') data = { total: 21, validCount: 21, rejectedCount: 0, averageScore: .8, averageDistance: null, averageCost: null };
    else if (config.url === '/api/matches/requirement/r1') {
      calls.push(config.params);
      data = { items: [raw], total: 21, totalPages: 2, page: config.params.page, pageSize: config.params.pageSize };
    } else throw new Error('Unexpected endpoint: ' + config.url);
    return { data, config, status: 200, statusText: 'OK', headers: {} };
  };
  render(<MemoryRouter initialEntries={['/app/manager/requirements/r1/matches']}><AuthProvider><App /></AuthProvider></MemoryRouter>);
  const table = await screen.findByRole('region', { name: 'Match comparison table' });
  for (const text of ['Tiles', 'Floor tiles', '400 pcs', '500 pcs', 'LKR 800.00', '80%', 'Route unavailable / failed', 'Route Failed', 'Not rejected'])
    expect(within(table).getByText(text)).toBeInTheDocument();
  expect(within(table).getByRole('link', { name: 'View material' })).toHaveAttribute('href', '/app/manager/materials/l1');
  await userEvent.click(screen.getByRole('button', { name: 'Next' }));
  await waitFor(() => expect(calls).toContainEqual(expect.objectContaining({ page: 2, pageSize: 20 })));
  await userEvent.selectOptions(screen.getByLabelText('Sort by'), 'cost');
  await waitFor(() => expect(calls.at(-1)).toMatchObject({ page: 1, sortBy: 'estimatedTransportCost' }));
  await userEvent.selectOptions(screen.getByLabelText('Match status'), 'ROUTE_FAILED');
  await waitFor(() => expect(calls.at(-1)).toMatchObject({ status: 'ROUTE_FAILED' }));
  await userEvent.click(screen.getByRole('button', { name: 'VALID' }));
  await waitFor(() => expect(calls.at(-1)).toMatchObject({ valid: true, status: 'ROUTE_FAILED' }));
  await userEvent.click(screen.getByRole('button', { name: 'REJECTED' }));
  await waitFor(() => expect(calls.at(-1)).toMatchObject({ rejected: true }));
});
function fake(): ManagerMatchesApi {
  return { list: vi.fn().mockResolvedValue({ items: [], total: 0, page: 1, pageSize: 20 }),
    requirement: vi.fn().mockResolvedValue(context),
    summary: vi.fn().mockResolvedValue({ total: 0, validCount: 0, rejectedCount: 0, averageScore: null, averageCost: null, averageRouteKm: null, topCandidate: null }) };
}
function show(api: ManagerMatchesApi) {
  return render(<MemoryRouter initialEntries={['/requirements/r1/matches']}><Routes>
    <Route path="/requirements/:requirementId/matches" element={<ManagerMatchComparisonPage api={api} />} />
  </Routes></MemoryRouter>);
}
it('empty and missing requirement states provide navigation instead of a blank table', async () => {
  const api = fake(); const view = show(api);
  expect(await screen.findByText('No match candidates have been generated for this requirement yet.')).toBeInTheDocument();
  view.unmount(); vi.mocked(api.list).mockClear();
  render(<MemoryRouter><ManagerMatchComparisonPage api={api} /></MemoryRouter>);
  expect(screen.getByRole('link', { name: 'Choose requirement' })).toHaveAttribute('href', '/app/manager/requirements');
  expect(api.list).not.toHaveBeenCalled();
});
for (const [error, message] of [
  [new ApiError('Forbidden', 403), 'You do not have permission to view these requirements.'],
  [new ApiError('Not found', 404), 'This requirement could not be found.'],
  [new ApiError('Unable to connect to the SurplusLink API.'), 'Unable to connect to the SurplusLink API.'],
] as const) {
  it(`candidate table handles ${error.status ?? 'network'} and retry`, async () => {
    const api = fake(); vi.mocked(api.list).mockRejectedValueOnce(error); show(api);
    expect(screen.getByText('Loading candidate matches…')).toBeInTheDocument();
    expect(await screen.findByRole('alert')).toHaveTextContent(message);
    await userEvent.click(screen.getByRole('button', { name: 'Retry' }));
    expect(await screen.findByText('No match candidates have been generated for this requirement yet.')).toBeInTheDocument();
  });
}
it('adapter preserves duration and missing optional metadata safely', async () => {
  const get = vi.fn().mockResolvedValue({ data: { items: [{ ...raw, status: 'ROUTED', distance: 12.5, durationMinutes: 30, estimatedTransportCost: 785, materialTitle: null, categoryName: null }], total: 1, page: 1, pageSize: 20 } });
  const result = await createManagerMatchesApi({ get }).list('r1', { sort: 'distance', sortDir: 'asc', page: 1, pageSize: 20 });
  expect(result.items[0]).toMatchObject({ status: 'ROUTED', materialTitle: 'Material candidate', durationMinutes: 30, routeDistanceKm: 12.5, estimatedCost: 785 });
});
