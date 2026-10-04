import { apiClient } from '../../api/apiClient';

export interface PhotoUploadResponse {
  profilePhotoUrl: string;
}

export const profileApi = {
  async uploadPhoto(file: File): Promise<PhotoUploadResponse> {
    const formData = new FormData();
    formData.append('file', file);
    const response = await apiClient.post<PhotoUploadResponse>('/api/profile-photos', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
    return response.data;
  },

  async deletePhoto(): Promise<void> {
    await apiClient.delete('/api/profile-photos');
  },

  async deleteAccount(): Promise<void> {
    await apiClient.delete('/api/auth/me');
  },
};
