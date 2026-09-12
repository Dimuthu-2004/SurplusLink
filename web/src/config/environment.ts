const defaultApiBaseUrl = 'http://localhost:5170';

function apiBaseUrl(): string {
  const configured = import.meta.env.VITE_API_BASE_URL?.trim() || defaultApiBaseUrl;
  const url = new URL(configured);
  if (url.protocol !== 'http:' && url.protocol !== 'https:') {
    throw new Error('VITE_API_BASE_URL must use HTTP or HTTPS.');
  }
  return url.toString().replace(/\/$/, '');
}

export const environment = Object.freeze({ apiBaseUrl: apiBaseUrl() });
