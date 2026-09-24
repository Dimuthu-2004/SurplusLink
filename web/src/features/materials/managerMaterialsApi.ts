import type { AxiosInstance } from 'axios';
import { apiClient, normalizeApiError } from '../../api/apiClient';

export interface MaterialListing {
  id: string;
  sellerId: string;
  seller?: { fullName: string | null; businessName: string | null; email: string; phoneNumber: string | null } | null;
  categoryId: string;
  categoryName: string;
  title: string;
  description: string;
  quantity: number;
  reservedQuantity: number;
  unit: string;
  condition: string;
  unitPrice: number;
  latitude: number | null;
  longitude: number | null;
  availableUntil: string;
  status: string;
  createdAtUtc: string;
  updatedAtUtc: string;
  photos: ListingPhoto[];
}

export interface ListingPhoto {
  id: string;
  photoUrl: string;
  sortOrder: number;
}

export interface MaterialCategory {
  allowedUnits?: string[];
  id: string;
  name: string;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface ListingHistoryItem {
  id: string;
  actorUserId: string | null;
  action: string;
  createdAtUtc: string;
}

export interface ListingSearch {
  search?: string;
  category?: string;
  status?: string;
  condition?: string;
  minPrice?: number;
  maxPrice?: number;
  sortBy: 'unitPrice' | 'quantity' | 'createdAt' | 'availableUntil';
  sortDir: 'asc' | 'desc';
  page: number;
  pageSize: number;
}

export interface PagedListings {
  items: MaterialListing[];
  totalCount: number;
  totalPages: number;
  page: number;
  pageSize: number;
}

export interface AnalyticsCount {
  key: string;
  count: number;
}

export interface InventoryAnalytics {
  activeCount: number;
  listingsByCategory: AnalyticsCount[];
  listingsByStatus: AnalyticsCount[];
  expiringListings: MaterialListing[];
  lowRemainingQuantityListings: MaterialListing[];
}

export interface ManagerMaterialsApi {
  listListings(query: ListingSearch): Promise<PagedListings>;
  getListing(id: string): Promise<MaterialListing>;
  getHistory(id: string): Promise<ListingHistoryItem[]>;
  verifyListing(id: string, approved: boolean): Promise<MaterialListing>;
  getCategories(): Promise<MaterialCategory[]>;
  getUnitCatalog(): Promise<string[]>;
  createCategory(name: string, allowedUnits: string[]): Promise<MaterialCategory>;
  updateCategory(id: string, name: string, allowedUnits: string[]): Promise<MaterialCategory>;
  deleteCategory(id: string): Promise<void>;
  getAnalytics(): Promise<InventoryAnalytics>;
}

export function createManagerMaterialsApi(
  client: Pick<AxiosInstance, 'get' | 'post' | 'put' | 'patch' | 'delete'> = apiClient,
): ManagerMaterialsApi {
  return {
    async listListings(query) {
      return request(client.get<PagedListings>('/api/materials', { params: compact(query) }));
    },
    async getListing(id) {
      return request(client.get<MaterialListing>(`/api/materials/${id}`));
    },
    async getHistory(id) {
      return request(client.get<ListingHistoryItem[]>(`/api/materials/${id}/history`));
    },
    async verifyListing(id, approved) {
      return request(client.patch<MaterialListing>(`/api/materials/${id}/verify`, { approved }));
    },
    async getCategories() {
      return request(client.get<MaterialCategory[]>('/api/material-categories'));
    },
    async getUnitCatalog() {
      return request(client.get<string[]>('/api/material-categories/unit-catalog'));
    },
    async createCategory(name, allowedUnits) {
      return request(client.post<MaterialCategory>('/api/material-categories', { name, allowedUnits }));
    },
    async updateCategory(id, name, allowedUnits) {
      return request(client.put<MaterialCategory>(`/api/material-categories/${id}`, { name, allowedUnits }));
    },
    async deleteCategory(id) {
      await request(client.delete(`/api/material-categories/${id}`));
    },
    async getAnalytics() {
      return request(client.get<InventoryAnalytics>('/api/materials/analytics/summary'));
    },
  };
}

export const managerMaterialsApi = createManagerMaterialsApi();

async function request<T>(operation: Promise<{ data: T }>): Promise<T> {
  try {
    return (await operation).data;
  } catch (error) {
    throw normalizeApiError(error);
  }
}

function compact(query: ListingSearch): Record<string, string | number> {
  return Object.fromEntries(
    Object.entries(query).filter(([, value]) => value !== '' && value !== undefined),
  ) as Record<string, string | number>;
}
