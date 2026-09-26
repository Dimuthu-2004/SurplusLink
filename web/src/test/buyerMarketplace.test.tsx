import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { describe, expect, it, vi, beforeEach } from 'vitest';
import { BuyerMarketplacePage } from '../pages/buyer/BuyerMarketplacePage';
import { BuyerListingDetailsPage } from '../pages/buyer/BuyerListingDetailsPage';
import * as api from '../api/buyerMarketplaceApi';
import type { MaterialListingItem, MaterialCategoryItem } from '../api/buyerMarketplaceApi';

vi.mock('../auth/AuthContext', () => ({
  useAuth: () => mockAuthValue,
}));

vi.mock('../api/buyerMarketplaceApi', () => ({
  fetchMaterialCategories: vi.fn(),
  fetchMarketplaceListings: vi.fn(),
  fetchMarketplaceListingDetails: vi.fn(),
  createMobileHandoff: vi.fn(),
}));

const sampleCategories: MaterialCategoryItem[] = [
  {
    id: 'cat-1',
    name: 'Cement',
    allowedUnits: ['kg', 'bags'],
    createdAtUtc: '2026-01-01T00:00:00Z',
    updatedAtUtc: '2026-01-01T00:00:00Z',
  },
  {
    id: 'cat-2',
    name: 'Steel',
    allowedUnits: ['kg', 'tons'],
    createdAtUtc: '2026-01-01T00:00:00Z',
    updatedAtUtc: '2026-01-01T00:00:00Z',
  },
];

const sampleListings: MaterialListingItem[] = [
  {
    id: 'list-1',
    sellerId: 'seller-1',
    categoryId: 'cat-1',
    categoryName: 'Cement',
    title: 'Portland Cement Bags 50kg',
    description: 'Fresh unopened bags stored in dry conditions.',
    quantity: 100,
    reservedQuantity: 20,
    unit: 'bags',
    condition: 'NEW',
    unitPrice: 2400,
    availableUntil: '2026-10-30T00:00:00Z',
    status: 'ACTIVE',
    createdAtUtc: '2026-09-20T10:00:00Z',
    updatedAtUtc: '2026-09-20T10:00:00Z',
    photos: [
      { id: 'photo-1', photoUrl: 'https://example.com/cement.jpg', sortOrder: 0 },
    ],
    seller: {
      businessName: 'Lanka Builders Mart',
      email: 'sales@lankabuilders.test',
      phoneNumber: '+94771234567',
    },
  },
  {
    id: 'list-2',
    sellerId: 'seller-2',
    categoryId: 'cat-2',
    categoryName: 'Steel',
    title: 'TMT Rebar 12mm 6m lengths',
    description: 'High tensile strength surplus rebar from commercial build.',
    quantity: 50,
    reservedQuantity: 0,
    unit: 'pcs',
    condition: 'EXCELLENT',
    unitPrice: 3800,
    availableUntil: '2026-11-15T00:00:00Z',
    status: 'ACTIVE',
    createdAtUtc: '2026-09-22T10:00:00Z',
    updatedAtUtc: '2026-09-22T10:00:00Z',
    photos: [],
    seller: {
      businessName: 'Apex Steel Distributors',
      email: 'info@apexsteel.test',
    },
  },
];

const mockAuthValue = {
  user: {
    id: 'buyer-user-1',
    email: 'buyer@example.com',
    roles: ['BUYER' as const],
  },
  status: 'authenticated' as const,
  login: vi.fn(),
  register: vi.fn(),
  logout: vi.fn(),
  error: null,
  clearError: vi.fn(),
  updateProfile: vi.fn(),
};

describe('Buyer React Marketplace & Handoff', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    vi.mocked(api.fetchMaterialCategories).mockResolvedValue(sampleCategories);
    vi.mocked(api.fetchMarketplaceListings).mockResolvedValue({
      items: sampleListings,
      totalCount: 2,
      totalPages: 1,
      page: 1,
      pageSize: 12,
    });
  });

  it('renders marketplace listings with real data, categories and search', async () => {
    render(
      <MemoryRouter initialEntries={['/app/buyer']}>
        <BuyerMarketplacePage />
      </MemoryRouter>,
    );

    // Categories render
    expect(await screen.findByRole('button', { name: 'All Materials' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Cement' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Steel' })).toBeInTheDocument();

    // Listings render
    expect(screen.getByText('Portland Cement Bags 50kg')).toBeInTheDocument();
    expect(screen.getByText('TMT Rebar 12mm 6m lengths')).toBeInTheDocument();

    // Prices and available quantities render
    expect(screen.getByText('2,400')).toBeInTheDocument();
    expect(screen.getByText('80 bags available')).toBeInTheDocument();
    expect(screen.getByText('Lanka Builders Mart')).toBeInTheDocument();

    // Verify NO direct purchase buttons exist
    expect(screen.queryByRole('button', { name: /buy now/i })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /reserve now/i })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /purchase/i })).not.toBeInTheDocument();

    // Verify privacy: phone number is not exposed on card
    expect(screen.queryByText('+94771234567')).not.toBeInTheDocument();
  });

  it('filters by category when category button is clicked', async () => {
    render(
      <MemoryRouter initialEntries={['/app/buyer']}>
        <BuyerMarketplacePage />
      </MemoryRouter>,
    );

    const cementBtn = await screen.findByRole('button', { name: 'Cement' });

    await userEvent.click(cementBtn);

    await waitFor(() => {
      expect(api.fetchMarketplaceListings).toHaveBeenCalledWith(
        expect.objectContaining({
          category: 'Cement',
        }),
      );
    });
  });

  it('renders listing details page and displays AI Mobile CTA without direct buy flow', async () => {
    vi.mocked(api.fetchMarketplaceListingDetails).mockResolvedValue(sampleListings[0]);

    render(
      <MemoryRouter initialEntries={['/app/buyer/materials/list-1']}>
        <Routes>
          <Route path="/app/buyer/materials/:listingId" element={<BuyerListingDetailsPage />} />
        </Routes>
      </MemoryRouter>,
    );

    expect(await screen.findByRole('heading', { name: 'Portland Cement Bags 50kg' })).toBeInTheDocument();
    expect(screen.getByText('Fresh unopened bags stored in dry conditions.')).toBeInTheDocument();
    expect(screen.getByText('Lanka Builders Mart')).toBeInTheDocument();
    expect(screen.getByText('80 bags')).toBeInTheDocument();

    // Verify NO Buy Now or Reserve Now button exists
    expect(screen.queryByRole('button', { name: /buy now/i })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /reserve now/i })).not.toBeInTheDocument();

    // Verify AI CTA exists
    expect(
      screen.getByRole('heading', { name: 'Looking for the best match for your project?' }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: /continue with surpluslink mobile/i }),
    ).toBeInTheDocument();
  });

  it('opens mobile handoff QR modal on CTA click and displays countdown timer', async () => {
    vi.mocked(api.fetchMarketplaceListingDetails).mockResolvedValue(sampleListings[0]);
    vi.mocked(api.createMobileHandoff).mockResolvedValue({
      id: 'handoff-1',
      code: 'unpredictable-hex-code-1234567890',
      deepLink: 'surpluslink://handoff/unpredictable-hex-code-1234567890',
      categoryId: 'cat-1',
      categoryName: 'Cement',
      source: 'REACT_MARKETPLACE',
      createdAt: new Date().toISOString(),
      expiresAt: new Date(Date.now() + 15 * 60 * 1000).toISOString(),
    });

    render(
      <MemoryRouter initialEntries={['/app/buyer/materials/list-1']}>
        <Routes>
          <Route path="/app/buyer/materials/:listingId" element={<BuyerListingDetailsPage />} />
        </Routes>
      </MemoryRouter>,
    );

    const ctaButton = await screen.findByRole('button', { name: /continue with surpluslink mobile/i });
    await userEvent.click(ctaButton);

    await waitFor(() => {
      expect(api.createMobileHandoff).toHaveBeenCalledWith('cat-1', 'REACT_MARKETPLACE');
    });

    expect(await screen.findByRole('heading', { name: /continue in surpluslink mobile/i })).toBeInTheDocument();
    expect(screen.getByText(/This handoff expires in/i)).toBeInTheDocument();

    // Verify deep link action
    const deepLinkBtn = screen.getByRole('link', { name: /open in mobile app/i });
    expect(deepLinkBtn).toHaveAttribute('href', 'surpluslink://handoff/unpredictable-hex-code-1234567890');
  });

  it('shares the same BuyerMarketplaceLayout between Marketplace and My Activity', async () => {
    const { BuyerMarketplaceLayout } = await import('../pages/buyer/BuyerMarketplaceLayout');
    render(
      <MemoryRouter initialEntries={['/app/buyer']}>
        <BuyerMarketplaceLayout>
          <div>Marketplace Content</div>
        </BuyerMarketplaceLayout>
      </MemoryRouter>,
    );

    expect(screen.getByTestId('buyer-unified-layout')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /surpluslink/i })).toHaveAttribute('href', '/app/buyer');
    expect(screen.getByRole('link', { name: 'Marketplace' })).toHaveClass('active');
    expect(screen.getByRole('link', { name: 'My Activity' })).not.toHaveClass('active');
    expect(screen.getByText('Marketplace Content')).toBeInTheDocument();
  });

  it('resolves material image URLs properly and falls back when missing', async () => {
    const { resolveImageUrl, FALLBACK_MATERIAL_IMAGE } = await import('../utils/imageUrl');
    const { environment } = await import('../config/environment');

    // Null or undefined
    expect(resolveImageUrl(null)).toBe(FALLBACK_MATERIAL_IMAGE);
    expect(resolveImageUrl(undefined)).toBe(FALLBACK_MATERIAL_IMAGE);
    expect(resolveImageUrl('')).toBe(FALLBACK_MATERIAL_IMAGE);
    expect(resolveImageUrl('   ')).toBe(FALLBACK_MATERIAL_IMAGE);

    // Absolute URLs
    expect(resolveImageUrl('https://images.example.com/brick.jpg')).toBe('https://images.example.com/brick.jpg');
    expect(resolveImageUrl('http://cdn.test/timber.png')).toBe('http://cdn.test/timber.png');

    // Relative backend path
    const relativePath = '/api/material-photos/owner-id/photo-id/jpg';
    const expected = new URL(relativePath, environment.apiBaseUrl).toString();
    expect(resolveImageUrl(relativePath)).toBe(expected);
  });
});
