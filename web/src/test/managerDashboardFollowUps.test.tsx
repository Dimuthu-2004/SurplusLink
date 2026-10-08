import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { expect, it, vi } from 'vitest';

// This is the production shape emitted before the follow-up DTO was given its
// string-enum converter: TransactionStatus.APPROVED is the C# enum value 1.
vi.mock('../api/apiClient', () => ({
  apiClient: {
    get: vi.fn((url: string) => Promise.resolve({
      data: url.includes('/follow-ups')
        ? [{ id: 'follow-up-1', reference: 'TX-00000001', materialTitle: 'Copper offcuts', status: 1, confirmationDeadline: null, daysRemaining: 0, sellerHandoverConfirmedAt: null, buyerReceivedConfirmedAt: null }]
        : { year: 2026, month: null, totalTransactions: 0, totalValue: 0, totalQuantity: 0, averageValue: 0, points: [] },
    })),
    post: vi.fn(),
  },
  normalizeApiError: (error: unknown) => error,
}));

vi.mock('../features/dashboard/managerDashboardApi', () => ({
  managerDashboardApi: {
    approvals: async () => ({ total: 0 }),
    transactions: async () => ({ pendingApprovalCount: 0, approvedCount: 0, rejectedCount: 0, completionCount: 0 }),
    inventory: async () => ({ activeCount: 0, listingsByStatus: [], expiringListings: [], listingsByCategory: [] }),
    requirements: async () => ({ openCount: 0, countsByStatus: [], upcomingDeadlineCount: 0, total: 0 }),
    matches: async () => ({ total: 0, validCount: 0, rejectedCount: 0, averageScore: null, averageDistance: null, topRejectionReasons: [], routeFailureCount: 0 }),
    usersSummary: async () => ({ totalUsers: 0, buyers: 0, sellers: 0, dualRoleUsers: 0, managers: 0 }),
  },
}));

import { ManagerDashboardPage } from '../pages/manager/ManagerDashboardPage';

it('renders manager follow-ups from the legacy numeric transaction status response', async () => {
  render(<MemoryRouter><ManagerDashboardPage /></MemoryRouter>);

  expect(await screen.findByTestId('manager-dashboard-page')).toBeVisible();
  expect(await screen.findByText('Approved')).toBeVisible();
  expect(screen.getByText('Copper offcuts')).toBeVisible();
});
