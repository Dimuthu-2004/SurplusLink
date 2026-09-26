import { apiClient } from './apiClient';

export interface MaterialListingPhoto {
  id: string;
  photoUrl: string;
  sortOrder: number;
}

export interface SellerContact {
  fullName?: string;
  businessName?: string;
  email: string;
  phoneNumber?: string;
}

export interface MaterialListingItem {
  id: string;
  sellerId: string;
  categoryId: string;
  categoryName: string;
  title: string;
  description: string;
  quantity: number;
  reservedQuantity: number;
  unit: string;
  condition: string;
  unitPrice: number;
  latitude?: number;
  longitude?: number;
  availableUntil: string;
  status: string;
  createdAtUtc: string;
  updatedAtUtc: string;
  photos: MaterialListingPhoto[];
  seller?: SellerContact;
}

export interface PagedMaterialListings {
  items: MaterialListingItem[];
  totalCount: number;
  totalPages: number;
  page: number;
  pageSize: number;
}

export interface MaterialCategoryItem {
  id: string;
  name: string;
  allowedUnits?: string[];
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface MarketplaceQueryParams {
  search?: string;
  category?: string;
  condition?: string;
  minPrice?: number;
  maxPrice?: number;
  sortBy?: 'unitPrice' | 'quantity' | 'createdAt' | 'availableUntil';
  sortDir?: 'asc' | 'desc';
  page?: number;
  pageSize?: number;
}

export interface MobileHandoffCreationResponse {
  id: string;
  code: string;
  deepLink: string;
  categoryId: string;
  categoryName: string;
  source: string;
  createdAt: string;
  expiresAt: string;
}

export interface MobileHandoffStatus {
  code: string;
  categoryId: string;
  categoryName: string;
  source: string;
  isExpired: boolean;
  isRedeemed: boolean;
  expiresAt: string;
}

export async function fetchMarketplaceListings(
  params: MarketplaceQueryParams = {},
): Promise<PagedMaterialListings> {
  const query: Record<string, string | number> = {};

  if (params.search?.trim()) query.search = params.search.trim();
  if (params.category?.trim()) query.category = params.category.trim();
  if (params.condition?.trim()) query.condition = params.condition.trim();
  if (params.minPrice !== undefined && params.minPrice !== null && !isNaN(params.minPrice)) {
    query.minPrice = params.minPrice;
  }
  if (params.maxPrice !== undefined && params.maxPrice !== null && !isNaN(params.maxPrice)) {
    query.maxPrice = params.maxPrice;
  }
  if (params.sortBy) query.sortBy = params.sortBy;
  if (params.sortDir) query.sortDir = params.sortDir;
  if (params.page) query.page = params.page;
  if (params.pageSize) query.pageSize = params.pageSize;

  const response = await apiClient.get<PagedMaterialListings>('/api/materials', {
    params: query,
  });
  return response.data;
}

export async function fetchMarketplaceListingDetails(
  listingId: string,
): Promise<MaterialListingItem> {
  const response = await apiClient.get<MaterialListingItem>(`/api/materials/${listingId}`);
  return response.data;
}

export async function fetchMaterialCategories(): Promise<MaterialCategoryItem[]> {
  const response = await apiClient.get<MaterialCategoryItem[]>('/api/material-categories');
  return response.data;
}

export async function createMobileHandoff(
  categoryId: string,
  source = 'REACT_MARKETPLACE',
): Promise<MobileHandoffCreationResponse> {
  const response = await apiClient.post<MobileHandoffCreationResponse>('/api/mobile-handoffs', {
    categoryId,
    source,
  });
  return response.data;
}

export async function getMobileHandoffStatus(code: string): Promise<MobileHandoffStatus> {
  const response = await apiClient.get<MobileHandoffStatus>(`/api/mobile-handoffs/${encodeURIComponent(code)}`);
  return response.data;
}
