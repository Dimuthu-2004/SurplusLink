export const roles = ['SELLER', 'BUYER', 'MANAGER'] as const;

export type UserRole = (typeof roles)[number];

export interface AuthUser {
  id: string;
  email: string;
  role: UserRole;
}

export interface AuthResponse {
  token: string;
  user: AuthUser;
}

export function parseUser(value: unknown): AuthUser {
  if (!isRecord(value)) {
    throw new Error('The server returned an invalid user.');
  }
  const { id, email, role } = value;
  if (
    typeof id !== 'string' ||
    typeof email !== 'string' ||
    typeof role !== 'string' ||
    !roles.includes(role as UserRole)
  ) {
    throw new Error('The server returned an invalid user.');
  }
  return { id, email, role: role as UserRole };
}

export function parseAuthResponse(value: unknown): AuthResponse {
  if (!isRecord(value) || typeof value.token !== 'string' || !value.token) {
    throw new Error('The server returned an invalid authentication response.');
  }
  return { token: value.token, user: parseUser(value.user) };
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null;
}
