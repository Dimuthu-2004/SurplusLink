import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, expect, it, vi } from 'vitest';
import { App } from '../app/App';
import { MyOffersPage } from '../pages/MyOffersPage';
import { OfferDetailsPage } from '../pages/OfferDetailsPage';
import { ManagerApprovalsPage } from '../pages/manager/ManagerApprovalsPage';
import { ManagerWorkflowDetailsPage } from '../pages/manager/ManagerWorkflowDetailsPage';
import { SurplusLinkLogo } from '../components/SurplusLinkLogo';

const state = vi.hoisted(() => ({ user: { id: 'buyer-id', roles: ['BUYER'], email: 'buyer@example.com' } }));
vi.mock('../auth/AuthContext', () => ({ useAuth: () => ({ user: state.user, status: 'authenticated', pending: false, logout: vi.fn() }) }));
const group = { requirementTitle: 'Tiles', buyerName: 'Nimal Perera', requestedQuantity: 50, selectedQuantity: 35, remainingQuantity: 15, unit: 'pcs', fulfillmentStatus: 'PARTIAL', sellerCount: 2, totalValue: 28500,
  allocations: ['ABC Materials', 'Reuse Supplies'].map((name, i) => ({ transactionId: `t${i}`, sellerId: `s${i}`, sellerName: `Seller ${i}`, sellerBusinessName: name, listingId: `l${i}`, listingTitle: 'Tiles', allocatedQuantity: i ? 15 : 20, availableQuantity: 25, unit: 'pcs', unitPrice: 800, materialValue: i ? 12000 : 16000, score: .9, distance: 5, transportCost: 250, status: 'PENDING_APPROVAL' })) };
const workflow = { id: 'wf', materialRequestId: 'req', materialMatchId: null, status: 'PENDING_APPROVAL', currentStage: 'REVIEW', approvalGroup: group, inputJson: '{}', outputJson: '{}', validationJson: '{}', steps: [], approvals: [], startedAtUtc: '2026-09-25' };
const page = (items: unknown[]) => ({ items, total: items.length, page: 1, pageSize: 20, totalPages: 1 });
const workflows = { list: vi.fn(async () => page([workflow])), get: vi.fn(async () => workflow), approve: vi.fn(), reject: vi.fn(), revise: vi.fn(), transactions: vi.fn(), transactionHistory: vi.fn(), transactionAnalytics: vi.fn() };
const materials = { listListings: vi.fn(async () => page([])), getCategories: vi.fn(async () => []), analytics: vi.fn() };
vi.mock('../features/workflows/managerWorkflowsApi', async importOriginal => ({ ...await importOriginal<object>(), get managerWorkflowsApi() { return workflows; } }));
vi.mock('../features/materials/managerMaterialsApi', async importOriginal => ({ ...await importOriginal<object>(), get managerMaterialsApi() { return materials; } }));
vi.mock('../features/requirements/managerRequirementsApi', () => ({ managerRequirementsApi: { get: vi.fn(async () => ({ categoryId: 'c', requiredQuantity: 50, unit: 'pcs', maximumBudget: 40000, notes: 'Bathroom tiles', deadline: '2026-10-01' })), categories: vi.fn(async () => [{ id: 'c', name: 'Tiles' }]) } }));
const offer = { id: 'technical-offer', materialMatchId: 'm', buyerId: 'buyer-id', sellerId: 'seller-id', buyerName: 'Nimal Perera', sellerName: 'Kamal Silva', sellerBusinessName: 'ABC Materials', materialName: 'Tiles', unit: 'pcs', quantity: 20, unitValue: 1000, totalValue: 20000, status: 'ACCEPTED' as const, createdAt: '2026-09-25', updatedAt: '2026-09-26' };
const offers = { offers: vi.fn(async () => page([offer])), offer: vi.fn(async () => offer), transactions: vi.fn(async () => page([])), history: vi.fn(async () => page([])) };
beforeEach(() => { state.user = { id: 'buyer-id', roles: ['BUYER'], email: 'buyer@example.com' }; vi.clearAllMocks(); });

it('manager navigation opens distinct queues and locks seller approvals to pending verification', async () => {
  state.user.roles = ['MANAGER'];
  render(<MemoryRouter initialEntries={['/app/manager/listing-approvals']}><App /></MemoryRouter>);
  expect(await screen.findByRole('heading', { name: 'Seller Listing Approvals' })).toBeInTheDocument();
  expect(materials.listListings).toHaveBeenCalledWith(expect.objectContaining({ status: 'PENDING_VERIFICATION' }));
  expect(screen.getByRole('link', { name: 'Seller Listing Approvals' })).toHaveAttribute('aria-current', 'page');
  await userEvent.click(screen.getByRole('link', { name: 'Buyer Requirement Approvals' }));
  expect(await screen.findByRole('heading', { name: 'Buyer Requirement Approvals' })).toBeInTheDocument();
  expect(screen.getByRole('link', { name: 'Buyer Requirement Approvals' })).toHaveAttribute('aria-current', 'page');
});
it('approval queue has separate business columns and correct partial quantities', async () => {
  render(<MemoryRouter><ManagerApprovalsPage /></MemoryRouter>);
  await screen.findByText('Nimal Perera');
  expect(screen.getAllByRole('columnheader').map(el => el.textContent)).toEqual(['Material / Requirement', 'Buyer', 'Requested', 'Selected', 'Remaining', 'Fulfillment', 'Sellers', 'Total Value', 'Status', 'Action']);
  for (const text of ['50 pcs', '35 pcs', '15 pcs', 'Partial', '2 sellers', 'LKR 28,500.00']) expect(screen.getByText(text)).toBeInTheDocument();
  expect(screen.queryByText('wf')).not.toBeInTheDocument();
});
it('details show every named allocation and existing requirement details', async () => {
  render(<MemoryRouter initialEntries={['/workflows/wf']}><Routes><Route path="/workflows/:workflowId" element={<ManagerWorkflowDetailsPage />} /></Routes></MemoryRouter>);
  await screen.findByText('Nimal Perera');
  for (const name of ['ABC Materials', 'Reuse Supplies']) {
    const card = screen.getByRole('heading', { name }).closest('article')!;
    expect(within(card).getByText('Allocated quantity')).toBeInTheDocument();
    expect(within(card).getByText('Unit price')).toBeInTheDocument();
  }
  expect(await screen.findByText('Bathroom tiles')).toBeInTheDocument();
  expect(document.body.textContent).not.toContain('$');
});
it.each(['BUYER', 'SELLER'])('%s offers use material, names, unit and LKR without context or ID columns', async role => {
  state.user = { id: role === 'BUYER' ? 'buyer-id' : 'seller-id', roles: [role], email: 'user@example.com' };
  render(<MemoryRouter><MyOffersPage api={offers as never} /></MemoryRouter>);
  expect(await screen.findByText(role === 'BUYER' ? 'ABC Materials' : 'Nimal Perera')).toBeInTheDocument();
  expect(screen.getByRole('columnheader', { name: role === 'BUYER' ? 'Seller' : 'Buyer' })).toBeInTheDocument();
  expect(screen.getByText('LKR 20,000.00')).toBeInTheDocument();
  expect(screen.getByText('pcs')).toBeInTheDocument();
  expect(screen.queryByRole('columnheader', { name: /context|offer id|^offer$/i })).not.toBeInTheDocument();
  expect(screen.queryByText('technical-offer')).not.toBeInTheDocument();
});
it.each(['BUYER', 'SELLER'])('%s details resolve both names without ID lookups', async role => {
  state.user = { id: role === 'BUYER' ? 'buyer-id' : 'seller-id', roles: [role], email: 'user@example.com' };
  render(<MemoryRouter initialEntries={['/offers/technical-offer']}><Routes><Route path="/offers/:offerId" element={<OfferDetailsPage api={offers as never} />} /></Routes></MemoryRouter>);
  expect(await screen.findByText('ABC Materials')).toBeInTheDocument();
  expect(screen.getByText('Nimal Perera')).toBeInTheDocument();
  expect(screen.queryByText('buyer-id')).not.toBeInTheDocument();
  expect(screen.queryByText('seller-id')).not.toBeInTheDocument();
  expect(offers.offer).toHaveBeenCalledTimes(1);
});
it('shared logo renders the transparent asset', () => {
  render(<SurplusLinkLogo />);
  expect(screen.getByRole('img')).toHaveAttribute('src', expect.stringContaining('branding/surpluslink-logo-transparent.png'));
});
