import React, { useState } from 'react';
import './userAvatar.css';

export interface UserAvatarProps {
  user?: {
    fullName?: string | null;
    email?: string;
    profilePhotoUrl?: string | null;
    roles?: string[];
  } | null;
  photoUrl?: string | null;
  name?: string | null;
  size?: number;
  animated?: boolean;
  className?: string;
}

export function UserAvatar({
  user,
  photoUrl,
  name,
  size = 36,
  animated = false,
  className = '',
}: UserAvatarProps) {
  const [loadError, setLoadError] = useState(false);

  const effectivePhotoUrl = photoUrl ?? user?.profilePhotoUrl;
  const effectiveName = name ?? user?.fullName ?? user?.email ?? 'User';
  const isManager = animated || (user?.roles?.includes('MANAGER') ?? false);

  const initials = getInitials(effectiveName);

  return (
    <div
      className={`user-avatar-container ${className}`}
      style={{ width: size, height: size }}
      title={effectiveName}
    >
      {isManager && <div className="user-avatar-manager-ring" aria-hidden="true" />}
      <div className="user-avatar-inner" style={{ fontSize: Math.max(12, Math.floor(size * 0.4)) }}>
        {effectivePhotoUrl && !loadError ? (
          <img
            src={effectivePhotoUrl}
            alt={effectiveName}
            className="user-avatar-image"
            onError={() => setLoadError(true)}
          />
        ) : (
          <span className="user-avatar-initials">{initials}</span>
        )}
      </div>
    </div>
  );
}

function getInitials(name: string): string {
  if (!name.trim()) return 'U';
  const parts = name.trim().split(/\s+/);
  if (parts.length >= 2) {
    return (parts[0][0] + parts[1][0]).toUpperCase();
  }
  return name.slice(0, 2).toUpperCase();
}
