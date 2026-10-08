import { expect, it } from 'vitest';
import { resolveNotificationRoute } from '../features/notifications/notificationRoutes';

it('preserves a transaction notification destination for each recipient workspace', () => {
  expect(resolveNotificationRoute({ entityType: 'Transaction', entityId: 'tx-1', actionRoute: '/app/manager/transactions/tx-1' } as never, ['MANAGER'])).toBe('/app/manager/transactions/tx-1');
  expect(resolveNotificationRoute({ entityType: 'Transaction', entityId: 'tx-1', actionRoute: '/app/offers/offer-1' } as never, ['BUYER'])).toBe('/app/offers/offer-1');
});
