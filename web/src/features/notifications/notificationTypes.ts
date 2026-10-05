export type NotificationPriority = 'INFO' | 'ACTION_REQUIRED' | 'WARNING' | 'CRITICAL';
export type NotificationContextType = 'SYSTEM' | 'BUYER' | 'SELLER' | 'MANAGER';

export interface NotificationItem {
  id: string;
  type: string;
  title: string;
  message: string;
  context: NotificationContextType;
  priority: NotificationPriority;
  entityType?: string | null;
  entityId?: string | null;
  actionRoute?: string | null;
  isRead: boolean;
  createdAt: string;
  readAt?: string | null;
}

export interface NotificationPageResponse {
  items: NotificationItem[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface NotificationQuery {
  unread?: boolean;
  context?: NotificationContextType;
  priority?: NotificationPriority;
  page?: number;
  pageSize?: number;
}
