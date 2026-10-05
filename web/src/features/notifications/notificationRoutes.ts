import type { NotificationItem } from './notificationTypes';

export function resolveNotificationRoute(item: NotificationItem, userRoles: string[]): string {
  const actionRoute = knownRoute(item.actionRoute);
  if (actionRoute) return actionRoute;

  // Fallback heuristic based on entity type
  if (item.entityType === 'Transaction' && item.entityId) {
    if (userRoles.includes('MANAGER')) {
      // There is no manager transaction-detail route; use the actual manager workspace.
      return '/app/manager';
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

function knownRoute(actionRoute?: string | null): string | null {
  if (!actionRoute?.startsWith('/')) return null;
  const route = actionRoute.startsWith('/app/') ? actionRoute : `/app${actionRoute}`;
  const known = [
    /^\/app\/(notifications|profile|seller|buyer|offers)\/?$/,
    /^\/app\/buyer\/materials\/[^/]+$/,
    /^\/app\/offers\/[^/]+$/,
    /^\/app\/manager\/?$/,
    /^\/app\/manager\/(materials|requirements|workflows)\/[^/]+$/,
    /^\/app\/manager\/(materials|categories|catalog|requirements|matches|listing-approvals|requirement-approvals|community)\/?$/,
  ];
  return known.some(pattern => pattern.test(route)) ? route : null;
}
