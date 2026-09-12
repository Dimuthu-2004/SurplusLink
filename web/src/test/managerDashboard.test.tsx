import { render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ManagerDashboardPage } from '../features/dashboard/ManagerDashboardPage';
import {
  getManagerDashboard,
  type ManagerDashboardData,
} from '../features/dashboard/managerDashboardApi';

vi.mock('../features/dashboard/managerDashboardApi', async (importOriginal) => {
  const original = await importOriginal<typeof import('../features/dashboard/managerDashboardApi')>();
  return { ...original, getManagerDashboard: vi.fn() };
});

const dashboardData: ManagerDashboardData = {
  listings: {
    activeListings: 12,
    categoryTotals: [
      { categoryId: 'category-1', categoryName: 'Tiles', listingCount: 7, totalQuantity: 850 },
    ],
  },
  requirements: {
    openRequirements: 4,
    pendingRequirements: 2,
    upcomingDeadlines: [
      {
        id: 'requirement-1',
        title: 'Floor tiles',
        categoryName: 'Tiles',
        deadlineUtc: '2026-09-20T00:00:00Z',
        status: 'PENDING_APPROVAL',
      },
    ],
  },
  matches: {
    averageScore: 0.875,
    averageDistanceKm: 12.4,
    topRejectionReasons: [{ reason: 'Insufficient quantity', count: 3 }],
  },
  transactions: {
    pendingApprovals: 2,
    approvedTransactions: 6,
    rejectedTransactions: 1,
    completedTransactions: 5,
  },
};

describe('ManagerDashboardPage', () => {
  beforeEach(() => vi.mocked(getManagerDashboard).mockReset());

  it('loads and renders analytics returned by every component endpoint', async () => {
    vi.mocked(getManagerDashboard).mockResolvedValue(dashboardData);

    render(<ManagerDashboardPage />);

    expect(screen.getByText('Loading manager analytics...')).toBeInTheDocument();
    expect(await screen.findByRole('heading', { name: 'Manager dashboard' })).toBeInTheDocument();
    expect(screen.getByText('12')).toBeInTheDocument();
    expect(screen.getAllByText('Tiles')).toHaveLength(2);
    expect(screen.getByText('87.5%')).toBeInTheDocument();
    expect(screen.getByText('12.4 km')).toBeInTheDocument();
    expect(screen.getByText('Insufficient quantity')).toBeInTheDocument();
    expect(screen.getByText('Floor tiles')).toBeInTheDocument();
  });

  it('renders explicit empty messages instead of placeholder data', async () => {
    vi.mocked(getManagerDashboard).mockResolvedValue({
      listings: { activeListings: 0, categoryTotals: [] },
      requirements: { openRequirements: 0, pendingRequirements: 0, upcomingDeadlines: [] },
      matches: { averageScore: null, averageDistanceKm: null, topRejectionReasons: [] },
      transactions: {
        pendingApprovals: 0,
        approvedTransactions: 0,
        rejectedTransactions: 0,
        completedTransactions: 0,
      },
    });

    render(<ManagerDashboardPage />);

    expect(await screen.findByText('No listing category totals are available.')).toBeInTheDocument();
    expect(screen.getByText('No upcoming requirement deadlines.')).toBeInTheDocument();
    expect(screen.getByText('No match rejection reasons have been recorded.')).toBeInTheDocument();
    expect(screen.getAllByText('No data')).toHaveLength(2);
  });
});
