import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';
import { OfferDetailsPage } from '../pages/OfferDetailsPage';
import type { TransactionsApi } from '../features/transactions/transactionsApi';
vi.mock('../auth/AuthContext', () => ({ useAuth: () => ({ user: { id: 'buyer' } }) }));
describe('offer details', () => {
  it('loads an older offer directly rather than searching the first page', async () => {
    const api: TransactionsApi = { offer: vi.fn().mockResolvedValue({ id: 'older-offer', buyerId: 'buyer', sellerId: 'seller', quantity: 400, unitValue: 800, totalValue: 320000, status: 'ACCEPTED', createdAt: '2026-09-22T00:00:00Z' }), offers: vi.fn(), transactions: vi.fn().mockResolvedValue({ items: [], total: 0, page: 1, pageSize: 1, totalPages: 0 }), history: vi.fn() };
    render(<MemoryRouter initialEntries={['/offers/older-offer']}><Routes><Route path="/offers/:offerId" element={<OfferDetailsPage api={api} />} /></Routes></MemoryRouter>);
    await screen.findByText('Participation context');
    expect(api.offer).toHaveBeenCalledWith('older-offer');
    expect(api.offers).not.toHaveBeenCalled();
    expect(api.transactions).toHaveBeenCalledWith(expect.objectContaining({ offerId: 'older-offer', pageSize: 1 }));
    expect(screen.getByText('You')).toBeInTheDocument();
  });
});
