import { apiClient } from '../../api/apiClient';
import type { NotificationItem, NotificationPageResponse, NotificationQuery } from './notificationTypes';

export const notificationsApi = {
  async list(query?: NotificationQuery): Promise<NotificationPageResponse> {
    const params = new URLSearchParams();
    if (query?.unread !== undefined) params.set('unread', String(query.unread));
    if (query?.context) params.set('context', query.context);
    if (query?.priority) params.set('priority', query.priority);
    if (query?.page) params.set('page', String(query.page));
    if (query?.pageSize) params.set('pageSize', String(query.pageSize));

    const qs = params.toString();
    const url = `/api/notifications${qs ? `?${qs}` : ''}`;
    const response = await apiClient.get<NotificationPageResponse>(url);
    return response.data;
  },

  async unreadCount(): Promise<number> {
    const response = await apiClient.get<{ count: number }>('/api/notifications/unread-count');
    return response.data.count;
  },

  async markRead(id: string): Promise<NotificationItem> {
    const response = await apiClient.post<NotificationItem>(`/api/notifications/${id}/read`);
    return response.data;
  },

  async markAllRead(): Promise<number> {
    const response = await apiClient.post<{ markedCount: number }>('/api/notifications/read-all');
    return response.data.markedCount;
  },
};
