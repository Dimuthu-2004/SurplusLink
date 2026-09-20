import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';
import { ManagerMatchComparisonPage } from '../pages/manager/ManagerMatchComparisonPage';
import { MatchAnalyticsWidget } from '../features/matches/MatchAnalyticsWidget';
import type { ManagerMatchesApi, MatchComparisonPage, MatchCandidate, MatchSummary } from '../features/matches/managerMatchesApi';

const requirementId = 'req-1';
const requirement = {
  id: requirementId,
  buyerId: 'buyer-1',
  categoryId: 'category-1',
  requiredQuantity: 50,
  unit: 'kg',
  maximumBudget: 1200,
  deadline: '2030-01-15T12:00:00Z',
  latitude: 6.9,
  longitude: 79.9,
  status: 'MATCHING',
  notes: 'Need cement near the depot',
  createdAt: '2026-09-01T00:00:00Z',
  updatedAt: '2026-09-03T00:00:00Z',
};

const candidates: MatchCandidate[] = [
  {
    id: 'match-1',
    requirementId,
    materialListingId: 'listing-1',
    sellerId: 'seller-1',
    materialTitle: 'Cement Blend 42.5',
    categoryName: 'Cement',
    status: 'VALID',
    score: 92,
    routeDistanceKm: 14.5,
    estimatedCost: 960,
    rejectionReason: null,
    createdAt: '2026-09-03T00:00:00Z',
    updatedAt: '2026-09-03T00:00:00Z',
  },
  {
    id: 'match-2',
    requirementId,
    materialListingId: 'listing-2',
    sellerId: 'seller-2',
    materialTitle: 'Premix Aggregate',
    categoryName: 'Aggregate',
    status: 'REJECTED',
    score: 67,
    routeDistanceKm: 27.2,
    estimatedCost: 1100,
    rejectionReason: 'SELF_MATCH_NOT_ALLOWED',
    createdAt: '2026-09-03T00:00:00Z',
    updatedAt: '2026-09-03T00:00:00Z',
  },
];

function makeApi(overrides?: Partial<ManagerMatchesApi>): ManagerMatchesApi {
  return {
    list: vi.fn().mockResolvedValue({
      items: candidates,
      total: 2,
      page: 1,
      pageSize: 20,
    } as MatchComparisonPage),
    requirement: vi.fn().mockResolvedValue(requirement),
    summary: vi.fn().mockResolvedValue({
      total: 2,
      validCount: 1,
      rejectedCount: 1,
      averageScore: 79.5,
      averageRouteKm: 20.85,
      averageCost: 1030,
      topCandidate: candidates[0],
    } as MatchSummary),
    ...overrides,
  };
}

describe('manager match comparison page', () => {
  it('renders requirement context, filters rejected entries, and keeps the page read-only', async () => {
    const api = makeApi();
    render(
      <MemoryRouter initialEntries={['/app/manager/requirements/req-1/matches']}>
        <Routes>
          <Route path="/app/manager/requirements/:requirementId/matches" element={<ManagerMatchComparisonPage api={api} />} />
        </Routes>
      </MemoryRouter>,
    );

    expect(await screen.findByRole('heading', { name: /match comparison/i })).toBeInTheDocument();
    expect(screen.getByText(/Need cement near the depot/i)).toBeInTheDocument();
    expect(screen.getByText(/50 kg/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /all/i })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /approve/i })).not.toBeInTheDocument();

    await userEvent.click(screen.getByRole('button', { name: /rejected/i }));
    await waitFor(() => expect(api.list).toHaveBeenLastCalledWith(expect.any(String), expect.objectContaining({ status: 'REJECTED' })));

    expect(screen.getByText('SELF_MATCH_NOT_ALLOWED')).toBeInTheDocument();
    expect(screen.getAllByRole('link', { name: /view material/i })).toHaveLength(2);
    expect(screen.getAllByRole('link', { name: /view requirement/i })).toHaveLength(3);
  });

  it('shows analytics summary and handles loading/error states', async () => {
    const api = makeApi({
      summary: vi.fn().mockRejectedValueOnce(new Error('Match service unavailable')).mockResolvedValue({
        total: 2,
        validCount: 1,
        rejectedCount: 1,
        averageScore: 79.5,
        averageRouteKm: 20.85,
        averageCost: 1030,
        topCandidate: candidates[0],
      } as MatchSummary),
    });

    render(
      <MemoryRouter>
        <MatchAnalyticsWidget api={api} requirementId={requirementId} />
      </MemoryRouter>,
    );

    expect(await screen.findByText('Match service unavailable')).toBeInTheDocument();
    expect(screen.getByText(/Match analytics/i)).toBeInTheDocument();

    await userEvent.click(screen.getByRole('button', { name: /retry/i }));
    expect(await screen.findByText('79.5')).toBeInTheDocument();
  });
});
