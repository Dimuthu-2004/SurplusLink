import type { UserRole } from '../auth/authTypes';

export const roleHomePaths: Record<UserRole, string> = {
  SELLER: '/app/seller',
  BUYER: '/app/buyer',
  MANAGER: '/app/manager',
};

export const roleLabels: Record<UserRole, string> = {
  SELLER: 'Seller',
  BUYER: 'Buyer',
  MANAGER: 'Manager',
};

export function roleHomePath(roles: readonly UserRole[]): string {
  return roleHomePaths[roles.includes('MANAGER') ? 'MANAGER' : roles.includes('SELLER') ? 'SELLER' : 'BUYER'];
}
