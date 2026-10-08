import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { expect, it, vi } from 'vitest';

const member = {
  id: 'member-1', email: 'member@example.com', fullName: 'Asha Perera', businessName: 'Circular Builders',
  phoneNumber: '0712345678', address: 'Colombo', profilePhotoUrl: null, roles: ['SELLER'],
  createdAtUtc: '2026-01-15T00:00:00Z', emailVerified: true,
};

vi.mock('../features/community/managerCommunityApi', () => ({
  managerCommunityApi: {
    listUsers: vi.fn(async () => [member]),
    getUserDetails: vi.fn(async () => ({ ...member, listingsCount: 4, requestsCount: 2, completedTransactionsCount: 3 })),
  },
}));

import { ManagerCommunityPage } from '../pages/manager/ManagerCommunityPage';

it('opens member details in a viewport-level modal and closes it', async () => {
  const user = userEvent.setup();
  render(<ManagerCommunityPage />);

  await user.click(await screen.findByText('View details →'));
  const dialog = await screen.findByRole('dialog', { name: 'Asha Perera' });
  const backdrop = screen.getByTestId('community-member-modal-backdrop');
  expect(dialog).toHaveAttribute('aria-modal', 'true');
  expect(backdrop.parentElement).toBe(document.body);
  expect(document.body.style.overflow).toBe('hidden');

  await user.click(screen.getByRole('button', { name: 'Close member details' }));
  expect(screen.queryByRole('dialog', { name: 'Asha Perera' })).not.toBeInTheDocument();
  expect(document.body.style.overflow).toBe('');
});
