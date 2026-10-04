import React from 'react';
import { Link } from 'react-router-dom';
import { useNotifications } from './NotificationContext';

export interface NotificationBellProps {
  className?: string;
  onClick?: () => void;
}

export function NotificationBell({ className = '', onClick }: NotificationBellProps) {
  const { unreadCount } = useNotifications();

  return (
    <Link
      to="/app/notifications"
      className={`notification-bell-btn ${className}`}
      onClick={onClick}
      aria-label={`Notifications${unreadCount > 0 ? ` (${unreadCount} unread)` : ''}`}
      title="Notifications"
    >
      <svg
        width="20"
        height="20"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
        strokeLinejoin="round"
      >
        <path d="M6 8a6 6 0 0 1 12 0c0 7 3 9 3 9H3s3-2 3-9" />
        <path d="M10.3 21a1.94 1.94 0 0 0 3.4 0" />
      </svg>
      {unreadCount > 0 && (
        <span className="notification-badge" aria-hidden="true">
          {unreadCount > 99 ? '99+' : unreadCount}
        </span>
      )}
    </Link>
  );
}
