import type { NotificationItem } from './notificationTypes';

export function resolveNotificationRoute(item: NotificationItem, userRoles: string[]): string {
  if (item.actionRoute && item.actionRoute.startsWith('/')) {
    // If route is already canonical /app/... path, use it directly
    if (item.actionRoute.startsWith('/app/')) return item.actionRoute;
    return `/app${item.actionRoute}`;
  }

  // Fallback heuristic based on entity type
  if (item.entityType === 'Transaction' && item.entityId) {
    if (userRoles.includes('MANAGER')) {
      return `/app/manager/transactions/${item.entityId}`;
    }
    return `/app/offers`;
  }

  if (item.entityType === 'Offer' || item.entityType === 'MaterialMatch') {
    return `/app/offers`;
  }

  if (item.entityType === 'Listing' && item.entityId) {
    if (userRoles.includes('MANAGER')) {
      return `/app/manager/materials/${item.entityId}`;
    }
    return `/app/buyer/materials/${item.entityId}`;
  }

  if (item.entityType === 'BuyerRequest' && item.entityId) {
    if (userRoles.includes('MANAGER')) {
      return `/app/manager/requirements/${item.entityId}`;
    }
    return `/app/buyer`;
  }

  return '/app/notifications';
}
