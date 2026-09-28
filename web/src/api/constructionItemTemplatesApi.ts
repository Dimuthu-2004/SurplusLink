import { apiClient } from './apiClient';

export interface ConstructionItemTemplate {
  id: string;
  name: string;
  categoryId: string;
  categoryName: string;
  itemClass: 'MATERIAL' | 'TOOL' | 'EQUIPMENT' | 'FIXTURE' | 'TEMPORARY_WORK' | 'OTHER_CONSTRUCTION' | string;
  quantityMode: 'PACKAGE' | 'PIECE' | 'CONTINUOUS_BULK' | 'LENGTH' | 'AREA' | 'VOLUME' | string;
  baseUnit: string;
  packageType: string | null;
  allowedUnits: string[];
  allowedPackageSizes: number[];
  attributeSchema: string; // JSON string
  priceBasis: string;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface AttributeFieldDefinition {
  id: string;
  label: string;
  type: 'string' | 'number' | 'select';
  required?: boolean;
  options?: string[];
  placeholder?: string;
  unit?: string;
}

export interface CreateConstructionItemTemplatePayload {
  name: string;
  categoryId: string;
  itemClass: string;
  quantityMode: string;
  baseUnit: string;
  packageType?: string | null;
  allowedUnits: string[];
  allowedPackageSizes?: number[];
  attributeSchema: string;
  priceBasis?: string;
}

export interface UpdateConstructionItemTemplatePayload extends CreateConstructionItemTemplatePayload {}

export async function fetchItemTemplates(params?: {
  search?: string;
  categoryId?: string;
  itemClass?: string;
  includeInactive?: boolean;
}): Promise<ConstructionItemTemplate[]> {
  const query = new URLSearchParams();
  if (params?.search) query.set('search', params.search);
  if (params?.categoryId) query.set('categoryId', params.categoryId);
  if (params?.itemClass) query.set('itemClass', params.itemClass);
  if (params?.includeInactive) query.set('includeInactive', 'true');

  const queryString = query.toString();
  const url = `/api/construction-item-templates${queryString ? `?${queryString}` : ''}`;
  const response = await apiClient.get<ConstructionItemTemplate[]>(url);
  return response.data;
}

export async function fetchItemTemplateById(id: string): Promise<ConstructionItemTemplate> {
  const response = await apiClient.get<ConstructionItemTemplate>(`/api/construction-item-templates/${id}`);
  return response.data;
}

export async function createItemTemplate(
  payload: CreateConstructionItemTemplatePayload,
): Promise<ConstructionItemTemplate> {
  const response = await apiClient.post<ConstructionItemTemplate>('/api/construction-item-templates', payload);
  return response.data;
}

export async function updateItemTemplate(
  id: string,
  payload: UpdateConstructionItemTemplatePayload,
): Promise<ConstructionItemTemplate> {
  const response = await apiClient.put<ConstructionItemTemplate>(`/api/construction-item-templates/${id}`, payload);
  return response.data;
}

export async function toggleItemTemplateStatus(
  id: string,
  isActive: boolean,
): Promise<ConstructionItemTemplate> {
  const response = await apiClient.patch<ConstructionItemTemplate>(`/api/construction-item-templates/${id}/toggle-status`, {
    isActive,
  });
  return response.data;
}

export async function resolvePhraseToTemplate(query: string): Promise<{
  templateId: string | null;
  templateName: string | null;
  itemClass: string | null;
  quantityMode: string | null;
  baseUnit: string | null;
  packageType: string | null;
  confidence: number;
}> {
  const response = await apiClient.get(`/api/construction-item-templates/resolve?query=${encodeURIComponent(query)}`);
  return response.data;
}
