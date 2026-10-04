export const roles = ['SELLER', 'BUYER', 'MANAGER'] as const;

export type UserRole = (typeof roles)[number];

export interface AuthUser {
  id: string;
  email: string;
  roles: UserRole[];
  fullName?: string | null;
  phoneNumber?: string | null;
  businessName?: string | null;
  address?: string | null;
  profilePhotoUrl?: string | null;
}

export interface AuthResponse {
  token: string;
  user: AuthUser;
}

/** The public ASP.NET Core RegisterRequest contract. */
export interface PublicRegistration {
  fullName: string;
  email: string;
  phoneNumber: string;
  nic: string;
  businessName: string;
  address: string;
  password: string;
  roles: Extract<UserRole, 'SELLER' | 'BUYER'>[];
}

export function parseUser(value: unknown): AuthUser {
  if (!isRecord(value)) {
    throw new Error('The server returned an invalid user.');
  }
  const { id, email, roles: assignedRoles, fullName, phoneNumber, businessName, address, profilePhotoUrl } = value;
  if (
    typeof id !== 'string' ||
    typeof email !== 'string' ||
    !Array.isArray(assignedRoles) || assignedRoles.length === 0 ||
    !assignedRoles.every(role => roles.includes(role as UserRole)) ||
    new Set(assignedRoles).size !== assignedRoles.length
  ) {
    throw new Error('The server returned an invalid user.');
  }
  return {
    id,
    email,
    roles: assignedRoles as UserRole[],
    fullName: typeof fullName === 'string' ? fullName : null,
    phoneNumber: typeof phoneNumber === 'string' ? phoneNumber : null,
    businessName: typeof businessName === 'string' ? businessName : null,
    address: typeof address === 'string' ? address : null,
    profilePhotoUrl: typeof profilePhotoUrl === 'string' ? profilePhotoUrl : null,
  };
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
