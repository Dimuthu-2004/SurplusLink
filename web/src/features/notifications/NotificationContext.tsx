import React, { createContext, useContext, useEffect, useState, useCallback, useRef, type ReactNode } from 'react';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../config/environment';
import { sessionTokenStorage } from '../../auth/tokenStorage';
import { useAuth } from '../../auth/AuthContext';
import { notificationsApi } from './notificationsApi';
import type { NotificationItem } from './notificationTypes';
import { NotificationToastContainer, type ToastItem } from './NotificationToast';
import './notifications.css';

interface NotificationContextValue {
  unreadCount: number;
  toasts: ToastItem[];
  dismissToast: (id: string) => void;
  markAsRead: (id: string) => Promise<void>;
  markAllAsRead: () => Promise<void>;
  refreshUnreadCount: () => Promise<void>;
}

const defaultContextValue: NotificationContextValue = {
  unreadCount: 0,
  toasts: [],
  dismissToast: () => {},
  markAsRead: async () => {},
  markAllAsRead: async () => {},
  refreshUnreadCount: async () => {},
};

const NotificationContext = createContext<NotificationContextValue | null>(null);

export function useNotifications(): NotificationContextValue {
  const context = useContext(NotificationContext);
  return context ?? defaultContextValue;
}

export function NotificationProvider({ children }: { children: ReactNode }) {
  const { user } = useAuth();
  const [unreadCount, setUnreadCount] = useState(0);
  const [toasts, setToasts] = useState<ToastItem[]>([]);
  const hubRef = useRef<signalR.HubConnection | null>(null);

  const refreshUnreadCount = useCallback(async () => {
    if (!user) {
      setUnreadCount(0);
      return;
    }
    try {
      const count = await notificationsApi.unreadCount();
      setUnreadCount(count);
    } catch {
      // Ignore background refresh errors
    }
  }, [user]);

  const dismissToast = useCallback((id: string) => {
    setToasts((current) =>
      current.map((item) => (item.id === id ? { ...item, exiting: true } : item))
    );
    setTimeout(() => {
      setToasts((current) => current.filter((item) => item.id !== id));
    }, 300);
  }, []);

  const markAsRead = useCallback(async (id: string) => {
    try {
      await notificationsApi.markRead(id);
      setUnreadCount((c) => Math.max(0, c - 1));
    } catch {
      // Ignore error
    }
  }, []);

  const markAllAsRead = useCallback(async () => {
    try {
      await notificationsApi.markAllRead();
      setUnreadCount(0);
    } catch {
      // Ignore error
    }
  }, []);

  // Fetch initial unread count on login
  useEffect(() => {
    void refreshUnreadCount();
  }, [refreshUnreadCount]);

  // Connect to SignalR NotificationHub
  useEffect(() => {
    if (!user) {
      if (hubRef.current) {
        void hubRef.current.stop();
        hubRef.current = null;
      }
      return;
    }

    const token = sessionTokenStorage.read();
    if (!token) return;

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.apiBaseUrl}/hubs/notifications`, {
        accessTokenFactory: () => sessionTokenStorage.read() || '',
        transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling,
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.None)
      .build();

    connection.on('ReceiveNotification', (notification: NotificationItem) => {
      // 1. Immediately increment unread counter
      setUnreadCount((count) => count + 1);

      // 2. Add to toast popup stack
      const newToast: ToastItem = {
        id: notification.id || `toast-${Date.now()}-${Math.random()}`,
        notification,
      };

      setToasts((current) => [newToast, ...current.slice(0, 3)]); // Keep at most 4 visible
    });

    connection
      .start()
      .then(() => {
        hubRef.current = connection;
      })
      .catch(() => {
        // Fallback gracefully without breaking UI if SignalR fails to connect
      });

    return () => {
      if (connection) {
        void connection.stop();
      }
    };
  }, [user]);

  return (
    <NotificationContext.Provider
      value={{
        unreadCount,
        toasts,
        dismissToast,
        markAsRead,
        markAllAsRead,
        refreshUnreadCount,
      }}
    >
      {children}
      <NotificationToastContainer toasts={toasts} onDismiss={dismissToast} />
    </NotificationContext.Provider>
  );
}
