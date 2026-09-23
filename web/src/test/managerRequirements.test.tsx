import { act, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { AxiosError } from 'axios';
import { apiClient } from '../api/apiClient';
import { App } from '../app/App';
import { AuthProvider } from '../auth/AuthContext';
import { ManagerRequirementsPage } from '../pages/manager/ManagerRequirementsPage';
import { ManagerRequirementDetailsPage } from '../pages/manager/ManagerRequirementDetailsPage';
import { ManagerRequirementHistoryPage } from '../pages/manager/ManagerRequirementHistoryPage';
import { RequestAnalyticsWidget } from '../features/requirements/RequestAnalyticsWidget';
import { createManagerRequirementsApi, managerRequirementsApi, type BuyerRequirement, type ManagerRequirementsApi, type RequirementAnalytics, type RequirementPage } from '../features/requirements/managerRequirementsApi';

const row: BuyerRequirement = {
  id: '11111111-1111-1111-1111-111111111111', buyerId: 'buyer-1', categoryId: 'category-1',
  requiredQuantity: 12, unit: 'kg', maximumBudget: 100, deadline: '2030-01-02T12:00:00Z',
  status: 'OPEN', notes: 'Deliver to loading bay', latitude: 6.9, longitude: 79.8,
  createdAt: '2026-09-01T00:00:00Z', updatedAt: '2026-09-02T00:00:00Z',
};
const summary: RequirementAnalytics = {
  total: 21, openCount: 7, asOf: '2026-09-01T00:00:00Z', upcomingUntil: '2026-09-08T00:00:00Z',
  upcomingDeadlineCount: 3, upcomingDeadlines: [row], countsByStatus: [{ status: 'OPEN', count: 7 }],
  countsByCategory: [{ categoryId: 'category-1', categoryName: 'Cement', count: 21 }],
  averageMaximumBudget: 100, averageQuantityByUnit: [{ unit: 'kg', averageRequiredQuantity: 12, count: 21 }],
};
const category = { id: 'category-1', name: 'Cement', createdAtUtc: row.createdAt, updatedAtUtc: row.updatedAt };
const page = (items: BuyerRequirement[] = [row], current = 1, total = 21): RequirementPage<BuyerRequirement> =>
  ({ items, total, page: current, pageSize: 20 });
function fakeApi() {
  return {
    list: vi.fn<ManagerRequirementsApi['list']>().mockResolvedValue(page()),
    get: vi.fn<ManagerRequirementsApi['get']>().mockResolvedValue(row),
    categories: vi.fn<ManagerRequirementsApi['categories']>().mockResolvedValue([category]),
    summary: vi.fn<ManagerRequirementsApi['summary']>().mockResolvedValue(summary),
    history: vi.fn<ManagerRequirementsApi['history']>().mockResolvedValue({
      items: [{ id: 'event-1', action: 'STATUS_CHANGED', fromStatus: 'DRAFT', toStatus: 'OPEN', actorUserId: row.buyerId, createdAt: row.updatedAt }],
      total: 21, page: 1, pageSize: 20,
    }),
  };
}
const adapter = apiClient.defaults.adapter;
afterEach(() => { apiClient.defaults.adapter = adapter; vi.restoreAllMocks(); });

describe('manager requirement API', () => {
  it('uses shared bearer authentication and exact GET routes and query contracts', async () => {
    window.sessionStorage.setItem('surpluslink.jwt', 'manager-jwt');
    const calls: string[] = [];
    apiClient.defaults.adapter = async (config) => {
      expect(config.headers.get('Authorization')).toBe('Bearer manager-jwt');
      expect(config.method).toBe('get');
      calls.push(config.url!);
      if (config.url === '/api/requirements') {
        expect(config.params).toEqual({ search: 'cement', categoryId: 'category-1', status: 'OPEN',
          deadlineFrom: '2030-01-01T00:00:00Z', deadlineTo: '2030-02-01T00:00:00Z',
          sort: 'budget', sortDir: 'asc', page: 2, pageSize: 20 });
      }
      if (config.url?.endsWith('/history')) expect(config.params).toEqual({ page: 2, pageSize: 20 });
      if (config.url?.endsWith('/summary')) expect(config.params).toEqual({ upcomingDays: 30 });
      return { data: config.url === '/api/requirements' ? page() : row, config, status: 200, statusText: 'OK', headers: {} };
    };
    const api = createManagerRequirementsApi();
    expect((await api.list({ search: 'cement', categoryId: 'category-1', status: 'OPEN',
      deadlineFrom: '2030-01-01T00:00:00Z', deadlineTo: '2030-02-01T00:00:00Z',
      sort: 'budget', sortDir: 'asc', page: 2, pageSize: 20 })).total).toBe(21);
    await api.get(row.id); await api.history(row.id, 2); await api.summary(30); await api.categories();
    expect(calls).toEqual(['/api/requirements', '/api/requirements/' + row.id,
      '/api/requirements/' + row.id + '/history', '/api/requirements/analytics/summary', '/api/material-categories']);
  });

  it('preserves ASP.NET ProblemDetails detail for access and request errors', async () => {
    apiClient.defaults.adapter = async (config) => {
      throw new AxiosError('Forbidden', 'ERR_BAD_REQUEST', config, undefined, {
        config, status: 403, statusText: 'Forbidden', headers: {},
        data: { title: 'Forbidden', detail: 'Only managers may monitor requirements.' },
      });
    };
    await expect(createManagerRequirementsApi().get(row.id)).rejects.toMatchObject({
      status: 403, message: 'Only managers may monitor requirements.',
    });
  });
});

describe('manager requirement components', () => {
  it('applies all filters, sorts, paginates with total and resets to page one', async () => {
    const api = fakeApi();
    api.list.mockImplementation(async (query) => page([row], query.page));
    render(<MemoryRouter><ManagerRequirementsPage api={api} /></MemoryRouter>);
    await screen.findByRole('link', { name: 'View requirement ' + row.id });
    const visitor = userEvent.setup();
    await visitor.type(screen.getByLabelText('Search category or notes'), ' loading ');
    await visitor.selectOptions(screen.getByLabelText('Category'), 'category-1');
    await visitor.selectOptions(screen.getByLabelText('Status'), 'OPEN');
    await visitor.selectOptions(screen.getByLabelText('Sort by'), 'budget');
    await visitor.selectOptions(screen.getByLabelText('Direction'), 'asc');
    fireEvent.change(screen.getByLabelText('Deadline from'), { target: { value: '2030-01-01' } });
    fireEvent.change(screen.getByLabelText('Deadline to'), { target: { value: '2030-01-31' } });
    await visitor.click(screen.getByRole('button', { name: 'Apply filters' }));
    await waitFor(() => expect(api.list).toHaveBeenLastCalledWith(expect.objectContaining({
      search: 'loading', categoryId: 'category-1', status: 'OPEN', sort: 'budget', sortDir: 'asc', page: 1,
      deadlineFrom: new Date(2030, 0, 1).toISOString(), deadlineTo: new Date(2030, 0, 31, 23, 59, 59, 999).toISOString(),
    })));
    expect(await screen.findByText('21 matching requests')).toBeInTheDocument();
    await visitor.click(screen.getByRole('button', { name: 'Next' }));
    await screen.findByText('Page 2 of 2');
    expect(api.list).toHaveBeenLastCalledWith(expect.objectContaining({ page: 2, sort: 'budget' }));
    await visitor.click(screen.getByRole('button', { name: 'Reset filters' }));
    await waitFor(() => expect(api.list).toHaveBeenLastCalledWith(expect.objectContaining({ page: 1, search: '', sort: 'createdAt' })));
  });

  it('validates deadline range and keeps table error recovery independent from analytics', async () => {
    const api = fakeApi();
    api.list.mockRejectedValueOnce(new Error('Network unavailable')).mockResolvedValue(page([], 1, 0));
    render(<MemoryRouter><ManagerRequirementsPage api={api} /></MemoryRouter>);
    expect(await screen.findByText('Network unavailable')).toBeInTheDocument();
    expect(screen.getByText('Total requests')).toBeInTheDocument();
    expect(screen.queryByText('Average maximum budget')).not.toBeInTheDocument();
    const visitor = userEvent.setup();
    await visitor.click(screen.getByRole('button', { name: 'Retry' }));
    expect(await screen.findByText('No requirements match these filters.')).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText('Deadline from'), { target: { value: '2030-02-01' } });
    fireEvent.change(screen.getByLabelText('Deadline to'), { target: { value: '2030-01-01' } });
    await visitor.click(screen.getByRole('button', { name: 'Apply filters' }));
    expect(screen.getByRole('alert')).toHaveTextContent('Deadline from must be on or before deadline to.');
    expect(api.list).toHaveBeenCalledTimes(2);
  });

  it('ignores old responses when a newer search has finished', async () => {
    const api = fakeApi();
    let finishOld!: (value: RequirementPage<BuyerRequirement>) => void;
    api.list.mockReturnValueOnce(new Promise((resolve) => { finishOld = resolve; }))
      .mockResolvedValue(page([{ ...row, id: 'new-request' }]));
    render(<MemoryRouter><ManagerRequirementsPage api={api} /></MemoryRouter>);
    expect(screen.getByText('Loading requirements…')).toBeInTheDocument();
    const visitor = userEvent.setup();
    await visitor.type(screen.getByLabelText('Search category or notes'), 'new');
    await visitor.click(screen.getByRole('button', { name: 'Apply filters' }));
    await screen.findByRole('link', { name: 'View requirement new-request' });
    await act(async () => { finishOld(page()); });
    expect(screen.queryByRole('link', { name: 'View requirement ' + row.id })).not.toBeInTheDocument();
  });

  it('renders read-only details, workflow status and paginated status history', async () => {
    const api = fakeApi();
    api.history.mockImplementation(async (_, current) => ({
      items: [{ id: 'a1', action: 'STATUS_CHANGED', fromStatus: 'DRAFT', toStatus: 'OPEN', actorUserId: null, createdAt: row.updatedAt }],
      total: 21, page: current, pageSize: 20,
    }));
    render(<MemoryRouter initialEntries={['/app/manager/requirements/' + row.id]}><Routes>
      <Route path="/app/manager/requirements/:requirementId" element={<ManagerRequirementDetailsPage api={api} />} />
      <Route path="/app/manager/requirements/:requirementId/workflow" element={<ManagerRequirementDetailsPage api={api} workflowOnly />} />
      <Route path="/app/manager/requirements/:requirementId/history" element={<ManagerRequirementHistoryPage api={api} />} />
    </Routes></MemoryRouter>);
    await screen.findByText('Deliver to loading bay');
    expect(screen.queryByRole('button', { name: /submit|cancel|start matching/i })).not.toBeInTheDocument();
    const visitor = userEvent.setup();
    await visitor.click(screen.getByRole('link', { name: 'View workflow status' }));
    expect(await screen.findByText('No workflow has been started for this requirement.')).toBeInTheDocument();
    expect(screen.getByText('This requirement is open. Matching has not started.')).toBeInTheDocument();
    await visitor.click(screen.getByRole('link', { name: 'View history' }));
    expect(await screen.findByText('Draft → Open')).toBeInTheDocument();
    expect(screen.getByText('System action')).toBeInTheDocument();
    await visitor.click(screen.getByRole('button', { name: 'Next' }));
    await screen.findByText('Page 2 of 2');
    expect(api.history).toHaveBeenLastCalledWith(row.id, 2);
  });

  it('analytics shows empty averages and updates its deadline window', async () => {
    const api = fakeApi();
    api.summary.mockResolvedValue({ ...summary, total: 0, openCount: 0, upcomingDeadlineCount: 0,
      averageMaximumBudget: null, upcomingDeadlines: [], countsByCategory: [], averageQuantityByUnit: [] });
    render(<MemoryRouter><RequestAnalyticsWidget api={api} /></MemoryRouter>);
    expect(await screen.findByText('No buyer requirements yet.')).toBeInTheDocument();
    const visitor = userEvent.setup();
    await visitor.selectOptions(screen.getByLabelText('Upcoming deadlines'), '30');
    await waitFor(() => expect(api.summary).toHaveBeenLastCalledWith(30));
    await visitor.click(screen.getByText('View status, category and deadline breakdown'));
    expect(screen.getByText('No active requests are due in this window.')).toBeInTheDocument();
  });
});

describe('protected manager requirement routes', () => {
  it.each(['BUYER', 'SELLER', null] as const)('blocks %s on list, details, history and workflow', async (role) => {
    const spy = vi.spyOn(managerRequirementsApi, 'list');
    const detail = vi.spyOn(managerRequirementsApi, 'get');
    const history = vi.spyOn(managerRequirementsApi, 'history');
    for (const suffix of ['', '/r1', '/r1/history', '/r1/workflow']) {
      const rendered = render(<MemoryRouter initialEntries={['/app/manager/requirements' + suffix]}>
        <AuthProvider client={{ get: vi.fn().mockResolvedValue({ data: { id: 'user-1', email: 'user@test', roles: [role] } }), post: vi.fn() }}
          storage={{ read: () => role ? 'token' : null, write: vi.fn(), clear: vi.fn() }}>
          <App />
        </AuthProvider></MemoryRouter>);
      if (role) await screen.findByRole('heading', { name: role === 'BUYER' ? 'Buyer home' : 'Seller home' });
      else await screen.findByRole('heading', { name: 'Welcome back' });
      expect(within(rendered.container).queryByRole('link', { name: 'Buyer Requirements' })).not.toBeInTheDocument();
      rendered.unmount();
    }
    expect(spy).not.toHaveBeenCalled(); expect(detail).not.toHaveBeenCalled(); expect(history).not.toHaveBeenCalled();
  });
});
