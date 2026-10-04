import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import type { NotificationItem } from './notificationTypes';
import { resolveNotificationRoute } from './notificationRoutes';
import { useAuth } from '../../auth/AuthContext';

export interface ToastItem {
  id: string;
  notification: NotificationItem;
  exiting?: boolean;
}

export function NotificationToastCard({
  item,
  onDismiss,
}: {
  item: ToastItem;
  onDismiss: (id: string) => void;
}) {
  const navigate = useNavigate();
  const { user } = useAuth();
  const [isHovered, setIsHovered] = useState(false);

  useEffect(() => {
    if (isHovered) return;
    const timer = setTimeout(() => {
      onDismiss(item.id);
    }, 6000);
    return () => clearTimeout(timer);
  }, [item.id, isHovered, onDismiss]);

  const targetRoute = resolveNotificationRoute(item.notification, user?.roles ?? []);

  const handleClick = () => {
    onDismiss(item.id);
    if (targetRoute) {
      navigate(targetRoute);
    }
  };

  const timeLabel = formatRelativeTime(item.notification.createdAtUtc);

  return (
    <div
      className={`notification-toast priority-${item.notification.priority} ${item.exiting ? 'is-exiting' : ''}`}
      role="alert"
      aria-live="assertive"
      onMouseEnter={() => setIsHovered(true)}
      onMouseLeave={() => setIsHovered(false)}
    >
      <div className="notification-toast-icon">
        {item.notification.priority === 'CRITICAL' ? (
          <span style={{ fontSize: '1.25rem' }}>🚨</span>
        ) : item.notification.priority === 'WARNING' ? (
          <span style={{ fontSize: '1.25rem' }}>⚠</span>
        ) : item.notification.priority === 'ACTION_REQUIRED' ? (
          <span style={{ fontSize: '1.25rem' }}>⚡</span>
        ) : (
          <span style={{ fontSize: '1.25rem' }}>🔔</span>
        )}
      </div>
      <div className="notification-toast-body">
        <h4 className="notification-toast-title">{item.notification.title}</h4>
        <p className="notification-toast-msg">{item.notification.message}</p>
        <div className="notification-toast-meta">
          <span>{timeLabel}</span>
          <button type="button" className="notification-toast-action" onClick={handleClick}>
            View details →
          </button>
        </div>
      </div>
      <button
        type="button"
        className="notification-toast-close"
        aria-label="Dismiss notification"
        onClick={(e) => {
          e.stopPropagation();
          onDismiss(item.id);
        }}
      >
        ✕
      </button>
    </div>
  );
}

export function NotificationToastContainer({
  toasts,
  onDismiss,
}: {
  toasts: ToastItem[];
  onDismiss: (id: string) => void;
}) {
  if (toasts.length === 0) return null;

  return (
    <aside className="notification-toast-container" aria-label="Notifications" role="region">
      {toasts.map((toast) => (
        <NotificationToastCard key={toast.id} item={toast} onDismiss={onDismiss} />
      ))}
    </aside>
  );
}

function formatRelativeTime(dateString: string): string {
  try {
    const date = new Date(dateString);
    const now = new Date();
    const diffSec = Math.floor((now.getTime() - date.getTime()) / 1000);
    if (diffSec < 45) return 'Just now';
    if (diffSec < 3600) return `${Math.floor(diffSec / 60)}m ago`;
    if (diffSec < 86400) return `${Math.floor(diffSec / 3600)}h ago`;
    return `${Math.floor(diffSec / 86400)}d ago`;
  } catch {
    return 'Just now';
  }
}
