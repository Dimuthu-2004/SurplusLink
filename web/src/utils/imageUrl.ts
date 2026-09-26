import { environment } from '../config/environment';

export const FALLBACK_MATERIAL_IMAGE = '/images/placeholder-material.svg';

/**
 * Resolves an image URL safely against the backend API base URL.
 * Handles:
 * - null / undefined / empty string -> fallback image
 * - absolute URLs (http://, https://, data:) -> returned as-is
 * - relative paths (/api/material-photos/..., api/material-photos/...) -> resolved against environment.apiBaseUrl
 */
export function resolveImageUrl(photoUrl?: string | null): string {
  if (!photoUrl || typeof photoUrl !== 'string') {
    return FALLBACK_MATERIAL_IMAGE;
  }

  const trimmed = photoUrl.trim();
  if (!trimmed) {
    return FALLBACK_MATERIAL_IMAGE;
  }

  if (
    trimmed.startsWith('http://') ||
    trimmed.startsWith('https://') ||
    trimmed.startsWith('data:')
  ) {
    return trimmed;
  }

  try {
    return new URL(trimmed, environment.apiBaseUrl).toString();
  } catch {
    return FALLBACK_MATERIAL_IMAGE;
  }
}

/**
 * Image error handler to gracefully replace failed image loads with the fallback image.
 */
export function handleImageError(event: React.SyntheticEvent<HTMLImageElement>) {
  const target = event.currentTarget;
  if (!target.src.includes('placeholder-material.svg')) {
    target.src = FALLBACK_MATERIAL_IMAGE;
  }
}
