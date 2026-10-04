import React, { useEffect, useState, useCallback } from 'react';
import { notificationsApi } from './notificationsApi';
import type { NotificationItem, NotificationPriority } from './notificationTypes';
import { NotificationCard } from './NotificationCard';
import { useNotifications } from './NotificationContext';
import './notifications.css';

type FilterTab = 'all' | 'unread' | 'action';

export function NotificationsPage() {
  const [tab, setTab] = useState<FilterTab>('all');
  const [items, setItems] = useState<NotificationItem[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const { markAllAsRead, refreshUnreadCount } = useNotifications();

  const loadNotifications = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const query: { unread?: boolean; priority?: NotificationPriority; page: number; pageSize: number } = {
        page,
        pageSize: 20,
      };

      if (tab === 'unread') {
        query.unread = true;
      } else if (tab === 'action') {
        query.priority = 'ACTION_REQUIRED';
      }

      const res = await notificationsApi.list(query);
      setItems(res.items);
      setTotal(res.total);
      setTotalPages(res.totalPages || 1);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unable to load notifications.');
    } finally {
      setLoading(false);
    }
  }, [tab, page]);

  useEffect(() => {
    void loadNotifications();
  }, [loadNotifications]);

  const handleMarkAllRead = async () => {
    await markAllAsRead();
    await loadNotifications();
    await refreshUnreadCount();
  };

  return (
    <div className="notifications-page" data-testid="notifications-page">
      <header className="notifications-header">
        <div>
          <p className="eyebrow" style={{ textTransform: 'uppercase', fontSize: '0.75rem', fontWeight: 700, color: '#f59e0b', margin: 0 }}>
            Activity Feed
          </p>
          <h1>Notifications</h1>
        </div>
        <div style={{ display: 'flex', gap: '0.75rem', alignItems: 'center' }}>
          <button
            type="button"
            className="button button-secondary"
            onClick={handleMarkAllRead}
            disabled={loading || items.every((i) => i.isRead)}
          >
            Mark all as read
          </button>
        </div>
      </header>

      <div className="notifications-filter-bar">
        <button
          type="button"
          className={`notifications-tab-btn ${tab === 'all' ? 'active' : ''}`}
          onClick={() => {
            setTab('all');
            setPage(1);
          }}
        >
          All
        </button>
        <button
          type="button"
          className={`notifications-tab-btn ${tab === 'unread' ? 'active' : ''}`}
          onClick={() => {
            setTab('unread');
            setPage(1);
          }}
        >
          Unread
        </button>
        <button
          type="button"
          className={`notifications-tab-btn ${tab === 'action' ? 'active' : ''}`}
          onClick={() => {
            setTab('action');
            setPage(1);
          }}
        >
          Action Required
        </button>
      </div>

      {loading ? (
        <p className="analytics-loading" role="status">Loading notifications…</p>
      ) : error ? (
        <div className="auth-error" role="alert">{error}</div>
      ) : items.length === 0 ? (
        <div className="notification-empty">
          <div className="notification-empty-icon">🔔</div>
          <h2>No notifications</h2>
          <p className="muted">
            {tab === 'unread'
              ? "You're all caught up! No unread notifications."
              : tab === 'action'
              ? 'No pending action requests at this time.'
              : 'You have no notifications yet.'}
          </p>
        </div>
      ) : (
        <div className="notifications-list">
          {items.map((item) => (
            <NotificationCard
              key={item.id}
              item={item}
              onRead={() => {
                void loadNotifications();
                void refreshUnreadCount();
              }}
            />
          ))}
        </div>
      )}

      {totalPages > 1 && (
        <nav className="marketplace-pagination" style={{ marginTop: '2rem' }}>
          <button disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>
            Previous
          </button>
          <span>
            {page} / {totalPages}
          </span>
          <button disabled={page >= totalPages} onClick={() => setPage((p) => p + 1)}>
            Next
          </button>
        </nav>
      )}
    </div>
  );
}
