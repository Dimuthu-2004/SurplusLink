import { act, render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { AxiosError } from 'axios';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, expect, it } from 'vitest';
import { apiClient } from '../api/apiClient';
import { App } from '../app/App';
import { AuthProvider } from '../auth/AuthContext';
import type { MatchAnalyticsSummary } from '../features/dashboard/managerDashboardApi';
import type { InventoryAnalytics } from '../features/materials/managerMaterialsApi';
import type { RequirementAnalytics } from '../features/requirements/managerRequirementsApi';
import type { TransactionAnalytics } from '../features/workflows/managerWorkflowsApi';

const inventory: InventoryAnalytics = {
  activeCount: 11, listingsByCategory: [{ key: 'Steel', count: 19 }],
  listingsByStatus: [{ key: 'ACTIVE', count: 15 }, { key: 'EXPIRED', count: 4 }],
  expiringListings: [], lowRemainingQuantityListings: [],
};
const requirements: RequirementAnalytics = {
  total: 29, openCount: 17, countsByStatus: [
    { status: 'OPEN', count: 17 }, { status: 'PENDING_APPROVAL', count: 7 }, { status: 'MATCHING', count: 5 },
  ],
  countsByCategory: [{ categoryId: 'category-1', categoryName: 'Steel', count: 29 }],
  asOf: '2026-09-21T00:00:00Z', upcomingUntil: '2026-09-28T00:00:00Z', upcomingDeadlineCount: 12,
  upcomingDeadlines: [{
    id: 'requirement-1', buyerId: 'buyer-1', categoryId: 'category-1', status: 'OPEN',
    deadline: '2026-09-25T12:00:00Z', requiredQuantity: 20, unit: 'kg', maximumBudget: 400,
    notes: '', latitude: null, longitude: null, createdAt: '2026-09-01T00:00:00Z', updatedAt: '2026-09-01T00:00:00Z',
  }],
  averageMaximumBudget: 400, averageQuantityByUnit: [],
};
const matches: MatchAnalyticsSummary = {
  validCount: 15, rejectedCount: 8, total: 23, averageScore: 0.81, averageDistance: 14.25,
  topRejectionReasons: [{ reason: 'BUDGET_EXCEEDED', count: 6 }],
  routeSuccessCount: 10, routeFailureCount: 2, routeSuccessRate: 10 / 12, routeFailureRate: 2 / 12,
};
const transactions: TransactionAnalytics = {
  pendingApprovalCount: 8, approvedCount: 13, rejectedCount: 3, completionCount: 5,
  reservedQuantity: 60, completedValue: 500, completionRate: 5 / 21,
};
const users = { totalUsers: 150, sellers: 60, buyers: 50, dualRoleUsers: 20, managers: 5 };

const originalAdapter = apiClient.defaults.adapter;
afterEach(() => { apiClient.defaults.adapter = originalAdapter; });

it('loads the manager dashboard from authenticated summaries, isolates errors, retries, and displays empty results', async () => {
  window.sessionStorage.setItem('surpluslink.jwt', 'manager-token');
  let release!: () => void;
  const gate = new Promise<void>((resolve) => { release = resolve; });
  let failMatches = true;
  let empty = false;
  const calls: string[] = [];

  apiClient.defaults.adapter = async (config) => {
    expect(config.headers.get('Authorization')).toBe('Bearer manager-token');
    expect(config.method).toBe('get');
    const respond = (data: unknown) => ({ data, config, status: 200, statusText: 'OK', headers: {} });
    if (config.url === '/api/auth/me') return respond({ id: 'manager-1', email: 'manager@example.com', roles: ['MANAGER'] });
    calls.push(config.url!);
    await gate;
    switch (config.url) {
      case '/api/workflows':
        expect(config.params.status).toBe('PENDING_APPROVAL');
        return respond({ total: 6, items: [], page: 1, pageSize: 1, totalPages: 6 });
      case '/api/materials/analytics/summary':
        return respond(empty ? { ...inventory, activeCount: 0, listingsByCategory: [], listingsByStatus: [] } : inventory);
      case '/api/requirements/analytics/summary':
        expect(config.params).toEqual({ upcomingDays: 7 });
        return respond(empty ? { ...requirements, total: 0, openCount: 0, countsByStatus: [], countsByCategory: [], upcomingDeadlineCount: 0, upcomingDeadlines: [] } : requirements);
      case '/api/matches/analytics/summary':
        if (failMatches) throw new AxiosError('Unavailable', 'ERR_BAD_RESPONSE', config, undefined, {
          config, status: 503, statusText: 'Unavailable', headers: {}, data: { detail: 'Match analytics is temporarily unavailable.' },
        });
        return respond(empty ? { ...matches, total: 0, averageScore: null, averageDistance: null, topRejectionReasons: [] } : matches);
      case '/api/transactions/analytics/summary':
        return respond(empty ? { ...transactions, pendingApprovalCount: 0, approvedCount: 0, rejectedCount: 0, completionCount: 0 } : transactions);
      case '/api/users/summary':
        return respond(empty ? { totalUsers: 0, sellers: 0, buyers: 0, dualRoleUsers: 0, managers: 0 } : users);
      default: throw new Error('Unexpected dashboard request: ' + config.url);
    }
  };

  render(<MemoryRouter initialEntries={['/app/manager']}><AuthProvider><App /></AuthProvider></MemoryRouter>);
  await screen.findByRole('heading', { name: 'Manager Dashboard' });
  expect(screen.getAllByRole('status')).toHaveLength(6);
  expect(screen.queryByText('Active listings')).not.toBeInTheDocument();
  expect(screen.getByRole('link', { name: 'Manager Dashboard' })).toHaveAttribute('aria-current', 'page');
  expect(screen.getByRole('link', { name: 'Material listings' })).toHaveAttribute('href', '/app/manager/materials');
  await waitFor(() => expect(calls).toHaveLength(6));
  await act(async () => { release(); await gate; });

  expect(await screen.findByRole('alert')).toHaveTextContent('Match analytics is temporarily unavailable.');
  expect(calls.slice().sort()).toEqual([
    '/api/workflows', '/api/materials/analytics/summary', '/api/requirements/analytics/summary',
    '/api/matches/analytics/summary', '/api/transactions/analytics/summary', '/api/users/summary'
  ].sort());
  expect(screen.queryByText('Average match score')).not.toBeInTheDocument();
  expect(screen.queryByText('Rejected transactions')).not.toBeInTheDocument();
  metric('Active listings', '11');
  expect(within(screen.getByRole('table', { name: 'Listing totals by category' })).getByRole('row', { name: 'Steel 19' })).toBeInTheDocument();
  metric('Expiring soon', '0');
  metric('Open requirements', '17');
  metric('Requirements awaiting approval', '7');
  metric('Upcoming requirement deadlines', '12');
  expect(screen.getByRole('link', { name: 'View buyer requirements' })).toHaveAttribute('href', '/app/manager/requirements');
  expect(screen.queryByRole('table', { name: 'Upcoming requirement deadlines' })).not.toBeInTheDocument();
  metric('Pending approvals', '8');
  metric('Approved transactions', '13');
  metric('Pending Match / Requirement Approvals', '6');
  metric('Pending Listing Approvals', '0');
  expect(screen.getByRole('link', { name: /Pending Listing Approvals/ })).toHaveAttribute('href', '/app/manager/materials?status=PENDING_VERIFICATION');
  expect(screen.getByRole('link', { name: /Pending Match \/ Requirement Approvals/ })).toHaveAttribute('href', '/app/manager/approvals');
  metric('Total Users', '150');
  metric('Sellers', '60');
  metric('Buyers', '50');
  metric('Dual-role Users', '20');
  metric('Managers', '5');
  expect(screen.queryByText('manager-1')).not.toBeInTheDocument();

  failMatches = false;
  const visitor = userEvent.setup();
  await visitor.click(screen.getByRole('button', { name: 'Retry matches' }));
  await screen.findByRole('table', { name: 'Top rejection reasons' });
  metric('Route failures', '2');
  metric('Valid matches', '15');
  metric('Rejected matches', '8');
  expect(screen.getByRole('row', { name: 'Budget Exceeded 6' })).toBeInTheDocument();
  expect(calls).toHaveLength(7);
  expect(calls[6]).toBe('/api/matches/analytics/summary');
  expect(screen.queryByRole('alert')).not.toBeInTheDocument();

  empty = true;
  for (const title of ['inventory', 'buyer requirements', 'matches', 'transactions', 'community']) {
    await visitor.click(screen.getByRole('button', { name: 'Refresh ' + title }));
  }
  expect(await screen.findByText('No transactions yet.')).toBeInTheDocument();
  expect(screen.getByText('No listings yet.')).toBeInTheDocument();
  expect(screen.getByText('No buyer requirements yet.')).toBeInTheDocument();
  expect(screen.getByText('No matches yet.')).toBeInTheDocument();
  expect(screen.getByText('No rejection reasons recorded.')).toBeInTheDocument();
  metric('Active listings', '0');
  metric('Requirements awaiting approval', '0');
  metric('Route failures', '2');
  metric('Total Users', '0');
  metric('Sellers', '0');
  expect(screen.queryByText('BUDGET_EXCEEDED')).not.toBeInTheDocument();
});

function metric(label: string, value: string) {
  const card = screen.getByText(label, { selector: '.analytics-metric > span' }).parentElement!;
  expect(within(card).getByLabelText(value, { selector: 'strong' })).toBeInTheDocument();
}
