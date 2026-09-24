import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';
import { ManagerCategoriesPage } from '../pages/manager/ManagerCategoriesPage';
import { ManagerListingsPage } from '../pages/manager/ManagerListingsPage';
import type {
  InventoryAnalytics,
  ManagerMaterialsApi,
  MaterialCategory,
  MaterialListing,
  PagedListings,
} from '../features/materials/managerMaterialsApi';

const listing: MaterialListing = {
  id: '11111111-1111-1111-1111-111111111111',
  sellerId: '22222222-2222-2222-2222-222222222222',
  categoryId: '33333333-3333-3333-3333-333333333333',
  categoryName: 'Steel',
  title: 'Steel beams',
  description: 'Surplus structural beams',
  quantity: 10,
  reservedQuantity: 2,
  unit: 'pieces',
  condition: 'GOOD',
  unitPrice: 25,
  latitude: null,
  longitude: null,
  availableUntil: '2026-12-31T00:00:00Z',
  status: 'PENDING_VERIFICATION',
  createdAtUtc: '2026-09-15T00:00:00Z',
  updatedAtUtc: '2026-09-15T00:00:00Z',
  photos: [],
};

describe('manager Material UI', () => {
  it('applies search and sorting filters and loads the next page', async () => {
    const api = fakeApi();
    api.listListings
      .mockResolvedValueOnce(page([listing], 1, 2))
      .mockResolvedValueOnce(page([listing], 1, 2))
      .mockResolvedValueOnce(page([{ ...listing, id: '44444444-4444-4444-4444-444444444444', title: 'Steel plates' }], 2, 2));

    render(
      <MemoryRouter>
        <ManagerListingsPage api={api} />
      </MemoryRouter>,
    );

    expect(await screen.findByText('Steel beams')).toBeInTheDocument();
    const visitor = userEvent.setup();
    await visitor.type(screen.getByLabelText('Search'), 'steel');
    await visitor.selectOptions(screen.getByLabelText('Sort by'), 'unitPrice');
    await visitor.selectOptions(screen.getByLabelText('Direction'), 'asc');
    await visitor.click(screen.getByRole('button', { name: 'Apply filters' }));

    await waitFor(() => expect(api.listListings).toHaveBeenLastCalledWith(
      expect.objectContaining({ search: 'steel', sortBy: 'unitPrice', sortDir: 'asc', page: 1 }),
    ));
    await visitor.click(screen.getByRole('button', { name: 'Next' }));

    expect(await screen.findByText('Steel plates')).toBeInTheDocument();
    expect(api.listListings).toHaveBeenLastCalledWith(
      expect.objectContaining({ page: 2, sortBy: 'unitPrice', sortDir: 'asc' }),
    );
  });

  it('loads assigned units for editing, filters options and saves multiple catalog units', async () => {
    const api = fakeApi();
    const category: MaterialCategory = { id: 'category-1', name: 'Steel', allowedUnits: ['kg'], createdAtUtc: '', updatedAtUtc: '' };
    vi.mocked(api.getCategories).mockResolvedValue([category]);
    vi.mocked(api.updateCategory).mockResolvedValue({ ...category, allowedUnits: ['kg', 'box'] });
    render(<MemoryRouter><ManagerCategoriesPage api={api} /></MemoryRouter>);
    const visitor = userEvent.setup();
    await visitor.click(await screen.findByRole('button', { name: 'Edit' }));
    expect(screen.getByRole('button', { name: 'Remove kg' })).toBeInTheDocument();
    await visitor.type(screen.getByRole('combobox', { name: 'Edit allowed units' }), 'bo');
    expect(screen.queryByRole('option', { name: 'kg' })).not.toBeInTheDocument();
    await visitor.keyboard('{Enter}');
    await visitor.click(screen.getByRole('button', { name: /^Save$/ }));
    await waitFor(() => expect(api.updateCategory).toHaveBeenCalledWith('category-1', 'Steel', ['kg', 'box']));
    expect(await screen.findByText('Units: kg, box')).toBeInTheDocument();
  });

  it('blocks category creation when the catalog fails and supports retry', async () => {
    const api = fakeApi();
    vi.mocked(api.getUnitCatalog).mockRejectedValueOnce(new Error('Catalog unavailable'));
    render(<MemoryRouter><ManagerCategoriesPage api={api} /></MemoryRouter>);
    expect(await screen.findByRole('alert')).toHaveTextContent('Catalog unavailable');
    expect(screen.getByRole('button', { name: 'Add category' })).toBeDisabled();
    await userEvent.click(screen.getByRole('button', { name: 'Retry categories and units' }));
    await screen.findByText('No categories have been added.');
    expect(screen.getByRole('button', { name: 'Add category' })).toBeEnabled();
  });

  it('validates a blank category and creates a valid category', async () => {
    const api = fakeApi();
    const created: MaterialCategory = {
      id: '55555555-5555-5555-5555-555555555555',
      name: 'Steel',
      createdAtUtc: '2026-09-15T00:00:00Z',
      updatedAtUtc: '2026-09-15T00:00:00Z',
    };
    api.createCategory.mockResolvedValue(created);

    render(
      <MemoryRouter>
        <ManagerCategoriesPage api={api} />
      </MemoryRouter>,
    );

    await screen.findByText('No categories have been added.');
    const visitor = userEvent.setup();
    await visitor.click(screen.getByRole('button', { name: 'Add category' }));
    expect(api.createCategory).not.toHaveBeenCalled();

    await visitor.type(screen.getByLabelText('Category name'), ' Steel ');
    await visitor.click(screen.getByRole('button', { name: 'Add category' }));
    expect(screen.getByRole('alert')).toHaveTextContent('Select at least one allowed unit');
    await visitor.type(screen.getByRole('combobox', { name: 'Allowed units' }), 'k');
    await visitor.click(screen.getByRole('option', { name: 'kg' }));
    await visitor.click(screen.getByRole('button', { name: 'Add category' }));

    expect(await screen.findByText('Steel')).toBeInTheDocument();
    expect(api.createCategory).toHaveBeenCalledWith('Steel', ['kg']);
  });
});

function page(items: MaterialListing[], pageNumber: number, totalPages: number): PagedListings {
  return { items, totalCount: totalPages * items.length, totalPages, page: pageNumber, pageSize: 20 };
}

function fakeApi(): ManagerMaterialsApi & {
  listListings: ReturnType<typeof vi.fn>;
  createCategory: ReturnType<typeof vi.fn>;
} {
  const analytics: InventoryAnalytics = {
    activeCount: 1,
    listingsByCategory: [{ key: 'Steel', count: 1 }],
    listingsByStatus: [{ key: 'PENDING_VERIFICATION', count: 1 }],
    expiringListings: [],
    lowRemainingQuantityListings: [],
  };
  return {
    listListings: vi.fn().mockResolvedValue(page([], 1, 1)),
    getListing: vi.fn(),
    getHistory: vi.fn().mockResolvedValue([]),
    verifyListing: vi.fn(),
    getCategories: vi.fn().mockResolvedValue([]),
    getUnitCatalog: vi.fn().mockResolvedValue(['kg', 'box', 'm2']),
    createCategory: vi.fn(),
    updateCategory: vi.fn(),
    deleteCategory: vi.fn().mockResolvedValue(undefined),
    getAnalytics: vi.fn().mockResolvedValue(analytics),
  } as unknown as ManagerMaterialsApi & {
    listListings: ReturnType<typeof vi.fn>;
    createCategory: ReturnType<typeof vi.fn>;
  };
}
