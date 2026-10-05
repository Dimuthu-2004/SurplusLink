import React from 'react';
import { useNavigate } from 'react-router-dom';
import type { NotificationItem } from './notificationTypes';
import { resolveNotificationRoute } from './notificationRoutes';
import { useAuth } from '../../auth/AuthContext';
import { useNotifications } from './NotificationContext';

export function NotificationCard({
  item,
  onRead,
}: {
  item: NotificationItem;
  onRead?: () => void;
}) {
  const navigate = useNavigate();
  const { user } = useAuth();
  const { markAsRead } = useNotifications();

  const targetRoute = resolveNotificationRoute(item, user?.roles ?? []);

  const handleClick = async () => {
    if (!item.isRead) {
      await markAsRead(item.id);
      onRead?.();
    }
    if (targetRoute) {
      navigate(targetRoute);
    }
  };

  const handleMarkRead = async (e: React.MouseEvent) => {
    e.stopPropagation();
    await markAsRead(item.id);
    onRead?.();
  };

  return (
    <article
      className={`notification-card ${!item.isRead ? 'unread' : ''} priority-${item.priority}`}
      onClick={handleClick}
      style={{ cursor: 'pointer' }}
      role="button"
      tabIndex={0}
      onKeyDown={(e) => {
        if (e.key === 'Enter' || e.key === ' ') {
          void handleClick();
        }
      }}
    >
      <div className="notification-toast-icon">
        {item.priority === 'CRITICAL' ? (
          <span>🚨</span>
        ) : item.priority === 'WARNING' ? (
          <span>⚠</span>
        ) : item.priority === 'ACTION_REQUIRED' ? (
          <span>⚡</span>
        ) : (
          <span>🔔</span>
        )}
      </div>

      <div style={{ flex: 1, minWidth: 0 }}>
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: '0.5rem', marginBottom: '0.25rem' }}>
          <h3 style={{ fontSize: '0.95rem', fontWeight: item.isRead ? 600 : 700, margin: 0, color: '#0f172a' }}>
            {item.title}
          </h3>
          <span style={{ fontSize: '0.75rem', color: '#94a3b8', whiteSpace: 'nowrap' }}>
            {formatDate(item.createdAt)}
          </span>
        </div>

        <p style={{ margin: '0 0 0.5rem 0', fontSize: '0.85rem', color: '#475569', lineHeight: 1.4 }}>
          {item.message}
        </p>

        <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', flexWrap: 'wrap' }}>
          <span
            style={{
              fontSize: '0.7rem',
              fontWeight: 700,
              textTransform: 'uppercase',
              letterSpacing: '0.04em',
              padding: '0.15rem 0.45rem',
              borderRadius: '4px',
              backgroundColor: '#f1f5f9',
              color: '#475569',
            }}
          >
            {item.context}
          </span>
          {targetRoute && (
            <span style={{ fontSize: '0.775rem', fontWeight: 600, color: '#2563eb' }}>
              Open details →
            </span>
          )}
          {!item.isRead && (
            <button
              type="button"
              onClick={handleMarkRead}
              style={{
                marginLeft: 'auto',
                background: 'none',
                border: 'none',
                color: '#64748b',
                fontSize: '0.75rem',
                cursor: 'pointer',
                textDecoration: 'underline',
              }}
            >
              Mark as read
            </button>
          )}
        </div>
      </div>

      {!item.isRead && <span className="notification-card-dot" aria-label="Unread" />}
    </article>
  );
}

function formatDate(timestamp: string): string {
  const date = new Date(timestamp);
  if (Number.isNaN(date.getTime())) return 'Date unavailable';
  return new Intl.DateTimeFormat(undefined, { month: 'short', day: 'numeric', year: 'numeric' }).format(date);
}
