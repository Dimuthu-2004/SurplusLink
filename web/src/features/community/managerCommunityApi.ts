import { apiClient } from '../../api/apiClient';

export interface CommunityUser {
  id: string;
  email: string;
  fullName: string | null;
  businessName: string | null;
  phoneNumber: string | null;
  address: string | null;
  profilePhotoUrl: string | null;
  roles: string[];
  createdAtUtc: string;
  emailVerified: boolean;
}

export interface CommunityUserDetails extends CommunityUser {
  listingsCount: number;
  requestsCount: number;
  completedTransactionsCount: number;
}

export const managerCommunityApi = {
  async listUsers(search?: string, role?: string): Promise<CommunityUser[]> {
    const params = new URLSearchParams();
    if (search?.trim()) params.set('search', search.trim());
    if (role?.trim() && role !== 'ALL') params.set('role', role.trim());

    const qs = params.toString();
    const response = await apiClient.get<CommunityUser[]>(`/api/users${qs ? `?${qs}` : ''}`);
    return response.data;
  },

  async getUserDetails(id: string): Promise<CommunityUserDetails> {
    const response = await apiClient.get<CommunityUserDetails>(`/api/users/${id}`);
    return response.data;
  },
};
